using System.Reflection;
using UnityEngine;

/// <summary>Кладёт раненого в постель, спит, проверяет, что к утру он здоров.</summary>
public static class VerifySleep
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        var plan = (ForestGuild.Core.Sim.DayPlan)type.GetField("_plan", Flags).GetValue(screen);
        if (!plan.Beds.Contains("leya")) plan.Beds.Add("leya");

        type.GetMethod("GoToNight", Flags).Invoke(screen, null);
        var afterNight = screen.World.ConditionOf("leya");

        type.GetMethod("GoToMorning", Flags).Invoke(screen, null);
        var afterMorning = screen.World.ConditionOf("leya");

        return "night=" + afterNight + " morning=" + afterMorning + " day=" + screen.World.Day;
    }
}
