using System.Reflection;
using UnityEngine;

/// <summary>Полный день через UI: два отряда, работник в деревне, конец дня.</summary>
public static class FullDay
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = screen.GetType();
        var roster = type.GetField("_roster", flags).GetValue(screen);
        var toggle = roster.GetType().GetMethod("Toggle", flags);

        type.GetMethod("ShowZone", flags).Invoke(screen, new object[] { "verge_springs" });
        screen.Map.GetType().GetMethod("Select", flags).Invoke(screen.Map, new object[] { "verge_springs" });

        toggle.Invoke(roster, new object[] { "brunni" });
        toggle.Invoke(roster, new object[] { "leya" });
        type.GetMethod("SendParty", flags).Invoke(screen, null);

        type.GetMethod("ShowZone", flags).Invoke(screen, new object[] { "verge_marsh" });
        screen.Map.GetType().GetMethod("Select", flags).Invoke(screen.Map, new object[] { "verge_marsh" });
        toggle.Invoke(roster, new object[] { "skadi" });
        toggle.Invoke(roster, new object[] { "tove" });
        type.GetMethod("SendParty", flags).Invoke(screen, null);

        toggle.Invoke(roster, new object[] { "valda" });
        type.GetMethod("SendToVillage", flags).Invoke(screen, null);

        type.GetMethod("EndDay", flags).Invoke(screen, null);
        return "day=" + screen.World.Day + " palisade=" + screen.World.PalisadeHp;
    }
}
