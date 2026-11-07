using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Читает фактическую прозрачность занятой и свободной плитки.</summary>
public static class ProbeOpacity
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var free = root.Q<VisualElement>("muster-free");

        var busy = free.Query<VisualElement>(className: "guild-tile").ToList()
            .FirstOrDefault(t => t.ClassListContains("busy"));
        var idle = free.Query<VisualElement>(className: "guild-tile").ToList()
            .FirstOrDefault(t => !t.ClassListContains("busy"));

        return "busy=" + (busy != null ? busy.resolvedStyle.opacity : -1)
            + " idle=" + (idle != null ? idle.resolvedStyle.opacity : -1);
    }
}
