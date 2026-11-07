using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Переключает отряды и проверяет лимит в четыре бойца.</summary>
public static class ProbeParty
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.Muster.GetType();
        var root = screen.GetComponent<UIDocument>().rootVisualElement;

        var tabs = root.Q<VisualElement>("muster-squads");
        var buttons = tabs.Query<Button>().ToList();

        buttons[1].SendEvent(new NavigationSubmitEvent { target = buttons[1] });
        var switched = (ForestGuild.Core.Sim.Squad)type.GetField("_squad", Flags).GetValue(screen.Muster);

        var add = type.GetMethod("AddMember", Flags);
        foreach (var id in new[] { "brunni", "skadi", "hilda", "tove", "run" })
        {
            add.Invoke(screen.Muster, new object[] { id });
        }

        return "tabs=" + buttons.Count
            + " switchedTo=" + switched.Name
            + " members=" + switched.Members.Count
            + " partyMax=" + screen.Tables.Tuning.GetInt("party_max");
    }
}
