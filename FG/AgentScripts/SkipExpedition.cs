using System.Reflection;
using UnityEngine;

/// <summary>Доматывает поход до конца и проверяет, что день закрылся вечером.</summary>
public static class SkipExpedition
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();

        if (screen.Expedition.IsOpen)
        {
            screen.Expedition.GetType().GetMethod("SkipAll", Flags).Invoke(screen.Expedition, null);
        }

        var phase = screen.GetType().GetField("_phase", Flags).GetValue(screen);
        var report = (ForestGuild.Core.Sim.DayReport)screen.GetType().GetField("_dayReport", Flags).GetValue(screen);

        return "phase=" + phase
            + " eveningOpen=" + screen.Evening.IsOpen
            + " parties=" + (report != null ? report.Parties.Count : -1)
            + " loot=" + (report != null ? report.Loot.Count : -1);
    }
}
