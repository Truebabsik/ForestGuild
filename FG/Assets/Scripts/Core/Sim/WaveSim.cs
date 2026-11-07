using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class WaveReport
    {
        public int Day;
        public int Strength;
        public int Killed;
        public int PalisadeDamage;
        public int Casualties;
        public bool Breached;
        public readonly List<string> Lines = new List<string>();
        public readonly List<string> Dead = new List<string>();
    }

    /// <summary>
    /// Волна (design.md §10): лес выплёвывает накопленное у деревни.
    /// Сила = твари приграничных зон + по одной с гнезда; охотники в деревне сбивают часть на подходе,
    /// остальные бьют по частоколу, излишек урона убивает жителей.
    /// </summary>
    public static class WaveSim
    {
        public static WaveReport Resolve(World world, Tables tables, IReadOnlyList<string> home, Rng rng)
        {
            var tuning = tables.Tuning;
            var report = new WaveReport { Day = world.Day };

            report.Strength = Strength(world, tables);

            if (report.Strength <= 0)
            {
                report.Lines.Add("Волна выдохлась: у деревни никого нет");
                Schedule(world, tuning);
                return report;
            }

            report.Killed = HuntersShoot(world, tables, home, report.Strength, tuning, rng);
            var remaining = report.Strength - report.Killed;

            var damage = remaining * tuning.GetInt("wave_beast_damage");
            var absorbed = Math.Min(world.PalisadeHp, damage);
            world.PalisadeHp -= absorbed;

            report.PalisadeDamage = absorbed;
            report.Breached = world.PalisadeHp <= 0;

            var overflow = damage - absorbed;
            if (overflow > 0)
            {
                var casualties = overflow / Math.Max(1, tuning.GetInt("wave_casualty_divisor"));
                report.Casualties = KillVillagers(world, tables, casualties, rng, report.Dead);
            }

            Describe(world, tables, report);
            Schedule(world, tuning);
            return report;
        }

        /// Твари приграничных зон (не дальше одного ребра от ворот) плюс по одной с каждого гнезда.
        public static int Strength(World world, Tables tables)
        {
            var distances = NightSim.DistancesToVillage(tables, tables.Tuning.Get("village_zone"));
            var strength = 0;

            foreach (var zone in world.Zones.Values)
            {
                if (distances.TryGetValue(zone.ZoneId, out var distance) && distance <= 1)
                    strength += world.BeastCount(zone.ZoneId);

                if (zone.Nest.Exists) strength += 1;
            }

            return strength;
        }

        private static int HuntersShoot(World world, Tables tables,
            IReadOnlyList<string> home, int strength, Tuning tuning, Rng rng)
        {
            var chance = tuning.GetFloat("hunter_kill_chance");
            var killed = 0;

            if (home != null)
            {
                foreach (var personId in home)
                {
                    if (!world.People.TryGetValue(personId, out var state)) continue;
                    if (state.Condition != PersonCondition.Healthy) continue;

                    var person = tables.Roster.Get(personId);
                    if (person.Profession != "hunter") continue;

                    if (rng.Chance(chance)) killed++;
                }
            }

            return Math.Min(killed, strength);
        }

        private static int KillVillagers(World world, Tables tables, int count, Rng rng, List<string> dead)
        {
            var candidates = new List<string>();
            foreach (var person in tables.Roster.All)
            {
                if (world.ConditionOf(person.Id) == PersonCondition.Healthy) candidates.Add(person.Id);
            }

            var killed = 0;
            for (var i = 0; i < count && candidates.Count > 0; i++)
            {
                var index = rng.Next(candidates.Count);
                var victim = candidates[index];
                candidates.RemoveAt(index);

                Works.Kill(world, victim);
                dead.Add(tables.Roster.Get(victim).Name);
                killed++;
            }

            return killed;
        }

        private static void Describe(World world, Tables tables, WaveReport report)
        {
            report.Lines.Add("К деревне идут: " + Plural(report.Strength));

            if (report.Killed > 0)
                report.Lines.Add("Охотники у стены сбили: " + Plural(report.Killed));

            report.Lines.Add("Частокол: −" + report.PalisadeDamage + " (осталось "
                + world.PalisadeHp + "/" + world.PalisadeMax + ")");

            if (report.Casualties > 0)
                report.Lines.Add("Пробились в деревню. Погибли: " + string.Join(", ", report.Dead));
            else if (report.Breached)
                report.Lines.Add("Частокол пробит, но твари ушли");
            else
                report.Lines.Add("Отбились");
        }

        private static string Plural(int count)
        {
            if (count == 1) return "одна тварь";
            if (count >= 2 && count <= 4) return count + " твари";
            return count + " тварей";
        }

        private static void Schedule(World world, Tuning tuning)
        {
            // Ночь уже переставила счётчик на утро: волна была в уходящем дне.
            world.NextWaveDay = (world.Day - 1) + tuning.GetInt("wave_interval_days");
        }
    }
}
