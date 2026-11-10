using System;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    /// <summary>
    /// Быстрый прогон похода: тот же `Expedition`, но все развилки решаются наказами по умолчанию.
    /// Нужен консольным прогонам и тестам; в игре поход идёт по шагам (`Expedition` + UI).
    /// </summary>
    public static class ExpeditionSim
    {
        public static ExpeditionResult Resolve(World world, Tables tables, PartyAssignment party, Rng rng)
        {
            var expedition = new Expedition(world, tables, party, rng);

            for (var guard = 0; guard < 500 && !expedition.Finished; guard++)
            {
                if (expedition.HasDecision)
                {
                    expedition.Decide(Default(expedition.Pending));
                    continue;
                }

                expedition.Advance();
            }

            return expedition.Result;
        }

        /// Спокойная политика по умолчанию: уйти, тушу бросить, раненого нести.
        public static string Default(ExpeditionDecision decision)
        {
            switch (decision.Kind)
            {
                case DecisionKind.Encounter: return "retreat";
                case DecisionKind.Battle: return "next";
                case DecisionKind.Carcass: return "leave";
                case DecisionKind.Wounded: return "carry";
                default: return decision.Options[0].Id;
            }
        }
    }
}
