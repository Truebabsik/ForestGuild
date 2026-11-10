using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    /// <summary>Боец: человек с оружием, бронёй и закалкой (mechanics.md §4–6).</summary>
    public sealed class BattleFighter
    {
        public string PersonId, Name;
        public bool Hunter;
        public int Level, Hp, HpMax, WeaponMin, WeaponMax, Armor, Hardening;
        public double HitBonus, DamageBonus;
        public int ArmorPierce;

        public bool Alive => Hp > 0;
    }

    /// <summary>Тварь в бою: одна или стая (стая — сумма HP, бьёт самая быстрая).</summary>
    public sealed class BattleBeast
    {
        public string Id, Name;
        public int Level, Hp, HpMax, AttackMin, AttackMax, Armor;
        public float Speed;
    }

    public sealed class BattleResult
    {
        public bool Won;
        public int Rounds, Minutes;
        public BattleBeast Beast;
        public readonly List<string> Log = new List<string>();
        public readonly List<string> Downed = new List<string>();
        public readonly List<BattleFighter> Fighters = new List<BattleFighter>();
    }

    /// <summary>
    /// Короткий бой по контракту `docs/handoff/combat/mechanics.md`:
    /// раунды, раздельные броски «попал/ранил», броня-вычитание, закалка-доля,
    /// засада через обнаружение, стая бьёт одним ударом. Детерминирован.
    /// </summary>
    public static class Battle
    {
        public static BattleResult Resolve(Tables tables, World world,
            IReadOnlyList<string> party, IReadOnlyList<string> beastIds, Rng rng)
        {
            var tuning = tables.CombatTuning;
            var result = new BattleResult();

            foreach (var personId in party)
                result.Fighters.Add(MakeFighter(tables, world, personId));

            result.Beast = MakePack(tables, beastIds);

            var partyAlert = 0.0;
            foreach (var fighter in result.Fighters) partyAlert += fighter.Hunter ? 2 : 1;

            partyAlert = result.Fighters.Count > 0 ? partyAlert / result.Fighters.Count : 1;
            var detectBonus = DetectBonus(tables, party);

            var partyDetect = tuning.GetFloat("detect_base")
                + tuning.GetFloat("detect_per_alert") * (partyAlert - tuning.GetFloat("detect_ref_alert"))
                + detectBonus;

            var beastDetect = tuning.GetFloat("detect_base");

            var ambusher = "";
            if (rng.Chance(partyDetect)) ambusher = "party";
            else if (rng.Chance(beastDetect)) ambusher = "beast";

            if (ambusher == "party")
            {
                result.Log.Add("Отряд застал " + result.Beast.Name + " первым.");
                foreach (var fighter in result.Fighters)
                {
                    if (!fighter.Hunter) continue;
                    StrikeBeast(tables, fighter, result.Beast, ignoreArmor: true, rng);
                    if (result.Beast.Hp <= 0) break;
                }
            }
            else if (ambusher == "beast")
            {
                result.Log.Add(result.Beast.Name + " застаёт отряд первым.");
                StrikeParty(tables, result.Fighters, result.Beast, rng, ignoreArmor: true);
            }

            var maxRounds = 200; // предохранитель от вечного цикла, не игровое правило
            while (result.Beast.Hp > 0 && AnyAlive(result.Fighters) && result.Rounds < maxRounds)
            {
                result.Rounds++;

                foreach (var fighter in result.Fighters)
                {
                    if (!fighter.Alive || !fighter.Hunter) continue;
                    StrikeBeast(tables, fighter, result.Beast, ignoreArmor: false, rng);
                    if (result.Beast.Hp <= 0) break;
                }

                if (result.Beast.Hp <= 0) break;

                StrikeParty(tables, result.Fighters, result.Beast, rng, ignoreArmor: false);
            }

            result.Won = result.Beast.Hp <= 0;
            result.Minutes = Math.Max(1, result.Rounds) * tuning.GetInt("minutes_per_round");

            foreach (var fighter in result.Fighters)
            {
                if (!fighter.Alive) result.Downed.Add(fighter.PersonId);
            }

            result.Log.Add(result.Won
                ? result.Beast.Name + " падает."
                : "Отряд смят: " + (result.Downed.Count > 0 ? "кто-то не поднимется" : "пришлось отойти") + ".");

            return result;
        }

        /// Скрытая оценка победы для слова риска: тот же `Resolve` на нескольких прогонах.
        public static double Estimate(Tables tables, World world,
            IReadOnlyList<string> party, IReadOnlyList<string> beastIds, int samples = 40)
        {
            var wins = 0;
            for (var seed = 0; seed < samples; seed++)
            {
                var copy = CopyWorld(world);
                if (Resolve(tables, copy, party, beastIds, new Rng(seed * 7919 + 13)).Won) wins++;
            }

            return (double)wins / samples;
        }

        private static World CopyWorld(World world)
        {
            // Для оценки нужны только HP людей, поэтому копируем состояния и зоны-заглушки.
            var copy = new World { Day = world.Day };
            foreach (var pair in world.People)
            {
                copy.People[pair.Key] = new PersonState
                {
                    Condition = pair.Value.Condition,
                    Hp = pair.Value.Hp,
                    HpMax = pair.Value.HpMax,
                    Hardening = pair.Value.Hardening,
                };
            }

            return copy;
        }

        private static BattleFighter MakeFighter(Tables tables, World world, string personId)
        {
            var person = tables.Roster.Get(personId);
            var state = world.People.TryGetValue(personId, out var s) ? s : new PersonState();

            var fighter = new BattleFighter
            {
                PersonId = personId,
                Name = person.Name,
                Level = People.SkillLevel(tables, person, "hunting"),
                Hp = Math.Max(1, state.Hp),
                HpMax = Math.Max(1, state.HpMax),
                Hardening = state.Hardening,
            };

            fighter.Hunter = fighter.Level > 0;
            fighter.WeaponMin = 3;
            fighter.WeaponMax = 9;
            fighter.Armor = 0;

            if (tables.RosterCombat.TryGet(personId, out var gear))
            {
                if (tables.Equipment.TryGet(gear.WeaponId, out var weapon))
                {
                    fighter.WeaponMin = weapon.DamageMin;
                    fighter.WeaponMax = weapon.DamageMax;
                }

                if (tables.Equipment.TryGet(gear.ArmorId, out var armor)) fighter.Armor = armor.Armor;
            }

            foreach (var take in tables.RosterSkills.All)
            {
                if (take.PersonId != personId) continue;

                var skill = tables.SkillsCombat.Get(take.SkillId);
                switch (skill.Effect)
                {
                    case "damage_bonus": fighter.DamageBonus += skill.Value; break;
                    case "hit_bonus": fighter.HitBonus += skill.Value; break;
                    case "hp_bonus":
                        fighter.HpMax += (int)skill.Value;
                        fighter.Hp += (int)skill.Value;
                        break;
                    case "armor_pierce": fighter.ArmorPierce += (int)skill.Value; break;
                }
            }

            if (fighter.Hp > fighter.HpMax) fighter.Hp = fighter.HpMax;
            return fighter;
        }

        private static BattleBeast MakePack(Tables tables, IReadOnlyList<string> beastIds)
        {
            var primary = tables.BeastCombat.Get(beastIds[0]);
            var beast = new BattleBeast
            {
                Id = primary.BeastId,
                Level = primary.Level,
                AttackMin = primary.AttackMin,
                AttackMax = primary.AttackMax,
                Armor = primary.Armor,
            };

            foreach (var id in beastIds)
            {
                var member = tables.BeastCombat.Get(id);
                beast.HpMax += member.Hp;
                if (member.Speed > beast.Speed)
                {
                    beast.Speed = member.Speed;
                    beast.Id = member.BeastId;
                    beast.AttackMin = member.AttackMin;
                    beast.AttackMax = member.AttackMax;
                    beast.Armor = member.Armor;
                }
            }

            beast.Hp = beast.HpMax;
            beast.Name = tables.Beasts.Get(beast.Id).Name;
            if (beastIds.Count > 1) beast.Name += " (стая из " + beastIds.Count + ")";
            return beast;
        }

        private static double DetectBonus(Tables tables, IReadOnlyList<string> party)
        {
            var bonus = 0.0;
            foreach (var personId in party)
            {
                foreach (var take in tables.RosterSkills.All)
                {
                    if (take.PersonId != personId) continue;
                    var skill = tables.SkillsCombat.Get(take.SkillId);
                    if (skill.Effect == "detect_bonus") bonus += skill.Value;
                }
            }

            return bonus;
        }

        private static bool AnyAlive(List<BattleFighter> fighters)
        {
            foreach (var fighter in fighters) if (fighter.Alive) return true;
            return false;
        }

        private static void StrikeBeast(Tables tables, BattleFighter fighter, BattleBeast beast,
            bool ignoreArmor, Rng rng)
        {
            if (!fighter.Alive || !fighter.Hunter) return;

            var tuning = tables.CombatTuning;
            var hitChance = Math.Min(0.95,
                tuning.GetFloat("hit_base")
                + tuning.GetFloat("hit_per_level") * (fighter.Level - 1)
                + fighter.HitBonus);

            if (!rng.Chance(hitChance)) return;

            var raw = rng.Range(fighter.WeaponMin, fighter.WeaponMax)
                + tuning.GetInt("wound_per_level") * (fighter.Level - 1)
                + (int)fighter.DamageBonus;

            var armor = ignoreArmor ? 0 : Math.Max(0, beast.Armor - fighter.ArmorPierce);
            var damage = Math.Max(tuning.GetInt("min_damage"), raw - armor);
            damage = ApplyWoundPenalty(tables, fighter, damage);

            beast.Hp -= damage;
        }

        private static void StrikeParty(Tables tables, List<BattleFighter> fighters, BattleBeast beast,
            Rng rng, bool ignoreArmor)
        {
            var alive = new List<BattleFighter>();
            foreach (var fighter in fighters) if (fighter.Alive) alive.Add(fighter);
            if (alive.Count == 0) return;

            var target = alive[rng.Next(alive.Count)];
            var tuning = tables.CombatTuning;

            // Сборщик не бьётся: он пытается уйти с линии удара.
            if (!target.Hunter && rng.Chance(tuning.GetFloat("gatherer_dodge"))) return;

            var raw = rng.Range(beast.AttackMin, beast.AttackMax);
            var armor = ignoreArmor ? 0 : target.Armor;
            var damage = Math.Max(tuning.GetInt("min_damage"), raw - armor);
            var share = 1.0 - tuning.GetFloat("hardening_armor") * target.Hardening;
            damage = Math.Max(tuning.GetInt("min_damage"), (int)Math.Round(damage * share));

            target.Hp -= damage;
        }

        private static int ApplyWoundPenalty(Tables tables, BattleFighter fighter, int damage)
        {
            var tuning = tables.CombatTuning;
            var share = (double)fighter.Hp / fighter.HpMax;

            if (share < tuning.GetFloat("hurt_threshold"))
                return Math.Max(tuning.GetInt("min_damage"), (int)(damage * tuning.GetFloat("hurt_damage_factor")));

            if (share < tuning.GetFloat("wound_threshold"))
                return Math.Max(tuning.GetInt("min_damage"), (int)(damage * tuning.GetFloat("wound_damage_factor")));

            return damage;
        }
    }
}
