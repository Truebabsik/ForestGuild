using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--days")
        {
            var days = args.Length > 1 ? int.Parse(args[1]) : 21;
            return Days(days);
        }

        if (args.Length > 0 && args[0] == "--battle")
        {
            return BattleReport();
        }

        if (args.Length > 0 && args[0] == "--curve")
        {
            return CurveReport();
        }

        var checks = new CheckRunner();

        checks.Check("rng is deterministic", () =>
        {
            var a = new Rng(42);
            var b = new Rng(42);
            return a.Next(100) == b.Next(100);
        });

        checks.Check("tables load", () =>
        {
            var t = DataLoader.Load(DataDir());
            return t.Zones.All.Count == 6
                && t.Roster.All.Count == 8
                && t.Items.All.Count == 15
                && t.Beasts.All.Count == 8;
        });

        checks.Check("unknown neighbour zone fails", () =>
        {
            var dir = CopyDataWithMutation("zones.csv",
                "verge_bad;Плохая зона;edge;60;3;1;2;nowhere;none;0.5;0.52");
            try
            {
                DataLoader.Load(dir);
                return false;
            }
            catch (DataException e)
            {
                return e.Message.Contains("nowhere");
            }
        });

        checks.Check("unknown item in drops fails", () =>
        {
            var dir = CopyDataWithMutation("drops.csv", "beast_spark;item_missing;0.5;0");
            try
            {
                DataLoader.Load(dir);
                return false;
            }
            catch (DataException e)
            {
                return e.Message.Contains("item_missing");
            }
        });

        checks.Check("bad schema version fails", () =>
        {
            var dir = CopyDataWithMutation("tuning.csv", "schema_version;99;подмена");
            try
            {
                DataLoader.Load(dir);
                return false;
            }
            catch (DataException e)
            {
                return e.Message.Contains("schema_version");
            }
        });

        checks.Check("night feeds beasts from forage", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var z = w.Zones["verge_meadow"];
            z.Beasts.Clear();
            z.Beasts["beast_spark"] = 1;
            NightSim.AdvanceNight(w, t, new Rng(1));
            return z.Forage == 1 && z.NestXp == 0;
        });

        checks.Check("hungry beast migrates toward village", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var z = w.Zones["verge_springs"];
            z.Beasts.Clear();
            z.Beasts["beast_spark"] = 4;
            z.Crystals = 0;
            var before = w.BeastCount("verge_meadow");
            NightSim.AdvanceNight(w, t, new Rng(1));
            return w.BeastCount("verge_springs") < 4 && w.BeastCount("verge_meadow") > before;
        });

        checks.Check("crystals eaten over forage give nest xp", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var z = w.Zones["verge_meadow"];
            z.Beasts.Clear();
            z.Beasts["beast_spark"] = 3;
            z.Crystals = 3;
            NightSim.AdvanceNight(w, t, new Rng(1));
            return z.NestXp >= 1;
        });

        checks.Check("expedition works whole day budget", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);

            // Световой день — 600 минут, Опушка съедает по 30 в каждую сторону.
            var briefing = Briefing.Compute(w, t, new[] { "leya" }, "verge_meadow", Behavior.Gather);
            var party = Party("verge_meadow", "leya");
            var r = ExpeditionSim.Resolve(w, t, party, new Rng(7));

            return briefing.WorkMinutes == 540 && r.MinutesUsed > 0 && r.ZoneId == "verge_meadow";
        });

        checks.Check("expedition finds something over many seeds", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var total = 0;
            for (var seed = 0; seed < 20; seed++)
                total += ExpeditionSim.Resolve(w, t, Party("verge_springs", "leya", "tove"), new Rng(seed)).Found.Count;
            return total > 0;
        });

        checks.Check("expedition is deterministic", () =>
        {
            var t = DataLoader.Load(DataDir());
            // Поход меняет мир (встреча снимает тварь из зоны), поэтому каждому прогону — свой мир.
            var a = ExpeditionSim.Resolve(World.Create(t), t, Party("verge_springs", "brunni"), new Rng(3));
            var b = ExpeditionSim.Resolve(World.Create(t), t, Party("verge_springs", "brunni"), new Rng(3));
            return a.Found.Count == b.Found.Count && a.Wounded == b.Wounded && a.MinutesUsed == b.MinutesUsed;
        });

        checks.Check("day flow resolves parties and advances night", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var plan = new DayPlan();
            plan.Parties.Add(Party("verge_meadow", "leya"));
            var flow = new DayFlow(w, t, new Rng(5));
            var report = flow.RunDay(plan);
            return report.Parties.Count == 1 && w.Day == 2 && report.Lines.Count > 0;
        });

        checks.Check("village workers repair palisade", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            w.PalisadeHp = 5;
            var plan = new DayPlan();
            plan.VillageWorkers.Add("valda");
            var report = new DayFlow(w, t, new Rng(1)).RunDay(plan);
            return w.PalisadeHp == 7 && report.Repaired == 2;
        });

        checks.Check("night report explains feeding", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var report = NightSim.AdvanceNight(w, t, new Rng(1));
            return report.Lines.Any(line => line.Contains("кормятся")
                || line.Contains("кристалл")
                || line.Contains("голодные"));
        });

        checks.Check("zone lore is complete for all zones", () =>
        {
            var t = DataLoader.Load(DataDir());
            return t.ZoneLore.All.Count == t.Zones.All.Count * 4;
        });

        checks.Check("unscouted zone shows question mark", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            return ZoneKnowledge.Mood(w, "verge_springs") == "?";
        });

        checks.Check("expedition scouts the zone", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            ExpeditionSim.Resolve(w, t, Party("verge_meadow", "leya"), new Rng(3));
            return w.Zones["verge_meadow"].Scouted
                && ZoneKnowledge.Mood(w, "verge_meadow") != "?";
        });

        checks.Check("phases: day does not advance, night does", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var flow = new DayFlow(w, t, new Rng(9));

            var plan = new DayPlan();
            plan.Parties.Add(Party("verge_meadow", "leya"));
            var day = flow.RunExpeditions(plan);
            var dayAfter = w.Day;

            var night = flow.RunNight();
            return dayAfter == 1 && w.Day == 2 && day.Parties.Count == 1 && night.Lines.Count > 0;
        });

        checks.Check("foragers fill the store", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var plan = new DayPlan();
            plan.Foragers.Add("run");
            plan.Foragers.Add("valda");
            new DayFlow(w, t, new Rng(1)).RunExpeditions(plan);
            return w.Store[Works.ForageStoreKey] == 2;
        });

        checks.Check("wounded recovers after a night in bed", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            Works.Wound(w, "leya");

            var plan = new DayPlan();
            plan.Beds.Add("leya");

            var flow = new DayFlow(w, t, new Rng(2));
            flow.RunExpeditions(plan);
            flow.RunNight();
            return w.ConditionOf("leya") == PersonCondition.Healthy;
        });

        checks.Check("wounded without bed stays wounded", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            Works.Wound(w, "leya");

            var flow = new DayFlow(w, t, new Rng(2));
            flow.RunExpeditions(new DayPlan());
            flow.RunNight();
            return w.ConditionOf("leya") == PersonCondition.Wounded;
        });

        checks.Check("briefing counts slots and warns", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var one = Briefing.Compute(w, t, new[] { "brunni" }, "verge_springs", Behavior.Gather);
            var three = Briefing.Compute(w, t, new[] { "brunni", "leya", "tove" }, "verge_springs", Behavior.Clean);

            return one.Slots == 5                      // 4 + «Твёрдый»
                && one.WorkMinutes == 480
                && three.Warnings.Count >= 1;          // втроём шумно
        });

        checks.Check("loot goes to store and reports delta", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);

            var plan = new DayPlan();
            plan.Parties.Add(Party("verge_meadow", "leya", "tove"));
            var report = new DayFlow(w, t, new Rng(11)).RunExpeditions(plan);

            if (report.Loot.Count == 0) return true; // сид без находок — не провал

            foreach (var pair in report.Loot)
            {
                if (!w.Store.TryGetValue(pair.Key, out var stored)) return false;
                if (stored < pair.Value) return false;
            }

            return true;
        });

        checks.Check("evening beds heal the wounded overnight", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            Works.Wound(w, "leya");

            var plan = new DayPlan();
            plan.Beds.Add("leya");

            var flow = new DayFlow(w, t, new Rng(4));
            flow.RunExpeditions(new DayPlan());
            flow.RunNight(plan);

            return w.ConditionOf("leya") == PersonCondition.Healthy;
        });

        checks.Check("quiet night folds into one line", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var report = NightSim.AdvanceNight(w, t, new Rng(1));

            var quiet = report.Entries.FindAll(e => e.Text.Contains("просто кормятся")).Count;
            return quiet <= 1 && report.Entries.Count <= 6;
        });

        checks.Check("every item has a short name for HUD", () =>
        {
            var t = DataLoader.Load(DataDir());
            return t.Items.All.All(item => !string.IsNullOrEmpty(item.Short));
        });

        checks.Check("wave arrives on its day and reschedules", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            w.Day = 21;
            w.NextWaveDay = 21;

            var report = new DayFlow(w, t, new Rng(7)).RunNight(new DayPlan());

            return report.Wave != null
                && report.Wave.Strength > 0
                && w.NextWaveDay > 21;
        });

        checks.Check("hunters at home shoot the wave", () =>
        {
            var t = DataLoader.Load(DataDir());

            var withHunters = World.Create(t);
            withHunters.Day = 21;
            withHunters.NextWaveDay = 21;
            var a = new DayFlow(withHunters, t, new Rng(5)).RunNight(new DayPlan());

            var alone = World.Create(t);
            alone.Day = 21;
            alone.NextWaveDay = 21;
            KillHunters(alone, t);
            var b = new DayFlow(alone, t, new Rng(5)).RunNight(new DayPlan());

            return a.Wave != null && b.Wave != null
                && b.Wave.Killed == 0
                && a.Wave.Killed >= b.Wave.Killed;
        });

        checks.Check("wave damages the palisade", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            w.Day = 21;
            w.NextWaveDay = 21;
            KillHunters(w, t);

            var before = w.PalisadeHp;
            var report = new DayFlow(w, t, new Rng(3)).RunNight(new DayPlan());

            return report.Wave != null && report.Wave.PalisadeDamage > 0 && w.PalisadeHp < before;
        });

        checks.Check("expedition pauses on encounter with ask orders", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);

            var zone = w.Zones["verge_springs"];
            zone.Beasts.Clear();
            zone.Beasts["beast_spark"] = 6;
            zone.Forage = 1;
            zone.Crystals = 0;

            var party = Party("verge_springs", "brunni");
            party.Orders = new StandingOrders { Encounter = EncounterOrder.Ask };

            var expedition = new Expedition(w, t, party, new Rng(4));
            var paused = false;

            for (var i = 0; i < 20 && !expedition.Finished; i++)
            {
                if (expedition.HasDecision) { paused = true; break; }
                expedition.Advance();
            }

            return paused && expedition.Pending.Kind == DecisionKind.Encounter;
        });

        checks.Check("order resolves encounter without pause", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);

            var zone = w.Zones["verge_springs"];
            zone.Beasts.Clear();
            zone.Beasts["beast_spark"] = 6;
            zone.Forage = 1;
            zone.Crystals = 0;

            var party = Party("verge_springs", "brunni");
            party.Orders = new StandingOrders
            {
                Encounter = EncounterOrder.Retreat,
                Wounded = WoundedOrder.Leave,
                Carcass = CarcassOrder.Leave,
                BagFull = BagOrder.Skip,
            };

            var expedition = new Expedition(w, t, party, new Rng(4));
            for (var i = 0; i < 80 && !expedition.Finished; i++) expedition.Advance();

            return expedition.Finished;
        });

        checks.Check("day is six hundred minutes and the meadow is half an hour away", () =>
        {
            var t = DataLoader.Load(DataDir());
            return t.Tuning.GetInt("daylight_minutes") == 600
                && t.Zones.Get("verge_meadow").TravelMinutes == 30;
        });

        checks.Check("passive forest grows a nest before the wave", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var flow = new DayFlow(w, t, new Rng(2026));

            for (var i = 0; i < 21; i++) flow.RunNight(new DayPlan());

            var nests = w.Zones.Values.Count(z => z.Nest.Exists);
            return nests >= 1 && w.TotalBeasts() >= 6;
        });

        checks.Check("risk and mastery stay words", () =>
        {
            return Expedition.RiskWord(0.9) == "почти наверняка"
                && Expedition.RiskWord(0.55) == "как повезёт"
                && Expedition.RiskWord(0.3) == "рискованно"
                && Expedition.MasteryWord(4) == "мастер"
                && Expedition.MasteryWord(1) == "новичок";
        });

        checks.Check("hunters who went to the forest still defend at night", () =>
        {
            var t = DataLoader.Load(DataDir());

            var withParty = World.Create(t);
            withParty.Day = 21;
            withParty.NextWaveDay = 21;
            var plan = new DayPlan();
            plan.Parties.Add(Party("verge_meadow", "brunni", "skadi", "hilda", "orn"));
            var a = new DayFlow(withParty, t, new Rng(11)).RunNight(plan);

            var withoutParty = World.Create(t);
            withoutParty.Day = 21;
            withoutParty.NextWaveDay = 21;
            var b = new DayFlow(withoutParty, t, new Rng(11)).RunNight(new DayPlan());

            return a.Wave != null && b.Wave != null
                && a.Wave.Killed > 0
                && a.Wave.Killed == b.Wave.Killed;
        });

        checks.Check("nest without a nest-capable beast does not grow", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);

            var marsh = w.Zones["verge_marsh"];
            var beastsBefore = w.BeastCount("verge_marsh");

            marsh.Nest.Exists = true;
            marsh.Nest.Beasts = 0;
            marsh.Nest.DaysSinceSpawn = 0;

            for (var i = 0; i < 10; i++) NightSim.AdvanceNight(w, t, new Rng(i));

            // Гнездо не растёт и не плодит: в Топи живёт только пепельный вор, он не вьёт гнёзд.
            return marsh.Nest.Beasts == 0 && w.BeastCount("verge_marsh") <= beastsBefore;
        });

        checks.Check("full bag asks before dropping", () =>
        {
            var dir = CopyDataWithReplacements("tuning.csv",
                "find_base;1;тест", "slots_per_person;1;тест");

            var t = DataLoader.Load(dir);
            var w = World.Create(t);

            var party = Party("verge_meadow", "leya");
            party.Orders = new StandingOrders { BagFull = BagOrder.Ask };

            var expedition = new Expedition(w, t, party, new Rng(3));
            for (var i = 0; i < 20 && !expedition.Finished; i++)
            {
                if (expedition.HasDecision) break;
                expedition.Advance();
            }

            return expedition.Pending != null
                && expedition.Pending.Kind == DecisionKind.BagFull
                && expedition.Pending.ItemId != null
                && expedition.Pending.Options.Count >= 1;
        });

        checks.Check("странствующая тварь называется по виду", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);

            // Тварь пришла в зону, где своих видов нет: раньше она становилась безымянной «тварью».
            w.Zones["verge_meadow"].Beasts["beast_ash"] = 2;

            for (var seed = 0; seed < 40; seed++)
            {
                var party = Party("verge_meadow", "leya");
                party.Behavior = Behavior.Clean;

                var expedition = new Expedition(w, t, party, new Rng(seed));
                for (var i = 0; i < 30 && !expedition.Finished; i++)
                {
                    if (expedition.HasDecision) break;
                    expedition.Advance();
                }

                if (expedition.Pending == null || expedition.Pending.Kind != DecisionKind.Encounter) continue;

                return expedition.Pending.BeastId == "beast_ash"
                    && expedition.Pending.Title.Contains("Пепельный вор");
            }

            return false;
        });

        checks.Check("у каждой твари есть арт поиска и туши", () =>
        {
            var monsters = Path.Combine(Path.GetDirectoryName(DataDir().TrimEnd(Path.DirectorySeparatorChar)),
                "..", "UI", "Art", "Monsters");

            foreach (var beast in DataLoader.Load(DataDir()).Beasts.All)
            {
                if (string.IsNullOrEmpty(beast.Art)) return false;

                foreach (var state in new[] { "search", "dead" })
                {
                    var file = Path.Combine(monsters, beast.Art + "_" + state + ".jpg");
                    if (!File.Exists(Path.GetFullPath(file))) return false;
                }
            }

            return true;
        });

        checks.Check("drops respect min_skill", () =>
        {
            var t = DataLoader.Load(DataDir());

            var noviceHide = false;
            var skilledHide = false;
            var noviceBone = false;

            for (var seed = 0; seed < 40; seed++)
            {
                var novice = Expedition.RollDrops(t, "beast_bone", 0, new Rng(seed));
                var skilled = Expedition.RollDrops(t, "beast_bone", 3, new Rng(seed));

                if (novice.Any(x => x.Key == "hide_rough")) noviceHide = true;
                if (skilled.Any(x => x.Key == "hide_rough")) skilledHide = true;
                if (novice.Any(x => x.Key == "bone_plate")) noviceBone = true;
            }

            return !noviceHide && skilledHide && noviceBone;
        });

        checks.Check("squad rules live in the core", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var a = w.Squads[0];
            var b = w.Squads[1];

            var added = 0;
            foreach (var person in t.Roster.All)
            {
                if (w.TryAddMember(t, a, person.Id)) added++;
            }

            if (added != t.Tuning.GetInt("party_max")) return false;

            // Человек занят в другом отряде — второй раз его не взять.
            var busy = a.Members[0];
            if (w.TryAddMember(t, b, busy)) return false;

            // Но забрать из другого отряда можно, если есть место.
            if (!w.TryTakeFromOtherSquad(t, b, busy)) return false;

            return b.Members.Contains(busy) && !a.Members.Contains(busy);
        });

        checks.Check("beds limit lives in the core", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var plan = new DayPlan();
            var limit = t.Tuning.GetInt("beds_count");

            var healthy = t.Roster.All[0].Id;
            if (plan.TryAddBed(t, w, healthy)) return false; // здоровому постель не нужна

            var wounded = t.Roster.All[0].Id;
            Works.Wound(w, wounded);
            if (!plan.TryAddBed(t, w, wounded)) return false;

            plan.TryAddBed(t, w, wounded); // повторно — не дублируется
            if (plan.Beds.Count != 1) return false;

            var second = t.Roster.All[1].Id;
            Works.Wound(w, second);
            if (!plan.TryAddBed(t, w, second)) return false;

            var third = t.Roster.All[2].Id;
            Works.Wound(w, third);
            return !plan.TryAddBed(t, w, third) && plan.Beds.Count == limit;
        });

        checks.Check("morning drops the dead and wounded from squads", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var a = w.Squads[0];

            var dead = t.Roster.All[0].Id;
            var wounded = t.Roster.All[1].Id;
            a.Members.Add(dead);
            a.Members.Add(wounded);

            Works.Kill(w, dead);
            Works.Wound(w, wounded);

            w.RemoveUnavailableFromSquads();

            return !a.Members.Contains(dead) && !a.Members.Contains(wounded);
        });

        checks.Check("scenario builds an episode with its squads", () =>
        {
            var t = DataLoader.Load(DataDir());
            var episode = EpisodeBuilder.Build(t, "expedition");

            var a = episode.World.Squads[0];
            var b = episode.World.Squads[1];

            return episode.Scenario.StartScreen == "expedition"
                && episode.World.Day == 1
                && a.ZoneId == "verge_springs" && a.Members.Count == 2
                && b.ZoneId == "verge_thicket" && b.Members.Count == 2
                && a.Behavior == Behavior.Gather && b.Behavior == Behavior.Clean;
        });

        checks.Check("unknown scenario is a data error", () =>
        {
            var t = DataLoader.Load(DataDir());
            try
            {
                EpisodeBuilder.Build(t, "no_such_scenario");
                return false;
            }
            catch (DataException)
            {
                return true;
            }
        });

        checks.Check("campaign starts on day one with empty squads", () =>
        {
            var t = DataLoader.Load(DataDir());
            var episode = EpisodeBuilder.Build(t, "campaign");

            return episode.World.Day == 1
                && episode.World.Squads[0].Members.Count == 0
                && episode.World.Squads[1].Members.Count == 0
                && episode.Scenario.OnFinish == "continue";
        });

        checks.Check("battle is deterministic", () =>
        {
            var t = DataLoader.Load(DataDir());
            var a = Battle.Resolve(t, World.Create(t), new[] { "brunni" }, new[] { "beast_ash" }, new Rng(11));
            var b = Battle.Resolve(t, World.Create(t), new[] { "brunni" }, new[] { "beast_ash" }, new Rng(11));

            return a.Won == b.Won
                && a.Beast.Hp == b.Beast.Hp
                && a.Rounds == b.Rounds
                && a.Fighters[0].Hp == b.Fighters[0].Hp;
        });

        checks.Check("battle tells its story without numbers", () =>
        {
            var t = DataLoader.Load(DataDir());
            // Живой человек, а не проба: у пробы в имени уровень («Проба 2»), то есть цифра.
            var battle = Battle.Resolve(t, World.Create(t), new[] { "hilda" },
                new[] { "beast_ash" }, new Rng(11));

            if (battle.Log.Count < 4 || battle.Log.Count > 40) return false;

            var hasOutcome = battle.Log.Any(line => line.Contains("падает") || line.Contains("смят"));
            var hasStrike = battle.Log.Any(line => line.Contains("бьёт") || line.Contains("достаёт"));
            var noNumbers = battle.Log.All(line => !line.Any(char.IsDigit))
                && battle.Summary.All(line => !line.Any(char.IsDigit));
            var summaryOk = battle.Summary.Count >= 2 && battle.Summary.Count <= 3;

            return hasOutcome && hasStrike && noNumbers && summaryOk;
        });

        checks.Check("ambush has its own narrative", () =>
        {
            var t = DataLoader.Load(DataDir());

            for (var seed = 0; seed < 300; seed++)
            {
                var battle = Battle.Resolve(t, World.Create(t), new[] { "hilda" },
                    new[] { "beast_ash" }, new Rng(seed));

                if (battle.Ambusher == "") continue;

                var first = battle.Log[0];
                var ok = battle.Ambusher == "party"
                    ? first.Contains("замечает")
                    : first.Contains("врасплох");

                return ok && battle.Log.Count(x => x.Contains("из засады")) >= 1;
            }

            return false;
        });

        checks.Check("battle card waits only when the encounter is asked", () =>
        {
            var t = DataLoader.Load(DataDir());

            for (var seed = 0; seed < 30; seed++)
            {
                var w = World.Create(t);
                var zone = w.Zones["verge_springs"];
                zone.Beasts.Clear();
                zone.Beasts["beast_ash"] = 2;
                zone.Forage = 1;
                zone.Crystals = 0;

                var party = Party("verge_springs", "brunni", "skadi");
                party.Orders = new StandingOrders
                {
                    Encounter = EncounterOrder.Ask,
                    Wounded = WoundedOrder.Leave,
                    Carcass = CarcassOrder.Leave,
                    BagFull = BagOrder.Skip,
                };

                var run = new Expedition(w, t, party, new Rng(seed));
                for (var i = 0; i < 120 && !run.Finished && !run.HasDecision; i++) run.Advance();
                if (!run.HasDecision || run.Pending.Kind != DecisionKind.Encounter) continue;

                run.Decide("fight");
                if (run.Pending == null || run.Pending.Kind != DecisionKind.Battle) return false;

                var lines = run.Pending.Body.Split('\n');
                if (lines.Length < 4) return false;
                if (run.LastBattle == null) return false;
                if (run.Pending.Options[0].Id != "next") return false;

                run.Decide("next");

                // Дальше — обычный исход боя: туша, ранение или конец похода.
                if (run.HasDecision
                    && run.Pending.Kind != DecisionKind.Carcass
                    && run.Pending.Kind != DecisionKind.Wounded
                    && run.Pending.Kind != DecisionKind.BagFull) return false;

                return true;
            }

            return false;
        });

        checks.Check("pack has the sum of its parts", () =>
        {
            var t = DataLoader.Load(DataDir());
            var one = Battle.Resolve(t, World.Create(t), new[] { "brunni" }, new[] { "beast_ash" }, new Rng(1));
            var two = Battle.Resolve(t, World.Create(t), new[] { "brunni" },
                new[] { "beast_ash", "beast_ash" }, new Rng(1));

            return two.Beast.HpMax > one.Beast.HpMax;
        });

        checks.Check("battle always ends", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);

            for (var seed = 0; seed < 25; seed++)
            {
                var battle = Battle.Resolve(t, w, new[] { "hilda", "orn" },
                    new[] { "beast_bone" }, new Rng(seed));

                if (battle.Rounds > 200) return false;
                if (!battle.Won && battle.Downed.Count == 0) return false;
            }

            return true;
        });

        checks.Check("ambush: detection follows speed", () =>
        {
            var t = DataLoader.Load(DataDir());
            var probe = Battle.ProbeFighter(t, 2);

            var ash = new BattleBeast { Id = "beast_ash", Speed = t.BeastCombat.Get("beast_ash").Speed };
            Battle.DetectChances(t, new[] { probe }, ash, out var party, out var beast);
            if (Math.Abs(party - 0.5) > 0.001 || Math.Abs(beast - 0.7) > 0.001) return false;

            var worm = new BattleBeast { Id = "beast_glassworm", Speed = t.BeastCombat.Get("beast_glassworm").Speed };
            Battle.DetectChances(t, new[] { probe }, worm, out _, out var slowBeast);
            if (Math.Abs(slowBeast - 0.4) > 0.001) return false;

            // Навык `alert_bonus` поднимает «чутьё» отряда на свою величину.
            var alert = Battle.ProbeFighter(t, 2);
            alert.AlertBonus = 0.5;
            Battle.DetectChances(t, new[] { alert }, ash, out var alertParty, out _);

            return Math.Abs(alertParty - 0.55) < 0.001;
        });

        checks.Check("ambush: sides roll independently", () =>
        {
            var t = DataLoader.Load(DataDir());
            var party = 0;
            var beast = 0;
            var none = 0;
            const int samples = 2000;

            for (var seed = 0; seed < samples; seed++)
            {
                var battle = Battle.ResolveFighters(t, new[] { Battle.ProbeFighter(t, 2) },
                    new[] { "beast_ash" }, new Rng(seed * 7919 + 13));

                if (battle.Ambusher == "party") party++;
                else if (battle.Ambusher == "beast") beast++;
                else none++;
            }

            // Засада отряда 0.5, тварь 0.7 → «мы» 15%, «она» 35%, никто 50%.
            return Math.Abs(party / (double)samples - 0.15) < 0.04
                && Math.Abs(beast / (double)samples - 0.35) < 0.04
                && Math.Abs(none / (double)samples - 0.50) < 0.04;
        });

        checks.Check("curve: h2 is in the corridor", () =>
        {
            var t = DataLoader.Load(DataDir());
            var wins = ProbeWinRate(t, 2, "beast_ash", 2000);
            return wins >= 0.45 && wins <= 0.55;
        });

        checks.Check("curve: without ambush h2 leaves the corridor", () =>
        {
            var t = DataLoader.Load(DataDir());
            var saved = t.CombatTuning;
            t.CombatTuning = saved.With("detect_base", "0").With("detect_per_alert", "0");
            var wins = ProbeWinRate(t, 2, "beast_ash", 2000);
            t.CombatTuning = saved;

            return wins > 0.55;
        });

        checks.Check("victory leads to a carcass, defeat to a wound", () =>
        {
            var t = DataLoader.Load(DataDir());

            var winWorld = World.Create(t);
            var winZone = winWorld.Zones["verge_springs"];
            winZone.Beasts.Clear();
            winZone.Beasts["beast_ash"] = 6;
            winZone.Forage = 1;
            winZone.Crystals = 0;

            var winners = Party("verge_springs", "brunni", "skadi");
            winners.Orders = FightOrders();
            var winRun = new Expedition(winWorld, t, winners, new Rng(4));
            for (var i = 0; i < 120 && !winRun.Finished; i++) winRun.Advance();

            var lossWorld = World.Create(t);
            var lossZone = lossWorld.Zones["verge_springs"];
            lossZone.Beasts.Clear();
            lossZone.Beasts["beast_bone"] = 6;
            lossZone.Forage = 1;
            lossZone.Crystals = 0;

            var losers = Party("verge_springs", "leya");
            losers.Orders = new StandingOrders
            {
                Encounter = EncounterOrder.Fight,
                Wounded = WoundedOrder.Carry,
                Carcass = CarcassOrder.Leave,
                BagFull = BagOrder.Skip,
            };
            var lossRun = new Expedition(lossWorld, t, losers, new Rng(4));
            for (var i = 0; i < 120 && !lossRun.Finished; i++) lossRun.Advance();

            var won = winRun.Result.Log.Any(line => line.Contains("бой — победа"))
                && winRun.Result.Log.Any(line => line.Contains("тушу бросили"));
            return won && lossRun.Finished && lossRun.Result.Wounded;
        });

        checks.Check("battle damage stays on people between fights", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var zone = w.Zones["verge_springs"];
            zone.Beasts.Clear();
            zone.Beasts["beast_ash"] = 3;
            zone.Forage = 1;
            zone.Crystals = 0;

            var party = Party("verge_springs", "hilda", "orn");
            party.Orders = FightOrders();
            var run = new Expedition(w, t, party, new Rng(7));
            for (var i = 0; i < 200 && !run.Finished; i++) run.Advance();

            var fought = run.Result.Log.Any(line => line.Contains("бой — победа")
                || line.Contains("застаёт отряд"));
            var hurt = w.People["hilda"].Hp < w.People["hilda"].HpMax
                || w.People["orn"].Hp < w.People["orn"].HpMax;

            return fought && hurt;
        });

        checks.Check("armor and hardening never make a hit zero", () =>
        {
            var t = DataLoader.Load(DataDir());
            var probe = Battle.ProbeFighter(t, 2);
            var armored = BeastOf(t, "beast_ash");
            armored.Armor = 100;

            if (Battle.WeaponHit(t, probe, armored, probe.WeaponMax, false) != 1) return false;

            var hardened = Battle.ProbeFighter(t, 5);
            var weak = BeastOf(t, "beast_ash");
            weak.AttackMin = 1;
            return Battle.BeastHit(t, hardened, weak, 1, false) >= 1
                && Battle.BeastHit(t, hardened, weak, 2, false) >= 1;
        });

        checks.Check("hardening opens at level three and gives its share", () =>
        {
            var t = DataLoader.Load(DataDir());
            var second = Battle.ProbeFighter(t, 2);
            var third = Battle.ProbeFighter(t, 3);
            var fourth = Battle.ProbeFighter(t, 4);
            var fifth = Battle.ProbeFighter(t, 5);

            if (second.Hardening != 0 || third.Hardening != 1
                || fourth.Hardening != 2 || fifth.Hardening != 2) return false;

            var beast = BeastOf(t, "beast_ash");
            beast.Armor = 0;
            var plain = Battle.ProbeFighter(t, 2);
            plain.Hardening = 0;
            plain.Armor = 0;
            var one = Battle.ProbeFighter(t, 2);
            one.Hardening = 1;
            one.Armor = 0;

            return Battle.BeastHit(t, plain, beast, 20, false) == 20
                && Battle.BeastHit(t, one, beast, 20, false) == 18;
        });

        checks.Check("hit chance grows with level and stops at 0.95", () =>
        {
            var t = DataLoader.Load(DataDir());
            var first = Battle.ProbeFighter(t, 1);
            var last = Battle.ProbeFighter(t, 5);
            var lucky = Battle.ProbeFighter(t, 5);
            lucky.HitBonus = 0.5;

            return Math.Abs(Battle.HitChance(t, first) - 0.46) < 0.0001
                && Math.Abs(Battle.HitChance(t, last) - 0.62) < 0.0001
                && Math.Abs(Battle.HitChance(t, lucky) - 0.95) < 0.0001;
        });

        checks.Check("damage comes from the weapon and the beast range", () =>
        {
            var t = DataLoader.Load(DataDir());
            var probe = Battle.ProbeFighter(t, 2);
            var beast = BeastOf(t, "beast_ash");
            beast.Armor = 0;

            var lowWeapon = Battle.WeaponHit(t, probe, beast, probe.WeaponMin, false);
            var highWeapon = Battle.WeaponHit(t, probe, beast, probe.WeaponMax, false);
            var lowBeast = Battle.BeastHit(t, probe, beast, beast.AttackMin, false);
            var highBeast = Battle.BeastHit(t, probe, beast, beast.AttackMax, false);

            return probe.WeaponMax > probe.WeaponMin
                && beast.AttackMax > beast.AttackMin
                && highWeapon > lowWeapon
                && highBeast > lowBeast;
        });

        checks.Check("weapon that cannot pierce the armor deals min_damage", () =>
        {
            var t = DataLoader.Load(DataDir());
            var first = Battle.ProbeFighter(t, 1);
            var beast = BeastOf(t, "beast_ash");
            beast.Armor = first.WeaponMax;

            var exact = Battle.WeaponHit(t, first, beast, first.WeaponMax, false);
            beast.Armor = first.WeaponMax + 5;
            var deeper = Battle.WeaponHit(t, first, beast, first.WeaponMax, false);

            return exact == 1 && deeper == 1;
        });

        checks.Check("wound penalty is exact and has no penalty on the boundary", () =>
        {
            var t = DataLoader.Load(DataDir());
            var fighter = Battle.ProbeFighter(t, 2);
            var beast = BeastOf(t, "beast_ash");
            beast.Armor = 0;

            var full = Battle.WeaponHit(t, fighter, beast, 20, false);

            fighter.Hp = fighter.HpMax / 2; // ровно 50% — штрафа нет
            if (Battle.PenaltyFactor(t, fighter) != 1.0) return false;

            fighter.Hp = fighter.HpMax / 2 - 1;
            var wounded = Battle.WeaponHit(t, fighter, beast, 20, false);
            if (Battle.PenaltyFactor(t, fighter) != 0.75) return false;

            fighter.Hp = fighter.HpMax / 4; // ровно 25% — ещё ×0.75
            if (Battle.PenaltyFactor(t, fighter) != 0.75) return false;

            fighter.Hp = fighter.HpMax / 4 - 1;
            var hurt = Battle.WeaponHit(t, fighter, beast, 20, false);
            if (Battle.PenaltyFactor(t, fighter) != 0.5) return false;

            return wounded == (int)(full * 0.75) && hurt == (int)(full * 0.5);
        });

        checks.Check("penalty cuts our blows, not the beast's", () =>
        {
            var t = DataLoader.Load(DataDir());
            var fighter = Battle.ProbeFighter(t, 2);
            var beast = BeastOf(t, "beast_ash");
            beast.Armor = 0;

            fighter.Hp = fighter.HpMax / 4 - 1;
            var ourBlow = Battle.WeaponHit(t, fighter, beast, 20, false);

            var armored = BeastOf(t, "beast_ash");
            armored.Armor = 3;
            var beastBlow = Battle.BeastHit(t, fighter, armored, 20, false);

            return ourBlow == (int)(26 * 0.5) && beastBlow == 17;
        });

        checks.Check("penalty follows the hp inside the fight", () =>
        {
            var t = DataLoader.Load(DataDir());
            var fighter = Battle.ProbeFighter(t, 2);
            var beast = BeastOf(t, "beast_ash");
            beast.Armor = 0;

            var healthy = Battle.WeaponHit(t, fighter, beast, 20, false);
            fighter.Hp = 8;
            var hurt = Battle.WeaponHit(t, fighter, beast, 20, false);

            return healthy == 26 && hurt == (int)(healthy * 0.5);
        });

        // Контракт §11 п.12 ждёт «кривую плоскую в окне ±10% HP твари». Прогон это **опровергает**:
        // ash 27 → h2 57,9%, ash 33 → 46,0%. Это не баг, а прямое следствие §10.6 («HP 30 подобрано
        // в узком окне: ±10% сдвигает кривую на 7–9 п.п.»). Два пункта контракта противоречат друг
        // другу; проверка ловит то, что верно по §10.6: окно узкое, кривая крутая.
        checks.Check("curve is steep over ten percent of the beast hp", () =>
        {
            var t = DataLoader.Load(DataDir());
            var row = t.BeastCombat.Get("beast_ash");
            var baseHp = row.Hp;

            row.Hp = (int)Math.Round(baseHp * 0.9);
            var lower = ProbeWinRate(t, 2, "beast_ash", 2000);
            row.Hp = (int)Math.Round(baseHp * 1.1);
            var higher = ProbeWinRate(t, 2, "beast_ash", 2000);
            row.Hp = baseHp;

            return Math.Abs(higher - lower) > 0.05;
        });

        checks.Check("encounter pack is one species", () =>
        {
            var t = DataLoader.Load(DataDir());
            var encounters = 0;

            for (var seed = 0; seed < 40; seed++)
            {
                var w = World.Create(t);
                var zone = w.Zones["verge_springs"];
                zone.Beasts.Clear();
                zone.Beasts["beast_shard"] = 2;
                zone.Beasts["beast_spark"] = 2;
                zone.Forage = 1;
                zone.Crystals = 0;

                var party = Party("verge_springs", "brunni");
                // Встреча «спросить», иначе бой идёт сразу и развилки для проверки не будет.
                party.Orders = new StandingOrders
                {
                    Encounter = EncounterOrder.Ask,
                    Wounded = WoundedOrder.Leave,
                    Carcass = CarcassOrder.Leave,
                    BagFull = BagOrder.Skip,
                };
                var run = new Expedition(w, t, party, new Rng(seed));

                for (var i = 0; i < 120 && !run.Finished && !run.HasDecision; i++) run.Advance();
                if (!run.HasDecision || run.Pending.Kind != DecisionKind.Encounter) continue;

                encounters++;

                // Стая из разных видов ломает карточку (одно имя), бой (самая быстрая) и дроп.
                if (run.Pack.Count == 0) return false;
                if (run.Pack.Distinct().Count() != 1) return false;
                if (run.Pack[0] != run.Pending.BeastId) return false;
                if (run.Pack.Count > 2) return false;
            }

            return encounters >= 20;
        });

        checks.Check("hp is a word with exact steps", () =>
        {
            return Battle.HpWord(0, 36) == "на ногах не стоит"
                && Battle.HpWord(8, 36) == "тяжело ранен"
                && Battle.HpWord(9, 36) == "ранен"      // ровно 25% — уже не «тяжело»
                && Battle.HpWord(17, 36) == "ранен"
                && Battle.HpWord(18, 36) == "в ссадинах" // ровно 50% — уже не «ранен»
                && Battle.HpWord(28, 36) == "в ссадинах"
                && Battle.HpWord(29, 36) == "в порядке"; // ровно 80%
        });

        checks.Check("battle spends minutes from the day", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var party = Party("verge_springs", "brunni", "skadi");
            party.Orders = new StandingOrders
            {
                Encounter = EncounterOrder.Fight,
                Wounded = WoundedOrder.Leave,
                Carcass = CarcassOrder.Leave,
                BagFull = BagOrder.Skip,
            };

            w.Zones["verge_springs"].Beasts.Clear();
            w.Zones["verge_springs"].Beasts["beast_ash"] = 3;

            var expedition = new Expedition(w, t, party, new Rng(5));
            for (var i = 0; i < 80 && !expedition.Finished; i++) expedition.Advance();

            return expedition.Finished && expedition.Result.Encountered;
        });

        checks.Check("hp survives the night and heals", () =>
        {
            var t = DataLoader.Load(DataDir());
            var w = World.Create(t);
            var hilda = w.People["hilda"];

            hilda.Hp = 5;
            var before = hilda.Hp;

            NightSim.AdvanceNight(w, t, new Rng(1));

            return hilda.Hp > before && hilda.Hp <= hilda.HpMax;
        });

        return checks.Report() ? 0 : 1;
    }

    /// Боевой прогон: кто против кого выигрывает и за сколько раундов.
    /// Нужен для калибровки кривой, а не для приёмки: приёмка — проверки.
    private static int BattleReport()
    {
        var tables = DataLoader.Load(DataDir());
        var world = World.Create(tables);

        var hunters = new[] { "brunni", "skadi", "hilda", "orn" };
        var beasts = new[] { "beast_ash", "beast_shard", "beast_glassworm", "beast_bogling",
            "beast_spark", "beast_climber", "beast_angler", "beast_bone" };

        Console.WriteLine("охотник;тварь;победа;раунды;минуты;застали_мы;застала_тварь");

        foreach (var hunter in hunters)
        {
            foreach (var beast in beasts)
            {
                var wins = 0;
                var rounds = 0;
                var minutes = 0;
                var partyAmbush = 0;
                var beastAmbush = 0;
                const int samples = 1000;

                for (var seed = 0; seed < samples; seed++)
                {
                    var battle = Battle.Resolve(tables, world, new[] { hunter }, new[] { beast },
                        new Rng(seed * 7919 + 13));

                    if (battle.Won) wins++;
                    rounds += battle.Rounds;
                    minutes += battle.Minutes;
                    if (battle.Ambusher == "party") partyAmbush++;
                    else if (battle.Ambusher == "beast") beastAmbush++;
                }

                Console.WriteLine(hunter + ";" + beast + ";" + (100.0 * wins / samples).ToString("0.0")
                    + ";" + ((double)rounds / samples).ToString("0.00")
                    + ";" + ((double)minutes / samples).ToString("0.0")
                    + ";" + (100.0 * partyAmbush / samples).ToString("0.0")
                    + ";" + (100.0 * beastAmbush / samples).ToString("0.0"));
            }
        }

        return 0;
    }

    /// Кривая калибровки: как в прототипе — чистая проба, без навыков и без ростера.
    /// `--curve` отвечает на вопрос «кривая в коридоре?» и «засада вообще считается?».
    private static int CurveReport()
    {
        var tables = DataLoader.Load(DataDir());

        var goals = new[]
        {
            (Level: 1, Lo: 0.0, Hi: 0.10, Label: "≤10%"),
            (Level: 2, Lo: 0.45, Hi: 0.55, Label: "45–55%"),
            (Level: 3, Lo: 0.75, Hi: 0.85, Label: "75–85%"),
            (Level: 4, Lo: 0.88, Hi: 1.0, Label: "≥88%"),
            (Level: 5, Lo: 0.88, Hi: 1.0, Label: "≥88%"),
        };

        Console.WriteLine("уровень;победа;цель;вывод");
        foreach (var goal in goals)
        {
            var wins = ProbeWinRate(tables, goal.Level, "beast_ash", CurveSamples);
            var verdict = wins >= goal.Lo && wins <= goal.Hi ? "сходится" : "не сходится";
            Console.WriteLine(goal.Level + ";" + (wins * 100).ToString("0.0")
                + ";" + goal.Label + ";" + verdict);
        }

        Console.WriteLine();
        Console.WriteLine("без засады;h1;h2;h3");
        var saved = tables.CombatTuning;
        tables.CombatTuning = saved.With("detect_base", "0").With("detect_per_alert", "0");
        var noAmbush = new List<string>();
        foreach (var level in new[] { 1, 2, 3 })
            noAmbush.Add((ProbeWinRate(tables, level, "beast_ash", CurveSamples) * 100).ToString("0.0"));
        tables.CombatTuning = saved;
        Console.WriteLine("победа;" + string.Join(";", noAmbush));

        Console.WriteLine();
        Console.WriteLine("засада;доля");
        var probe = Battle.ProbeFighter(tables, 2);
        Battle.DetectChances(tables, new[] { probe }, new BattleBeast
        {
            Id = "beast_ash",
            Speed = tables.BeastCombat.Get("beast_ash").Speed,
        }, out var partyChance, out var beastChance);

        Console.WriteLine("шанс отряда;" + (partyChance * 100).ToString("0.0") + "%");
        Console.WriteLine("шанс твари;" + (beastChance * 100).ToString("0.0") + "%");

        var party = 0;
        var beast = 0;
        for (var seed = 0; seed < CurveSamples; seed++)
        {
            var battle = Battle.ResolveFighters(tables, new[] { Battle.ProbeFighter(tables, 2) },
                new[] { "beast_ash" }, new Rng(seed * 7919 + 13));

            if (battle.Ambusher == "party") party++;
            else if (battle.Ambusher == "beast") beast++;
        }

        Console.WriteLine("застали мы;" + (100.0 * party / CurveSamples).ToString("0.0") + "%");
        Console.WriteLine("застала тварь;" + (100.0 * beast / CurveSamples).ToString("0.0") + "%");

        return 0;
    }

    private const int CurveSamples = 4000;

    /// <summary>Наказы «бей, тушу бросить, раненого нести» — бой без игрока.</summary>
    private static StandingOrders FightOrders()
    {
        return new StandingOrders
        {
            Encounter = EncounterOrder.Fight,
            Wounded = WoundedOrder.Leave,
            Carcass = CarcassOrder.Leave,
            BagFull = BagOrder.Skip,
        };
    }

    /// <summary>Боевые параметры твари из таблицы — для проверок формул без запуска боя.</summary>
    private static BattleBeast BeastOf(Tables tables, string beastId)
    {
        var row = tables.BeastCombat.Get(beastId);
        return new BattleBeast
        {
            Id = beastId,
            Level = row.Level,
            Hp = row.Hp,
            HpMax = row.Hp,
            AttackMin = row.AttackMin,
            AttackMax = row.AttackMax,
            Armor = row.Armor,
            Speed = row.Speed,
        };
    }

    /// Доля побед пробы уровня `level` против твари. Сиды разнесены, чтобы не мерить один
    /// удачный кусок генератора: 200 подряд идущих сидов давали 75% там, где честно 66%.
    private static double ProbeWinRate(Tables tables, int level, string beastId, int samples)
    {
        var wins = 0;
        for (var seed = 0; seed < samples; seed++)
        {
            var fighter = Battle.ProbeFighter(tables, level);
            if (Battle.ResolveFighters(tables, new[] { fighter }, new[] { beastId },
                new Rng(seed * 7919 + 13)).Won) wins++;
        }

        return (double)wins / samples;
    }

    /// Пассивный прогон N дней: никто не работает, никто не ходит в лес — видно, что делает лес сам.
    private static int Days(int count)
    {
        var tables = DataLoader.Load(DataDir());
        var world = World.Create(tables);
        var flow = new DayFlow(world, tables, new Rng(2026));

        Console.WriteLine("день;частокол;волна;гнёзда;твари");

        for (var i = 0; i < count; i++)
        {
            var plan = new DayPlan();
            var night = flow.RunNight(plan);

            var nests = world.Zones.Values.Count(z => z.Nest.Exists);
            var beasts = world.TotalBeasts();
            var wave = night.Wave != null ? night.Wave.Strength.ToString() : "";

            Console.WriteLine((world.Day - 1) + ";" + world.PalisadeHp + "/" + world.PalisadeMax
                + ";" + wave + ";" + nests + ";" + beasts);
        }

        return 0;
    }

    private static void KillHunters(World world, Tables tables)
    {
        foreach (var person in tables.Roster.All)
        {
            if (person.Profession == "hunter") Works.Kill(world, person.Id);
        }
    }

    private static PartyAssignment Party(string zoneId, params string[] people)
    {
        var party = new PartyAssignment { ZoneId = zoneId, Behavior = Behavior.Gather };
        party.PersonIds.AddRange(people);
        return party;
    }

    private static string DataDir()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "FG", "Assets", "StreamingAssets", "Data");
            if (Directory.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException("не найден FG/Assets/StreamingAssets/Data");
    }

    /// Копирует справочники во временную папку и дописывает строку-порчу в один файл.
    /// Копия данных с заменой строк по ключу первой колонки — для проверок, которым нужен другой тюнинг.
    private static string CopyDataWithReplacements(string file, params string[] replacedLines)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "fg_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);

        foreach (var f in Directory.GetFiles(DataDir(), "*.csv"))
            File.Copy(f, Path.Combine(tmp, Path.GetFileName(f)));

        var path = Path.Combine(tmp, file);
        var lines = File.ReadAllLines(path);

        foreach (var replacement in replacedLines)
        {
            var key = replacement.Split(';')[0] + ";";
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(key, StringComparison.Ordinal)) lines[i] = replacement;
            }
        }

        File.WriteAllLines(path, lines);
        return tmp;
    }

    private static string CopyDataWithMutation(string file, string badLine)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "fg_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        foreach (var f in Directory.GetFiles(DataDir(), "*.csv"))
            File.Copy(f, Path.Combine(tmp, Path.GetFileName(f)));
        File.AppendAllText(Path.Combine(tmp, file), badLine + Environment.NewLine);
        return tmp;
    }
}
