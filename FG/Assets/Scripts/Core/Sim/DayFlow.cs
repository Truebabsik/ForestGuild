using System;
using System.Collections.Generic;
using System.Linq;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class DayReport
    {
        public readonly List<ExpeditionResult> Parties = new List<ExpeditionResult>();
        public readonly List<string> Lines = new List<string>();

        /// <summary>Добыча дня по предметам — уходит в склад (ui.md §6).</summary>
        public readonly Dictionary<string, int> Loot = new Dictionary<string, int>(StringComparer.Ordinal);

        public NightReport Night;
        public int Repaired;
    }

    /// <summary>
    /// День по фазам: вылазки и работы (День) и жизнь леса (Ночь) разделены, как в UI (docs/ui.md §3).
    /// `RunDay` остаётся для консольных прогонов и тестов.
    /// </summary>
    public sealed class DayFlow
    {
        private readonly World _world;
        private readonly Tables _tables;
        private readonly Rng _rng;

        public DayFlow(World world, Tables tables, Rng rng)
        {
            _world = world;
            _tables = tables;
            _rng = rng;
        }

        /// Фаза «День»: работы в деревне. Походы идут отдельно — по шагам (UI) или разом (`RunExpeditions`).
        public DayReport BeginDay(DayPlan plan)
        {
            var report = new DayReport();
            RunWorks(plan, report);
            return report;
        }

        /// Итог походов: добыча в склад, строки в отчёт.
        public void FinishExpeditions(DayReport report, IReadOnlyList<ExpeditionResult> results)
        {
            foreach (var result in results)
            {
                report.Parties.Add(result);
                report.Lines.AddRange(result.Log);

                foreach (var found in result.Found)
                {
                    report.Lines.Add("Принесено: " + _tables.Items.Get(found.Key).Name + " ×" + found.Value);
                }
            }

            if (report.Parties.Count == 0)
            {
                report.Lines.Add("В лес никто не пошёл");
            }

            CollectLoot(report);
        }

        /// Весь день разом — для консольных прогонов и тестов.
        public DayReport RunExpeditions(DayPlan plan)
        {
            var report = BeginDay(plan);

            var results = new List<ExpeditionResult>();
            foreach (var party in plan.Parties)
            {
                results.Add(ExpeditionSim.Resolve(_world, _tables, party, _rng));
            }

            FinishExpeditions(report, results);
            return report;
        }

        /// Добыча уходит в склад сразу: вечер разбирает её, а не решает, доедет ли она.
        private void CollectLoot(DayReport report)
        {
            foreach (var party in report.Parties)
            {
                foreach (var found in party.Found)
                {
                    report.Loot.TryGetValue(found.Key, out var current);
                    report.Loot[found.Key] = current + found.Value;

                    _world.Store.TryGetValue(found.Key, out var stored);
                    _world.Store[found.Key] = stored + found.Value;
                }
            }
        }

        private void RunWorks(DayPlan plan, DayReport report)
        {
            if (plan.VillageWorkers.Count > 0)
            {
                report.Repaired = Works.RepairPalisade(_world, plan.VillageWorkers.Count);
                var workers = plan.VillageWorkers.Count + " человеко-дн.";
                report.Lines.Add(report.Repaired == 0
                    ? "Частокол и так цел: " + _world.PalisadeHp + "/" + _world.PalisadeMax + " (" + workers + ")"
                    : "Частокол: " + _world.PalisadeHp + "/" + _world.PalisadeMax
                      + " (+" + report.Repaired + " за " + workers + ")");
            }

            var forage = Works.Forage(_world, plan.Foragers.Count);
            if (forage > 0) report.Lines.Add("Заготовка: +" + forage + " корма в склад");

            foreach (var personId in plan.Beds) Works.PutToBed(_world, personId);
        }

        /// Фаза «Ночь»: постели, лес живёт, день сменяется.
        public NightReport RunNight(DayPlan plan = null)
        {
            if (plan != null)
            {
                foreach (var personId in plan.Beds) Works.PutToBed(_world, personId);
            }

            var report = NightSim.AdvanceNight(_world, _tables, _rng);

            var healed = Works.Recover(_world);
            if (healed.Count > 0)
            {
                var names = healed.Select(id => _tables.Roster.Get(id).Name);
                report.Add("К утру встали с постели: " + string.Join(", ", names));
            }

            var endedDay = _world.Day - 1;
            if (endedDay == _world.NextWaveDay)
            {
                report.Wave = WaveSim.Resolve(_world, _tables, Defenders(plan), _rng);
                report.Lines.AddRange(report.Wave.Lines);
            }

            return report;
        }

        /// Кто остался в деревне: все, кроме ушедших в лес.
        private List<string> Defenders(DayPlan plan)
        {
            var home = new List<string>();

            foreach (var person in _tables.Roster.All)
            {
                var inForest = false;

                if (plan != null)
                {
                    foreach (var party in plan.Parties)
                    {
                        if (party.PersonIds.Contains(person.Id))
                        {
                            inForest = true;
                            break;
                        }
                    }
                }

                if (!inForest) home.Add(person.Id);
            }

            return home;
        }

        /// Один день целиком — для консольных прогонов и тестов.
        public DayReport RunDay(DayPlan plan)
        {
            var report = RunExpeditions(plan);
            report.Night = RunNight();
            report.Lines.AddRange(report.Night.Lines);
            return report;
        }
    }
}
