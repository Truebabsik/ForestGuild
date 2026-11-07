using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class DayReport
    {
        public readonly List<ExpeditionResult> Parties = new List<ExpeditionResult>();
        public readonly List<string> Lines = new List<string>();
        public NightReport Night;
    }

    /// <summary>
    /// Один день: вылазки, затем ночь. Единственное место, где день «идёт» —
    /// чтобы UI не решал порядок шагов сам.
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

        public DayReport RunDay(DayPlan plan)
        {
            var report = new DayReport();

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

            report.Night = NightSim.AdvanceNight(_world, _tables, _rng);
            report.Lines.AddRange(report.Night.Lines);
            return report;
        }
    }
}
