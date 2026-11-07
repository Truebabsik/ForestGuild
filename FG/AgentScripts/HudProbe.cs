using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>День с добычей, вечер, ночь, утро — и чтение строки HUD.</summary>
public static class HudProbe
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        var a = screen.World.Squads[0];
        a.Members.Clear();
        a.Members.Add("brunni");
        a.Members.Add("leya");
        a.ZoneId = "verge_springs";
        screen.Board.Rebuild();

        type.GetMethod("OpenOrder", Flags).Invoke(screen, null);
        type.GetMethod("ConfirmOrder", Flags).Invoke(screen, null);
        type.GetMethod("GoToEvening", Flags).Invoke(screen, null);
        type.GetMethod("GoToNight", Flags).Invoke(screen, null);
        type.GetMethod("GoToMorning", Flags).Invoke(screen, null);

        var hud = screen.GetComponent<UIDocument>().rootVisualElement.Q<Label>("hud");
        return hud != null ? hud.text.Replace("    |    ", " | ") : "?";
    }
}
