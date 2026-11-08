using System.Reflection;
using UnityEngine;

/// <summary>Открывает досье Чащи на разведанной зоне.</summary>
public static class ProbeThicket
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.GetType().GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "campaign" });
        screen.World.Zones["verge_thicket"].Scouted = true;
        screen.Dossier.Open("verge_thicket", screen.Tables, screen.World, false);
        return "thicket открыт";
    }
}
