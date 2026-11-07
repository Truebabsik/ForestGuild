using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Наполняет отряд «А» и открывает приказ — как кликами по доске.</summary>
public static class DriveMorning
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        if (screen == null) return "нет MapScreen";

        var board = screen.Board;
        var boardType = board.GetType();

        board.Active = screen.World.Squads[0];
        boardType.GetMethod("CycleZone", Flags).Invoke(board, new object[] { screen.World.Squads[0] });

        var click = boardType.GetMethod("OnPersonClicked", Flags);
        click.Invoke(board, new object[] { "brunni" });
        click.Invoke(board, new object[] { "leya" });

        board.Rebuild();

        screen.GetType().GetMethod("OpenOrder", Flags).Invoke(screen, null);

        var modal = screen.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("modal");
        var lines = screen.GetComponent<UIDocument>().rootVisualElement.Q<ScrollView>("modal-lines");

        return "squad=" + screen.World.Squads[0].Members.Count
            + " zone=" + screen.World.Squads[0].ZoneId
            + " modal=" + (modal != null && !modal.ClassListContains("hidden"))
            + " lines=" + (lines != null ? lines.contentContainer.childCount : -1);
    }
}
