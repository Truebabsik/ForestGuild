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
        public double HitBonus, DamageBonus, DetectBonus, AlertBonus;
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

        /// <summary>Кто застал: "party", "beast" или "" — заметили одновременно (никто не застал).</summary>
        public string Ambusher = "";

        public BattleBeast Beast;
        public readonly List<string> Log = new List<string>();
        public readonly List<string> Downed = new List<string>();
        public readonly List<BattleFighter> Fighters = new List<BattleFighter>();
    }

    /// <summary>
    /// Короткий бой по контракту `docs/handoff/combat/mechanics.md`:
    /// раунды, раздельные броски «попал/ранил», броня-вычитание, закалка-доля,
    /// засада через обнаружение, стая бьёт одним ударом. Детерминирован.
    ///
    /// Порядок хода по умолчанию — люди бьют, тварь отвечает (mechanics.md §6.1).
    /// Первым бьёт только сторона, выигравшая обнаружение (§6.3): обе стороны
    /// бросают независимо, при равенстве бросков засады нет ни у кого.
    /// </summary>
    public static class Battle
    {
        public static BattleResult Resolve(Tables tables, World world,
            IReadOnlyList<string> party, IReadOnlyList<string> beastIds, Rng rng)
        {
            var fighters = new List<BattleFighter>();
            foreach (var personId in party)
                fighters.Add(MakeFighter(tables, world, personId));

            return ResolveFighters(tables, fighters, beastIds, rng);
        }

        /// <summary>Бой готовых бойцов. Нужен проверкам кривой: проба — не человек из ростера.</summary>
        public static BattleResult ResolveFighters(Tables tables,
            IReadOnlyList<BattleFighter> fighters, IReadOnlyList<string> beastIds, Rng rng)
        {
            var tuning = tables.CombatTuning;
            var result = new BattleResult();

            foreach (var fighter in fighters) result.Fighters.Add(fighter);
            result.Beast = MakePack(tables, beastIds);

            DetectChances(tables, fighters, result.Beast,
                out var partyDetect, out var beastDetect);

            // Обе стороны бросают независимо: побеждает тот, кто заметил, а не тот, кто бросил первым.
            var partyRoll = rng.Chance(partyDetect);
            var beastRoll = rng.Chance(beastDetect);
            if (partyRoll && !beastRoll) result.Ambusher = "party";
            else if (beastRoll && !partyRoll) result.Ambusher = "beast";

            var noArmorOnAmbush = tuning.GetInt("ambush_no_armor") != 0;
            var ambushBonus = tuning.GetFloat("ambush_bonus");
            var skipReply = tuning.GetInt("ambush_skip_reply") != 0;
            var allHit = tuning.GetInt("ambush_all_hit") != 0;

            var partySkipsRoundOne = false;
            var beastSkipsRoundOne = false;

            if (result.Ambusher == "party")
            {
                result.Log.Add("Отряд застал " + result.Beast.Name + " первым.");

                var hit = false;
                foreach (var fighter in result.Fighters)
                {
                    if (!fighter.Hunter || !fighter.Alive) continue;
                    if (hit && !allHit) break;

                    if (StrikeBeast(tables, fighter, result.Beast, true, rng)) hit = true;
                    if (result.Beast.Hp <= 0) break;
                }

                if (skipReply) beastSkipsRoundOne = true;
            }
            else if (result.Ambusher == "beast")
            {
                result.Log.Add(result.Beast.Name + " застаёт отряд первым.");
                StrikeParty(tables, result.Fighters, result.Beast, rng, true);

                // Застигнутый отряд теряет свой ход в первом раунде (§6.3, `ambush_skip_reply`).
                if (skipReply)
                {
                    partySkipsRoundOne = true;
                    beastSkipsRoundOne = true;
                }
            }

            var maxRounds = 200; // предохранитель от вечного цикла, не игровое правило
            while (result.Beast.Hp > 0 && AnyAlive(result.Fighters) && result.Rounds < maxRounds)
            {
                result.Rounds++;

                if (!(result.Rounds == 1 && partySkipsRoundOne))
                {
                    foreach (var fighter in result.Fighters)
                    {
                        if (!fighter.Alive || !fighter.Hunter) continue;
                        StrikeBeast(tables, fighter, result.Beast, false, rng);
                        if (result.Beast.Hp <= 0) break;
                    }
                }

                if (result.Beast.Hp <= 0) break;
                if (result.Rounds == 1 && beastSkipsRoundOne) continue;

                StrikeParty(tables, result.Fighters, result.Beast, rng, false);
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

        /// <summary>
        /// Шанс каждой стороны заметить другую первой (mechanics.md §6.1).
        /// Люди: база + «чутьё» выше опорного + лучшее из навыков обнаружения.
        /// Тварь: база + её `speed` против того же опорного «чутья».
        /// </summary>
        public static void DetectChances(Tables tables, IReadOnlyList<BattleFighter> fighters,
            BattleBeast beast, out double partyChance, out double beastChance)
        {
            var tuning = tables.CombatTuning;
            var refAlert = tuning.GetFloat("detect_ref_alert");
            var perAlert = tuning.GetFloat("detect_per_alert");

            var alert = 0.0;
            var detectBonus = 0.0;
            var alive = 0;

            foreach (var fighter in fighters)
            {
                if (!fighter.Alive) continue;
                alive++;
                alert += (fighter.Hunter ? 2 : 1) + fighter.AlertBonus;
                if (fighter.DetectBonus > detectBonus) detectBonus = fighter.DetectBonus;
            }

            alert = alive > 0 ? alert / alive : 1;

            partyChance = Clamp(tuning.GetFloat("detect_base") + perAlert * (alert - refAlert)
                + detectBonus);
            beastChance = Clamp(tuning.GetFloat("detect_base") + perAlert * (beast.Speed - refAlert));
        }

        /// <summary>
        /// Проба из калибровки: только уровень, дубина, стёганка, закалка по уровню.
        /// Навыков нет — так меряли кривую в прототипе (`sim/battle_sim.py`).
        /// </summary>
        public static BattleFighter ProbeFighter(Tables tables, int level)
        {
            var tuning = tables.CombatTuning;
            var fighter = new BattleFighter
            {
                PersonId = "probe_" + level,
                Name = "Проба " + level,
                Hunter = true,
                Level = level,
                WeaponMin = 3,
                WeaponMax = 9,
                HpMax = tuning.GetInt("base_hp") + tuning.GetInt("hp_per_level") * level,
                Hardening = HardeningOfLevel(tables, level),
            };

            if (tables.Equipment.TryGet("weapon_club", out var club))
            {
                fighter.WeaponMin = club.DamageMin;
                fighter.WeaponMax = club.DamageMax;
            }

            if (tables.Equipment.TryGet("armor_padded", out var padded)) fighter.Armor = padded.Armor;

            fighter.Hp = fighter.HpMax;
            return fighter;
        }

        private static int HardeningOfLevel(Tables tables, int level)
        {
            var tuning = tables.CombatTuning;
            var from = tuning.GetInt("hardening_from_level");
            if (level < from) return 0;

            return Math.Min(level - from + 1, tuning.GetInt("hardening_max"));
        }

        /// <summary>Скрытая оценка победы для слова риска: тот же `Resolve` на нескольких прогонах.</summary>
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
                    case "detect_bonus":
                        // В прототипе берётся лучший навык отряда, а не сумма по людям.
                        if (skill.Value > fighter.DetectBonus) fighter.DetectBonus = skill.Value;
                        break;
                    case "alert_bonus": fighter.AlertBonus += skill.Value; break;
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

        private static double Clamp(double value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        /// <summary>Точность удара: база + уровень, потолок 0.95 (mechanics.md §6.3).</summary>
        public static double HitChance(Tables tables, BattleFighter fighter)
        {
            var tuning = tables.CombatTuning;
            return Math.Min(0.95,
                tuning.GetFloat("hit_base")
                + tuning.GetFloat("hit_per_level") * (fighter.Level - 1)
                + fighter.HitBonus);
        }

        /// <summary>Штраф за ранение: 1.0 / 0.75 / 0.5 по остатку HP. Считается на момент удара.</summary>
        public static double PenaltyFactor(Tables tables, BattleFighter fighter)
        {
            var tuning = tables.CombatTuning;
            var share = (double)fighter.Hp / fighter.HpMax;

            if (share < tuning.GetFloat("hurt_threshold")) return tuning.GetFloat("hurt_damage_factor");
            if (share < tuning.GetFloat("wound_threshold")) return tuning.GetFloat("wound_damage_factor");
            return 1.0;
        }

        /// <summary>
        /// Урон одного удара человека по твари. `roll` — сырой бросок оружия (`weapon_min..weapon_max`).
        /// `surprise` — удар из засады: мимо брони твари, с надбавкой `ambush_bonus`.
        /// </summary>
        public static int WeaponHit(Tables tables, BattleFighter fighter, BattleBeast beast,
            int roll, bool surprise)
        {
            var tuning = tables.CombatTuning;

            var raw = roll
                + tuning.GetInt("wound_per_level") * (fighter.Level - 1)
                + (int)fighter.DamageBonus;

            var armor = surprise && tuning.GetInt("ambush_no_armor") != 0
                ? 0
                : Math.Max(0, beast.Armor - fighter.ArmorPierce);

            var damage = Math.Max(tuning.GetInt("min_damage"), raw - armor);
            damage = Math.Max(tuning.GetInt("min_damage"),
                (int)(damage * PenaltyFactor(tables, fighter)));

            if (surprise) damage = AmbushDamage(tables, damage);
            return damage;
        }

        /// <summary>
        /// Урон одного удара твари по человеку. `roll` — сырой бросок твари (`attack_min..attack_max`).
        /// `surprise` — удар из засады: мимо брони экипировки и мимо закалки.
        /// </summary>
        public static int BeastHit(Tables tables, BattleFighter target, BattleBeast beast,
            int roll, bool surprise)
        {
            var tuning = tables.CombatTuning;

            if (surprise && tuning.GetInt("ambush_no_armor") != 0) return AmbushDamage(tables, roll);

            var damage = Math.Max(tuning.GetInt("min_damage"), roll - target.Armor);
            var share = 1.0 - tuning.GetFloat("hardening_armor") * target.Hardening;
            damage = Math.Max(tuning.GetInt("min_damage"), (int)Math.Round(damage * share));

            if (surprise) damage = AmbushDamage(tables, damage);
            return damage;
        }

        private static bool AnyAlive(List<BattleFighter> fighters)
        {
            foreach (var fighter in fighters) if (fighter.Alive) return true;
            return false;
        }

        /// <summary>Удар человека по твари. true — попал (для засады «бьёт до первого попадания»).</summary>
        private static bool StrikeBeast(Tables tables, BattleFighter fighter, BattleBeast beast,
            bool surprise, Rng rng)
        {
            if (!fighter.Alive || !fighter.Hunter) return false;
            if (!rng.Chance(HitChance(tables, fighter))) return false;

            var roll = rng.Range(fighter.WeaponMin, fighter.WeaponMax);
            beast.Hp -= WeaponHit(tables, fighter, beast, roll, surprise);
            return true;
        }

        /// <summary>Удар твари по отряду. `surprise` — первый удар из засады: мимо брони и закалки.</summary>
        private static void StrikeParty(Tables tables, List<BattleFighter> fighters, BattleBeast beast,
            Rng rng, bool surprise)
        {
            var alive = new List<BattleFighter>();
            foreach (var fighter in fighters) if (fighter.Alive) alive.Add(fighter);
            if (alive.Count == 0) return;

            var tuning = tables.CombatTuning;
            var targetRandom = tuning.GetInt("target_random") != 0;
            var target = targetRandom ? alive[rng.Next(alive.Count)] : alive[0];

            // Сборщик не бьётся: он пытается уйти с линии удара.
            if (!target.Hunter && rng.Chance(tuning.GetFloat("gatherer_dodge"))) return;

            var roll = rng.Range(beast.AttackMin, beast.AttackMax);
            target.Hp -= BeastHit(tables, target, beast, roll, surprise);
        }

        private static int AmbushDamage(Tables tables, int damage)
        {
            var bonus = tables.CombatTuning.GetFloat("ambush_bonus");
            if (bonus <= 0) return damage;

            return Math.Max(tables.CombatTuning.GetInt("min_damage"),
                (int)Math.Round(damage * (1.0 + bonus)));
        }
    }
}
