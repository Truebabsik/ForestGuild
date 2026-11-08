using System.Reflection;
using UnityEngine;

/// <summary>Проверяет, что тестовые режимы не трогают настоящий мир.</summary>
public static class ProbeDemoIsolation
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        var before = Snapshot(screen);

        type.GetMethod("StartMusterDemo", Flags).Invoke(screen, null);
        screen.Muster.Close();

        type.GetMethod("StartExpeditionDemo", Flags).Invoke(screen, null);
        if (screen.Expedition.IsOpen)
            screen.Expedition.GetType().GetMethod("SkipAll", Flags).Invoke(screen.Expedition, null);

        var after = Snapshot(screen);
        return "before=[" + before + "] after=[" + after + "] same=" + (before == after);
    }

    private static string Snapshot(ForestGuild.Game.MapScreen screen)
    {
        var store = 0;
        foreach (var pair in screen.World.Store) store += pair.Value;

        var wounded = 0;
        var dead = 0;
        foreach (var person in screen.Tables.Roster.All)
        {
            var condition = screen.World.ConditionOf(person.Id);
            if (condition == ForestGuild.Core.Sim.PersonCondition.Wounded) wounded++;
            if (condition == ForestGuild.Core.Sim.PersonCondition.Dead) dead++;
        }

        var squad = screen.World.Squads[0];
        return "store=" + store + " wounded=" + wounded + " dead=" + dead
            + " A=" + string.Join("+", squad.Members) + " zone=" + (squad.ZoneId ?? "-");
    }
}
