using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ForestGuild.Core.Sim;
using ForestGuild.Game;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// T4: слово о состоянии человека вне боя. Одно и то же слово на доске утра, в Сборе и вечером.
/// </summary>
public static class DriveHp
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    private static MapScreen Map()
    {
        return Object.FindAnyObjectByType<MapScreen>();
    }

    /// <summary>Сбор отряда: плитки людей показывают состояние словами.</summary>
    public static string Muster()
    {
        var screen = Map();
        screen.GetType().GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "muster" });
        Hurt(screen);

        var muster = screen.Muster;
        muster.GetType().GetMethod("Rebuild", Flags).Invoke(muster, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var words = root.Query<Label>(className: "person-hp").ToList()
            .Select(label => label.text)
            .ToList();

        return "personHp=" + words.Count + " words=[" + string.Join(" | ", words) + "]";
    }

    /// <summary>Доска утра: состав отряда с состоянием словами.</summary>
    public static string Board()
    {
        var screen = Map();
        screen.GetType().GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "campaign" });
        // Кампания начинается с окна начала дня: доска живёт на следующем шаге.
        screen.GetType().GetMethod("EnterPlanning", Flags).Invoke(screen, null);
        Hurt(screen);

        var squad = screen.World.Squads[0];
        squad.Members.Clear();
        squad.Members.Add("brunni");
        squad.Members.Add("skadi");
        squad.Members.Add("hilda");
        squad.ZoneId = "verge_meadow";

        screen.Board.Rebuild();

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var lines = root.Query<Label>(className: "squad-line").ToList()
            .Select(label => label.text)
            .Where(text => text.Contains(" — "))
            .ToList();

        return "squadLines=[" + string.Join(" | ", lines) + "]";
    }

    /// <summary>Вечер: строки о раненых — имена и то же слово о состоянии.</summary>
    public static string Evening()
    {
        var screen = Map();
        var type = screen.GetType();
        type.GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "campaign" });

        var squad = screen.World.Squads[0];
        squad.Members.Clear();
        squad.Members.Add("brunni");
        squad.Members.Add("leya");
        squad.ZoneId = "verge_springs";
        screen.Board.Rebuild();
        Works.Wound(screen.World, "leya");

        type.GetMethod("OpenOrder", Flags).Invoke(screen, null);
        type.GetMethod("ConfirmOrder", Flags).Invoke(screen, null);

        var flow = type.GetField("_flow", Flags).GetValue(screen);
        var report = type.GetField("_dayReport", Flags).GetValue(screen);
        flow.GetType().GetMethod("FinishExpeditions")
            .Invoke(flow, new object[] { report, new List<ExpeditionResult>() });

        type.GetMethod("GoToEvening", Flags).Invoke(screen, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var lines = root.Query<Label>().ToList()
            .Select(label => label.text)
            .Where(text => text.Contains("Раненые") || text.Contains("вернулись целыми"))
            .ToList();

        return "eveningOpen=" + screen.Evening.IsOpen
            + " lines=[" + string.Join(" | ", lines) + "]";
    }

    private static void Hurt(MapScreen screen)
    {
        // 10/54 = 18% (тяжело ранен), 20/45 = 44% (ранен), 30/36 = 83% (в порядке).
        screen.World.People["brunni"].Hp = 10;
        screen.World.People["skadi"].Hp = 20;
        screen.World.People["hilda"].Hp = 30;
    }
}
