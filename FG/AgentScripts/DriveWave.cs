using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Ставит день 21 и прогоняет ночь — проверка карточки волны.</summary>
public static class DriveWave
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();

        screen.World.Day = 21;
        screen.World.NextWaveDay = 21;

        foreach (var person in screen.Tables.Roster.All)
        {
            if (person.Profession == "hunter") ForestGuild.Core.Sim.Works.Kill(screen.World, person.Id);
        }

        screen.GetType().GetMethod("GoToNight", Flags).Invoke(screen, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var title = root.Q<Label>("pane-title");
        var lines = root.Q<ScrollView>("pane-lines");

        return "paneTitle=" + (title != null ? title.text : "?")
            + " lines=" + (lines != null ? lines.contentContainer.childCount : -1)
            + " palisade=" + screen.World.PalisadeHp + "/" + screen.World.PalisadeMax
            + " nextWave=" + screen.World.NextWaveDay
            + " dead=" + CountDead(screen);
    }

    private static int CountDead(ForestGuild.Game.MapScreen screen)
    {
        var dead = 0;
        foreach (var person in screen.Tables.Roster.All)
        {
            if (screen.World.ConditionOf(person.Id) == ForestGuild.Core.Sim.PersonCondition.Dead) dead++;
        }

        return dead;
    }
}
