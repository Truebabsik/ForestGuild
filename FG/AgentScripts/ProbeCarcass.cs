using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Встреча → «бой» → должна появиться карточка туши.</summary>
public static class ProbeCarcass
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var screen = Object.FindAnyObjectByType<ForestGuild.Game.MapScreen>();
        screen.GetType().GetMethod("StartExpeditionDemo", Flags).Invoke(screen, null);

        var expedition = screen.Expedition;
        var type = expedition.GetType();
        var tick = type.GetMethod("Tick", Flags);
        var currentField = type.GetField("_current", Flags);

        var hit = "";
        for (var i = 0; i < 60; i++)
        {
            var current = currentField.GetValue(expedition);
            if (current == null) break;

            var hasDecision = (bool)current.GetType().GetProperty("HasDecision").GetValue(current);
            if (hasDecision) break;

            tick.Invoke(expedition, null);
        }

        var cur = currentField.GetValue(expedition);
        if (cur == null) return "no current";

        var pending = cur.GetType().GetProperty("Pending").GetValue(cur);
        var firstKind = pending != null ? pending.GetType().GetField("Kind").GetValue(pending).ToString() : "none";

        if (pending != null && firstKind == "Encounter")
        {
            type.GetMethod("Choose", Flags).Invoke(expedition, new object[] { "fight" });
            hit = "chosen fight";
        }

        cur = currentField.GetValue(expedition);
        pending = cur.GetType().GetProperty("Pending").GetValue(cur);
        var secondKind = pending != null ? pending.GetType().GetField("Kind").GetValue(pending).ToString() : "none";

        var root = screen.GetComponent<UIDocument>().rootVisualElement;
        var card = root.Q<VisualElement>("expedition-card");
        var title = root.Q<Label>("decision-title");

        return "first=" + firstKind + " " + hit
            + " second=" + secondKind
            + " cardShown=" + (card != null && !card.ClassListContains("hidden"))
            + " title=[" + (title != null ? title.text : "?") + "]";
    }
}
