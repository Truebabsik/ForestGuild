using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Прогон до ночи и проверка строк: сколько их и у скольких есть ссылка на зону.</summary>
public static class DriveNight
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        // С начала суток: кампания -> планирование, иначе мир и доска ещё не созданы.
        type.GetMethod("StartScenario", Flags).Invoke(screen, new object[] { "campaign" });
        type.GetMethod("EnterPlanning", Flags).Invoke(screen, null);

        var a = screen.World.Squads[0];
        a.Members.Clear();
        a.Members.Add("brunni");
        a.Members.Add("leya");
        a.ZoneId = "verge_springs";
        screen.Board.Rebuild();

        type.GetMethod("OpenOrder", Flags).Invoke(screen, null);
        type.GetMethod("ConfirmOrder", Flags).Invoke(screen, null);
        type.GetMethod("GoToEvening", Flags).Invoke(screen, null);

        // Голодная зона: тварей больше, чем корма, — будет строка миграции со ссылкой на зону.
        var springs = screen.World.Zones["verge_springs"];
        springs.Beasts.Clear();
        springs.Beasts["beast_spark"] = 4;
        springs.Crystals = 0;

        type.GetMethod("GoToNight", Flags).Invoke(screen, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var lines = root.Q<ScrollView>("pane-lines");
        var labels = lines.contentContainer.Query<Label>().ToList();
        var links = labels.Count(l => l.ClassListContains("link"));

        return "phaseNight=" + screen.World.Day
            + " lines=" + labels.Count
            + " links=" + links;
    }
}
