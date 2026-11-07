using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Открывает Сбор и читает строку под составом.</summary>
public static class ProbeMuster
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.Muster.Open(screen.World.Squads[0], screen.Tables, screen.World, new ForestGuild.Core.Sim.DayPlan());

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var summary = root.Q<Label>("muster-summary");
        var text = summary != null ? summary.text : "null";

        return "summary=[" + text + "]"
            + " hasSlots=" + text.Contains("слоты")
            + " hasEncounters=" + text.Contains("встреч")
            + " hasRoad=" + text.Contains("дорога");
    }
}
