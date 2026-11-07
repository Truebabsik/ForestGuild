using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Отряд А полон, Б пуст: открываем Сбор из меню, смотрим Б и забираем человека из А.</summary>
public static class ProbeSteal
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();

        var a = screen.World.Squads[0];
        a.Members.Clear();
        foreach (var id in new[] { "brunni", "skadi", "hilda", "orn" }) a.Members.Add(id);

        var b = screen.World.Squads[1];
        b.Members.Clear();

        screen.GetType().GetMethod("StartMusterDemo", Flags).Invoke(screen, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var tabs = root.Q<VisualElement>("muster-squads");
        var buttons = tabs.Query<Button>().ToList();
        buttons[1].SendEvent(new NavigationSubmitEvent { target = buttons[1] });

        return "A=" + string.Join(",", a.Members) + " B=" + string.Join(",", b.Members);
    }
}
