using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Замер панелей и карточки зоны.</summary>
public static class ProbeLayout
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;

        var left = root.Q<VisualElement>("left-panel");
        var right = root.Q<VisualElement>("right-panel");
        var card = root.Q<ScrollView>("zone-card");
        var first = card != null && card.contentContainer.childCount > 0
            ? card.contentContainer[0]
            : null;

        return "left=" + left.layout
            + " right=" + right.layout
            + " card=" + (card != null ? card.layout.ToString() : "?")
            + " first=" + (first != null ? first.layout + " type=" + first.GetType().Name : "?")
            + " root=" + root.layout;
    }
}
