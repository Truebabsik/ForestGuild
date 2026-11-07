using System.Reflection;
using UnityEngine;

/// <summary>Один день в Ключи, возврат в утро и открытие досье этой зоны.</summary>
public static class DriveDossier
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

        var state = screen.World.Zones["verge_springs"];
        screen.Dossier.Open("verge_springs", screen.Tables, screen.World, true);

        return "day=" + screen.World.Day
            + " scouted=" + state.Scouted
            + " mood=" + ForestGuild.Core.Sim.ZoneKnowledge.Mood(screen.World, "verge_springs")
            + " sightings=" + state.Sightings.Count
            + " dossierOpen=" + screen.Dossier.IsOpen;
    }
}
