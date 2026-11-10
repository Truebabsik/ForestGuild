using System.Collections.Generic;

namespace ForestGuild.Core.Data
{
    /// <summary>Строка zones.csv.</summary>
    public sealed class Zone
    {
        public string Id, Name, Depth, EnvRule;
        public int TravelMinutes;
        public float DistanceKm, MapX, MapY;
        public int CrystalsPerDay, ForagePerDay;
        public string[] Neighbours;
    }

    /// <summary>Строка items.csv: ресурс, трофей и предмет — одна сущность.</summary>
    public sealed class Item
    {
        public string Id, Name, Category, Tags;

        /// <summary>Короткое имя для HUD и списков (ui.md §8).</summary>
        public string Short;

        public float Slots;
        public int Value;
    }

    /// <summary>Строка zone_resources.csv.</summary>
    public sealed class ZoneResource
    {
        public string ZoneId, ItemId;
        public int Abundance;
    }

    /// <summary>Строка beasts.csv.</summary>
    public sealed class Beast
    {
        public string Id, Name;

        /// <summary>Префикс арта в `UI/Art/Monsters`: spark / bone / ash.</summary>
        public string Art;

        public int FoodNeed;
        public bool NestCapable;
    }

    /// <summary>Строка zone_beasts.csv.</summary>
    public sealed class ZoneBeast
    {
        public string ZoneId, BeastId;
        public int Weight, InitialCount;
    }

    /// <summary>Строка drops.csv.</summary>
    public sealed class Drop
    {
        public string BeastId, ItemId;
        public double Chance;
        public int MinSkill;
    }

    /// <summary>Строка zone_lore.csv: нарратив досье зоны.</summary>
    public sealed class ZoneLore    {
        public string ZoneId, Field, Text;
    }

    /// <summary>Строка skills.csv.</summary>
    public sealed class Skill
    {
        public string Id, Name, Kind;
        public int MaxLevel;
    }

    /// <summary>Строка traits.csv.</summary>
    public sealed class Trait
    {
        public string Id, Name;
        public int FightBonus, ButcherBonus, CarryBonus;
        public double FindBonus;
    }

    /// <summary>Строка professions.csv: класс, а не пресет.</summary>
    public sealed class Profession
    {
        public string Id, Name, RequiresTrait;
        public string[] SkillsAllowed;
        public Dictionary<string, int> StartSkills;
    }

    /// <summary>Строка roster.csv.</summary>
    public sealed class Person
    {
        public string Id, Name, Profession;
        public string[] Traits;
        public Dictionary<string, int> SkillsOverride;
    }

    /// <summary>Строка stations.csv.</summary>
    public sealed class Station
    {
        public string Id, Name, SkillId;
        public int PeopleSlots;
    }

    /// <summary>Строка recipes.csv.</summary>
    public sealed class Recipe
    {
        public string Id, Station, SkillId;
        public int SkillReq, Hours;
        public Dictionary<string, int> Inputs, Outputs;
    }

    /// <summary>Строка recipe_quality.csv.</summary>
    public sealed class RecipeQuality
    {
        public string RecipeId, OutputId;
        public int MinSkill, Qty;
        public double Chance;
    }

    /// <summary>Строка scenarios.csv: с какими входными данными запускать мир.</summary>
    public sealed class Scenario
    {
        public string Id, Title, StartScreen, OnFinish, Notes;
        public int Seed, Day;
    }

    /// <summary>Строка scenario_parties.csv: кто и куда идёт в этом сценарии.</summary>
    public sealed class ScenarioParty
    {
        public string ScenarioId, Squad, Zone, Behavior;
        public string EncounterOrder = "ask";
        public string Members; // a|b|c
    }

    /// <summary>Строка beast_combat.csv: боевые параметры твари.</summary>
    public sealed class BeastCombat
    {
        public string BeastId;
        public int Level, Hp, AttackMin, AttackMax, Armor;
        public float Speed;
    }

    /// <summary>Строка equipment.csv: оружие или броня.</summary>
    public sealed class Equipment
    {
        public string Id, Name, Slot;
        public int DamageMin, DamageMax, Armor, Value;
    }

    /// <summary>Строка roster_combat.csv: с чем и в какой закалке человек выходит.</summary>
    public sealed class RosterCombat
    {
        public string PersonId, WeaponId, ArmorId;
        public int Hardening;
    }

    /// <summary>Строка roster_skills.csv: какой боевой навык взят человеком.</summary>
    public sealed class RosterSkill
    {
        public string PersonId, SkillId;
    }

    /// <summary>Строка skills_combat.csv: боевой навык уровня 2–5.</summary>
    public sealed class SkillCombat
    {
        public string Id, Name, Branch, Effect;
        public int Level;
        public double Value;
    }
}
