using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Доматывает демо-поход и проверяет возврат в меню.</summary>
public static class CloseExpeditionDemo
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.Expedition.GetType().GetMethod("SkipAll", Flags).Invoke(screen.Expedition, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var menu = root.Q<VisualElement>("menu");
        var phase = screen.GetType().GetField("_phase", Flags).GetValue(screen);
        var report = (ForestGuild.Core.Sim.DayReport)screen.GetType().GetField("_dayReport", Flags).GetValue(screen);

        return "phase=" + phase
            + " menu=" + (menu != null && !menu.ClassListContains("hidden"))
            + " parties=" + (report != null ? report.Parties.Count : -1);
    }
}
