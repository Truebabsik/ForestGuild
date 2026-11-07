using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Замер карточки зоны: панель, скролл, контент, первый ребёнок.</summary>
public static class ProbeCard
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = screen.GetType();
        var card = (ScrollView)type.GetField("_zoneCard", flags).GetValue(screen);

        var sb = new StringBuilder();
        sb.AppendLine("card=" + card.layout + " offset=" + card.scrollOffset);
        sb.AppendLine("viewport=" + card.contentViewport?.layout);
        sb.AppendLine("content=" + card.contentContainer.layout);
        foreach (var child in card.contentContainer.Children())
        {
            sb.AppendLine("  " + child.name + " " + child.GetType().Name + " " + child.layout + " w=" + child.resolvedStyle.width);
            break;
        }

        var panel = card.parent;
        sb.AppendLine("panel=" + panel.name + " " + panel.layout);
        var root = panel.parent;
        sb.AppendLine("root=" + root.name + " " + root.layout);
        return sb.ToString();
    }
}
