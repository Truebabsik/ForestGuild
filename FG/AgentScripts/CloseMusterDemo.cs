using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Закрывает симуляцию Сбора и проверяет возврат в меню.</summary>
public static class CloseMusterDemo
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.Muster.Close();

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var menu = root.Q<VisualElement>("menu");

        return "muster=" + screen.Muster.IsOpen
            + " menu=" + (menu != null && !menu.ClassListContains("hidden"));
    }
}
