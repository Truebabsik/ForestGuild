using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Подписать приказ и пройти до утра: проверка, что фазы живы после переделки доски.</summary>
public static class ConfirmOrder
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        type.GetMethod("ConfirmOrder", Flags).Invoke(screen, null);
        var afterDay = Hud(screen);

        type.GetMethod("GoToEvening", Flags).Invoke(screen, null);
        var afterEvening = Hud(screen);

        type.GetMethod("GoToNight", Flags).Invoke(screen, null);
        var afterNight = Hud(screen);

        type.GetMethod("GoToMorning", Flags).Invoke(screen, null);
        var afterMorning = Hud(screen);

        return "day=" + afterDay + " | evening=" + afterEvening + " | night=" + afterNight + " | morning=" + afterMorning;
    }

    private static string Hud(ForestGuild.Game.MapScreen screen)
    {
        var hud = screen.GetComponent<UIDocument>().rootVisualElement.Q<Label>("hud");
        return hud != null ? hud.text.Replace("    |    ", " | ") : "?";
    }
}
