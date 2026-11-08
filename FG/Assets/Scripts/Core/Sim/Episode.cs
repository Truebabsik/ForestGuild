using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    /// <summary>
    /// Эпизод мира: конкретный мир + сид + сценарий, с которого он начался.
    /// «Начать игру» и любой тестовый запуск — это один и тот же механизм.
    /// </summary>
    public sealed class Episode
    {
        public readonly Scenario Scenario;
        public readonly World World;
        public readonly DayPlan Plan = new DayPlan();
        public readonly Rng Rng;

        public Episode(Scenario scenario, World world, Rng rng)
        {
            Scenario = scenario;
            World = world;
            Rng = rng;
        }

        public bool EndsInMenu => Scenario.OnFinish == "menu";
    }

    /// <summary>Сборка эпизода из таблиц: сценарий задаёт сид, день и стартовые отряды.</summary>
    public static class EpisodeBuilder
    {
        public static Episode Build(Tables tables, string scenarioId)
        {
            if (!tables.Scenarios.TryGet(scenarioId, out var scenario))
                throw new DataException("scenarios.csv", "неизвестный сценарий " + scenarioId);

            var world = World.Create(tables);
            world.Day = scenario.Day;

            foreach (var party in tables.ScenarioParties.All)
            {
                if (party.ScenarioId != scenario.Id) continue;

                var squad = SquadById(world, party.Squad);
                if (squad == null)
                    throw new DataException("scenario_parties.csv", "нет отряда " + party.Squad);

                squad.ZoneId = string.IsNullOrEmpty(party.Zone) ? null : party.Zone;
                squad.Behavior = ParseBehavior(party.Behavior);
                squad.Orders.Encounter = ParseOrder(party.EncounterOrder);

                squad.Members.Clear();
                foreach (var member in Members(party.Members))
                {
                    if (!tables.Roster.TryGet(member, out _))
                        throw new DataException("scenario_parties.csv", "неизвестный житель " + member);
                    squad.Members.Add(member);
                }
            }

            return new Episode(scenario, world, new Rng(scenario.Seed));
        }

        /// Список доступных сценариев — для меню.
        public static IEnumerable<Scenario> All(Tables tables)
        {
            return tables.Scenarios.All;
        }

        private static Squad SquadById(World world, string id)
        {
            foreach (var squad in world.Squads)
            {
                if (squad.Id == id) return squad;
            }

            return null;
        }

        private static IEnumerable<string> Members(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) yield break;

            foreach (var part in value.Split('|'))
            {
                var id = part.Trim();
                if (id.Length > 0) yield return id;
            }
        }

        private static Behavior ParseBehavior(string value)
        {
            switch (value)
            {
                case "avoid": return Behavior.Avoid;
                case "clean": return Behavior.Clean;
                default: return Behavior.Gather;
            }
        }

        private static EncounterOrder ParseOrder(string value)
        {
            switch (value)
            {
                case "hide": return EncounterOrder.Hide;
                case "retreat": return EncounterOrder.Retreat;
                case "fight": return EncounterOrder.Fight;
                default: return EncounterOrder.Ask;
            }
        }
    }
}
