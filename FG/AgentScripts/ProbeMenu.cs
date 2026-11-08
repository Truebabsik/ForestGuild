using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Показывает меню и печатает пункты, собранные из scenarios.csv.</summary>
public static class ProbeMenu
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.GetType().GetMethod("ShowMenu", Flags).Invoke(screen, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var items = root.Q<VisualElement>("menu-items");
        var titles = "";
        foreach (var button in items.Query<Button>().ToList()) titles += button.text + " | ";

        return "items=" + items.childCount + " titles=[" + titles.Trim() + "]";
    }
}
