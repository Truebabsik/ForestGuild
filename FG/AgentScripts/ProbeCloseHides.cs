using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Проверяет, что Close() похода гасит и модал встречи.</summary>
public static class ProbeCloseHides
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        type.GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "expedition" });

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var modal = root.Q<VisualElement>("monster-modal");
        var panel = root.Q<VisualElement>("expedition");

        modal.RemoveFromClassList("hidden");
        screen.Expedition.Close();

        return "modal hidden=" + modal.ClassListContains("hidden")
            + " panel hidden=" + panel.ClassListContains("hidden");
    }
}
