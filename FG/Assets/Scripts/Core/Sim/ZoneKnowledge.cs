using ForestGuild.Core.Sim;

namespace ForestGuild.Core.Sim
{
    /// <summary>
    /// Что деревня знает о зоне. Точных чисел нет — только настроение словами (design.md §6.5).
    /// </summary>
    public static class ZoneKnowledge
    {
        public static string Mood(World world, string zoneId)
        {
            if (!world.Zones.TryGetValue(zoneId, out var state)) return "?";

            if (!state.Scouted) return "?";
            if (state.Nest.Exists) return "гнездо";

            var beasts = world.BeastCount(zoneId);
            if (beasts == 0) return "тихо";
            if (beasts <= 2) return "следы";
            return "неспокойно";
        }
    }
}
