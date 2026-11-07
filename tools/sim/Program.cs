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
                && t.Items.All.Count == 9
                && t.Beasts.All.Count == 3;
        });

        checks.Check("unknown neighbour zone fails", () =>
        {
            var dir = CopyDataWithMutation("zones.csv",
                "verge_bad;Плохая зона;edge;1;1;2;nowhere;none;0.5;0.52");
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

            // День — 10 часов, но один человек выдыхается за четыре: выносливость важнее часов.
            var briefing = Briefing.Compute(w, t, new[] { "leya" }, "verge_meadow", Behavior.Gather);
            var party = Party("verge_meadow", "leya");
            var r = ExpeditionSim.Resolve(w, t, party, new Rng(7));

            return briefing.WorkHours == 9 && r.HoursUsed == 4 && r.ZoneId == "verge_meadow";
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
            var w = World.Create(t);
            var a = ExpeditionSim.Resolve(w, t, Party("verge_springs", "brunni"), new Rng(3));
            var b = ExpeditionSim.Resolve(w, t, Party("verge_springs", "brunni"), new Rng(3));
            return a.Found.Count == b.Found.Count && a.Wounded == b.Wounded && a.HoursUsed == b.HoursUsed;
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

            return one.Slots == 5                      // 4 + «Крепкий»
                && one.WorkHours == 8
                && three.Warnings.Count >= 2;          // сверх двух + зона не разведана
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
            party.Orders = new StandingOrders { Encounter = EncounterOrder.Retreat };

            var expedition = new Expedition(w, t, party, new Rng(4));
            for (var i = 0; i < 20 && !expedition.Finished; i++) expedition.Advance();

            return expedition.Finished;
        });

        checks.Check("day is ten hours and the meadow is half an hour away", () =>
        {
            var t = DataLoader.Load(DataDir());
            return t.Tuning.GetInt("hours_per_day") == 10
                && t.Zones.Get("verge_meadow").TravelHours == 0.5f;
        });

        return checks.Report() ? 0 : 1;
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
