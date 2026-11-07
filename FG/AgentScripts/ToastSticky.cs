using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Показывает тост без таймера — чтобы поймать его на скриншоте.</summary>
public static class ToastSticky
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var toast = (Label)screen.GetType().GetField("_toast", flags).GetValue(screen);
        if (toast == null) return "нет toast";

        toast.text = "Больше 2 отрядов деревня не поднимет";
        toast.RemoveFromClassList("hidden");
        return "shown layout=" + toast.layout;
    }
}
