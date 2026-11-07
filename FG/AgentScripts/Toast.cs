using System.Reflection;
using UnityEngine;

/// <summary>Показать тост, чтобы поймать его на скриншоте.</summary>
public static class Toast
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        var method = screen.GetType().GetMethod("ShowMessage",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(screen, new object[] { "Проверка тоста: больше 2 отрядов деревня не поднимет" });
        return "shown";
    }
}
