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

    /// <summary>Состояние жителя между днями: рана, постель, смерть.</summary>
    public sealed class PersonState
    {
        public PersonCondition Condition = PersonCondition.Healthy;
        public int InBedDays;
    }

    public enum EncounterOrder { Ask, Hide, Retreat, Fight }
    public enum CarcassOrder { Ask, Haul, Butcher, Leave }
    public enum WoundedOrder { Ask, Carry, Leave }
    public enum BagOrder { Ask, ReplaceCheapest, Skip }

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

        public Squad(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public Squad Clone()
        {
            var clone = new Squad(Id, Name) { ZoneId = ZoneId, Behavior = Behavior };
            clone.Members.AddRange(Members);
            clone.Orders.Encounter = Orders.Encounter;
            clone.Orders.Carcass = Orders.Carcass;
            clone.Orders.Wounded = Orders.Wounded;
            clone.Orders.BagFull = Orders.BagFull;
            return clone;
        }
    }
}
