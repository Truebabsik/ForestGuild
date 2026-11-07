using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Проверка отладочного режима: при включённом тумблере карточка показывает числа.</summary>
public static class ProbeDebug
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var toggle = root.Q<Toggle>("debug-toggle");
        if (toggle == null) return "нет тумблера";

        toggle.value = true;

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var card = (ScrollView)screen.GetType().GetField("_zoneCard", flags).GetValue(screen);

        var found = false;
        foreach (var child in card.contentContainer.Children())
        {
            if (child is Label label && label.text.Contains("отладка")) found = true;
        }

        toggle.value = false;

        return "debug=" + toggle.value + " строкаОтладкиНайдена=" + found;
    }
}
