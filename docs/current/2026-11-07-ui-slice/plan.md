# ForestGuild — шаг 1: каркас, карта, первый день

> **Для агентов:** ОБЯЗАТЕЛЬНЫЙ ПОД-НАВЫК: `subagent-driven-development` (рекомендуется) или `executing-plans` — исполнять этот план задача за задачей. Шаги отмечены чекбоксами (`- [ ]`).

**Цель:** играбельный первый день: карта леса из `zones.csv`, ростер из `roster.csv`, назначение отрядов, кнопка «конец дня» → авторезолв вылазки по часам → ночная жизнь леса → отчёт.

**Архитектура:** данные в CSV (`Assets/StreamingAssets/Data`), чистое ядро без `UnityEngine` (`Assets/Scripts/Core`), UI Toolkit поверх (`Assets/Scripts/Game` + UXML/USS), консольный прогон `tools/sim` подключает те же файлы ядра через `<Compile Include>`.

**Стек:** Unity 6000.6.6f1, URP (шаблон `com.unity.template.universal-2d`), UI Toolkit, .NET 10 консоль.

**Спека:** `docs/design.md`, `docs/architecture.md`, `docs/decisions.md` (ADR-0001, ADR-0002).

## Global Constraints

- Unity **6000.6.6f1**, URP. Новый проект в `F:\WORK\ForestGuild` (репозиторий уже существует — проект создаётся во временной папке и переносится сюда).
- Сцена меняется **только через живой редактор** (`unity_status` → `unity_command`/`unity_eval`), не правкой `.unity` на диске.
- `Assets/Scripts/Core/**` **не содержит** `using UnityEngine`. Проверяется тестом.
- `asmdef` не заводить, весь код — `Assembly-CSharp`.
- `id` в таблицах — ASCII `snake_case`; русские названия — в колонке `name`.
- CSV: UTF-8, `;`, десятичная точка; `#` — комментарий; списки — `a:1|b:2` (единственная мини-грамматика).
- Валидация таблиц падает (`DataException` с `файл:строка:колонка`), а не подставляет дефолты.
- Ядро детерминировано: случайность только через переданный `Rng(seed)`; никаких `DateTime.Now`, `UnityEngine.Random`.
- В шаге 1 **нет пауз** в вылазке и нет туши/разделки — это шаг 2. Нет балансовых прогонов на 21 день.
- Деньги/караван/станции в шаге 1 не работают; таблицы станций и рецептов существуют ради проверки формата.

## Поправка от 2026-11-07: Unity-проект живёт в `FG/`

После задачи 1 проект перенесён в подпапку: корень репозитория — `F:\WORK\ForestGuild`, Unity — `F:\WORK\ForestGuild\FG`.

- Все пути вида `Assets/...`, `Packages/...`, `ProjectSettings/...` читаются как `FG/Assets/...` и т.д.
- `unity open` / `unity pipeline install` / `unity_command projectPath` — с `F:\WORK\ForestGuild\FG`.
- В `tools/sim/ForestGuild.Sim.csproj` путь к ядру: `..\..\FG\Assets\Scripts\Core\**\*.cs`.
- `DataDir()` в `Program.cs` ищет `FG\Assets\StreamingAssets\Data` — в хелпере заменить `"Assets"` на `"FG", "Assets"`.
- `Application.streamingAssetsPath` в Unity уже указывает внутрь `FG`, менять код `MapScreen` не нужно.

## Структура файлов

```
Assets/
  StreamingAssets/Data/            # 11 CSV (задача 3)
  Scripts/Core/Data/               # DataException, CsvReader, Table<T>, табличные типы, DataLoader
  Scripts/Core/Sim/                # Rng, World, NightSim, ExpeditionSim, DayPlan
  Scripts/Game/                    # MapScreen, MapController, RosterPanel, ExpeditionRunner, DayFlow
  UI/Map.uxml, UI/Map.uss          # интерфейс карты
  Scenes/Main.unity                # создаётся живым редактором
tools/sim/                         # ForestGuild.Sim.csproj, Program.cs
docs/current/2026-11-07-ui-slice/  # этот план
```

---

### Task 1: Проект Unity и перенос в репозиторий

**Files:**
- Create: `F:\WORK\_fg_tmp\ForestGuild\` (временный проект)
- Modify: `F:\WORK\ForestGuild\` (перенос `Assets/`, `Packages/`, `ProjectSettings/`)
- Modify: `.gitignore`

**Interfaces:**
- Produces: проект Unity в корне репозитория; `Assets/`, `Packages/`, `ProjectSettings/`, `Assets/StreamingAssets/Data/`.

- [ ] **Step 1: Создать проект во временной папке**

```powershell
unity projects create ForestGuild --path F:\WORK\_fg_tmp --editor-version 6000.6.6f1 --template com.unity.template.universal-2d --with-pipeline --no-cloud --non-interactive
```

Ожидание: код 0, `F:\WORK\_fg_tmp\ForestGuild\ProjectSettings\ProjectVersion.txt` содержит `6000.6.6f1`.

- [ ] **Step 2: Перенести содержимое в репозиторий**

```powershell
$src="F:\WORK\_fg_tmp\ForestGuild"; $dst="F:\WORK\ForestGuild"
foreach ($d in @("Assets","Packages","ProjectSettings")) { Copy-Item "$src\$d" $dst -Recurse -Force }
Remove-Item "F:\WORK\_fg_tmp" -Recurse -Force
```

- [ ] **Step 3: Проверить, что проект на месте**

Run: `Test-Path F:\WORK\ForestGuild\ProjectSettings\ProjectVersion.txt; Get-ChildItem F:\WORK\ForestGuild\Assets -Name`
Ожидание: `True`, в `Assets` есть `Scenes`, `Settings` (или пусто), нет ошибок.

- [ ] **Step 4: Дополнить `.gitignore`**

Добавить в конец `.gitignore`:

```text
*.meta.tmp
/[Aa]ssets/[Ss]treamingAssets/Data/*.tmp
```

- [ ] **Step 5: Коммит**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat: проект Unity 6000.6.6f1 (URP 2D, UI Toolkit)"
```

---

### Task 2: Поднять редактор и убедиться, что он тикает

**Files:** нет (только состояние редактора).

**Interfaces:**
- Consumes: проект из задачи 1.
- Produces: запущенный редактор с пакетом Pipeline, автотик включён, `unity_status` возвращает `state: ready`.

- [ ] **Step 1: Прочитать скилл**

Прочитать `.dsh/skills/unity-pipeline/SKILL.md` целиком (правило 11 из `AGENTS.md`). Без него `recompile` и тесты висят.

- [ ] **Step 2: Открыть проект**

```powershell
unity open F:\WORK\ForestGuild --args -automated
```

- [ ] **Step 3: Установить пакет Pipeline и включить автотик**

Установка — через CLI:

```powershell
unity pipeline install --project-path F:\WORK\ForestGuild
```

Автотик — через MCP-инструменты, а не через shell: сначала `unity_list_commands`, чтобы взять **точное** имя команды управления автотиком (ожидается `set_autotick`), затем:

```
unity_command(command: "set_autotick", args: ["--enabled", "true"])
```

Ожидание: `success: true`. Если имени нет в списке — не выдумывать: взять из `unity_list_commands` то, что отвечает за тик редактора, и записать фактическое имя в журнал шага.

- [ ] **Step 4: Проверить состояние**

`unity_status` → `success: true`, `state: "ready"`. Если `STATUS_NO_INSTANCES` — подождать и повторить, не перезапуская редактор.

- [ ] **Step 5: Зафиксировать факт в плане**

Дописать в конец файла: `- [x] Редактор запущен, автотик включён (<дата>)`.

---

### Task 3: Консольный harness `tools/sim` c ядром

**Files:**
- Create: `tools/sim/ForestGuild.Sim.csproj`
- Create: `tools/sim/Program.cs`
- Create: `tools/sim/CheckRunner.cs`
- Modify: `.gitignore` (добавить `tools/sim/bin/`, `tools/sim/obj/`)

**Interfaces:**
- Produces: `ForestGuild.Core` код компилируется в .NET; `dotnet run --project tools/sim -- --selfcheck` возвращает 0 при успехе и 1 при провале.

- [ ] **Step 1: Создать csproj, подключающий ядро**

`tools/sim/ForestGuild.Sim.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <LangVersion>9</LangVersion>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
    <Compile Include="..\..\Assets\Scripts\Core\**\*.cs" />
  </ItemGroup>
</Project>
```

`LangVersion 9` — как в Unity 6, чтобы код не скомпилировался в консоли и не упал потом в редакторе.

- [ ] **Step 2: Написать падающий selfcheck**

`tools/sim/Program.cs`:

```csharp
using System;
using ForestGuild.Core.Sim;

internal static class Program
{
    private static int Main(string[] args)
    {
        var failures = new CheckRunner();
        failures.Check("rng is deterministic", () =>
        {
            var a = new Rng(42);
            var b = new Rng(42);
            return a.Next(100) == b.Next(100);
        });
        var ok = failures.Report();
        return ok ? 0 : 1;
    }
}
```

`tools/sim/CheckRunner.cs`:

```csharp
using System;
using System.Collections.Generic;

internal sealed class CheckRunner
{
    private readonly List<string> _failed = new List<string>();
    public void Check(string name, Func<bool> body)
    {
        try { if (!body()) _failed.Add(name + ": вернул false"); }
        catch (Exception e) { _failed.Add(name + ": " + e.GetType().Name + " " + e.Message); }
        Console.WriteLine((_failed.Count == 0 ? "ok   " : "FAIL ") + name);
    }
    public bool Report()
    {
        if (_failed.Count == 0) { Console.WriteLine("все проверки прошли"); return true; }
        foreach (var f in _failed) Console.WriteLine("  ✗ " + f);
        return false;
    }
}
```

Добавить `<Compile Include="CheckRunner.cs" />` в csproj.

- [ ] **Step 3: Запустить — убедиться, что падает**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: ошибка компиляции `The type or namespace name 'Rng' could not be found`.

- [ ] **Step 4: Написать минимальный `Rng`**

`Assets/Scripts/Core/Sim/Rng.cs`:

```csharp
namespace ForestGuild.Core.Sim
{
    /// Детерминированный генератор: один и тот же сид — одна и та же игра.
    public sealed class Rng
    {
        private ulong _state;
        public Rng(int seed) { _state = (ulong)seed * 6364136223846793005UL + 1442695040888963407UL; }
        public ulong NextULong()
        {
            _state ^= _state << 13; _state ^= _state >> 7; _state ^= _state << 17;
            return _state;
        }
        public int Next(int maxExclusive) { return maxExclusive <= 0 ? 0 : (int)(NextULong() % (ulong)maxExclusive); }
        public int Range(int minInclusive, int maxInclusive) { return minInclusive + Next(maxInclusive - minInclusive + 1); }
        public double NextDouble() { return (NextULong() >> 11) * (1.0 / 9007199254740992.0); }
        public bool Chance(double probability) { return NextDouble() < probability; }
    }
}
```

- [ ] **Step 5: Запустить — убедиться, что проходит**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: `ok   rng is deterministic`, `все проверки прошли`, код 0.

- [ ] **Step 6: Коммит**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(sim): консольный harness и детерминированный Rng"
```

---

### Task 4: CSV-справочники MVP

**Files:**
- Create: `Assets/StreamingAssets/Data/tuning.csv`
- Create: `Assets/StreamingAssets/Data/items.csv`
- Create: `Assets/StreamingAssets/Data/zones.csv`
- Create: `Assets/StreamingAssets/Data/zone_resources.csv`
- Create: `Assets/StreamingAssets/Data/beasts.csv`
- Create: `Assets/StreamingAssets/Data/zone_beasts.csv`
- Create: `Assets/StreamingAssets/Data/drops.csv`
- Create: `Assets/StreamingAssets/Data/skills.csv`
- Create: `Assets/StreamingAssets/Data/traits.csv`
- Create: `Assets/StreamingAssets/Data/professions.csv`
- Create: `Assets/StreamingAssets/Data/roster.csv`
- Create: `Assets/StreamingAssets/Data/stations.csv`
- Create: `Assets/StreamingAssets/Data/recipes.csv`
- Create: `Assets/StreamingAssets/Data/recipe_quality.csv`

**Interfaces:**
- Produces: 14 файлов с заголовками, которые ожидает `DataLoader` (задача 5). Колонки — ровно как ниже.

- [ ] **Step 1: `tuning.csv`**

```csv
id;value;note
schema_version;1;версия схемы таблиц
hours_per_day;6;рабочих часов в сутках
slots_per_person;4;слотов груза на человека
stamina_per_person;4;выносливости на человека
encounter_per_hour;0.25;шанс встречи за час на единицу плотности
find_base;0.12;база шанса находки
find_per_skill;0.06;прибавка за уровень навыка
xp_to_nest;5;опыта до гнезда
nest_beasts_cap;2;тварей в гнезде максимум
nest_spawn_days;2;дней на одну тварь в гнезде
migration_per_night;1;миграций в зону за ночь
heart_influx_days;3;дней между притоком из сердца
crystal_life_days;2;дней жизни кристалла на земле
palisade_hp;10;прочность частокола
wave_day;21;день первой волны
xp_per_day;1;опыта за день работы
xp_per_fight;1;бонус опыта за бой
xp_per_depth;1;бонус опыта за пояс
level_xp;10|30|70|150;опыта на переходы 1-2,2-3,3-4,4-5
```

- [ ] **Step 2: `items.csv`**

```csv
id;name;category;slots;value;tags
herb_fire_leaf;Огненная крапива;material;0.5;3;fire
herb_ash_lichen;Пепельный лишай;material;0.5;4;
herb_glass_stem;Стеклянница;material;0.5;3;
ore_obsidian;Обсидиановая руда;material;2;8;heavy
sulfur_nodule;Серный натёк;material;1;5;fire
crystal_fire;Кристалл огня;material;1;25;crystal
moss_common;Обычный мох;material;0.5;1;
bone_plate;Костяная пластина;trophy;1;12;bone
hide_rough;Грубая шкура;trophy;1;6;hide
```

- [ ] **Step 3: `zones.csv` + `zone_resources.csv`**

```csv
id;name;depth;travel_hours;crystals_per_day;forage_per_day;neighbours;env_rule;map_x;map_y
verge_meadow;Опушка;edge;1;1;2;verge_springs|verge_thicket;none;0.50;0.52
verge_springs;Вулканические ключи;edge;1.5;1;2;verge_meadow|verge_marsh|belt_stone;heat;0.28;0.38
verge_marsh;Топь;edge;1.5;1;2;verge_springs|verge_thicket;mud;0.16;0.62
verge_thicket;Чаща;edge;1;1;2;verge_meadow|verge_marsh|belt_stone;thicket;0.74;0.40
belt_stone;Каменный лог;belt;3;2;3;verge_springs|verge_thicket|heart_root;none;0.58;0.20
heart_root;Сердце леса;heart;6;4;4;belt_stone;none;0.86;0.08
```

```csv
zone_id;item_id;abundance
verge_meadow;moss_common;3
verge_meadow;herb_ash_lichen;2
verge_springs;herb_fire_leaf;3
verge_springs;herb_ash_lichen;1
verge_springs;ore_obsidian;2
verge_springs;sulfur_nodule;1
verge_marsh;herb_glass_stem;2
verge_marsh;herb_ash_lichen;1
verge_thicket;moss_common;3
verge_thicket;herb_fire_leaf;1
belt_stone;ore_obsidian;3
belt_stone;sulfur_nodule;2
belt_stone;crystal_fire;1
```

- [ ] **Step 4: `beasts.csv`, `zone_beasts.csv`, `drops.csv`**

```csv
id;name;strength;speed;food_need;nest_capable
beast_spark;Искромёт;2;3;1;1
beast_bone;Костяной бегун;3;2;1;1
beast_ash;Пепельный вор;1;4;1;0
```

```csv
zone_id;beast_id;weight;initial_count
verge_springs;beast_spark;3;1
verge_springs;beast_bone;1;0
verge_marsh;beast_ash;2;0
verge_thicket;beast_bone;2;1
verge_thicket;beast_ash;1;0
belt_stone;beast_bone;2;1
belt_stone;beast_spark;1;1
```

```csv
beast_id;item_id;chance;min_skill
beast_spark;sulfur_nodule;0.5;0
beast_spark;bone_plate;0.2;2
beast_bone;bone_plate;0.8;0
beast_bone;hide_rough;0.4;1
beast_ash;crystal_fire;0.6;0
```

- [ ] **Step 5: `skills.csv`, `traits.csv`, `professions.csv`, `roster.csv`**

```csv
id;name;kind;max_level
gathering;Собирательство;field;5
hunting;Охота;field;5
butchering;Разделка;field;5
smithing;Кузнечное дело;craft;5
alchemy;Алхимия;craft;5
tanning;Кожевенное дело;craft;5
```

```csv
id;name;fight_bonus;find_bonus;butcher_bonus;carry_bonus
brave;Храбрый;1;0;0;0
keen;Зоркий;0;0.05;0;0
steady;Твёрдая рука;0;0;1;0
strong;Крепкий;0;0;0;1
patient;Терпеливый;0;0.03;1;0
```

```csv
id;name;skills_allowed;requires_trait;start_skills
gatherer;Сборщик;gathering|butchering;patient;gathering:2|butchering:1
hunter;Охотник;hunting|butchering;brave;hunting:2|butchering:1
```

```csv
id;name;profession;traits;skills_override
brunni;Брунни;hunter;brave|strong;hunting:4|butchering:3
skadi;Скади;hunter;brave;hunting:3|butchering:2
hilda;Хильда;hunter;brave|keen;hunting:2|butchering:2
orn;Орн;hunter;strong;hunting:2|butchering:1
leya;Лея;gatherer;keen|patient;gathering:3|butchering:2
tove;Тове;gatherer;patient;gathering:2|butchering:2
run;Рун;gatherer;steady;gathering:2|butchering:1
valda;Вальда;gatherer;patient;gathering:1|butchering:0
```

Внимание: **id — только ASCII** (`run`, `valda`), русские имена — в колонке `name`.

- [ ] **Step 6: `stations.csv`, `recipes.csv`, `recipe_quality.csv`**

```csv
id;name;skill_id;people_slots
smithy;Кузня;smithing;1
workshop;Мастерская;alchemy;1
```

```csv
id;station;skill_id;skill_req;hours;inputs;outputs
knife_obsidian;smithy;smithing;2;2;ore_obsidian:1|sulfur_nodule:1;bone_plate:1
salve_ash;workshop;alchemy;1;1;herb_ash_lichen:2;moss_common:1
```

```csv
recipe_id;min_skill;output_id;qty;chance
knife_obsidian;2;bone_plate;1;1.0
knife_obsidian;4;bone_plate;2;0.5
salve_ash;1;moss_common;1;1.0
```

- [ ] **Step 7: Проверить, что файлы читаются и валидны по форме**

```powershell
Get-ChildItem F:\WORK\ForestGuild\Assets\StreamingAssets\Data\*.csv |
  ForEach-Object { "{0}: {1} строк" -f $_.Name, ((Get-Content $_.FullName).Count) }
```

Ожидание: 14 файлов, у каждого минимум 2 строки, нет пустых.

- [ ] **Step 8: Коммит**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(data): 14 CSV-справочника MVP"
```

---

### Task 5: Загрузчик и валидатор таблиц

**Files:**
- Create: `Assets/Scripts/Core/Data/DataException.cs`
- Create: `Assets/Scripts/Core/Data/CsvReader.cs`
- Create: `Assets/Scripts/Core/Data/Table.cs`
- Create: `Assets/Scripts/Core/Data/Rows.cs` (типы строк)
- Create: `Assets/Scripts/Core/Data/DataLoader.cs`
- Modify: `tools/sim/Program.cs` (добавить проверки загрузки реальных таблиц)

**Interfaces:**
- Produces:
  - `Tables DataLoader.Load(string dataDir)` — читает все 14 файлов, валидирует, бросает `DataException`.
  - `Table<T>.Get(string id)`, `.All`, `.TryGet(string, out T)`.
  - `Tables.Tuning`, `Tables.Zones`, `Tables.Items`, `Tables.ZoneResources`, `Tables.Beasts`, `Tables.ZoneBeasts`, `Tables.Drops`, `Tables.Skills`, `Tables.Traits`, `Tables.Professions`, `Tables.Roster`, `Tables.Stations`, `Tables.Recipes`, `Tables.RecipeQuality`.
  - `Tuning.GetFloat(string key)`, `GetInt(string key)`, `GetIntList(string key)`.

- [ ] **Step 1: Написать падающую проверку**

В `Program.cs` перед `var ok = failures.Report();` добавить:

```csharp
failures.Check("tables load", () =>
{
    var t = ForestGuild.Core.Data.DataLoader.Load(DataDir());
    return t.Zones.All.Count == 6 && t.Roster.All.Count == 8;
});
failures.Check("unknown zone in neighbours fails", () =>
{
    var dir = CopyDataWithMutation("zones.csv", "verge_meadow;Опушка;edge;1;1;2;nowhere;none;0.5;0.52");
    try { ForestGuild.Core.Data.DataLoader.Load(dir); return false; }
    catch (ForestGuild.Core.Data.DataException) { return true; }
});
```

и хелперы в конце `Program`:

```csharp
private static string DataDir()
{
    var dir = AppContext.BaseDirectory;
    while (dir != null && !Directory.Exists(Path.Combine(dir, "Assets", "StreamingAssets", "Data")))
        dir = Path.GetDirectoryName(dir);
    return Path.Combine(dir ?? ".", "Assets", "StreamingAssets", "Data");
}

private static string CopyDataWithMutation(string file, string replacementLine)
{
    var tmp = Path.Combine(Path.GetTempPath(), "fg_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tmp);
    foreach (var f in Directory.GetFiles(DataDir(), "*.csv"))
        File.Copy(f, Path.Combine(tmp, Path.GetFileName(f)));
    File.WriteAllText(Path.Combine(tmp, file), replacementLine + Environment.NewLine);
    return tmp;
}
```

(`using System.IO; using System.Linq; using System.Collections.Generic;` — добавить.)

- [ ] **Step 2: Запустить — убедиться, что падает**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: `FAIL tables load ... 'DataLoader' could not be found` (ошибка компиляции).

- [ ] **Step 3: Написать `DataException`, `CsvReader`, `Table<T>`**

`DataException.cs`:

```csharp
using System;

namespace ForestGuild.Core.Data
{
    public sealed class DataException : Exception
    {
        public DataException(string where, string message) : base(where + ": " + message) { }
    }
}
```

`CsvReader.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ForestGuild.Core.Data
{
    /// Читает CSV: ';', UTF-8, '#' — комментарий, первая непустая строка — заголовок.
    public static class CsvReader
    {
        public static List<Dictionary<string, string>> Read(string path)
        {
            var rows = new List<Dictionary<string, string>>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            string[] header = null;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var cells = line.Split(';');
                if (header == null)
                {
                    header = new string[cells.Length];
                    for (var c = 0; c < cells.Length; c++) header[c] = cells[c].Trim();
                    continue;
                }
                if (cells.Length != header.Length)
                    throw new DataException(Path.GetFileName(path) + ":" + (i + 1),
                        "колонок " + cells.Length + ", ожидалось " + header.Length);
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var c = 0; c < header.Length; c++) row[header[c]] = cells[c].Trim();
                rows.Add(row);
            }
            if (header == null) throw new DataException(Path.GetFileName(path), "нет заголовка");
            return rows;
        }
    }
}
```

`Table.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace ForestGuild.Core.Data
{
    public sealed class Table<T>
    {
        private readonly Dictionary<string, T> _byId;
        public IReadOnlyList<T> All { get; }
        public Func<T, string> IdOf { get; }

        public Table(IReadOnlyList<T> all, Func<T, string> idOf)
        {
            All = all; IdOf = idOf;
            _byId = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var i = 0; i < all.Count; i++)
            {
                var id = idOf(all[i]);
                if (_byId.ContainsKey(id)) throw new DataException("table", "дубликат id: " + id);
                _byId.Add(id, all[i]);
            }
        }

        public T Get(string id)
        {
            if (!_byId.TryGetValue(id, out var value)) throw new DataException("table", "нет id: " + id);
            return value;
        }

        public bool TryGet(string id, out T value) { return _byId.TryGetValue(id, out value); }
    }
}
```

- [ ] **Step 4: Написать типы строк**

`Rows.cs` — все типы публичные, поля-свойства только для чтения:

```csharp
using System.Collections.Generic;

namespace ForestGuild.Core.Data
{
    public sealed class Zone
    {
        public string Id, Name, Depth, EnvRule;
        public float TravelHours, MapX, MapY;
        public int CrystalsPerDay, ForagePerDay;
        public string[] Neighbours;
    }

    public sealed class Item { public string Id, Name, Category, Tags; public float Slots; public int Value; }

    public sealed class ZoneResource { public string ZoneId, ItemId; public int Abundance; }

    public sealed class Beast { public string Id, Name; public int Strength, Speed, FoodNeed; public bool NestCapable; }

    public sealed class ZoneBeast { public string ZoneId, BeastId; public int Weight, InitialCount; }

    public sealed class Drop { public string BeastId, ItemId; public double Chance; public int MinSkill; }

    public sealed class Skill { public string Id, Name, Kind; public int MaxLevel; }

    public sealed class Trait
    {
        public string Id, Name;
        public int FightBonus, ButcherBonus, CarryBonus;
        public double FindBonus;
    }

    public sealed class Profession
    {
        public string Id, Name, RequiresTrait;
        public string[] SkillsAllowed;
        public Dictionary<string, int> StartSkills;
    }

    public sealed class Person
    {
        public string Id, Name, Profession;
        public string[] Traits;
        public Dictionary<string, int> SkillsOverride;
    }

    public sealed class Station { public string Id, Name, SkillId; public int PeopleSlots; }

    public sealed class Recipe
    {
        public string Id, Station, SkillId;
        public int SkillReq, Hours;
        public Dictionary<string, int> Inputs, Outputs;
    }

    public sealed class RecipeQuality { public string RecipeId, OutputId; public int MinSkill, Qty; public double Chance; }
}
```

- [ ] **Step 5: Написать `DataLoader`**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ForestGuild.Core.Data
{
    public sealed class Tables
    {
        public Tuning Tuning;
        public Table<Zone> Zones;
        public Table<Item> Items;
        public Table<ZoneResource> ZoneResources;
        public Table<Beast> Beasts;
        public Table<ZoneBeast> ZoneBeasts;
        public Table<Drop> Drops;
        public Table<Skill> Skills;
        public Table<Trait> Traits;
        public Table<Profession> Professions;
        public Table<Person> Roster;
        public Table<Station> Stations;
        public Table<Recipe> Recipes;
        public Table<RecipeQuality> RecipeQuality;
    }

    public static class DataLoader
    {
        public const int SchemaVersion = 1;

        public static Tables Load(string dir)
        {
            var t = new Tables();
            t.Tuning = ReadTuning(Path.Combine(dir, "tuning.csv"));
            if (t.Tuning.GetInt("schema_version") != SchemaVersion)
                throw new DataException("tuning.csv", "schema_version " + t.Tuning.GetInt("schema_version") + ", ожидалось " + SchemaVersion);

            t.Items = Read(dir, "items.csv", (r, p) => new Item
            {
                Id = Get(r, p, "id"), Name = Get(r, p, "name"), Category = Get(r, p, "category"),
                Slots = F(r, p, "slots"), Value = I(r, p, "value"), Tags = Opt(r, "tags"),
            }, x => x.Id);

            t.Zones = Read(dir, "zones.csv", (r, p) => new Zone
            {
                Id = Get(r, p, "id"), Name = Get(r, p, "name"), Depth = Get(r, p, "depth"),
                TravelHours = F(r, p, "travel_hours"), CrystalsPerDay = I(r, p, "crystals_per_day"),
                ForagePerDay = I(r, p, "forage_per_day"), Neighbours = SplitList(Get(r, p, "neighbours")),
                EnvRule = Get(r, p, "env_rule"), MapX = F(r, p, "map_x"), MapY = F(r, p, "map_y"),
            }, x => x.Id);

            t.Skills = Read(dir, "skills.csv", (r, p) => new Skill
            {
                Id = Get(r, p, "id"), Name = Get(r, p, "name"), Kind = Get(r, p, "kind"), MaxLevel = I(r, p, "max_level"),
            }, x => x.Id);

            t.Traits = Read(dir, "traits.csv", (r, p) => new Trait
            {
                Id = Get(r, p, "id"), Name = Get(r, p, "name"), FightBonus = I(r, p, "fight_bonus"),
                FindBonus = F(r, p, "find_bonus"), ButcherBonus = I(r, p, "butcher_bonus"), CarryBonus = I(r, p, "carry_bonus"),
            }, x => x.Id);

            t.Beasts = Read(dir, "beasts.csv", (r, p) => new Beast
            {
                Id = Get(r, p, "id"), Name = Get(r, p, "name"), Strength = I(r, p, "strength"), Speed = I(r, p, "speed"),
                FoodNeed = I(r, p, "food_need"), NestCapable = Get(r, p, "nest_capable") == "1",
            }, x => x.Id);

            t.Professions = Read(dir, "professions.csv", (r, p) => new Profession
            {
                Id = Get(r, p, "id"), Name = Get(r, p, "name"), SkillsAllowed = SplitList(Get(r, p, "skills_allowed")),
                RequiresTrait = Opt(r, "requires_trait"), StartSkills = ParsePairs(Get(r, p, "start_skills")),
            }, x => x.Id);

            t.Roster = Read(dir, "roster.csv", (r, p) => new Person
            {
                Id = Get(r, p, "id"), Name = Get(r, p, "name"), Profession = Get(r, p, "profession"),
                Traits = SplitList(Opt(r, "traits")), SkillsOverride = ParsePairs(Opt(r, "skills_override")),
            }, x => x.Id);

            t.Stations = Read(dir, "stations.csv", (r, p) => new Station
            {
                Id = Get(r, p, "id"), Name = Get(r, p, "name"), SkillId = Get(r, p, "skill_id"), PeopleSlots = I(r, p, "people_slots"),
            }, x => x.Id);

            t.ZoneResources = Read(dir, "zone_resources.csv", (r, p) => new ZoneResource
            {
                ZoneId = Get(r, p, "zone_id"), ItemId = Get(r, p, "item_id"), Abundance = I(r, p, "abundance"),
            }, x => x.ZoneId + "|" + x.ItemId);

            t.ZoneBeasts = Read(dir, "zone_beasts.csv", (r, p) => new ZoneBeast
            {
                ZoneId = Get(r, p, "zone_id"), BeastId = Get(r, p, "beast_id"),
                Weight = I(r, p, "weight"), InitialCount = I(r, p, "initial_count"),
            }, x => x.ZoneId + "|" + x.BeastId);

            t.Drops = Read(dir, "drops.csv", (r, p) => new Drop
            {
                BeastId = Get(r, p, "beast_id"), ItemId = Get(r, p, "item_id"),
                Chance = F(r, p, "chance"), MinSkill = I(r, p, "min_skill"),
            }, x => x.BeastId + "|" + x.ItemId);

            t.Recipes = Read(dir, "recipes.csv", (r, p) => new Recipe
            {
                Id = Get(r, p, "id"), Station = Get(r, p, "station"), SkillId = Get(r, p, "skill_id"),
                SkillReq = I(r, p, "skill_req"), Hours = I(r, p, "hours"),
                Inputs = ParsePairs(Get(r, p, "inputs")), Outputs = ParsePairs(Get(r, p, "outputs")),
            }, x => x.Id);

            t.RecipeQuality = Read(dir, "recipe_quality.csv", (r, p) => new RecipeQuality
            {
                RecipeId = Get(r, p, "recipe_id"), MinSkill = I(r, p, "min_skill"), OutputId = Get(r, p, "output_id"),
                Qty = I(r, p, "qty"), Chance = F(r, p, "chance"),
            }, x => x.RecipeId + "|" + x.MinSkill);

            foreach (var z in t.Zones.All)
                foreach (var nb in z.Neighbours)
                    if (!t.Zones.TryGet(nb, out _))
                        throw new DataException("zones.csv", "зона " + z.Id + " ссылается на неизвестного соседа " + nb);

            return t;
        }

        private static Table<T> Read<T>(string dir, string file,
            Func<Dictionary<string, string>, string, T> map, Func<T, string> idOf)
        {
            var path = Path.Combine(dir, file);
            var list = CsvReader.Read(path).Select(r => map(r, path)).ToList();
            return new Table<T>(list, idOf);
        }

        private static Dictionary<string, int> ParsePairs(string value)
        {
            var result = new Dictionary<string, int>();
            if (string.IsNullOrWhiteSpace(value)) return result;
            foreach (var pair in SplitList(value))
            {
                var parts = pair.Split(':');
                if (parts.Length != 2)
                    throw new DataException("table", "ожидалось id:число, получено " + pair);
                result[parts[0].Trim()] = int.Parse(parts[1].Trim(),
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            return result;
        }

        private static Tuning ReadTuning(string path)
        {
            var raw = new Dictionary<string, string>();
            foreach (var row in CsvReader.Read(path)) raw[Get(row, path, "id")] = Opt(row, "value");
            return new Tuning(raw);
        }

        private static string Opt(Dictionary<string, string> r, string col) =>
            r.TryGetValue(col, out var v) ? v : "";
    }
}
```

Хелперы `Get`, `I`, `F`, `SplitList` (список `a|b`, пары `a:1|b:2`):

```csharp
private static string Get(Dictionary<string, string> r, string path, string col)
{
    if (!r.TryGetValue(col, out var v) || string.IsNullOrEmpty(v))
        throw new DataException(Path.GetFileName(path), "пустая колонка " + col + " в строке " + string.Join(";", r.Values));
    return v;
}
private static int I(Dictionary<string, string> r, string path, string col)
{
    if (!int.TryParse(Get(r, path, col), System.Globalization.NumberStyles.Integer,
        System.Globalization.CultureInfo.InvariantCulture, out var v))
        throw new DataException(Path.GetFileName(path), "не число: " + col + "=" + Get(r, path, col));
    return v;
}
private static float F(Dictionary<string, string> r, string path, string col)
{
    if (!float.TryParse(Get(r, path, col), System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture, out var v))
        throw new DataException(Path.GetFileName(path), "не число: " + col + "=" + Get(r, path, col));
    return v;
}
private static string[] SplitList(string value) =>
    value.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
```

`Tuning`:

```csharp
public sealed class Tuning
{
    private readonly Dictionary<string, string> _raw;
    public Tuning(Dictionary<string, string> raw) { _raw = raw; }
    public string Get(string key) => _raw.TryGetValue(key, out var v) ? v : throw new DataException("tuning.csv", "нет ключа " + key);
    public int GetInt(string key) => int.Parse(Get(key), System.Globalization.CultureInfo.InvariantCulture);
    public float GetFloat(string key) => float.Parse(Get(key), System.Globalization.CultureInfo.InvariantCulture);
    public int[] GetIntList(string key) => Get(key).Split('|').Select(s => int.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
}
```

- [ ] **Step 6: Добавить проверки внешних ключей**

В `Load`, после чтения всех таблиц:

```csharp
RequireRefs(t.ZoneResources.All.Select(x => x.ZoneId), t.Zones, "zone_resources.csv", "zone_id");
RequireRefs(t.ZoneResources.All.Select(x => x.ItemId), t.Items, "zone_resources.csv", "item_id");
RequireRefs(t.ZoneBeasts.All.Select(x => x.ZoneId), t.Zones, "zone_beasts.csv", "zone_id");
RequireRefs(t.ZoneBeasts.All.Select(x => x.BeastId), t.Beasts, "zone_beasts.csv", "beast_id");
RequireRefs(t.Drops.All.Select(x => x.BeastId), t.Beasts, "drops.csv", "beast_id");
RequireRefs(t.Drops.All.Select(x => x.ItemId), t.Items, "drops.csv", "item_id");
RequireRefs(t.Roster.All.Select(x => x.Profession), t.Professions, "roster.csv", "profession");
RequireRefs(t.Roster.All.SelectMany(x => x.Traits), t.Traits, "roster.csv", "traits");
RequireRefs(t.Professions.All.Where(x => !string.IsNullOrEmpty(x.RequiresTrait)).Select(x => x.RequiresTrait), t.Traits, "professions.csv", "requires_trait");
RequireRefs(t.Professions.All.SelectMany(x => x.SkillsAllowed), t.Skills, "professions.csv", "skills_allowed");
RequireRefs(t.Recipes.All.Select(x => x.Station), t.Stations, "recipes.csv", "station");
RequireRefs(t.Recipes.All.Select(x => x.SkillId), t.Skills, "recipes.csv", "skill_id");
```

`RequireRefs` — обобщённая, без `dynamic`:

```csharp
private static void RequireRefs<T>(IEnumerable<string> ids, Table<T> table, string file, string col)
{
    foreach (var id in ids)
        if (!table.TryGet(id, out _))
            throw new DataException(file, "неизвестная ссылка " + col + "=" + id);
}
```

- [ ] **Step 7: Запустить проверки**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: `ok   tables load`, `ok   unknown zone in neighbours fails`, код 0.

- [ ] **Step 8: Коммит**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(core): загрузчик CSV с валидацией внешних ключей"
```

---

### Task 6: Мир и ночная симуляция

**Files:**
- Create: `Assets/Scripts/Core/Sim/World.cs`
- Create: `Assets/Scripts/Core/Sim/NightSim.cs`
- Modify: `tools/sim/Program.cs` (проверки правил ночи)

**Interfaces:**
- Produces:
  - `World World.Create(Tables tables)` — зоны из `zone_beasts.initial_count`, кристаллы 0, корм 0, `NestXp 0`, `Day 1`.
  - `ZoneState` с полями `ZoneId`, `Beasts` (`Dictionary<string,int>`), `Crystals`, `Forage`, `NestXp`, `Nest` (`NestState`), `Scouted`.
  - `NestState { bool Exists; int Beasts; int DaysSinceSpawn; }`
  - `NightReport NightSim.AdvanceNight(World world, Tables tables, Rng rng)` — обновляет мир, возвращает `NightReport` со `List<NightEvent>` и `List<string> Lines`.
  - `World.BeastCount(string zoneId)`.

- [ ] **Step 1: Написать падающие проверки**

В `Program.cs`:

```csharp
failures.Check("night feeds beasts from forage", () =>
{
    var t = DataLoader.Load(DataDir());
    var w = World.Create(t);
    var z = w.Zones["verge_meadow"];
    z.Beasts["beast_spark"] = 1;
    NightSim.AdvanceNight(w, t, new Rng(1));
    return z.Forage == 1 && z.NestXp == 0;
});
failures.Check("hungry beast migrates out", () =>
{
    var t = DataLoader.Load(DataDir());
    var w = World.Create(t);
    var z = w.Zones["verge_springs"];
    z.Forage = 0; z.Crystals = 0;
    z.Beasts["beast_spark"] = 2;
    NightSim.AdvanceNight(w, t, new Rng(1));
    return w.BeastCount("verge_springs") < 2;
});
failures.Check("crystals eaten give nest xp", () =>
{
    var t = DataLoader.Load(DataDir());
    var w = World.Create(t);
    var z = w.Zones["verge_meadow"];
    z.Beasts["beast_spark"] = 1;
    NightSim.AdvanceNight(w, t, new Rng(1));   // корм есть, кристалл цел
    z.Forage = 0; z.Crystals = 3;
    NightSim.AdvanceNight(w, t, new Rng(1));   // корм кончился — ест кристалл
    return z.NestXp > 0;
});
```

- [ ] **Step 2: Запустить — убедиться, что падает**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: ошибка компиляции `'World' could not be found`.

- [ ] **Step 3: Написать `World`**

```csharp
using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class NestState { public bool Exists; public int Beasts; public int DaysSinceSpawn; }

    public sealed class ZoneState
    {
        public string ZoneId;
        public Dictionary<string, int> Beasts = new Dictionary<string, int>(StringComparer.Ordinal);
        public int Crystals;
        public int Forage;
        public int NestXp;
        public NestState Nest = new NestState();
        public bool Scouted;
        public int AlertDays;
    }

    public sealed class World
    {
        public int Day = 1;
        public readonly Dictionary<string, ZoneState> Zones = new Dictionary<string, ZoneState>(StringComparer.Ordinal);

        public static World Create(Tables tables)
        {
            var w = new World();
            foreach (var z in tables.Zones.All)
                w.Zones[z.Id] = new ZoneState { ZoneId = z.Id, Forage = z.ForagePerDay };
            foreach (var zb in tables.ZoneBeasts.All)
                if (zb.InitialCount > 0) w.Zones[zb.ZoneId].Beasts[zb.BeastId] = zb.InitialCount;
            return w;
        }

        public int BeastCount(string zoneId)
        {
            var sum = 0;
            foreach (var kv in Zones[zoneId].Beasts) sum += kv.Value;
            return sum;
        }
    }
}
```

- [ ] **Step 4: Написать `NightSim`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class NightReport
    {
        public readonly List<string> Lines = new List<string>();
        public void Add(string line) { Lines.Add(line); }
    }

    public static class NightSim
    {
        public static NightReport AdvanceNight(World world, Tables tables, Rng rng)
        {
            var report = new NightReport();
            var t = tables.Tuning;

            // 1. Приход еды.
            foreach (var z in world.Zones.Values)
            {
                var def = tables.Zones.Get(z.ZoneId);
                z.Forage = def.ForagePerDay;
                z.Crystals += def.CrystalsPerDay;
            }

            // 2. Кормление: корм → труп → кристалл (трупов в шаге 1 нет).
            var hungry = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var z in world.Zones.Values)
            {
                var starved = 0;
                foreach (var beastId in z.Beasts.Keys.ToList())
                {
                    var count = z.Beasts[beastId];
                    for (var i = 0; i < count; i++)
                    {
                        if (z.Forage > 0) { z.Forage--; continue; }
                        if (z.Crystals > 0) { z.Crystals--; z.NestXp++; continue; }
                        starved++;
                    }
                }
                if (starved > 0) hungry[z.ZoneId] = starved;
            }

            // 3. Миграция голодных наружу, не больше migration_per_night на зону.
            var limit = t.GetInt("migration_per_night");
            foreach (var pair in hungry)
            {
                var z = world.Zones[pair.Key];
                var outward = OutwardNeighbour(tables, pair.Key);
                if (outward == null) continue;
                var move = Math.Min(Math.Min(pair.Value, limit), world.BeastCount(pair.Key));
                for (var i = 0; i < move; i++) MoveOneBeast(world, pair.Key, outward);
                if (move > 0) report.Add($"{tables.Zones.Get(pair.Key).Name}: {move} твар. уходит в {tables.Zones.Get(outward).Name}");
            }

            // 4. Гнездо из опыта.
            var xpToNest = t.GetInt("xp_to_nest");
            foreach (var z in world.Zones.Values)
            {
                if (z.Nest.Exists || z.NestXp < xpToNest) continue;
                z.NestXp -= xpToNest;
                z.Nest.Exists = true; z.Nest.Beasts = 0; z.Nest.DaysSinceSpawn = 0;
                report.Add($"{tables.Zones.Get(z.ZoneId).Name}: появилось гнездо");
            }

            // 5. Гнездо плодит.
            var spawnDays = t.GetInt("nest_spawn_days");
            var cap = t.GetInt("nest_beasts_cap");
            foreach (var z in world.Zones.Values)
            {
                if (!z.Nest.Exists) continue;
                z.Nest.DaysSinceSpawn++;
                if (z.Nest.DaysSinceSpawn >= spawnDays && z.Nest.Beasts < cap)
                {
                    z.Nest.DaysSinceSpawn = 0; z.Nest.Beasts++;
                    var nestBeast = tables.ZoneBeasts.All.FirstOrDefault(x => x.ZoneId == z.ZoneId && tables.Beasts.Get(x.BeastId).NestCapable);
                    if (nestBeast != null)
                    {
                        z.Beasts.TryGetValue(nestBeast.BeastId, out var c);
                        z.Beasts[nestBeast.BeastId] = c + 1;
                        report.Add($"{tables.Zones.Get(z.ZoneId).Name}: гнездо растит тварь");
                    }
                }
            }

            // 6. Кристаллы, которые никто не взял, впитываются.
            var life = t.GetInt("crystal_life_days");
            foreach (var z in world.Zones.Values)
            {
                var def = tables.Zones.Get(z.ZoneId);
                var capCrystals = def.CrystalsPerDay * life;
                if (z.Crystals > capCrystals) z.Crystals = capCrystals;
            }

            world.Day++;
            report.Add($"Наступил день {world.Day}");
            return report;
        }

        private static void MoveOneBeast(World world, string from, string to)
        {
            var src = world.Zones[from];
            var id = src.Beasts.Where(kv => kv.Value > 0).Select(kv => kv.Key).FirstOrDefault();
            if (id == null) return;
            src.Beasts[id]--;
            if (src.Beasts[id] <= 0) src.Beasts.Remove(id);
            var dst = world.Zones[to];
            dst.Beasts.TryGetValue(id, out var c);
            dst.Beasts[id] = c + 1;
        }

        /// Голодная тварь идёт наружу: к деревне. Рёбра берём из zones.csv,
        /// «наружу» = зона с меньшей глубиной; окраина смотрит на деревню.
        private static string OutwardNeighbour(Tables tables, string zoneId)
        {
            var z = tables.Zones.Get(zoneId);
            var order = new Dictionary<string, int> { { "edge", 0 }, { "belt", 1 }, { "heart", 2 } };
            string best = null; var bestRank = int.MaxValue;
            foreach (var nb in z.Neighbours)
            {
                var rank = order[tables.Zones.Get(nb).Depth];
                if (rank < bestRank) { bestRank = rank; best = nb; }
            }
            var own = order[z.Depth];
            return bestRank < own ? best : null;
        }
    }
}
```

- [ ] **Step 5: Запустить проверки**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: три проверки ночи и предыдущие — `ok`, код 0.

- [ ] **Step 6: Коммит**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(core): мир и ночная симуляция леса"
```

---

### Task 7: Ядро — назначения и авторезолв вылазки

**Files:**
- Create: `Assets/Scripts/Core/Sim/DayPlan.cs`
- Create: `Assets/Scripts/Core/Sim/ExpeditionSim.cs`
- Modify: `tools/sim/Program.cs`

**Interfaces:**
- Produces:
  - `DayPlan { List<PartyAssignment> Parties; List<string> VillageWorkers; }`
  - `PartyAssignment { string ZoneId; List<string> PersonIds; Behavior Behavior; }`, `enum Behavior { Gather, Avoid, Clean }`
  - `ExpeditionResult { string ZoneId; List<KeyValuePair<string,int>> Found; int HoursUsed; int Stamina; bool Encountered; bool Wounded; List<string> Log; }`
  - `ExpeditionSim.Resolve(World world, Tables tables, PartyAssignment party, Rng rng)`
  - `Person.SkillLevel(Person p, string skillId)` (в `Data/People.cs`).

- [ ] **Step 1: Написать падающую проверку**

```csharp
failures.Check("expedition finds resources", () =>
{
    var t = DataLoader.Load(DataDir());
    var w = World.Create(t);
    var party = new PartyAssignment { ZoneId = "verge_meadow", Behavior = Behavior.Gather };
    party.PersonIds.Add("leya");
    var r = ExpeditionSim.Resolve(w, t, party, new Rng(7));
    return r.Log.Count > 0 && r.HoursUsed >= 1;
});
failures.Check("expedition is deterministic", () =>
{
    var t = DataLoader.Load(DataDir());
    var w = World.Create(t);
    var a = ExpeditionSim.Resolve(w, t, Party("verge_springs", "brunni"), new Rng(3));
    var b = ExpeditionSim.Resolve(w, t, Party("verge_springs", "brunni"), new Rng(3));
    return a.Found.Count == b.Found.Count && a.Wounded == b.Wounded;
});
private static PartyAssignment Party(string zone, params string[] people)
{
    var p = new PartyAssignment { ZoneId = zone, Behavior = Behavior.Gather };
    p.PersonIds.AddRange(people);
    return p;
}
```

- [ ] **Step 2: Запустить — убедиться, что падает**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: `'PartyAssignment' could not be found`.

- [ ] **Step 3: Написать `DayPlan` и навыки человека**

```csharp
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public enum Behavior { Gather, Avoid, Clean }

    public sealed class PartyAssignment
    {
        public string ZoneId;
        public List<string> PersonIds = new List<string>();
        public Behavior Behavior = Behavior.Gather;
    }

    public sealed class DayPlan
    {
        public List<PartyAssignment> Parties = new List<PartyAssignment>();
        public List<string> VillageWorkers = new List<string>();
    }

    public static class People
    {
        public static int SkillLevel(Tables tables, Person p, string skillId)
        {
            if (p.SkillsOverride != null && p.SkillsOverride.TryGetValue(skillId, out var ov)) return ov;
            var prof = tables.Professions.Get(p.Profession);
            if (prof.StartSkills != null && prof.StartSkills.TryGetValue(skillId, out var start)) return start;
            return 0;
        }

        public static bool CanGrow(Tables tables, Person p, string skillId) =>
            System.Array.IndexOf(tables.Professions.Get(p.Profession).SkillsAllowed, skillId) >= 0;
    }
}
```

- [ ] **Step 4: Написать `ExpeditionSim`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class ExpeditionResult
    {
        public string ZoneId;
        public List<KeyValuePair<string, int>> Found = new List<KeyValuePair<string, int>>();
        public List<string> Log = new List<string>();
        public int HoursUsed;
        public int Stamina;
        public bool Encountered;
        public bool Wounded;
    }

    public static class ExpeditionSim
    {
        public static ExpeditionResult Resolve(World world, Tables tables, PartyAssignment party, Rng rng)
        {
            var t = tables.Tuning;
            var zone = tables.Zones.Get(party.ZoneId);
            var zoneState = world.Zones[party.ZoneId];
            var result = new ExpeditionResult { ZoneId = party.ZoneId, Stamina = party.PersonIds.Count * t.GetInt("stamina_per_person") };

            var budget = t.GetInt("hours_per_day");
            var workHours = Math.Max(0, budget - (int)Math.Ceiling(zone.TravelHours * 2));
            if (workHours <= 0) { result.Log.Add("Дорога съедает весь день: отряд не дошёл"); return result; }

            var density = Density(world, zoneState);
            var intensity = party.Behavior == Behavior.Avoid ? 0.5 : party.Behavior == Behavior.Clean ? 2.5 : 1.0;
            if (party.PersonIds.Count > 2) intensity *= 1.2;
            if (zoneState.AlertDays > 0) intensity *= 1.2;

            var slotsPerPerson = t.GetInt("slots_per_person");
            var free = party.PersonIds.Count * slotsPerPerson;

            for (var hour = 1; hour <= workHours; hour++)
            {
                result.HoursUsed = hour;
                if (result.Stamina <= 0) { result.Log.Add($"{zone.Name}: отряд выбился из сил"); break; }

                // Находки.
                foreach (var personId in party.PersonIds)
                {
                    var person = tables.Roster.Get(personId);
                    var skill = People.SkillLevel(tables, person, "gathering");
                    var chance = t.GetFloat("find_base") + t.GetFloat("find_per_skill") * skill;
                    if (!rng.Chance(chance)) continue;

                    var pick = PickResource(tables, zone.Id, rng);
                    if (pick == null) continue;
                    var item = tables.Items.Get(pick);
                    if (free < item.Slots) { result.Log.Add($"{person.Name}: мешок полон, {item.Name} брошено"); continue; }
                    free -= item.Slots;
                    AddFound(result, pick);
                }

                // Встреча.
                var encounter = t.GetFloat("encounter_per_hour") * density * intensity;
                if (!rng.Chance(encounter)) continue;
                result.Encountered = true;
                result.Stamina -= 1;
                if (rng.Chance(0.5)) { result.Log.Add($"{zone.Name}: тварь рядом, отряд отошёл"); continue; }
                if (rng.Chance(0.5)) { result.Log.Add($"{zone.Name}: бой — победа, туша брошена"); continue; }
                result.Wounded = true;
                result.Log.Add($"{zone.Name}: бой — ранение, отряд возвращается");
                break;
            }

            return result;
        }

        private static double Density(World world, ZoneState z)
        {
            var food = Math.Max(1, z.Forage + z.Crystals);
            return (double)world.BeastCount(z.ZoneId) / food;
        }

        private static string PickResource(Tables tables, string zoneId, Rng rng)
        {
            var pool = tables.ZoneResources.All.Where(x => x.ZoneId == zoneId).ToList();
            if (pool.Count == 0) return null;
            var total = pool.Sum(x => x.Abundance);
            var roll = rng.Next(total);
            foreach (var r in pool)
            {
                roll -= r.Abundance;
                if (roll < 0) return r.ItemId;
            }
            return pool[pool.Count - 1].ItemId;
        }

        private static void AddFound(ExpeditionResult result, string itemId)
        {
            for (var i = 0; i < result.Found.Count; i++)
                if (result.Found[i].Key == itemId)
                {
                    result.Found[i] = new KeyValuePair<string, int>(itemId, result.Found[i].Value + 1);
                    return;
                }
            result.Found.Add(new KeyValuePair<string, int>(itemId, 1));
        }
    }
}
```

- [ ] **Step 5: Запустить проверки**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: все проверки `ok`, код 0.

- [ ] **Step 6: Коммит**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(core): назначения и авторезолв вылазки по часам"
```

---

### Task 8: Сцена, UIDocument и карта леса

**Files:**
- Create: `Assets/UI/Map.uxml`
- Create: `Assets/UI/Map.uss`
- Create: `Assets/Scripts/Game/MapScreen.cs`
- Create: `Assets/Scripts/Game/MapController.cs`
- Create: `Assets/Scripts/Game/ZoneNodeView.cs`
- Create: `Assets/Scripts/Game/EdgeLine.cs`
- Create: `Assets/Scenes/Main.unity` (живой редактор)

**Interfaces:**
- Consumes: `Tables`, `World`, `World.Create`.
- Produces: сцена `Main` с узлами зон и связями; `MapScreen.Tables`, `MapScreen.World` доступны другим компонентам.

- [ ] **Step 1: Написать `Map.uxml`**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:u="UnityEditor.UIElements">
  <Style src="Map.uss" />
  <ui:VisualElement name="root" class="root">
    <ui:VisualElement name="map" class="map" />
    <ui:VisualElement name="left-panel" class="panel left">
      <ui:Label text="ЖИТЕЛИ" class="panel-title" />
      <ui:ScrollView name="roster" class="roster" />
      <ui:Button name="send-button" text="В ЛЕС" class="action" />
    </ui:VisualElement>
    <ui:VisualElement name="right-panel" class="panel right">
      <ui:Label name="zone-name" text="—" class="panel-title" />
      <ui:Label name="zone-info" text="" class="zone-info" />
      <ui:Label name="zone-beasts" text="" class="zone-info" />
    </ui:VisualElement>
    <ui:VisualElement name="bottom-bar" class="bottom">
      <ui:Label name="day-label" text="День 1" class="day-label" />
      <ui:Button name="end-day" text="КОНЕЦ ДНЯ" class="action primary" />
    </ui:VisualElement>
    <ui:VisualElement name="report" class="report hidden" />
  </ui:VisualElement>
</ui:UXML>
```

- [ ] **Step 2: Написать `Map.uss`**

```css
.root { flex-grow: 1; background-color: rgb(24, 26, 24); }
.map { position: absolute; left: 240px; right: 320px; top: 0; bottom: 90px; }
.panel { position: absolute; top: 0; bottom: 90px; width: 240px; padding: 12px;
         background-color: rgb(38, 40, 36); border-color: rgb(196, 170, 120); border-width: 2px; }
.panel.left { left: 0; }
.panel.right { right: 0; width: 320px; }
.bottom { position: absolute; left: 0; right: 0; bottom: 0; height: 90px;
          flex-direction: row; align-items: center; justify-content: space-between;
          padding: 0 16px; background-color: rgb(38, 40, 36); border-top-width: 2px; border-color: rgb(196, 170, 120); }
.panel-title { color: rgb(230, 214, 180); font-size: 18px; unity-font-style: bold; margin-bottom: 8px; }
.zone-info { color: rgb(200, 200, 190); font-size: 14px; white-space: normal; }
.day-label { color: rgb(230, 214, 180); font-size: 22px; }
.action { height: 40px; padding: 0 16px; background-color: rgb(70, 72, 62);
          color: rgb(230, 214, 180); border-width: 2px; border-color: rgb(196, 170, 120); }
.action.primary { background-color: rgb(150, 96, 40); }
.zone-node { position: absolute; width: 96px; height: 96px; border-radius: 48px;
             background-color: rgb(52, 56, 48); border-width: 3px; border-color: rgb(196, 170, 120); }
.zone-node.selected { border-color: rgb(240, 190, 90); border-width: 4px; }
.zone-node .label { color: rgb(230, 214, 180); font-size: 12px; -unity-text-align: middle-center; }
.hidden { display: none; }
.report { position: absolute; left: 260px; right: 340px; top: 40px; bottom: 120px;
          background-color: rgb(20, 22, 20); border-width: 2px; border-color: rgb(196, 170, 120); padding: 16px; }
```

- [ ] **Step 3: Написать `EdgeLine` и `ZoneNodeView`**

`EdgeLine.cs` — линия между узлами:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// Линия связи зон: два узла в координатах карты.
    public sealed class EdgeLine : VisualElement
    {
        public Vector2 A;
        public Vector2 B;

        public EdgeLine()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerate;
        }

        private void OnGenerate(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            p.strokeColor = new Color(0.60f, 0.55f, 0.42f, 0.9f);
            p.lineWidth = 3f;
            p.BeginPath();
            p.MoveTo(A);
            p.LineTo(B);
            p.Stroke();
        }
    }
}
```

`ZoneNodeView.cs`:

```csharp
using System;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    public sealed class ZoneNodeView : VisualElement
    {
        public string ZoneId;
        public event Action<string> Clicked;

        public ZoneNodeView(string zoneId, string title)
        {
            ZoneId = zoneId;
            AddToClassList("zone-node");
            var label = new Label(title) { name = "label" };
            label.AddToClassList("label");
            Add(label);
            RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(ZoneId));
        }
    }
}
```

- [ ] **Step 4: Написать `MapController` — построение карты из таблиц**

```csharp
using System.Collections.Generic;
using ForestGuild.Core.Data;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// Строит карту: узлы по map_x/map_y, рёбра по neighbours. Координаты нормализованы.
    public sealed class MapController
    {
        private readonly VisualElement _map;
        private readonly Dictionary<string, ZoneNodeView> _nodes = new();
        private readonly List<EdgeLine> _edges = new();
        public string Selected { get; private set; }
        public event System.Action<string> ZoneSelected;

        public MapController(VisualElement map) { _map = map; }

        public void Build(Tables tables)
        {
            _map.Clear(); _nodes.Clear(); _edges.Clear();
            _map.RegisterCallback<GeometryChangedEvent>(_ => Layout(tables));

            foreach (var z in tables.Zones.All)
            {
                var node = new ZoneNodeView(z.Id, z.Name);
                node.Clicked += id => { Select(id); ZoneSelected?.Invoke(id); };
                _nodes[z.Id] = node;
                _map.Add(node);
            }

            foreach (var z in tables.Zones.All)
                foreach (var nb in z.Neighbours)
                {
                    if (string.CompareOrdinal(z.Id, nb) >= 0) continue; // ребро один раз
                    var line = new EdgeLine();
                    _edges.Add(line);
                    _map.Insert(0, line);
                }
            Layout(tables);
        }

        private void Layout(Tables tables)
        {
            var w = _map.resolvedStyle.width;
            var h = _map.resolvedStyle.height;
            if (w <= 1 || h <= 1) return;

            foreach (var z in tables.Zones.All)
            {
                var node = _nodes[z.Id];
                node.style.left = z.MapX * w - 48;
                node.style.top = z.MapY * h - 48;
            }

            var i = 0;
            foreach (var z in tables.Zones.All)
                foreach (var nb in z.Neighbours)
                {
                    if (string.CompareOrdinal(z.Id, nb) >= 0) continue;
                    var a = _nodes[z.Id]; var b = _nodes[nb];
                    _edges[i].A = new UnityEngine.Vector2(a.style.left.value.value + 48, a.style.top.value.value + 48);
                    _edges[i].B = new UnityEngine.Vector2(b.style.left.value.value + 48, b.style.top.value.value + 48);
                    _edges[i].MarkDirtyRepaint();
                    i++;
                }
        }

        private void Select(string id)
        {
            foreach (var kv in _nodes) kv.Value.RemoveFromClassList("selected");
            if (_nodes.TryGetValue(id, out var node)) node.AddToClassList("selected");
            Selected = id;
        }
    }
}
```

- [ ] **Step 5: Написать `MapScreen`**

```csharp
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MapScreen : MonoBehaviour
    {
        public Tables Tables;
        public World World;
        public MapController Map;

        private UIDocument _doc;
        private Label _zoneName, _zoneInfo, _zoneBeasts, _dayLabel;
        private VisualElement _report;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            var root = _doc.rootVisualElement;

            var dataDir = System.IO.Path.Combine(Application.streamingAssetsPath, "Data");
            Tables = DataLoader.Load(dataDir);
            World = World.Create(Tables);

            Map = new MapController(root.Q<VisualElement>("map"));
            Map.Build(Tables);
            Map.ZoneSelected += ShowZone;

            _zoneName = root.Q<Label>("zone-name");
            _zoneInfo = root.Q<Label>("zone-info");
            _zoneBeasts = root.Q<Label>("zone-beasts");
            _dayLabel = root.Q<Label>("day-label");
            _report = root.Q<VisualElement>("report");
            _dayLabel.text = "День " + World.Day;
        }

        private void ShowZone(string zoneId)
        {
            var zone = Tables.Zones.Get(zoneId);
            var state = World.Zones[zoneId];
            _zoneName.text = zone.Name;
            _zoneInfo.text = $"Глубина: {zone.Depth}, дорога {zone.TravelHours:0.#} ч\n" +
                             $"Кристаллы: {state.Crystals}, корм: {state.Forage}";
            _zoneBeasts.text = state.Beasts.Count == 0
                ? "Тварей не видно"
                : string.Join("\n", state.Beasts.Select(kv => $"{Tables.Beasts.Get(kv.Key).Name}: {kv.Value}"));
        }
    }
}
```

(`using System.Linq;` добавить.)

- [ ] **Step 6: Создать сцену живым редактором**

Через `unity_eval` (однострочник не подойдёт — использовать `run_script` из `unity-pipeline`, файл вне `Assets/`):

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public static class CreateMainScene
{
    public static void Run()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.10f, 0.09f);

        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/MapPanelSettings.asset");
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panelSettings, "Assets/UI/MapPanelSettings.asset");
        }

        var go = new GameObject("UI");
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = panelSettings;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Map.uxml");
        go.AddComponent<ForestGuild.Game.MapScreen>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
    }
}
```

Ожидание: `Assets/Scenes/Main.unity` существует, в консоли редактора нет ошибок.

- [ ] **Step 7: Проверить визуально**

Открыть `Main`, войти в Play Mode, сделать скриншот через `unity_command screenshot` (или `look`), убедиться: видны 6 узлов, связи, панели, «День 1».

- [ ] **Step 8: Коммит**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(ui): карта леса из таблиц (UI Toolkit, узлы и связи)"
```

---

### Task 9: Ростер, назначение отрядов и конец дня

**Files:**
- Modify: `Assets/UI/Map.uxml` (кнопка «сформировать отряд» в узле зоны)
- Create: `Assets/Scripts/Game/RosterPanel.cs`
- Create: `Assets/Scripts/Game/DayFlow.cs`

**Interfaces:**
- Consumes: `MapScreen.Tables`, `MapScreen.World`, `DayPlan`, `ExpeditionSim`, `NightSim`.
- Produces: рабочий день 1: отряд отправлен, «КОНЕЦ ДНЯ» → отчёт.

- [ ] **Step 1: Написать падающую проверку в ядре**

В `Program.cs`:

```csharp
failures.Check("day plan sends people to zone", () =>
{
    var t = DataLoader.Load(DataDir());
    var w = World.Create(t);
    var plan = new DayPlan();
    var p = new PartyAssignment { ZoneId = "verge_meadow" };
    p.PersonIds.Add("leya");
    plan.Parties.Add(p);
    var flow = new DayFlow(w, t, new Rng(5));
    var report = flow.RunDay(plan);
    return report.Parties.Count == 1 && w.Day == 2;
});
```

- [ ] **Step 2: Запустить — убедиться, что падает**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: `'DayFlow' could not be found`.

- [ ] **Step 3: Написать `DayFlow` в ядре (не в `Game/`)**

`Assets/Scripts/Core/Sim/DayFlow.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public sealed class DayReport
    {
        public List<ExpeditionResult> Parties = new List<ExpeditionResult>();
        public NightReport Night;
        public List<string> Lines = new List<string>();
    }

    /// Один день: вылазки → ночь. Это единственное место, где день «идёт».
    public sealed class DayFlow
    {
        private readonly World _world;
        private readonly Tables _tables;
        private readonly Rng _rng;

        public DayFlow(World world, Tables tables, Rng rng) { _world = world; _tables = tables; _rng = rng; }

        public DayReport RunDay(DayPlan plan)
        {
            var report = new DayReport { Parties = new List<ExpeditionResult>() };
            foreach (var party in plan.Parties)
            {
                var result = ExpeditionSim.Resolve(_world, _tables, party, _rng);
                report.Parties.Add(result);
                report.Lines.AddRange(result.Log);
                foreach (var found in result.Found)
                    report.Lines.Add($"Принесено: {_tables.Items.Get(found.Key).Name} ×{found.Value}");
            }
            report.Night = NightSim.AdvanceNight(_world, _tables, _rng);
            report.Lines.AddRange(report.Night.Lines);
            return report;
        }
    }
}
```

- [ ] **Step 4: Запустить проверку**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: `ok   day plan sends people to zone`, код 0.

- [ ] **Step 5: Написать `RosterPanel` (UI)**

```csharp
using System.Collections.Generic;
using System.Linq;
using ForestGuild.Core.Data;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// Список жителей с выбором: клик — взять/отпустить.
    public sealed class RosterPanel
    {
        private readonly VisualElement _root;
        private Tables _tables;
        public readonly List<string> Selected = new();

        public RosterPanel(VisualElement root) { _root = root; }

        public void Build(Tables tables)
        {
            _tables = tables;
            _root.Clear();
            foreach (var person in tables.Roster.All)
            {
                var button = new Button { text = $"{person.Name} · {PersonLine(person)}", name = "person-" + person.Id };
                button.clicked += () => Toggle(person.Id, button);
                _root.Add(button);
            }
        }

        private string PersonLine(Person p)
        {
            var prof = _tables.Professions.Get(p.Profession);
            var skills = prof.SkillsAllowed
                .Select(s => $"{_tables.Skills.Get(s).Name[0]}{People.SkillLevel(_tables, p, s)}");
            return prof.Name + " " + string.Join(" ", skills);
        }

        private void Toggle(string personId, Button button)
        {
            if (Selected.Remove(personId)) button.RemoveFromClassList("selected");
            else { Selected.Add(personId); button.AddToClassList("selected"); }
        }
    }
}
```

Добавить в `.uss`: `.selected { border-color: rgb(240, 190, 90); border-width: 3px; }`

- [ ] **Step 6: Связать всё в `MapScreen`**

Дописать в `MapScreen`:

```csharp
private RosterPanel _roster;
private DayFlow _flow;
private DayPlan _plan = new DayPlan();

private void Start()
{
    _roster = new RosterPanel(_doc.rootVisualElement.Q<VisualElement>("roster"));
    _roster.Build(Tables);
    _flow = new DayFlow(World, Tables, new Rng(12345));

    _doc.rootVisualElement.Q<Button>("send-button").clicked += SendParty;
    _doc.rootVisualElement.Q<Button>("end-day").clicked += EndDay;
}

private void SendParty()
{
    if (Map.Selected == null) { ShowMessage("Сначала выберите зону"); return; }
    if (_roster.Selected.Count == 0) { ShowMessage("Выберите хотя бы одного жителя"); return; }
    var party = new PartyAssignment { ZoneId = Map.Selected };
    party.PersonIds.AddRange(_roster.Selected);
    _plan.Parties.Add(party);
    ShowMessage($"Отряд из {party.PersonIds.Count} идёт в {Tables.Zones.Get(Map.Selected).Name}");
    _roster.Selected.Clear();
}

private void EndDay()
{
    var report = _flow.RunDay(_plan);
    _plan = new DayPlan();
    _dayLabel.text = "День " + World.Day;
    ShowReport(report);
    RefreshMapState();
}
```

`ShowMessage`/`ShowReport`/`RefreshMapState` — тоже дописать в `MapScreen`:

```csharp
private void ShowMessage(string text)
{
    _report.Clear();
    _report.Add(new Label(text));
    _report.RemoveFromClassList("hidden");
}

private void ShowReport(DayReport report)
{
    _report.Clear();
    _report.Add(new Label("День " + World.Day));
    foreach (var line in report.Lines) _report.Add(new Label("· " + line));
    _report.RemoveFromClassList("hidden");
}

private void RefreshMapState()
{
    if (Map.Selected != null) ShowZone(Map.Selected);
}
```

- [ ] **Step 7: Проверить в Play Mode**

Скриншот: отряд отправлен, нажатие «КОНЕЦ ДНЯ» даёт отчёт с строками о находках и ночи, день становится 2.

- [ ] **Step 8: Коммит**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(game): ростер, отправка отряда и конец дня с ночным отчётом"
```

---

### Task 10: Документация и приёмка шага

**Files:**
- Modify: `docs/architecture.md` (колонки `map_x/map_y` в `zones.csv`, фактический список файлов)
- Create: `docs/current/2026-11-07-ui-slice/result.md`

**Interfaces:** нет.

- [ ] **Step 1: Обновить `docs/architecture.md`**

В таблицу справочников добавить `map_x; map_y` в строку `zones.csv` и дописать раздел:

```markdown
## 10. Шаг 1: что уже работает

- `tools/sim --selfcheck` — проверки ядра без редактора.
- Сцена `Assets/Scenes/Main.unity`: карта из `zones.csv`, панель зоны, ростер, «конец дня».
- Ночь: кормление (корм → труп → кристалл), миграция наружу, гнездо по опыту, плод гнезда.
- Вылазка: авторезолв по часам (находки, встречи 50/50 отход/бой), без пауз.
```

- [ ] **Step 2: Написать `result.md`**

```markdown
# Шаг 1 — каркас, карта, первый день

Дата: <дата>. Статус: принято.

## Сделано
- Unity 6000.6.6f1 (URP 2D), проект в корне репозитория.
- 14 CSV-справочников, загрузчик с валидацией внешних ключей.
- Ядро: `Rng`, `World`, `NightSim`, `DayPlan`, `ExpeditionSim`, `DayFlow`.
- UI Toolkit: карта (узлы, рёбра), панель зоны, ростер, кнопка дня, отчёт.
- `tools/sim --selfcheck`: N проверок.

## Чем проверено
- `dotnet run ... --selfcheck` → все проверки ok, код 0 (вывод приложить).
- Play Mode: скриншот карты и скриншот отчёта дня 2.
- `grep` по `Core` не находит `UnityEngine`.

## Что не вошло
Паузы в вылазке, туша/разделка, раненый, полноценный бой, волна на 21-й день.
```

- [ ] **Step 3: Проверить, что ядро без Unity**

```powershell
cd F:\WORK\ForestGuild; Get-ChildItem Assets\Scripts\Core -Recurse -Filter *.cs |
  Select-String -Pattern "using UnityEngine" | Out-String
```

Ожидание: пусто. Если что-то нашлось — убрать.

- [ ] **Step 4: Финальный прогон**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
Ожидание: код 0, все `ok`.

- [ ] **Step 5: Коммит и перенос в архив**

```powershell
cd F:\WORK\ForestGuild
git add -A
git commit -m "docs: результат шага 1 и обновление архитектуры"
```

Папку `docs/current/2026-11-07-ui-slice` перенести в `docs/archive/` **после** приёмки пользователем, отдельным коммитом.
