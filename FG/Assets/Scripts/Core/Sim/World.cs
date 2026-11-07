using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class NestState
    {
        public bool Exists;
        public int Beasts;
        public int DaysSinceSpawn;
    }

    /// <summary>Слух: что люди принесли из леса, с днём.</summary>
    public sealed class Sighting
    {
        public int Day;
        public string Text;
    }

    /// <summary>Состояние одной зоны леса на текущий день.</summary>
    public sealed class ZoneState
    {
        public string ZoneId;
        public Dictionary<string, int> Beasts = new Dictionary<string, int>(StringComparer.Ordinal);
        public int Crystals;
        public int Forage;
        public int NestXp;
        public NestState Nest = new NestState();
        public bool Scouted;
        public int AlertDays;
        public readonly List<Sighting> Sightings = new List<Sighting>();

        public void AddSighting(int day, string text)
        {
            Sightings.Add(new Sighting { Day = day, Text = text });
            if (Sightings.Count > 6) Sightings.RemoveAt(0);
        }
    }

    /// <summary>Мир: только данные и правила, без UnityEngine (ADR-0002).</summary>
    public sealed class World
    {
        public int Day = 1;
        public int PalisadeHp;
        public int PalisadeMax;

        public readonly Dictionary<string, ZoneState> Zones =
            new Dictionary<string, ZoneState>(StringComparer.Ordinal);

        /// <summary>Состояние жителей: ранен, на постели, мёртв.</summary>
        public readonly Dictionary<string, PersonState> People =
            new Dictionary<string, PersonState>(StringComparer.Ordinal);

        /// <summary>Склад деревни: itemId → количество. `forage` — подножный корм.</summary>
        public readonly Dictionary<string, int> Store =
            new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Отряды-пресеты: два, живут между днями (ui.md §4.2).</summary>
        public readonly List<Squad> Squads = new List<Squad>();

        public static World Create(Tables tables)
        {
            var w = new World();
            w.PalisadeMax = tables.Tuning.GetInt("palisade_hp");
            w.PalisadeHp = w.PalisadeMax;

            foreach (var person in tables.Roster.All)
                w.People[person.Id] = new PersonState();

            w.Squads.Add(new Squad("A", "А"));
            w.Squads.Add(new Squad("B", "Б"));

            foreach (var z in tables.Zones.All)
                w.Zones[z.Id] = new ZoneState { ZoneId = z.Id, Forage = z.ForagePerDay };

            foreach (var zb in tables.ZoneBeasts.All)
            {
                if (zb.InitialCount <= 0) continue;
                if (!w.Zones.ContainsKey(zb.ZoneId)) continue;
                w.Zones[zb.ZoneId].Beasts[zb.BeastId] = zb.InitialCount;
            }

            return w;
        }

        public PersonCondition ConditionOf(string personId)
        {
            return People.TryGetValue(personId, out var state) ? state.Condition : PersonCondition.Dead;
        }

        public Squad SquadOf(string personId)
        {
            foreach (var squad in Squads)
            {
                if (squad.Members.Contains(personId)) return squad;
            }

            return null;
        }

        public int TotalCrystals()
        {
            var sum = 0;
            foreach (var z in Zones.Values) sum += z.Crystals;
            return sum;
        }

        public int BeastCount(string zoneId)
        {
            var sum = 0;
            foreach (var kv in Zones[zoneId].Beasts) sum += kv.Value;
            return sum;
        }

        public int TotalBeasts()
        {
            var sum = 0;
            foreach (var z in Zones.Values) sum += BeastCount(z.ZoneId);
            return sum;
        }
    }
}
