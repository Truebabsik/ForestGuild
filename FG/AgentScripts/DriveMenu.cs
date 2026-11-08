using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Проверяет оба пункта меню: игра и симуляция Сбора.</summary>
public static class DriveMenu
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;

        var menu = root.Q<VisualElement>("menu");
        var startday = root.Q<VisualElement>("startday");

        var menuVisible = menu != null && !menu.ClassListContains("hidden");

        type.GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "campaign" });
        var gameOk = startday != null && !startday.ClassListContains("hidden");
        var menuHidden = menu.ClassListContains("hidden");

        type.GetMethod("ShowMenu", Flags).Invoke(screen, null);
        type.GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "muster" });
        var musterOk = screen.Muster.IsOpen;

        return "menuVisible=" + menuVisible
            + " afterPlay startday=" + gameOk
            + " menuHidden=" + menuHidden
            + " afterSim muster=" + musterOk;
    }
}
