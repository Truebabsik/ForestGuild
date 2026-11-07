using System;
using System.Collections.Generic;
using System.Linq;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class NightReport
    {
        public readonly List<string> Lines = new List<string>();

        public void Add(string line)
        {
            Lines.Add(line);
        }
    }

    /// <summary>Что произошло в зоне за ночь — для отчёта и для миграции.</summary>
    public sealed class ZoneNightLog
    {
        public string ZoneId;
        public int ForageEaten;
        public int CrystalsEaten;
        public int Starved;
    }

    /// <summary>
    /// Ночь леса: приход еды, кормление голодных, миграция наружу, гнездо из опыта, плод гнезда.
    /// Порядок шагов фиксирован — от него зависят тесты и баланс.
    /// </summary>
    public static class NightSim
    {
        public static NightReport AdvanceNight(World world, Tables tables, Rng rng)
        {
            var report = new NightReport();
            var tuning = tables.Tuning;

            AddDailyFood(world, tables);

            var logs = Feed(world, tuning);
            ReportFeeding(world, tables, logs, report);

            var hungry = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var log in logs)
                if (log.Starved > 0) hungry[log.ZoneId] = log.Starved;

            Migrate(world, tables, hungry, tuning, report);
            GrowNests(world, tables, tuning, report);
            AgeCrystals(world, tables, tuning);

            report.Add("В лесу тварей: " + world.TotalBeasts() + ", кристаллов на земле: " + world.TotalCrystals());

            world.Day++;
            report.Add("Наступил день " + world.Day);
            return report;
        }

        private static void AddDailyFood(World world, Tables tables)
        {
            foreach (var state in world.Zones.Values)
            {
                var zone = tables.Zones.Get(state.ZoneId);
                state.Forage = zone.ForagePerDay;
                state.Crystals += zone.CrystalsPerDay;
            }
        }

        /// Кормление: корм → труп → кристалл.
        private static List<ZoneNightLog> Feed(World world, Tuning tuning)
        {
            var logs = new List<ZoneNightLog>();

            foreach (var state in world.Zones.Values)
            {
                var log = new ZoneNightLog { ZoneId = state.ZoneId };

                foreach (var beastId in state.Beasts.Keys.ToList())
                {
                    var count = state.Beasts[beastId];
                    for (var i = 0; i < count; i++)
                    {
                        if (state.Forage > 0)
                        {
                            state.Forage--;
                            log.ForageEaten++;
                            continue;
                        }

                        if (state.Crystals > 0)
                        {
                            state.Crystals--;
                            state.NestXp++;
                            log.CrystalsEaten++;
                            continue;
                        }

                        log.Starved++;
                    }
                }

                if (world.BeastCount(state.ZoneId) > 0) logs.Add(log);
            }

            return logs;
        }

        private static void ReportFeeding(World world, Tables tables, List<ZoneNightLog> logs, NightReport report)
        {
            foreach (var log in logs)
            {
                var beasts = world.BeastCount(log.ZoneId);
                var text = tables.Zones.Get(log.ZoneId).Name + ": " + beasts + " твар.";
                if (log.ForageEaten > 0) text += ", съедено корма " + log.ForageEaten;
                if (log.CrystalsEaten > 0) text += ", кристаллов " + log.CrystalsEaten;
                if (log.Starved > 0) text += ", голодных " + log.Starved;
                report.Add(text);
            }
        }

        private static void Migrate(World world, Tables tables,
            Dictionary<string, int> hungry, Tuning tuning, NightReport report)
        {
            if (hungry.Count == 0) return;

            var limit = tuning.GetInt("migration_per_night");
            var distances = DistancesToVillage(tables, tuning.Get("village_zone"));

            foreach (var pair in hungry)
            {
                var outward = OutwardNeighbour(tables, distances, pair.Key);
                if (outward == null) continue;

                var move = Math.Min(Math.Min(pair.Value, limit), world.BeastCount(pair.Key));
                for (var i = 0; i < move; i++) MoveOneBeast(world, pair.Key, outward);

                if (move > 0)
                {
                    report.Add(tables.Zones.Get(pair.Key).Name + ": " + move
                        + " твар. уходит в " + tables.Zones.Get(outward).Name);
                }
            }
        }

        private static void GrowNests(World world, Tables tables, Tuning tuning, NightReport report)
        {
            var xpToNest = tuning.GetInt("xp_to_nest");
            foreach (var state in world.Zones.Values)
            {
                if (state.Nest.Exists || state.NestXp < xpToNest) continue;

                state.NestXp -= xpToNest;
                state.Nest.Exists = true;
                state.Nest.Beasts = 0;
                state.Nest.DaysSinceSpawn = 0;
                report.Add(tables.Zones.Get(state.ZoneId).Name + ": появилось гнездо");
            }

            var spawnDays = tuning.GetInt("nest_spawn_days");
            var cap = tuning.GetInt("nest_beasts_cap");

            foreach (var state in world.Zones.Values)
            {
                if (!state.Nest.Exists) continue;

                state.Nest.DaysSinceSpawn++;
                if (state.Nest.DaysSinceSpawn < spawnDays || state.Nest.Beasts >= cap) continue;

                state.Nest.DaysSinceSpawn = 0;
                state.Nest.Beasts++;

                var nestBeast = tables.ZoneBeasts.All
                    .FirstOrDefault(x => x.ZoneId == state.ZoneId && tables.Beasts.Get(x.BeastId).NestCapable);
                if (nestBeast == null) continue;

                state.Beasts.TryGetValue(nestBeast.BeastId, out var current);
                state.Beasts[nestBeast.BeastId] = current + 1;
                report.Add(tables.Zones.Get(state.ZoneId).Name + ": гнездо растит тварь");
            }
        }

        private static void AgeCrystals(World world, Tables tables, Tuning tuning)
        {
            var life = tuning.GetInt("crystal_life_days");
            foreach (var state in world.Zones.Values)
            {
                var cap = tables.Zones.Get(state.ZoneId).CrystalsPerDay * life;
                if (state.Crystals > cap) state.Crystals = cap;
            }
        }

        private static void MoveOneBeast(World world, string from, string to)
        {
            var src = world.Zones[from];
            var beastId = src.Beasts.Where(kv => kv.Value > 0).Select(kv => kv.Key).FirstOrDefault();
            if (beastId == null) return;

            src.Beasts[beastId]--;
            if (src.Beasts[beastId] <= 0) src.Beasts.Remove(beastId);

            var dst = world.Zones[to];
            dst.Beasts.TryGetValue(beastId, out var count);
            dst.Beasts[beastId] = count + 1;
        }

        /// Расстояние по графу зон до зоны-ворот: «наружу» = туда, где ближе к деревне.
        public static Dictionary<string, int> DistancesToVillage(Tables tables, string gateZoneId)
        {
            var distances = new Dictionary<string, int>(StringComparer.Ordinal) { [gateZoneId] = 0 };
            var queue = new Queue<string>();
            queue.Enqueue(gateZoneId);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                foreach (var neighbour in tables.Zones.Get(id).Neighbours)
                {
                    if (distances.ContainsKey(neighbour)) continue;
                    distances[neighbour] = distances[id] + 1;
                    queue.Enqueue(neighbour);
                }
            }

            return distances;
        }

        private static string OutwardNeighbour(Tables tables, Dictionary<string, int> distances, string zoneId)
        {
            if (!distances.TryGetValue(zoneId, out var own)) return null;

            string best = null;
            var bestDistance = own;

            foreach (var neighbour in tables.Zones.Get(zoneId).Neighbours)
            {
                if (!distances.TryGetValue(neighbour, out var distance)) continue;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = neighbour;
            }

            return best;
        }
    }
}
