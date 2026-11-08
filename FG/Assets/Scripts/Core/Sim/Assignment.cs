namespace ForestGuild.Core.Sim
{
    /// <summary>
    /// Кто где занят. Правило «человек — одно дело в день» живёт в ядре,
    /// UI только печатает причину.
    /// </summary>
    public static class Assignment
    {
        /// Причина занятости или null, если человек свободен.
        public static string Where(World world, DayPlan plan, Squad currentSquad, string personId)
        {
            var condition = world.ConditionOf(personId);
            if (condition == PersonCondition.Dead) return "погиб";
            if (condition == PersonCondition.Wounded) return "ранен, лечится";

            foreach (var squad in world.Squads)
            {
                if (ReferenceEquals(squad, currentSquad)) continue;
                if (squad.Members.Contains(personId)) return "в отряде «" + squad.Name + "»";
            }

            if (currentSquad != null && currentSquad.Members.Contains(personId)) return null;

            if (plan.VillageWorkers.Contains(personId)) return "на частоколе";
            if (plan.Beds.Contains(personId)) return "на постели";
            if (plan.Foragers.Contains(personId)) return "на заготовке";

            return null;
        }

        public static bool IsFree(World world, DayPlan plan, Squad currentSquad, string personId)
        {
            return Where(world, plan, currentSquad, personId) == null;
        }
    }
}
