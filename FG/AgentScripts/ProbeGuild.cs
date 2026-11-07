using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Заполняет отряд А, переключается на Б и печатает плитки гильдий.</summary>
public static class ProbeGuild
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();

        var a = screen.World.Squads[0];
        a.Members.Clear();
        foreach (var id in new[] { "brunni", "skadi", "hilda", "orn" }) a.Members.Add(id);

        var b = screen.World.Squads[1];
        b.Members.Clear();

        screen.Muster.Open(a, screen.Tables, screen.World, new ForestGuild.Core.Sim.DayPlan());

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var text = "A: " + Tiles(root);

        var tabs = root.Q<VisualElement>("muster-squads");
        var buttons = tabs.Query<Button>().ToList();
        buttons[1].SendEvent(new NavigationSubmitEvent { target = buttons[1] });

        text += " || B: " + Tiles(root);
        return text;
    }

    private static string Tiles(VisualElement root)
    {
        var free = root.Q<VisualElement>("muster-free");
        var names = new List<string>();

        foreach (var tile in free.Query<VisualElement>(className: "guild-tile").ToList())
        {
            var name = tile.Q<Label>(className: "person-name");
            var busy = tile.ClassListContains("busy") ? "*" : "";
            names.Add((name != null ? name.text : "?") + busy);
        }

        return string.Join(", ", names);
    }
}
