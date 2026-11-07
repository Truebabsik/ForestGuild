using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Замер левой панели, карты и карточки отряда.</summary>
public static class ProbeBoard
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;

        var left = root.Q<VisualElement>("left-panel");
        var map = root.Q<VisualElement>("map");
        var squads = root.Q<VisualElement>("squads");
        var card = squads != null && squads.childCount > 0 ? squads[0] : null;

        return "root=" + root.layout.width
            + " left=" + left.layout
            + " map=" + map.layout
            + " squads=" + (squads != null ? squads.layout.ToString() : "?")
            + " card=" + (card != null ? card.layout.ToString() : "?");
    }
}
