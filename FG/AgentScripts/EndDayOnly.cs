using System.Reflection;
using UnityEngine;

/// <summary>Закончить день без пересборки плана.</summary>
public static class EndDayOnly
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        screen.GetType()
            .GetMethod("EndDay", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(screen, null);

        return "day=" + screen.World.Day + " palisade=" + screen.World.PalisadeHp;
    }
}
