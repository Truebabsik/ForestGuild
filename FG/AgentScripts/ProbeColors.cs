using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Сравнивает цвета занятой и свободной плитки.</summary>
public static class ProbeColors
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var free = root.Q<VisualElement>("muster-free");
        var tiles = free.Query<VisualElement>(className: "guild-tile").ToList();

        var busy = tiles.FirstOrDefault(t => t.ClassListContains("busy"));
        var idle = tiles.FirstOrDefault(t => !t.ClassListContains("busy"));

        return Describe("busy", busy) + " || " + Describe("idle", idle);
    }

    private static string Describe(string tag, VisualElement tile)
    {
        if (tile == null) return tag + "=none";

        var name = tile.Q<Label>(className: "person-name");
        return tag + ": bg=" + tile.resolvedStyle.backgroundColor
            + " border=" + tile.resolvedStyle.borderTopColor
            + " nameColor=" + (name != null ? name.resolvedStyle.color.ToString() : "?")
            + " visible=" + tile.resolvedStyle.visibility;
    }
}
