using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Открывает окно деревни с работами и читает состояние.</summary>
public static class DriveVillage
{
    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var plan = (ForestGuild.Core.Sim.DayPlan)screen.GetType()
            .GetField("_plan", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(screen);

        plan.VillageWorkers.Clear();
        plan.Beds.Clear();
        plan.Foragers.Clear();

        screen.Village.Open(screen.Tables, screen.World, plan);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var works = root.Q<VisualElement>("village-works");
        var people = root.Q<ScrollView>("village-people");

        return "open=" + screen.Village.IsOpen
            + " works=" + (works != null ? works.childCount : -1)
            + " people=" + (people != null ? people.contentContainer.childCount : -1);
    }
}
