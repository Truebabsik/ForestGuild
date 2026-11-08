using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Открывает досье Опушки и печатает заголовок и первые строки.</summary>
public static class ProbeMeadow
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.GetType().GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "campaign" });

        // Досье показывает лор только у разведанной зоны.
        screen.World.Zones["verge_meadow"].Scouted = true;
        screen.Dossier.Open("verge_meadow", screen.Tables, screen.World, false);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var text = root.Q<ScrollView>("dossier-text");
        var title = text.Q<Label>(className: "zone-title");

        var fields = 0;
        foreach (var label in text.Query<Label>(className: "zone-text").ToList())
        {
            if (label.text.Contains("Первая зона")) fields++;
            if (label.text.Contains("грузд") || label.text.Contains("смола")) fields++;
        }

        return "title=[" + (title != null ? title.text : "?") + "]" + " проверок=" + fields;
    }
}
