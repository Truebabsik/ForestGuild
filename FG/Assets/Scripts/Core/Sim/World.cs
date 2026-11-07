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
    }

    /// <summary>Мир: только данные и правила, без UnityEngine (ADR-0002).</summary>
    public sealed class World
    {
        public int Day = 1;
        public int PalisadeHp;
        public int PalisadeMax;

        public readonly Dictionary<string, ZoneState> Zones =
            new Dictionary<string, ZoneState>(StringComparer.Ordinal);

        public static World Create(Tables tables)
        {
            var w = new World();
            w.PalisadeMax = tables.Tuning.GetInt("palisade_hp");
            w.PalisadeHp = w.PalisadeMax;

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
