using System.Reflection;
using UnityEngine;

/// <summary>
/// Прогон плана дня через UI: два отряда, работник в деревне и попытка третьего отряда.
/// Методы вызываются рефлексией — тот же путь, что и клик.
/// </summary>
public static class DriveUi
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = screen.GetType();
        var roster = type.GetField("_roster", flags).GetValue(screen);
        var toggle = roster.GetType().GetMethod("Toggle", flags);
        var sendParty = type.GetMethod("SendParty", flags);
        var sendVillage = type.GetMethod("SendToVillage", flags);

        type.GetMethod("ShowZone", flags).Invoke(screen, new object[] { "verge_springs" });
        screen.Map.GetType().GetMethod("Select", flags).Invoke(screen.Map, new object[] { "verge_springs" });

        toggle.Invoke(roster, new object[] { "brunni" });
        toggle.Invoke(roster, new object[] { "leya" });
        sendParty.Invoke(screen, null);

        toggle.Invoke(roster, new object[] { "skadi" });
        toggle.Invoke(roster, new object[] { "hilda" });
        sendParty.Invoke(screen, null);

        toggle.Invoke(roster, new object[] { "tove" });
        sendVillage.Invoke(screen, null);

        toggle.Invoke(roster, new object[] { "orn" });
        toggle.Invoke(roster, new object[] { "run" });
        sendParty.Invoke(screen, null); // третий отряд — должен упереться в лимит

        var plan = type.GetField("_plan", flags).GetValue(screen);
        var parties = (System.Collections.ICollection)plan.GetType().GetField("Parties").GetValue(plan);
        var workers = (System.Collections.ICollection)plan.GetType().GetField("VillageWorkers").GetValue(plan);

        return "parties=" + parties.Count + " village=" + workers.Count + " day=" + screen.World.Day;
    }
}
