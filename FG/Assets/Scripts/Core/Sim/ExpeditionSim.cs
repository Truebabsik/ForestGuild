using System;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    /// <summary>
    /// Быстрый прогон похода: тот же `Expedition`, но все развилки решаются наказами отряда.
    /// Нужен консольным прогонам и тестам; в игре поход идёт по шагам (`Expedition` + UI).
    /// </summary>
    public static class ExpeditionSim
    {
        public static ExpeditionResult Resolve(World world, Tables tables, PartyAssignment party, Rng rng)
        {
            var expedition = new Expedition(world, tables, party, rng);
            var orders = party.Orders ?? new StandingOrders();

            for (var guard = 0; guard < 500 && !expedition.Finished; guard++)
            {
                if (expedition.HasDecision)
                {
                    expedition.Decide(Default(expedition.Pending, orders));
                    continue;
                }

                expedition.Advance();
            }

            return expedition.Result;
        }

        /// <summary>
        /// Решение по наказам отряда — те же правила, что игрок видит в карточке.
        /// «Домотать» в UI зовёт это же: поставил «бей» — поход дерётся, а не отступает.
        /// </summary>
        public static string Default(ExpeditionDecision decision, StandingOrders orders)
        {
            orders ??= new StandingOrders();

            switch (decision.Kind)
            {
                case DecisionKind.Encounter:
                    return orders.Encounter == EncounterOrder.Fight ? "fight"
                        : orders.Encounter == EncounterOrder.Hide ? "hide"
                        : "retreat";

                case DecisionKind.Battle:
                    return "next";

                case DecisionKind.Carcass:
                    return orders.Carcass == CarcassOrder.Haul ? "haul"
                        : orders.Carcass == CarcassOrder.Butcher ? "butcher"
                        : "leave";

                case DecisionKind.Wounded:
                    return orders.Wounded == WoundedOrder.Leave ? "leave" : "carry";

                default:
                    return decision.Options[0].Id;
            }
        }
    }
}
