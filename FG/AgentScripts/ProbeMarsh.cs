using System.Reflection;
using UnityEngine;

/// <summary>Открывает досье Топи на разведанной зоне.</summary>
public static class ProbeMarsh
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.GetType().GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "campaign" });
        screen.World.Zones["verge_marsh"].Scouted = true;
        screen.Dossier.Open("verge_marsh", screen.Tables, screen.World, false);
        return "marsh открыт";
    }
}
