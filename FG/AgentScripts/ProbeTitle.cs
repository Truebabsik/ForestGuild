using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Где на самом деле стоит заголовок карточки зоны.</summary>
public static class ProbeTitle
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var card = root.Q<ScrollView>("zone-card");

        var labels = card.Query<Label>().ToList();
        var sb = "";
        foreach (var label in labels.Take(3))
        {
            sb += "[" + label.text + "] class=" + label.GetClasses().FirstOrDefault()
                + " layout=" + label.layout + " world=" + label.worldBound + "\n";
        }

        return sb + "card content=" + card.contentContainer.worldBound;
    }
}
