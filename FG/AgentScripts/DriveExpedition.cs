using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Отправляет отряд в кишащую тварями зону и прогоняет несколько часов,
/// чтобы поймать карточку развилки.
/// </summary>
public static class DriveExpedition
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        var zone = screen.World.Zones["verge_springs"];
        zone.Beasts.Clear();
        zone.Beasts["beast_spark"] = 8;
        zone.Forage = 1;
        zone.Crystals = 0;

        var squad = screen.World.Squads[0];
        squad.Members.Clear();
        squad.Members.Add("brunni");
        squad.Members.Add("leya");
        squad.ZoneId = "verge_springs";
        squad.Orders.Encounter = ForestGuild.Core.Sim.EncounterOrder.Ask;

        screen.Board.Rebuild();
        type.GetMethod("EnterPlanning", Flags).Invoke(screen, null);
        type.GetMethod("OpenOrder", Flags).Invoke(screen, null);
        type.GetMethod("ConfirmOrder", Flags).Invoke(screen, null);

        var expedition = screen.Expedition;
        var expType = expedition.GetType();
        var advance = expType.GetMethod("Tick", Flags);

        for (var i = 0; i < 8; i++)
        {
            var current = expType.GetField("_current", Flags).GetValue(expedition);
            if (current == null) break;

            var hasDecision = (bool)current.GetType().GetProperty("HasDecision").GetValue(current);
            if (hasDecision) break;

            advance.Invoke(expedition, null);
        }

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var card = root.Q<VisualElement>("expedition-card");
        var options = root.Q<VisualElement>("decision-options");
        var title = root.Q<Label>("decision-title");

        return "expeditionOpen=" + expedition.IsOpen
            + " card=" + (card != null && !card.ClassListContains("hidden"))
            + " options=" + (options != null ? options.childCount : -1)
            + " title=[" + (title != null ? title.text : "?") + "]";
    }
}
