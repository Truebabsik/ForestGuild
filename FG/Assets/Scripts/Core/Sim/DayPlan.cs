using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public enum Behavior
    {
        Gather,
        Avoid,
        Clean,
    }

    /// <summary>Один отряд, отправленный в зону на день.</summary>
    public sealed class PartyAssignment
    {
        public string ZoneId;

        /// <summary>Имя отряда-пресета («А», «Б») — для логов и отчётов.</summary>
        public string Name;

        public readonly List<string> PersonIds = new List<string>();
        public Behavior Behavior = Behavior.Gather;

        /// <summary>Наказы отряда: политика решений вместо кликов (ui.md §5.5).</summary>
        public StandingOrders Orders;
    }

    /// <summary>План дня: отряды в лес и работы в деревне (ui.md §4.1).</summary>
    public sealed class DayPlan
    {
        public const int MaxParties = 2;

        public readonly List<PartyAssignment> Parties = new List<PartyAssignment>();

        /// <summary>Работы в деревне: три стока рабочей силы.</summary>
        public readonly List<string> VillageWorkers = new List<string>(); // починка частокола
        public readonly List<string> Beds = new List<string>();           // постели
        public readonly List<string> Foragers = new List<string>();       // заготовка

        public bool IsBusy(string personId)
        {
            foreach (var party in Parties)
                foreach (var id in party.PersonIds)
                    if (id == personId) return true;

            return VillageWorkers.Contains(personId) || Beds.Contains(personId) || Foragers.Contains(personId);
        }

        public int PartyOf(string personId)
        {
            for (var i = 0; i < Parties.Count; i++)
                if (Parties[i].PersonIds.Contains(personId)) return i;

            return -1;
        }

        public int WorkingCount => Parties.Count + VillageWorkers.Count + Beds.Count + Foragers.Count;
    }

    /// <summary>Навыки жителя: override из ростера важнее пресета класса.</summary>
    public static class People
    {
        public static int SkillLevel(Tables tables, Person person, string skillId)
        {
            if (person.SkillsOverride != null && person.SkillsOverride.TryGetValue(skillId, out var overridden))
                return overridden;

            var profession = tables.Professions.Get(person.Profession);
            if (profession.StartSkills != null && profession.StartSkills.TryGetValue(skillId, out var start))
                return start;

            return 0;
        }

        public static bool CanGrow(Tables tables, Person person, string skillId)
        {
            return Array.IndexOf(tables.Professions.Get(person.Profession).SkillsAllowed, skillId) >= 0;
        }

        public static int TraitSum(Tables tables, Person person, Func<Trait, int> pick)
        {
            var sum = 0;
            foreach (var traitId in person.Traits)
            {
                if (tables.Traits.TryGet(traitId, out var trait)) sum += pick(trait);
            }
            return sum;
        }

        public static double TraitSumDouble(Tables tables, Person person, Func<Trait, double> pick)
        {
            var sum = 0.0;
            foreach (var traitId in person.Traits)
            {
                if (tables.Traits.TryGet(traitId, out var trait)) sum += pick(trait);
            }
            return sum;
        }
    }
}
