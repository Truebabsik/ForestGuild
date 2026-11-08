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

        /// <summary>День следующей волны (design.md §10).</summary>
        public int NextWaveDay;

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
            w.NextWaveDay = tables.Tuning.GetInt("wave_day");

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

        /// <summary>Правило состава: живой, свободен, в отряде есть место.</summary>
        public bool TryAddMember(Tables tables, Squad squad, string personId)
        {
            if (squad == null) return false;
            if (squad.Members.Contains(personId)) return true;
            if (squad.Members.Count >= tables.Tuning.GetInt("party_max")) return false;
            if (ConditionOf(personId) != PersonCondition.Healthy) return false;

            var other = SquadOf(personId);
            if (other != null) return false;

            squad.Members.Add(personId);
            return true;
        }

        /// <summary>Забрать человека из второго отряда, если в этом есть место.</summary>
        public bool TryTakeFromOtherSquad(Tables tables, Squad target, string personId)
        {
            if (target == null) return false;
            if (target.Members.Count >= tables.Tuning.GetInt("party_max")) return false;
            if (ConditionOf(personId) != PersonCondition.Healthy) return false;

            var other = SquadOf(personId);
            if (other == null || ReferenceEquals(other, target)) return false;

            other.Members.Remove(personId);
            target.Members.Add(personId);
            return true;
        }

        /// <summary>Утром из отрядов выпадают раненые и погибшие.</summary>
        public void RemoveUnavailableFromSquads()
        {
            foreach (var squad in Squads)
            {
                squad.Members.RemoveAll(id => ConditionOf(id) != PersonCondition.Healthy);
            }
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
