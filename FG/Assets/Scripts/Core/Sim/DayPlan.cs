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
        public readonly List<string> PersonIds = new List<string>();
        public Behavior Behavior = Behavior.Gather;
    }

    /// <summary>План дня: кто куда идёт и кто остаётся в деревне.</summary>
    public sealed class DayPlan
    {
        public readonly List<PartyAssignment> Parties = new List<PartyAssignment>();
        public readonly List<string> VillageWorkers = new List<string>();
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
