using System.Reflection;
using UnityEngine;

/// <summary>Останавливается на первой встрече, не выбирая.</summary>
public static class ProbeEncounter
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

        for (var i = 0; i < 60; i++)
        {
            var current = currentField.GetValue(expedition);
            if (current == null) break;
            if ((bool)current.GetType().GetProperty("HasDecision").GetValue(current)) break;
            tick.Invoke(expedition, null);
        }

        var cur = currentField.GetValue(expedition);
        var pending = cur != null ? cur.GetType().GetProperty("Pending").GetValue(cur) : null;
        return pending != null ? "pending=" + pending.GetType().GetField("Kind").GetValue(pending) : "no decision";
    }
}
