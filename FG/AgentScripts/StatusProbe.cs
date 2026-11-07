using System.Reflection;
using UnityEngine;

/// <summary>Ранит одного и убивает другого, затем открывает Сбор — проверка цветов статуса.</summary>
public static class StatusProbe
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();

        ForestGuild.Core.Sim.Works.Wound(screen.World, "tove");
        ForestGuild.Core.Sim.Works.Kill(screen.World, "valda");

        var squad = screen.World.Squads[0];
        squad.ZoneId = "verge_springs";
        squad.Members.Clear();

        var plan = (ForestGuild.Core.Sim.DayPlan)screen.GetType().GetField("_plan", Flags).GetValue(screen);
        screen.Muster.Open(squad, screen.Tables, screen.World, plan);

        var add = screen.Muster.GetType().GetMethod("AddMember", Flags);
        add.Invoke(screen.Muster, new object[] { "brunni" });

        return "tove=" + screen.World.ConditionOf("tove")
            + " valda=" + screen.World.ConditionOf("valda")
            + " squad=" + squad.Members.Count;
    }
}
