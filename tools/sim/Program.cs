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

        return checks.Report() ? 0 : 1;
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
