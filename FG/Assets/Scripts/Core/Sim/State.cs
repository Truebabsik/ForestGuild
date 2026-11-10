using System;
using System.Collections.Generic;

namespace ForestGuild.Core.Sim
{
    public enum PersonCondition
    {
        Healthy,
        Wounded,
        Dead,
    }

    /// <summary>Состояние жителя между днями: рана, постель, смерть, здоровье.</summary>
    public sealed class PersonState
    {
        public PersonCondition Condition = PersonCondition.Healthy;
        public int InBedDays;

        /// <summary>Здоровье накопительное: переживает день и вылазки (ADR-0043).</summary>
        public int Hp;
        public int HpMax;
        public int Hardening;
    }

    public enum EncounterOrder { Ask, Hide, Retreat, Fight }
    public enum CarcassOrder { Ask, Haul, Butcher, Leave }
    public enum WoundedOrder { Ask, Carry, Leave }
    public enum BagOrder { Ask, ReplaceCheapest, Skip }

    /// <summary>Роль охотника в отряде (ui.md §4.3).</summary>
    public enum HunterRole { Guard, Gather, Track }

    /// <summary>Наказы на развилки (ui.md §5.5): политика решений вместо кликов.</summary>
    public sealed class StandingOrders
    {
        public EncounterOrder Encounter = EncounterOrder.Ask;
        public CarcassOrder Carcass = CarcassOrder.Ask;
        public WoundedOrder Wounded = WoundedOrder.Ask;
        public BagOrder BagFull = BagOrder.Ask;

        public bool IsAsk =>
            Encounter == EncounterOrder.Ask
            && Carcass == CarcassOrder.Ask
            && Wounded == WoundedOrder.Ask
            && BagFull == BagOrder.Ask;
    }

    /// <summary>Отряд-пресет: живёт между днями, состав и наказы сохраняются.</summary>
    public sealed class Squad
    {
        public string Id;
        public string Name;
        public readonly List<string> Members = new List<string>();
        public string ZoneId;
        public Behavior Behavior = Behavior.Gather;
        public readonly StandingOrders Orders = new StandingOrders();

        /// <summary>Роль охотника в отряде; по умолчанию «охраняю».</summary>
        public readonly Dictionary<string, HunterRole> Roles =
            new Dictionary<string, HunterRole>(StringComparer.Ordinal);

        public Squad(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public HunterRole RoleOf(string personId)
        {
            return Roles.TryGetValue(personId, out var role) ? role : HunterRole.Guard;
        }

        public void SetRole(string personId, HunterRole role)
        {
            Roles[personId] = role;
        }
    }
}
