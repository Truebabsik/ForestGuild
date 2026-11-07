using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Замер узлов и рёбер карты.</summary>
public static class ProbeMap
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var map = root.Q("map");
        sb.AppendLine("map=" + map.layout);

        foreach (var node in map.Children())
        {
            if (node is ForestGuild.Game.ZoneNodeView zoneNode)
                sb.AppendLine("node " + zoneNode.ZoneId + " layout=" + node.layout + " radius=" + node.resolvedStyle.borderTopLeftRadius);
            else
                sb.AppendLine("edge element " + node.GetType().Name + " layout=" + node.layout);
        }

        var edges = 0;
        foreach (var child in map.Children())
            if (child is ForestGuild.Game.EdgeLine) edges++;

        sb.AppendLine("edges=" + edges);
        return sb.ToString();
    }
}
