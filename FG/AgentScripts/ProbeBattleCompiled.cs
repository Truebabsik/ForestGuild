using System.Reflection;
using UnityEngine;

/// <summary>Проверяет, что боевое ядро собралось и видно из Unity.</summary>
public static class ProbeBattleCompiled
{
    public static string Run()
    {
        var type = System.Type.GetType("ForestGuild.Core.Sim.Battle, Assembly-CSharp");
        var tablesType = System.Type.GetType("ForestGuild.Core.Data.Tables, Assembly-CSharp");

        return "Battle=" + (type != null)
            + " Tables=" + (tablesType != null)
            + " fields=" + (tablesType != null ? tablesType.GetField("BeastCombat") != null : false);
    }
}
