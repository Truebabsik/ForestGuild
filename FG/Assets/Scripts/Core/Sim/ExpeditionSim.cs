using System;
using System.Collections.Generic;
using System.Linq;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class ExpeditionResult
    {
        public string ZoneId;
        public readonly List<KeyValuePair<string, int>> Found = new List<KeyValuePair<string, int>>();
        public readonly List<string> Log = new List<string>();
        public int HoursUsed;
        public int Stamina;
        public bool Encountered;
        public bool Wounded;
        public string WoundedPersonId;
    }

    /// <summary>
    /// Авторезолв вылазки по часам: находки, встречи, мешок.
    /// Шаг 1 — без пауз и без туш; это приедет в шаге 2.
    /// </summary>
    public static class ExpeditionSim
    {
        public static ExpeditionResult Resolve(World world, Tables tables, PartyAssignment party, Rng rng)
        {
            var tuning = tables.Tuning;
            var zone = tables.Zones.Get(party.ZoneId);
            var state = world.Zones[party.ZoneId];
            state.Scouted = true;

            var result = new ExpeditionResult
            {
                ZoneId = party.ZoneId,
                Stamina = party.PersonIds.Count * tuning.GetInt("stamina_per_person"),
            };

            var budget = tuning.GetInt("hours_per_day");
            var travel = (int)Math.Ceiling(zone.TravelHours * 2);
            var workHours = Math.Max(0, budget - travel);
            if (workHours <= 0)
            {
                result.Log.Add(zone.Name + ": дорога съедает весь день, отряд не дошёл");
                return result;
            }

            var freeSlots = 0f;
            foreach (var personId in party.PersonIds)
            {
                var person = tables.Roster.Get(personId);
                freeSlots += tuning.GetInt("slots_per_person")
                    + People.TraitSum(tables, person, tr => tr.CarryBonus);
            }

            var density = Density(world, state);
            var intensity = Intensity(party, state);

            for (var hour = 1; hour <= workHours; hour++)
            {
                result.HoursUsed = hour;
                if (result.Stamina <= 0)
                {
                    result.Log.Add(zone.Name + ": отряд выбился из сил");
                    break;
                }

                result.Stamina--;

                foreach (var personId in party.PersonIds)
                {
                    var person = tables.Roster.Get(personId);
                    var skill = People.SkillLevel(tables, person, "gathering");
                    var chance = tuning.GetFloat("find_base")
                        + tuning.GetFloat("find_per_skill") * skill
                        + People.TraitSumDouble(tables, person, tr => tr.FindBonus);

                    if (!rng.Chance(chance)) continue;

                    var itemId = PickResource(tables, zone.Id, rng);
                    if (itemId == null) continue;

                    var item = tables.Items.Get(itemId);
                    if (freeSlots < item.Slots)
                    {
                        result.Log.Add(person.Name + ": мешок полон, брошено " + item.Name);
                        continue;
                    }

                    freeSlots -= item.Slots;
                    AddFound(result, itemId);
                }

                var encounterChance = tuning.GetFloat("encounter_per_hour") * density * intensity;
                if (!rng.Chance(encounterChance)) continue;

                result.Encountered = true;
                result.Stamina -= 1;

                var beastName = PickBeast(tables, zone.Id, rng);

                if (rng.Chance(0.5))
                {
                    result.Log.Add(zone.Name + ": тварь рядом — отряд отошёл");
                    state.AddSighting(world.Day, zone.Name + ": отряд столкнулся с " + beastName + " — отошли");
                    continue;
                }

                if (rng.Chance(0.5))
                {
                    result.Log.Add(zone.Name + ": бой — победа, туша брошена");
                    state.AddSighting(world.Day, zone.Name + ": " + beastName + " — приняли бой и ушли с добычей");
                    continue;
                }

                result.Wounded = true;
                var victim = party.PersonIds[rng.Next(party.PersonIds.Count)];
                result.WoundedPersonId = victim;
                Works.Wound(world, victim);
                result.Log.Add(zone.Name + ": бой — ранение, отряд возвращается");
                state.AddSighting(world.Day, zone.Name + ": " + beastName + " — ранили человека");
                break;
            }

            if (result.Found.Count > 0)
            {
                result.Log.Add(zone.Name + ": добыто " + result.Found.Count + " вид(ов)");
            }

            return result;
        }

        private static double Density(World world, ZoneState state)
        {
            var food = Math.Max(1, state.Forage + state.Crystals);
            return (double)world.BeastCount(state.ZoneId) / food;
        }

        private static double Intensity(PartyAssignment party, ZoneState state)
        {
            var intensity = party.Behavior == Behavior.Avoid ? 0.5
                : party.Behavior == Behavior.Clean ? 2.5
                : 1.0;

            if (party.PersonIds.Count > 2) intensity *= 1.2;
            if (state.AlertDays > 0) intensity *= 1.2;
            return intensity;
        }

        private static string PickBeast(Tables tables, string zoneId, Rng rng)
        {
            var pool = tables.ZoneBeasts.All.Where(x => x.ZoneId == zoneId).ToList();
            if (pool.Count == 0) return "тварь";

            var total = 0;
            foreach (var entry in pool) total += entry.Weight;
            if (total <= 0) return "тварь";

            var roll = rng.Next(total);
            foreach (var entry in pool)
            {
                roll -= entry.Weight;
                if (roll < 0) return tables.Beasts.Get(entry.BeastId).Name;
            }

            return tables.Beasts.Get(pool[pool.Count - 1].BeastId).Name;
        }

        private static string PickResource(Tables tables, string zoneId, Rng rng)
        {
            var pool = tables.ZoneResources.All.Where(x => x.ZoneId == zoneId).ToList();
            if (pool.Count == 0) return null;

            var total = 0;
            foreach (var resource in pool) total += resource.Abundance;
            if (total <= 0) return null;

            var roll = rng.Next(total);
            foreach (var resource in pool)
            {
                roll -= resource.Abundance;
                if (roll < 0) return resource.ItemId;
            }

            return pool[pool.Count - 1].ItemId;
        }

        private static void AddFound(ExpeditionResult result, string itemId)
        {
            for (var i = 0; i < result.Found.Count; i++)
            {
                if (result.Found[i].Key != itemId) continue;
                result.Found[i] = new KeyValuePair<string, int>(itemId, result.Found[i].Value + 1);
                return;
            }

            result.Found.Add(new KeyValuePair<string, int>(itemId, 1));
        }
    }
}
