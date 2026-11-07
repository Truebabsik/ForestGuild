using System.Reflection;
using UnityEngine;

/// <summary>
/// Прогон дня через UI: выбрать зону, взять двоих, отправить отряд, закончить день.
/// Методы вызываются рефлексией — это тот же путь, что и клик, только без мыши.
/// </summary>
public static class DriveUi
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var screenType = screen.GetType();

        screenType.GetMethod("ShowZone", flags).Invoke(screen, new object[] { "verge_springs" });
        screen.Map.GetType().GetMethod("Select", flags).Invoke(screen.Map, new object[] { "verge_springs" });

        var roster = screenType.GetField("_roster", flags).GetValue(screen);
        roster.GetType().GetMethod("Toggle", flags).Invoke(roster, new object[] { "brunni" });
        roster.GetType().GetMethod("Toggle", flags).Invoke(roster, new object[] { "leya" });

        screenType.GetMethod("SendParty", flags).Invoke(screen, null);
        screenType.GetMethod("SendParty", flags).Invoke(screen, null); // второе нажатие — должен отругать за пустой выбор

        screenType.GetMethod("EndDay", flags).Invoke(screen, null);

        return "day=" + screen.World.Day + " parties=" + screen.World.TotalBeasts();
    }
}
