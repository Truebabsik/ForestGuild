using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Диагностика: жив ли UIDocument и какой размер у его корня.</summary>
public static class ProbeUi
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var doc = Object.FindAnyObjectByType<UIDocument>();
        sb.AppendLine("uidocument=" + (doc != null));
        if (doc == null) return sb.ToString();

        sb.AppendLine("visualTree=" + (doc.visualTreeAsset != null));
        sb.AppendLine("panelSettings=" + (doc.panelSettings != null));
        if (doc.panelSettings != null)
        {
            sb.AppendLine("theme=" + (doc.panelSettings.themeStyleSheet != null));
            sb.AppendLine("refRes=" + doc.panelSettings.referenceResolution);
        }

        var root = doc.rootVisualElement;
        sb.AppendLine("root=" + (root != null));
        if (root == null) return sb.ToString();

        sb.AppendLine("root.layout=" + root.layout);
        sb.AppendLine("root.children=" + root.childCount);
        sb.AppendLine("root.panel=" + (root.panel != null));
        if (root.panel != null) sb.AppendLine("panel.visualTree=" + root.panel.visualTree.layout);

        foreach (var child in root.Children())
        {
            sb.AppendLine("  child " + child.name + " class=" + child.GetType().Name + " layout=" + child.layout);
        }

        var map = root.Q("map");
        sb.AppendLine("map=" + (map != null) + (map != null ? " layout=" + map.layout : ""));
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        sb.AppendLine("mapscreen=" + (screen != null) + (screen != null ? " tables=" + (screen.Tables != null) + " world=" + (screen.World != null) : ""));

        return sb.ToString();
    }
}
