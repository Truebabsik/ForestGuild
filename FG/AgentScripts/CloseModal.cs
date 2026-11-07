using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Закрыть модал приказа, чтобы снять доску.</summary>
public static class CloseModal
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var modal = root.Q<VisualElement>("modal");
        if (modal == null) return "нет модала";

        modal.AddToClassList("hidden");
        var right = root.Q<VisualElement>("right-panel");
        return "modal скрыт, right=" + right.layout.width;
    }
}
