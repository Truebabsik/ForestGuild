using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Печатает строки приказа и часы похода.</summary>
public static class ProbeTimes
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        var squad = screen.World.Squads[0];
        squad.Members.Clear();
        squad.Members.Add("brunni");
        squad.Members.Add("leya");
        squad.ZoneId = "verge_meadow";

        screen.Board.Rebuild();
        type.GetMethod("EnterPlanning", Flags).Invoke(screen, null);
        type.GetMethod("OpenOrder", Flags).Invoke(screen, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var lines = root.Q<VisualElement>("modal-lines");
        var text = "";
        foreach (var label in lines.Query<Label>().ToList())
        {
            if (label.text.Contains("дорога")) text += label.text.Trim() + " | ";
        }

        var meadow = ForestGuild.Core.Sim.Briefing.Compute(screen.World, screen.Tables,
            new[] { "brunni", "leya" }, "verge_meadow", ForestGuild.Core.Sim.Behavior.Gather);
        var heart = ForestGuild.Core.Sim.Briefing.Compute(screen.World, screen.Tables,
            new[] { "brunni", "leya" }, "heart_root", ForestGuild.Core.Sim.Behavior.Gather);

        return "lines=[" + text + "] meadowWork=" + meadow.WorkHours
            + " heartWork=" + heart.WorkHours
            + " hoursPerDay=" + screen.Tables.Tuning.GetInt("hours_per_day");
    }
}
