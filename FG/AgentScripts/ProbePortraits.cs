using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Смотрит фактические размеры портретов в модале приказа.</summary>
public static class ProbePortraits
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var row = root.Q<VisualElement>(className: "modal-portraits");

        if (row == null) return "row=null";

        var text = "row w=" + row.resolvedStyle.width + " h=" + row.resolvedStyle.height;
        foreach (var child in row.Children())
        {
            text += " | child w=" + child.resolvedStyle.width + " h=" + child.resolvedStyle.height
                + " flex=" + child.resolvedStyle.flexShrink;
        }

        return text;
    }
}
