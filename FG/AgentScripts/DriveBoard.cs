using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Наполняет отряды напрямую и пересобирает доску — для снимка.</summary>
public static class DriveBoard
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();

        var a = screen.World.Squads[0];
        a.Members.Clear();
        a.Members.Add("brunni");
        a.Members.Add("leya");
        a.ZoneId = "verge_springs";
        a.Behavior = ForestGuild.Core.Sim.Behavior.Gather;

        var b = screen.World.Squads[1];
        b.Members.Clear();
        b.Members.Add("skadi");
        b.Members.Add("hilda");
        b.Members.Add("tove");
        b.ZoneId = "verge_marsh";
        b.Behavior = ForestGuild.Core.Sim.Behavior.Clean;

        screen.Board.Rebuild();
        screen.GetType()
            .GetMethod("ShowActiveSquadZone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(screen, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var roster = root.Q<VisualElement>("roster");

        return "A=" + a.Members.Count + " B=" + b.Members.Count
            + " rosterНаКарте=" + (roster != null);
    }
}
