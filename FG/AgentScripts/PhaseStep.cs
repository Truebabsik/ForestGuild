using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Проход по фазам каркаса: собрать план, подписать, вечер, ночь, утро.</summary>
public static class PhaseStep
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    private static ForestGuild.Game.MapScreen Screen()
    {
        return Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
    }

    public static string Plan()
    {
        var screen = Screen();
        var type = screen.GetType();
        var roster = type.GetField("_roster", Flags).GetValue(screen);
        var toggle = roster.GetType().GetMethod("Toggle", Flags);

        type.GetMethod("ShowZone", Flags, null, new[] { typeof(string) }, null)
            .Invoke(screen, new object[] { "verge_springs" });
        screen.Map.GetType().GetMethod("Select", Flags).Invoke(screen.Map, new object[] { "verge_springs" });

        toggle.Invoke(roster, new object[] { "brunni" });
        toggle.Invoke(roster, new object[] { "leya" });
        type.GetMethod("SendParty", Flags).Invoke(screen, null);

        toggle.Invoke(roster, new object[] { "valda" });
        type.GetMethod("SendToVillage", Flags).Invoke(screen, null);

        return Hud(screen);
    }

    public static string Day()
    {
        var screen = Screen();
        screen.GetType().GetMethod("SignOrder", Flags).Invoke(screen, null);
        return Hud(screen);
    }

    public static string Evening()
    {
        var screen = Screen();
        screen.GetType().GetMethod("GoToEvening", Flags).Invoke(screen, null);
        return Hud(screen);
    }

    public static string Night()
    {
        var screen = Screen();
        screen.GetType().GetMethod("GoToNight", Flags).Invoke(screen, null);
        return Hud(screen);
    }

    public static string Morning()
    {
        var screen = Screen();
        screen.GetType().GetMethod("GoToMorning", Flags).Invoke(screen, null);
        return Hud(screen);
    }

    private static string Hud(ForestGuild.Game.MapScreen screen)
    {
        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var hud = root.Q<Label>("hud");
        var pane = root.Q<VisualElement>("pane");
        var visible = pane != null && !pane.ClassListContains("hidden");
        return "hud=[" + (hud != null ? hud.text : "?") + "] pane=" + visible;
    }
}
