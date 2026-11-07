using System.Reflection;
using UnityEngine;

/// <summary>День с раненым и добычей, затем вечер — для снимка разбора.</summary>
public static class DriveEvening
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        var a = screen.World.Squads[0];
        a.Members.Clear();
        a.Members.Add("brunni");
        a.Members.Add("leya");
        a.ZoneId = "verge_springs";
        screen.Board.Rebuild();

        ForestGuild.Core.Sim.Works.Wound(screen.World, "leya");

        type.GetMethod("OpenOrder", Flags).Invoke(screen, null);
        type.GetMethod("ConfirmOrder", Flags).Invoke(screen, null);
        type.GetMethod("GoToEvening", Flags).Invoke(screen, null);

        var report = (ForestGuild.Core.Sim.DayReport)type.GetField("_dayReport", Flags).GetValue(screen);
        var plan = (ForestGuild.Core.Sim.DayPlan)type.GetField("_plan", Flags).GetValue(screen);

        return "eveningOpen=" + screen.Evening.IsOpen
            + " loot=" + (report != null ? report.Loot.Count : -1)
            + " beds=" + plan.Beds.Count
            + " wounded=" + (screen.World.ConditionOf("leya") == ForestGuild.Core.Sim.PersonCondition.Wounded);
    }
}
