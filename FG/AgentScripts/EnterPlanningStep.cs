using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Переход с окна начала дня на экран планирования.</summary>
public static class EnterPlanningStep
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.GetType()
            .GetMethod("EnterPlanning", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(screen, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var start = root.Q<VisualElement>("startday");
        var right = root.Q<VisualElement>("right-panel");
        var debug = root.Q<Toggle>("debug-toggle");
        var sign = root.Q<Button>("sign-button");

        return "startdayVisible=" + (start != null && !start.ClassListContains("hidden"))
            + " rightPanel=" + (right != null)
            + " debugToggle=" + (debug != null)
            + " sign=[" + (sign != null ? sign.text : "?") + "]";
    }
}
