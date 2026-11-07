using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Готовит отряд и открывает приказ — проверка портретов и нарративных строк.</summary>
public static class DriveOrder
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        var type = screen.GetType();

        var a = screen.World.Squads[0];
        a.Members.Clear();
        a.Members.Add("brunni");
        a.Members.Add("leya");
        a.Members.Add("tove");
        a.ZoneId = "verge_springs";

        screen.Board.Rebuild();
        type.GetMethod("EnterPlanning", Flags).Invoke(screen, null);
        type.GetMethod("OpenOrder", Flags).Invoke(screen, null);

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var modal = root.Q<VisualElement>("modal");
        var lines = root.Q<VisualElement>("modal-lines");
        var portraits = lines.Query<VisualElement>(className: "portrait").ToList();
        var warnings = lines.Query<Label>(className: "warning-line").ToList();

        return "modalOpen=" + (modal != null && !modal.ClassListContains("hidden"))
            + " portraits=" + portraits.Count
            + " warnings=" + warnings.Count
            + " first=[" + (warnings.Count > 0 ? warnings[0].text.Trim() : "—") + "]";
    }
}
