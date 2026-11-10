using System.Linq;
using ForestGuild.Game;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Показывает строки досье с глубиной зоны — проверка «edge» → «опушка».</summary>
public static class ProbeDepth
{
    public static string Run()
    {
        var screen = UnityEngine.Object.FindAnyObjectByType<MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;

        var texts = root.Query<Label>().ToList()
            .Select(label => label.text)
            .Where(text => text.Contains("опушка") || text.Contains("пояс") || text.Contains("сердце"))
            .ToList();

        return "depthLines=" + texts.Count + " [" + string.Join(" | ", texts) + "]";
    }
}
