using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Кликает по занятой плитке и проверяет переход человека в текущий отряд.</summary>
public static class ProbeClickSteal
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var free = root.Q<VisualElement>("muster-free");

        var tile = free.Query<VisualElement>(className: "guild-tile").ToList()
            .FirstOrDefault(t => t.ClassListContains("busy"));

        if (tile == null) return "no busy tile";

        var name = tile.Q<Label>(className: "person-name").text;

        using (var click = ClickEvent.GetPooled())
        {
            click.target = tile;
            tile.SendEvent(click);
        }

        var a = screen.World.Squads[0].Members.Count;
        var b = screen.World.Squads[1].Members.Count;

        return "clicked=[" + name + "] A=" + a + " B=" + b;
    }
}
