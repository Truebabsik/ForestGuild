using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ForestGuild.Core.Sim;
using ForestGuild.Game;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// T1: бой в Unity после правок ядра (засада, чистые формулы). Ведёт настоящий поход через
/// приватный тик экрана, останавливается на развилке и возвращает состояние одной строкой.
/// </summary>
public static class DriveBattle
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    private static MapScreen Map()
    {
        return Object.FindAnyObjectByType<MapScreen>();
    }

    /// <summary>Эпизод «ПОХОД В ЛЕСУ» и шаги до первой развилки.</summary>
    public static string Start()
    {
        var screen = Map();
        screen.GetType().GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "expedition" });
        return Step(screen, 90);
    }

    /// <summary>Выбрать «бой» в карточке встречи и дойти до следующей развилки.</summary>
    public static string Fight()
    {
        var screen = Map();
        var ui = screen.Expedition;
        var current = Current(ui);
        if (current == null) return "no current: " + State(ui);

        if (!current.HasDecision || current.Pending.Kind != DecisionKind.Encounter)
            return "not an encounter: " + State(ui);

        ui.GetType().GetMethod("Choose", Flags).Invoke(ui, new object[] { "fight" });
        return Step(screen, 90);
    }

    /// <summary>Пропустить развилку по умолчанию и идти дальше — так ловим несколько боёв подряд.</summary>
    public static string Next()
    {
        var screen = Map();
        var ui = screen.Expedition;
        var current = Current(ui);
        if (current == null) return "no current: " + State(ui);
        if (!current.HasDecision) return "no decision: " + State(ui);

        var decision = current.Pending;
        var option = "fight";
        switch (decision.Kind)
        {
            case DecisionKind.Battle: option = "next"; break;
            case DecisionKind.Carcass: option = "leave"; break;
            case DecisionKind.Wounded: option = "carry"; break;
            case DecisionKind.BagFull: option = decision.Options[0].Id; break;
        }

        ui.GetType().GetMethod("Choose", Flags).Invoke(ui, new object[] { option });
        return Step(screen, 90);
    }

    /// <summary>Полный отчёт о мире: HP людей, минуты, лог похода.</summary>
    public static string Report()
    {
        var screen = Map();
        return State(screen.Expedition);
    }

    /// <summary>Гоняет поход до конца: все встречи — бой, туша — бросить, раненый — нести.</summary>
    public static string Auto()
    {
        var screen = Map();
        screen.GetType().GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "expedition" });
        var ui = screen.Expedition;
        var choose = ui.GetType().GetMethod("Choose", Flags);
        var tick = ui.GetType().GetMethod("Tick", Flags);

        var encounters = 0;
        var titles = new List<string>();
        var hpBefore = Snapshot(screen);

        for (var step = 0; step < 600; step++)
        {
            var current = Current(ui);
            if (current == null || current.Finished) break;

            if (current.HasDecision)
            {
                var decision = current.Pending;
                var option = "leave";
                if (decision.Kind == DecisionKind.Encounter)
                {
                    encounters++;
                    titles.Add(decision.Title);
                    option = "fight";
                }
                else if (decision.Kind == DecisionKind.Battle)
                {
                    option = "next";
                }
                else if (decision.Kind == DecisionKind.Wounded)
                {
                    option = "carry";
                }
                else if (decision.Kind == DecisionKind.BagFull)
                {
                    option = decision.Options[0].Id;
                }

                choose.Invoke(ui, new object[] { option });
                continue;
            }

            tick.Invoke(ui, null);
        }

        var lines = Current(ui) != null ? Current(ui).Result.Log : new List<string>();
        var fightsWon = lines.Count(x => x.Contains("бой — победа"));
        var ambushes = lines.Count(x => x.Contains("застаёт отряд"));
        var partyAmbushes = lines.Count(x => x.Contains("застал ") && x.Contains(" первым")
            || x.Contains("Отряд застал"));
        var downed = lines.Count(x => x.Contains("Отряд смят"));

        return "encounters=" + encounters
            + " won=" + fightsWon
            + " lost=" + downed
            + " beastAmbush=" + ambushes
            + " partyAmbush=" + partyAmbushes
            + " hpBefore=[" + hpBefore + "]"
            + " hpAfter=[" + Snapshot(screen) + "]"
            + " titles=[" + string.Join(" | ", titles) + "]";
    }

    /// <summary>Слабый отряд: бой обязан закончиться выбитым и карточкой «нести/оставить».</summary>
    public static string Wound()
    {
        var screen = Map();
        screen.GetType().GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "expedition" });

        // HP руками и тварь 3 уровня: проверяем именно путь «выбит → ранение», а не силу отряда.
        var zone = screen.World.Zones["verge_springs"];
        zone.Beasts.Clear();
        zone.Beasts["beast_bone"] = 3;
        screen.World.People["brunni"].Hp = 4;
        screen.World.People["skadi"].Hp = 4;

        var ui = screen.Expedition;
        var current = Current(ui);
        for (var i = 0; i < 90 && current != null && !current.HasDecision; i++)
        {
            ui.GetType().GetMethod("Tick", Flags).Invoke(ui, null);
            current = Current(ui);
        }

        if (current == null || !current.HasDecision || current.Pending.Kind != DecisionKind.Encounter)
            return "no encounter: " + State(ui);

        ui.GetType().GetMethod("Choose", Flags).Invoke(ui, new object[] { "fight" });
        return Step(screen, 90);
    }

    /// <summary>Наказ «бей»: карточка боя не должна держать поход — бой идёт без паузы.</summary>
    public static string AutoFight()
    {
        var screen = Map();
        screen.GetType().GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "expedition" });
        var ui = screen.Expedition;

        var current = Current(ui);
        if (current != null) AutoOrders(current.Party);

        var queueField = ui.GetType().GetField("_queue", Flags);
        var queue = queueField.GetValue(ui) as Queue<PartyAssignment>;
        if (queue != null)
        {
            foreach (var party in queue.ToList()) AutoOrders(party);
        }

        var choose = ui.GetType().GetMethod("Choose", Flags);
        var tick = ui.GetType().GetMethod("Tick", Flags);
        var battleCards = 0;
        var encounters = 0;
        var fights = 0;

        for (var step = 0; step < 600; step++)
        {
            var exp = Current(ui);
            if (exp == null || exp.Finished) break;

            if (exp.HasDecision)
            {
                if (exp.Pending.Kind == DecisionKind.Battle) battleCards++;
                else if (exp.Pending.Kind == DecisionKind.Encounter) encounters++;

                var option = exp.Pending.Kind == DecisionKind.Encounter ? "fight"
                    : exp.Pending.Kind == DecisionKind.Battle ? "next"
                    : exp.Pending.Kind == DecisionKind.Carcass ? "leave"
                    : exp.Pending.Kind == DecisionKind.Wounded ? "carry"
                    : exp.Pending.Options[0].Id;

                choose.Invoke(ui, new object[] { option });
                continue;
            }

            tick.Invoke(ui, null);
            if (exp.LastBattle != null) fights = 1;
        }

        var done = Current(ui);
        return "encounters=" + encounters
            + " battleCards=" + battleCards
            + " fightHappened=" + (done != null && done.LastBattle != null || fights == 1)
            + " hp=[" + Snapshot(screen) + "]";
    }

    private static void AutoOrders(PartyAssignment party)
    {
        var orders = party.Orders ?? (party.Orders = new StandingOrders());
        orders.Encounter = EncounterOrder.Fight;
        orders.Carcass = CarcassOrder.Leave;
        orders.Wounded = WoundedOrder.Carry;
        orders.BagFull = BagOrder.Skip;
    }

    private static string Snapshot(MapScreen screen)
    {
        var text = new List<string>();
        foreach (var hunter in new[] { "brunni", "skadi", "hilda", "orn" })
        {
            var state = screen.World.People[hunter];
            text.Add(hunter + " " + state.Hp + "/" + state.HpMax + " " + state.Condition);
        }

        return string.Join(" ; ", text);
    }

    private static Expedition Current(ExpeditionScreen ui)
    {
        return (Expedition)ui.GetType().GetField("_current", Flags).GetValue(ui);
    }

    private static string Step(MapScreen screen, int limit)
    {
        var ui = screen.Expedition;
        var tick = ui.GetType().GetMethod("Tick", Flags);

        for (var i = 0; i < limit; i++)
        {
            var current = Current(ui);
            if (current == null) break;
            if (current.HasDecision) break;
            tick.Invoke(ui, null);
        }

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        root.MarkDirtyRepaint();

        var hud = root.Q<Label>("hud");
        var hour = root.Q<Label>("expedition-hour");
        return State(ui)
            + " hud=[" + (hud != null ? hud.text : "?") + "]"
            + " hourLabel=[" + (hour != null ? hour.text : "?") + "]";
    }

    /// <summary>Состояние одним сообщением: развилка, арт, опции, HP, минуты, хвост лога.</summary>
    private static string State(ExpeditionScreen ui)
    {
        var screen = Map();
        var current = Current(ui);
        var text = new StringBuilder();

        text.Append("open=").Append(ui.IsOpen);

        if (current == null)
        {
            text.Append(" current=none");
            return text.ToString();
        }

        text.Append(" finished=").Append(current.Finished)
            .Append(" minutes=").Append(current.MinutesLeft);

        if (current.HasDecision)
        {
            var decision = current.Pending;
            text.Append(" decision=").Append(decision.Kind)
                .Append(" title=[").Append(decision.Title).Append(']');

            var body = decision.Body ?? "";
            text.Append(" body=[").Append(body.Replace('\n', ' ')).Append(']');

            text.Append(" options=[");
            for (var i = 0; i < decision.Options.Count; i++)
            {
                if (i > 0) text.Append(" ; ");
                var option = decision.Options[i];
                text.Append(option.Label).Append('/').Append(option.Note);
            }

            text.Append(']');

            var artField = ui.GetType().GetField("_artClass", Flags);
            if (artField != null) text.Append(" art=").Append(artField.GetValue(ui));

            var packField = current.GetType().GetField("_pack", Flags);
            var pack = packField != null ? packField.GetValue(current) as List<string> : null;
            if (pack != null)
            {
                text.Append(" pack=[");
                for (var i = 0; i < pack.Count; i++)
                {
                    if (i > 0) text.Append(',');
                    text.Append(pack[i]);
                }

                text.Append(']');
            }
        }
        else
        {
            text.Append(" decision=none");
        }

        text.Append(" hp=[");
        var party = current.Party;
        var people = party != null ? party.PersonIds : new List<string>();
        for (var i = 0; i < people.Count; i++)
        {
            if (i > 0) text.Append(" ; ");
            var id = people[i];
            var state = screen.World.People[id];
            text.Append(id).Append(' ').Append(state.Hp).Append('/').Append(state.HpMax)
                .Append(' ').Append(state.Condition);
        }

        text.Append(']');

        text.Append(" log=[");
        var lines = current.Result.Log;
        var from = Mathf.Max(0, lines.Count - 6);
        for (var i = from; i < lines.Count; i++)
        {
            if (i > from) text.Append(" | ");
            text.Append(lines[i]);
        }

        text.Append(']');
        return text.ToString();
    }
}
