using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Встреча → бой → освежевать: что попало в мешок из drops.csv.</summary>
public static class ProbeButcher
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
            if (current == null) return "no party";
            if ((bool)current.GetType().GetProperty("HasDecision").GetValue(current)) break;
            tick.Invoke(expedition, null);
        }

        var party = currentField.GetValue(expedition);
        var result = party.GetType().GetField("Result").GetValue(party);
        var pendingProp = party.GetType().GetProperty("Pending");

        var pending = pendingProp.GetValue(party);
        var kind = pending.GetType().GetField("Kind").GetValue(pending).ToString();
        if (kind != "Encounter") return "no encounter: " + kind;

        type.GetMethod("Choose", Flags).Invoke(expedition, new object[] { "fight" });

        pending = pendingProp.GetValue(party);
        if (pending == null) return "fight left no decision";

        kind = pending.GetType().GetField("Kind").GetValue(pending).ToString();
        if (kind != "Carcass") return "after fight: " + kind;

        var found = (List<KeyValuePair<string, int>>)result.GetType().GetField("Found").GetValue(result);
        var before = found.Count;

        type.GetMethod("Choose", Flags).Invoke(expedition, new object[] { "haul" });

        var log = (List<string>)result.GetType().GetField("Log").GetValue(result);
        var tail = new List<string>();
        for (var i = System.Math.Max(0, log.Count - 4); i < log.Count; i++) tail.Add(log[i]);

        return "foundBefore=" + before + " foundAfter=" + found.Count + " log=[" + string.Join(" | ", tail) + "]";
    }
}
