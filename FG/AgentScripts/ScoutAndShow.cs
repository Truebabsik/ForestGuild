using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Гоняет дни в Ключи, пока не появится слух, затем открывает досье зоны и прячет отчёт.
/// </summary>
public static class ScoutAndShow
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = screen.GetType();
        var roster = type.GetField("_roster", flags).GetValue(screen);
        var toggle = roster.GetType().GetMethod("Toggle", flags);
        var showZone = type.GetMethod("ShowZone", flags, null, new[] { typeof(string) }, null);
        var sendParty = type.GetMethod("SendParty", flags);
        var endDay = type.GetMethod("EndDay", flags);
        var report = (VisualElement)type.GetField("_report", flags).GetValue(screen);

        var state = screen.World.Zones["verge_springs"];
        var days = 0;
        while (state.Sightings.Count == 0 && days < 12)
        {
            showZone.Invoke(screen, new object[] { "verge_springs" });
            screen.Map.GetType().GetMethod("Select", flags).Invoke(screen.Map, new object[] { "verge_springs" });

            toggle.Invoke(roster, new object[] { "brunni" });
            toggle.Invoke(roster, new object[] { "leya" });
            sendParty.Invoke(screen, null);
            endDay.Invoke(screen, null);
            days++;
        }

        report.AddToClassList("hidden");
        screen.Map.GetType().GetMethod("Select", flags).Invoke(screen.Map, new object[] { "verge_springs" });
        showZone.Invoke(screen, new object[] { "verge_springs" });

        return "days=" + days + " day=" + screen.World.Day
            + " mood=" + ForestGuild.Core.Sim.ZoneKnowledge.Mood(screen.World, "verge_springs")
            + " sightings=" + state.Sightings.Count;
    }
}
