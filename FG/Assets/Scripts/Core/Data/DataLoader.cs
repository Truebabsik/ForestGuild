using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ForestGuild.Core.Data
{
    /// <summary>
    /// Загрузка всех CSV с проверкой схемы и внешних ключей.
    /// Любая неизвестная ссылка — DataException с указанием файла и колонки.
    /// </summary>
    public static class DataLoader
    {
        public const int SchemaVersion = 1;

        public static Tables Load(string dir)
        {
            var t = new Tables();

            t.Tuning = ReadTuning(Path.Combine(dir, "tuning.csv"));
            if (t.Tuning.GetInt("schema_version") != SchemaVersion)
            {
                throw new DataException("tuning.csv",
                    "schema_version " + t.Tuning.GetInt("schema_version") + ", ожидалось " + SchemaVersion);
            }

            t.Items = Read(dir, "items.csv", (r, p) => new Item
            {
                Id = Get(r, p, "id"),
                Name = Get(r, p, "name"),
                Category = Get(r, p, "category"),
                Slots = F(r, p, "slots"),
                Value = I(r, p, "value"),
                Tags = Opt(r, "tags"),
                Short = Opt(r, "short"),
            }, x => x.Id);

            t.Zones = Read(dir, "zones.csv", (r, p) => new Zone
            {
                Id = Get(r, p, "id"),
                Name = Get(r, p, "name"),
                Depth = Get(r, p, "depth"),
                TravelMinutes = I(r, p, "travel_minutes"),
                DistanceKm = F(r, p, "distance_km"),
                CrystalsPerDay = I(r, p, "crystals_per_day"),
                ForagePerDay = I(r, p, "forage_per_day"),
                Neighbours = SplitList(Get(r, p, "neighbours")),
                EnvRule = Get(r, p, "env_rule"),
                MapX = F(r, p, "map_x"),
                MapY = F(r, p, "map_y"),
            }, x => x.Id);

            t.Skills = Read(dir, "skills.csv", (r, p) => new Skill
            {
                Id = Get(r, p, "id"),
                Name = Get(r, p, "name"),
                Kind = Get(r, p, "kind"),
                MaxLevel = I(r, p, "max_level"),
            }, x => x.Id);

            t.ZoneLore = Read(dir, "zone_lore.csv", (r, p) => new ZoneLore
            {
                ZoneId = Get(r, p, "zone_id"),
                Field = Get(r, p, "field"),
                Text = Get(r, p, "text"),
            }, x => x.ZoneId + "|" + x.Field);

            t.Traits = Read(dir, "traits.csv", (r, p) => new Trait
            {
                Id = Get(r, p, "id"),
                Name = Get(r, p, "name"),
                FightBonus = I(r, p, "fight_bonus"),
                FindBonus = F(r, p, "find_bonus"),
                ButcherBonus = I(r, p, "butcher_bonus"),
                CarryBonus = I(r, p, "carry_bonus"),
            }, x => x.Id);

            t.Beasts = Read(dir, "beasts.csv", (r, p) => new Beast
            {
                Id = Get(r, p, "id"),
                Name = Get(r, p, "name"),
                FoodNeed = I(r, p, "food_need"),
                NestCapable = Get(r, p, "nest_capable") == "1",
                Art = Opt(r, "art") ?? "",
            }, x => x.Id);

            t.Professions = Read(dir, "professions.csv", (r, p) => new Profession
            {
                Id = Get(r, p, "id"),
                Name = Get(r, p, "name"),
                SkillsAllowed = SplitList(Get(r, p, "skills_allowed")),
                RequiresTrait = Opt(r, "requires_trait"),
                StartSkills = ParsePairs(Get(r, p, "start_skills")),
            }, x => x.Id);

            t.Roster = Read(dir, "roster.csv", (r, p) => new Person
            {
                Id = Get(r, p, "id"),
                Name = Get(r, p, "name"),
                Profession = Get(r, p, "profession"),
                Traits = SplitList(Opt(r, "traits")),
                SkillsOverride = ParsePairs(Opt(r, "skills_override")),
            }, x => x.Id);

            t.Stations = Read(dir, "stations.csv", (r, p) => new Station
            {
                Id = Get(r, p, "id"),
                Name = Get(r, p, "name"),
                SkillId = Get(r, p, "skill_id"),
                PeopleSlots = I(r, p, "people_slots"),
            }, x => x.Id);

            t.ZoneResources = Read(dir, "zone_resources.csv", (r, p) => new ZoneResource
            {
                ZoneId = Get(r, p, "zone_id"),
                ItemId = Get(r, p, "item_id"),
                Abundance = I(r, p, "abundance"),
            }, x => x.ZoneId + "|" + x.ItemId);

            t.ZoneBeasts = Read(dir, "zone_beasts.csv", (r, p) => new ZoneBeast
            {
                ZoneId = Get(r, p, "zone_id"),
                BeastId = Get(r, p, "beast_id"),
                Weight = I(r, p, "weight"),
                InitialCount = I(r, p, "initial_count"),
            }, x => x.ZoneId + "|" + x.BeastId);

            t.Drops = Read(dir, "drops.csv", (r, p) => new Drop
            {
                BeastId = Get(r, p, "beast_id"),
                ItemId = Get(r, p, "item_id"),
                Chance = F(r, p, "chance"),
                MinSkill = I(r, p, "min_skill"),
            }, x => x.BeastId + "|" + x.ItemId);

            t.Recipes = Read(dir, "recipes.csv", (r, p) => new Recipe
            {
                Id = Get(r, p, "id"),
                Station = Get(r, p, "station"),
                SkillId = Get(r, p, "skill_id"),
                SkillReq = I(r, p, "skill_req"),
                Hours = I(r, p, "hours"),
                Inputs = ParsePairs(Get(r, p, "inputs")),
                Outputs = ParsePairs(Get(r, p, "outputs")),
            }, x => x.Id);

            t.RecipeQuality = Read(dir, "recipe_quality.csv", (r, p) => new RecipeQuality
            {
                RecipeId = Get(r, p, "recipe_id"),
                MinSkill = I(r, p, "min_skill"),
                OutputId = Get(r, p, "output_id"),
                Qty = I(r, p, "qty"),
                Chance = F(r, p, "chance"),
            }, x => x.RecipeId + "|" + x.MinSkill);

            t.Scenarios = Read(dir, "scenarios.csv", (r, p) => new Scenario
            {
                Id = Get(r, p, "id"),
                Title = Get(r, p, "title"),
                Seed = I(r, p, "seed"),
                Day = I(r, p, "day"),
                StartScreen = Get(r, p, "start_screen"),
                OnFinish = Opt(r, "on_finish") ?? "continue",
                Notes = Opt(r, "notes"),
            }, x => x.Id);

            t.ScenarioParties = Read(dir, "scenario_parties.csv", (r, p) => new ScenarioParty
            {
                ScenarioId = Get(r, p, "scenario_id"),
                Squad = Get(r, p, "squad"),
                Zone = Opt(r, "zone") ?? "",
                Behavior = Opt(r, "behavior") ?? "gather",
                EncounterOrder = Opt(r, "encounter_order") ?? "ask",
                Members = Opt(r, "members") ?? "",
            }, x => x.ScenarioId + "|" + x.Squad);

            t.CombatTuning = ReadTuning(Path.Combine(dir, "combat_tuning.csv"));

            t.BeastCombat = Read(dir, "beast_combat.csv", (r, p) => new BeastCombat
            {
                BeastId = Get(r, p, "beast_id"),
                Level = I(r, p, "level"),
                Hp = I(r, p, "hp"),
                AttackMin = I(r, p, "attack_min"),
                AttackMax = I(r, p, "attack_max"),
                Armor = I(r, p, "armor"),
                Speed = F(r, p, "speed"),
            }, x => x.BeastId);

            t.Equipment = Read(dir, "equipment.csv", (r, p) => new Equipment
            {
                Id = Get(r, p, "id"),
                Name = Get(r, p, "name"),
                Slot = Get(r, p, "slot"),
                DamageMin = I(r, p, "damage_min"),
                DamageMax = I(r, p, "damage_max"),
                Armor = I(r, p, "armor"),
                Value = Opt(r, "value") == null ? 0 : I(r, p, "value"),
            }, x => x.Id);

            t.RosterCombat = Read(dir, "roster_combat.csv", (r, p) => new RosterCombat
            {
                PersonId = Get(r, p, "person_id"),
                Hardening = I(r, p, "hardening"),
                WeaponId = Get(r, p, "weapon_id"),
                ArmorId = Get(r, p, "armor_id"),
            }, x => x.PersonId);

            t.SkillsCombat = Read(dir, "skills_combat.csv", (r, p) => new SkillCombat
            {
                Id = Get(r, p, "id"),
                Level = I(r, p, "level"),
                Name = Get(r, p, "name"),
                Branch = Get(r, p, "branch"),
                Effect = Get(r, p, "effect"),
                Value = F(r, p, "value"),
            }, x => x.Id);

            t.RosterSkills = Read(dir, "roster_skills.csv", (r, p) => new RosterSkill
            {
                PersonId = Get(r, p, "person_id"),
                SkillId = Get(r, p, "skill_id"),
            }, x => x.PersonId + "|" + x.SkillId);

            Validate(t);
            return t;
        }

        private static void Validate(Tables t)
        {
            RequireRefs(t.ZoneLore.All.Select(x => x.ZoneId), t.Zones, "zone_lore.csv", "zone_id");

            foreach (var z in t.Zones.All)
            {
                foreach (var nb in z.Neighbours)
                {
                    if (!t.Zones.TryGet(nb, out _))
                        throw new DataException("zones.csv", "зона " + z.Id + " ссылается на неизвестного соседа " + nb);
                }
            }

            RequireRefs(t.ZoneResources.All.Select(x => x.ZoneId), t.Zones, "zone_resources.csv", "zone_id");
            RequireRefs(t.ZoneResources.All.Select(x => x.ItemId), t.Items, "zone_resources.csv", "item_id");
            RequireRefs(t.ZoneBeasts.All.Select(x => x.ZoneId), t.Zones, "zone_beasts.csv", "zone_id");
            RequireRefs(t.ZoneBeasts.All.Select(x => x.BeastId), t.Beasts, "zone_beasts.csv", "beast_id");
            RequireRefs(t.Drops.All.Select(x => x.BeastId), t.Beasts, "drops.csv", "beast_id");
            RequireRefs(t.Drops.All.Select(x => x.ItemId), t.Items, "drops.csv", "item_id");
            RequireRefs(t.Roster.All.Select(x => x.Profession), t.Professions, "roster.csv", "profession");
            RequireRefs(t.Roster.All.SelectMany(x => x.Traits), t.Traits, "roster.csv", "traits");
            RequireRefs(t.Professions.All.Where(x => !string.IsNullOrEmpty(x.RequiresTrait)).Select(x => x.RequiresTrait),
                t.Traits, "professions.csv", "requires_trait");
            RequireRefs(t.Professions.All.SelectMany(x => x.SkillsAllowed), t.Skills, "professions.csv", "skills_allowed");
            RequireRefs(t.Professions.All.SelectMany(x => x.StartSkills.Keys), t.Skills, "professions.csv", "start_skills");
            RequireRefs(t.Roster.All.SelectMany(x => x.SkillsOverride.Keys), t.Skills, "roster.csv", "skills_override");
            RequireRefs(t.Stations.All.Select(x => x.SkillId), t.Skills, "stations.csv", "skill_id");
            RequireRefs(t.Recipes.All.Select(x => x.Station), t.Stations, "recipes.csv", "station");
            RequireRefs(t.Recipes.All.Select(x => x.SkillId), t.Skills, "recipes.csv", "skill_id");
            RequireRefs(t.Recipes.All.SelectMany(x => x.Inputs.Keys), t.Items, "recipes.csv", "inputs");
            RequireRefs(t.Recipes.All.SelectMany(x => x.Outputs.Keys), t.Items, "recipes.csv", "outputs");
            RequireRefs(t.RecipeQuality.All.Select(x => x.RecipeId), t.Recipes, "recipe_quality.csv", "recipe_id");
            RequireRefs(t.RecipeQuality.All.Select(x => x.OutputId), t.Items, "recipe_quality.csv", "output_id");

            ValidateScenarios(t);
            ValidateCombat(t);

            var fields = new[] { "look", "beasts", "finds", "special" };
            foreach (var zone in t.Zones.All)
            {
                foreach (var field in fields)
                {
                    if (!t.ZoneLore.TryGet(zone.Id + "|" + field, out _))
                        throw new DataException("zone_lore.csv", "у зоны " + zone.Id + " нет поля " + field);
                }
            }
        }

        /// <summary>Бой: твари, снаряжение и навыки ссылаются на существующие строки.</summary>
        private static void ValidateCombat(Tables t)
        {
            RequireRefs(t.BeastCombat.All.Select(x => x.BeastId), t.Beasts, "beast_combat.csv", "beast_id");
            RequireRefs(t.RosterCombat.All.Select(x => x.PersonId), t.Roster, "roster_combat.csv", "person_id");

            foreach (var beast in t.BeastCombat.All)
            {
                if (beast.Level < 1 || beast.Level > 5)
                    throw new DataException("beast_combat.csv", "уровень " + beast.BeastId + " вне 1..5");
                if (beast.Hp <= 0 || beast.AttackMin < 0 || beast.AttackMax < beast.AttackMin)
                    throw new DataException("beast_combat.csv", "плохие hp/attack у " + beast.BeastId);
                if (beast.Armor < 0)
                    throw new DataException("beast_combat.csv", "отрицательная броня у " + beast.BeastId);
            }

            foreach (var row in t.RosterCombat.All)
            {
                if (!t.Equipment.TryGet(row.WeaponId, out var weapon) || weapon.Slot != "weapon")
                    throw new DataException("roster_combat.csv", "у " + row.PersonId + " нет оружия " + row.WeaponId);

                if (!t.Equipment.TryGet(row.ArmorId, out var armor) || armor.Slot != "armor")
                    throw new DataException("roster_combat.csv", "у " + row.PersonId + " нет брони " + row.ArmorId);

                var max = t.CombatTuning.GetInt("hardening_max");
                if (row.Hardening < 0 || row.Hardening > max)
                    throw new DataException("roster_combat.csv",
                        "закалка " + row.PersonId + " вне 0.." + max);
            }

            var effects = new[] { "damage_bonus", "hit_bonus", "hp_bonus", "detect_bonus", "alert_bonus", "armor_pierce" };
            foreach (var skill in t.SkillsCombat.All)
            {
                if (skill.Level < 2 || skill.Level > 5)
                    throw new DataException("skills_combat.csv", "уровень навыка " + skill.Id + " вне 2..5");
                if (System.Array.IndexOf(effects, skill.Effect) < 0)
                    throw new DataException("skills_combat.csv", "неизвестный эффект " + skill.Effect + " у " + skill.Id);
            }

            RequireRefs(t.RosterSkills.All.Select(x => x.PersonId), t.Roster, "roster_skills.csv", "person_id");
            RequireRefs(t.RosterSkills.All.Select(x => x.SkillId), t.SkillsCombat, "roster_skills.csv", "skill_id");

            foreach (var take in t.RosterSkills.All)
            {
                if (!t.Roster.TryGet(take.PersonId, out var person)) continue;

                var level = ForestGuild.Core.Sim.People.SkillLevel(t, person, "hunting");
                var skill = t.SkillsCombat.Get(take.SkillId);
                if (skill.Level > level)
                    throw new DataException("roster_skills.csv",
                        take.PersonId + " не может взять " + take.SkillId + ": нужен уровень " + skill.Level);
            }
        }

        /// <summary>Сценарии: ссылки на отряды, зоны и живых людей должны существовать.</summary>
        private static void ValidateScenarios(Tables t)
        {
            var screens = new[] { "campaign", "muster", "expedition" };
            var finishes = new[] { "continue", "menu" };
            var squads = new[] { "A", "B" };
            var behaviors = new[] { "gather", "avoid", "clean" };
            var orders = new[] { "ask", "hide", "retreat", "fight" };

            if (t.Scenarios.All.Count == 0)
                throw new DataException("scenarios.csv", "нет ни одного сценария");

            foreach (var scenario in t.Scenarios.All)
            {
                if (System.Array.IndexOf(screens, scenario.StartScreen) < 0)
                    throw new DataException("scenarios.csv", "неизвестный start_screen " + scenario.StartScreen);

                if (System.Array.IndexOf(finishes, scenario.OnFinish) < 0)
                    throw new DataException("scenarios.csv", "неизвестный on_finish " + scenario.OnFinish);

                if (scenario.Day < 1)
                    throw new DataException("scenarios.csv", "день должен быть >= 1 у " + scenario.Id);
            }

            foreach (var party in t.ScenarioParties.All)
            {
                if (!t.Scenarios.TryGet(party.ScenarioId, out _))
                    throw new DataException("scenario_parties.csv", "неизвестный сценарий " + party.ScenarioId);

                if (System.Array.IndexOf(squads, party.Squad) < 0)
                    throw new DataException("scenario_parties.csv", "неизвестный отряд " + party.Squad);

                if (!string.IsNullOrEmpty(party.Zone) && !t.Zones.TryGet(party.Zone, out _))
                    throw new DataException("scenario_parties.csv", "неизвестная зона " + party.Zone);

                if (System.Array.IndexOf(behaviors, party.Behavior) < 0)
                    throw new DataException("scenario_parties.csv", "неизвестное намерение " + party.Behavior);

                if (System.Array.IndexOf(orders, party.EncounterOrder) < 0)
                    throw new DataException("scenario_parties.csv", "неизвестный наказ " + party.EncounterOrder);

                foreach (var member in SplitList(party.Members))
                {
                    if (!t.Roster.TryGet(member, out _))
                        throw new DataException("scenario_parties.csv", "неизвестный житель " + member);
                }
            }
        }

        private static void RequireRefs<T>(IEnumerable<string> ids, Table<T> table, string file, string col)
        {
            foreach (var id in ids)
            {
                if (!table.TryGet(id, out _))
                    throw new DataException(file, "неизвестная ссылка " + col + "=" + id);
            }
        }

        private static Table<T> Read<T>(string dir, string file,
            Func<Dictionary<string, string>, string, T> map, Func<T, string> idOf)
        {
            var path = Path.Combine(dir, file);
            var list = CsvReader.Read(path).Select(r => map(r, path)).ToList();
            return new Table<T>(list, idOf);
        }

        private static Tuning ReadTuning(string path)
        {
            var raw = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in CsvReader.Read(path)) raw[Get(row, path, "id")] = Opt(row, "value");
            return new Tuning(raw);
        }

        private static Dictionary<string, int> ParsePairs(string value)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(value)) return result;

            foreach (var pair in SplitList(value))
            {
                var parts = pair.Split(':');
                if (parts.Length != 2)
                    throw new DataException("table", "ожидалось id:число, получено " + pair);

                result[parts[0].Trim()] = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
            }

            return result;
        }

        private static string[] SplitList(string value)
        {
            return value
                .Split('|')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();
        }

        private static string Get(Dictionary<string, string> r, string path, string col)
        {
            if (!r.TryGetValue(col, out var v) || string.IsNullOrEmpty(v))
                throw new DataException(Path.GetFileName(path), "пустая колонка " + col);
            return v;
        }

        private static string Opt(Dictionary<string, string> r, string col)
        {
            return r.TryGetValue(col, out var v) ? v : "";
        }

        private static int I(Dictionary<string, string> r, string path, string col)
        {
            var raw = Get(r, path, col);
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                throw new DataException(Path.GetFileName(path), "не число: " + col + "=" + raw);
            return v;
        }

        private static float F(Dictionary<string, string> r, string path, string col)
        {
            var raw = Get(r, path, col);
            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                throw new DataException(Path.GetFileName(path), "не число: " + col + "=" + raw);
            return v;
        }
    }
}
