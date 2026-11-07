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

        /// Фаза «День»: работы в деревне и вылазки. Ночь не наступает, день не меняется.
        public DayReport RunExpeditions(DayPlan plan)
        {
            var report = new DayReport();

            RunWorks(plan, report);

            foreach (var party in plan.Parties)
            {
                var result = ExpeditionSim.Resolve(_world, _tables, party, _rng);
                report.Parties.Add(result);
                report.Lines.AddRange(result.Log);

                foreach (var found in result.Found)
                {
                    report.Lines.Add("Принесено: " + _tables.Items.Get(found.Key).Name + " ×" + found.Value);
                }
            }

            if (plan.Parties.Count == 0)
            {
                report.Lines.Add("В лес никто не пошёл");
            }

            return report;
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

        /// Фаза «Ночь»: лес живёт, день сменяется, постели делают своё дело.
        public NightReport RunNight()
        {
            var report = NightSim.AdvanceNight(_world, _tables, _rng);

            var healed = Works.Recover(_world);
            if (healed.Count > 0)
            {
                var names = healed.Select(id => _tables.Roster.Get(id).Name);
                report.Add("К утру встали с постели: " + string.Join(", ", names));
            }

            return report;
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
