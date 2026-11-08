using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>Сколько MapScreen в сцене и жив ли _menu.</summary>
public static class ProbeScreenState
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var all = Object.FindObjectsByType<ForestGuild.Game.MapScreen>(FindObjectsSortMode.None);
        var text = "instances=" + all.Length;

        foreach (var screen in all)
        {
            var menu = screen.GetType().GetField("_menu", Flags).GetValue(screen);
            var world = screen.World;
            text += " | active=" + screen.isActiveAndEnabled
                + " enabled=" + screen.enabled
                + " menu=" + (menu != null)
                + " world=" + (world != null);
        }

        return text;
    }
}
