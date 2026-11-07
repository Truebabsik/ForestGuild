using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Третий пункт меню: поход двух отрядов без утра.</summary>
public static class DriveExpeditionDemo
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();

        screen.GetType().GetMethod("StartExpeditionDemo", Flags).Invoke(screen, null);

        var phase = screen.GetType().GetField("_phase", Flags).GetValue(screen);
        var plan = (ForestGuild.Core.Sim.DayPlan)screen.GetType().GetField("_plan", Flags).GetValue(screen);

        return "phase=" + phase
            + " expeditionOpen=" + screen.Expedition.IsOpen
            + " parties=" + plan.Parties.Count
            + " zones=" + (plan.Parties.Count > 0 ? plan.Parties[0].ZoneId : "-")
            + "," + (plan.Parties.Count > 1 ? plan.Parties[1].ZoneId : "-");
    }
}
