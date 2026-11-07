using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Открывает Сбор отряда, добавляет двоих, ставит наказ и возвращает сводку.</summary>
public static class DriveMuster
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var squad = screen.World.Squads[0];
        squad.ZoneId = "verge_springs";
        squad.Members.Clear();

        var plan = (ForestGuild.Core.Sim.DayPlan)screen.GetType().GetField("_plan", Flags).GetValue(screen);
        screen.Muster.Open(squad, screen.Tables, screen.World, plan);

        var musterType = screen.Muster.GetType();
        var add = musterType.GetMethod("AddMember", Flags);
        add.Invoke(screen.Muster, new object[] { "brunni" });
        add.Invoke(screen.Muster, new object[] { "leya" });

        squad.Orders.Encounter = ForestGuild.Core.Sim.EncounterOrder.Retreat;
        musterType.GetMethod("Rebuild", Flags).Invoke(screen.Muster, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var summary = root.Q<Label>("muster-summary");
        var free = root.Q<ScrollView>("muster-free");
        var tray = root.Q<VisualElement>("muster-tray");

        return "open=" + screen.Muster.IsOpen
            + " members=" + squad.Members.Count
            + " free=" + (free != null ? free.contentContainer.childCount : -1)
            + " traySlots=" + (tray != null ? tray.childCount : -1)
            + " summary=[" + (summary != null ? summary.text : "?") + "]";
    }
}
