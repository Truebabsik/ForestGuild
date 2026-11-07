> Статус: **выполнен**. Сделаны задачи 1–9, задача 10 (этот отчёт) — esult.md. Отклонения: паузы вылазки, туши и раненые вынесены в шаг 2 (см. result.md).

# ForestGuild вЂ” С€Р°Рі 1: РєР°СЂРєР°СЃ, РєР°СЂС‚Р°, РїРµСЂРІС‹Р№ РґРµРЅСЊ

> **Р”Р»СЏ Р°РіРµРЅС‚РѕРІ:** РћР‘РЇР—РђРўР•Р›Р¬РќР«Р™ РџРћР”-РќРђР’Р«Рљ: `subagent-driven-development` (СЂРµРєРѕРјРµРЅРґСѓРµС‚СЃСЏ) РёР»Рё `executing-plans` вЂ” РёСЃРїРѕР»РЅСЏС‚СЊ СЌС‚РѕС‚ РїР»Р°РЅ Р·Р°РґР°С‡Р° Р·Р° Р·Р°РґР°С‡РµР№. РЁР°РіРё РѕС‚РјРµС‡РµРЅС‹ С‡РµРєР±РѕРєСЃР°РјРё (`- [ ]`).

**Р¦РµР»СЊ:** РёРіСЂР°Р±РµР»СЊРЅС‹Р№ РїРµСЂРІС‹Р№ РґРµРЅСЊ: РєР°СЂС‚Р° Р»РµСЃР° РёР· `zones.csv`, СЂРѕСЃС‚РµСЂ РёР· `roster.csv`, РЅР°Р·РЅР°С‡РµРЅРёРµ РѕС‚СЂСЏРґРѕРІ, РєРЅРѕРїРєР° В«РєРѕРЅРµС† РґРЅСЏВ» в†’ Р°РІС‚РѕСЂРµР·РѕР»РІ РІС‹Р»Р°Р·РєРё РїРѕ С‡Р°СЃР°Рј в†’ РЅРѕС‡РЅР°СЏ Р¶РёР·РЅСЊ Р»РµСЃР° в†’ РѕС‚С‡С‘С‚.

**РђСЂС…РёС‚РµРєС‚СѓСЂР°:** РґР°РЅРЅС‹Рµ РІ CSV (`Assets/StreamingAssets/Data`), С‡РёСЃС‚РѕРµ СЏРґСЂРѕ Р±РµР· `UnityEngine` (`Assets/Scripts/Core`), UI Toolkit РїРѕРІРµСЂС… (`Assets/Scripts/Game` + UXML/USS), РєРѕРЅСЃРѕР»СЊРЅС‹Р№ РїСЂРѕРіРѕРЅ `tools/sim` РїРѕРґРєР»СЋС‡Р°РµС‚ С‚Рµ Р¶Рµ С„Р°Р№Р»С‹ СЏРґСЂР° С‡РµСЂРµР· `<Compile Include>`.

**РЎС‚РµРє:** Unity 6000.6.6f1, URP (С€Р°Р±Р»РѕРЅ `com.unity.template.universal-2d`), UI Toolkit, .NET 10 РєРѕРЅСЃРѕР»СЊ.

**РЎРїРµРєР°:** `docs/design.md`, `docs/architecture.md`, `docs/decisions.md` (ADR-0001, ADR-0002).

## Global Constraints

- Unity **6000.6.6f1**, URP. РќРѕРІС‹Р№ РїСЂРѕРµРєС‚ РІ `F:\WORK\ForestGuild` (СЂРµРїРѕР·РёС‚РѕСЂРёР№ СѓР¶Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚ вЂ” РїСЂРѕРµРєС‚ СЃРѕР·РґР°С‘С‚СЃСЏ РІРѕ РІСЂРµРјРµРЅРЅРѕР№ РїР°РїРєРµ Рё РїРµСЂРµРЅРѕСЃРёС‚СЃСЏ СЃСЋРґР°).
- РЎС†РµРЅР° РјРµРЅСЏРµС‚СЃСЏ **С‚РѕР»СЊРєРѕ С‡РµСЂРµР· Р¶РёРІРѕР№ СЂРµРґР°РєС‚РѕСЂ** (`unity_status` в†’ `unity_command`/`unity_eval`), РЅРµ РїСЂР°РІРєРѕР№ `.unity` РЅР° РґРёСЃРєРµ.
- `Assets/Scripts/Core/**` **РЅРµ СЃРѕРґРµСЂР¶РёС‚** `using UnityEngine`. РџСЂРѕРІРµСЂСЏРµС‚СЃСЏ С‚РµСЃС‚РѕРј.
- `asmdef` РЅРµ Р·Р°РІРѕРґРёС‚СЊ, РІРµСЃСЊ РєРѕРґ вЂ” `Assembly-CSharp`.
- `id` РІ С‚Р°Р±Р»РёС†Р°С… вЂ” ASCII `snake_case`; СЂСѓСЃСЃРєРёРµ РЅР°Р·РІР°РЅРёСЏ вЂ” РІ РєРѕР»РѕРЅРєРµ `name`.
- CSV: UTF-8, `;`, РґРµСЃСЏС‚РёС‡РЅР°СЏ С‚РѕС‡РєР°; `#` вЂ” РєРѕРјРјРµРЅС‚Р°СЂРёР№; СЃРїРёСЃРєРё вЂ” `a:1|b:2` (РµРґРёРЅСЃС‚РІРµРЅРЅР°СЏ РјРёРЅРё-РіСЂР°РјРјР°С‚РёРєР°).
- Р’Р°Р»РёРґР°С†РёСЏ С‚Р°Р±Р»РёС† РїР°РґР°РµС‚ (`DataException` СЃ `С„Р°Р№Р»:СЃС‚СЂРѕРєР°:РєРѕР»РѕРЅРєР°`), Р° РЅРµ РїРѕРґСЃС‚Р°РІР»СЏРµС‚ РґРµС„РѕР»С‚С‹.
- РЇРґСЂРѕ РґРµС‚РµСЂРјРёРЅРёСЂРѕРІР°РЅРѕ: СЃР»СѓС‡Р°Р№РЅРѕСЃС‚СЊ С‚РѕР»СЊРєРѕ С‡РµСЂРµР· РїРµСЂРµРґР°РЅРЅС‹Р№ `Rng(seed)`; РЅРёРєР°РєРёС… `DateTime.Now`, `UnityEngine.Random`.
- Р’ С€Р°РіРµ 1 **РЅРµС‚ РїР°СѓР·** РІ РІС‹Р»Р°Р·РєРµ Рё РЅРµС‚ С‚СѓС€Рё/СЂР°Р·РґРµР»РєРё вЂ” СЌС‚Рѕ С€Р°Рі 2. РќРµС‚ Р±Р°Р»Р°РЅСЃРѕРІС‹С… РїСЂРѕРіРѕРЅРѕРІ РЅР° 21 РґРµРЅСЊ.
- Р”РµРЅСЊРіРё/РєР°СЂР°РІР°РЅ/СЃС‚Р°РЅС†РёРё РІ С€Р°РіРµ 1 РЅРµ СЂР°Р±РѕС‚Р°СЋС‚; С‚Р°Р±Р»РёС†С‹ СЃС‚Р°РЅС†РёР№ Рё СЂРµС†РµРїС‚РѕРІ СЃСѓС‰РµСЃС‚РІСѓСЋС‚ СЂР°РґРё РїСЂРѕРІРµСЂРєРё С„РѕСЂРјР°С‚Р°.

## РџРѕРїСЂР°РІРєР° РѕС‚ 2026-11-07: Unity-РїСЂРѕРµРєС‚ Р¶РёРІС‘С‚ РІ `FG/`

РџРѕСЃР»Рµ Р·Р°РґР°С‡Рё 1 РїСЂРѕРµРєС‚ РїРµСЂРµРЅРµСЃС‘РЅ РІ РїРѕРґРїР°РїРєСѓ: РєРѕСЂРµРЅСЊ СЂРµРїРѕР·РёС‚РѕСЂРёСЏ вЂ” `F:\WORK\ForestGuild`, Unity вЂ” `F:\WORK\ForestGuild\FG`.

- Р’СЃРµ РїСѓС‚Рё РІРёРґР° `Assets/...`, `Packages/...`, `ProjectSettings/...` С‡РёС‚Р°СЋС‚СЃСЏ РєР°Рє `FG/Assets/...` Рё С‚.Рґ.
- `unity open` / `unity pipeline install` / `unity_command projectPath` вЂ” СЃ `F:\WORK\ForestGuild\FG`.
- Р’ `tools/sim/ForestGuild.Sim.csproj` РїСѓС‚СЊ Рє СЏРґСЂСѓ: `..\..\FG\Assets\Scripts\Core\**\*.cs`.
- `DataDir()` РІ `Program.cs` РёС‰РµС‚ `FG\Assets\StreamingAssets\Data` вЂ” РІ С…РµР»РїРµСЂРµ Р·Р°РјРµРЅРёС‚СЊ `"Assets"` РЅР° `"FG", "Assets"`.
- `Application.streamingAssetsPath` РІ Unity СѓР¶Рµ СѓРєР°Р·С‹РІР°РµС‚ РІРЅСѓС‚СЂСЊ `FG`, РјРµРЅСЏС‚СЊ РєРѕРґ `MapScreen` РЅРµ РЅСѓР¶РЅРѕ.

## РЎС‚СЂСѓРєС‚СѓСЂР° С„Р°Р№Р»РѕРІ

```
Assets/
  StreamingAssets/Data/            # 11 CSV (Р·Р°РґР°С‡Р° 3)
  Scripts/Core/Data/               # DataException, CsvReader, Table<T>, С‚Р°Р±Р»РёС‡РЅС‹Рµ С‚РёРїС‹, DataLoader
  Scripts/Core/Sim/                # Rng, World, NightSim, ExpeditionSim, DayPlan
  Scripts/Game/                    # MapScreen, MapController, RosterPanel, ExpeditionRunner, DayFlow
  UI/Map.uxml, UI/Map.uss          # РёРЅС‚РµСЂС„РµР№СЃ РєР°СЂС‚С‹
  Scenes/Main.unity                # СЃРѕР·РґР°С‘С‚СЃСЏ Р¶РёРІС‹Рј СЂРµРґР°РєС‚РѕСЂРѕРј
tools/sim/                         # ForestGuild.Sim.csproj, Program.cs
docs/current/2026-11-07-ui-slice/  # СЌС‚РѕС‚ РїР»Р°РЅ
```

---

### Task 1: РџСЂРѕРµРєС‚ Unity Рё РїРµСЂРµРЅРѕСЃ РІ СЂРµРїРѕР·РёС‚РѕСЂРёР№

**Files:**
- Create: `F:\WORK\_fg_tmp\ForestGuild\` (РІСЂРµРјРµРЅРЅС‹Р№ РїСЂРѕРµРєС‚)
- Modify: `F:\WORK\ForestGuild\` (РїРµСЂРµРЅРѕСЃ `Assets/`, `Packages/`, `ProjectSettings/`)
- Modify: `.gitignore`

**Interfaces:**
- Produces: РїСЂРѕРµРєС‚ Unity РІ РєРѕСЂРЅРµ СЂРµРїРѕР·РёС‚РѕСЂРёСЏ; `Assets/`, `Packages/`, `ProjectSettings/`, `Assets/StreamingAssets/Data/`.

- [ ] **Step 1: РЎРѕР·РґР°С‚СЊ РїСЂРѕРµРєС‚ РІРѕ РІСЂРµРјРµРЅРЅРѕР№ РїР°РїРєРµ**

```powershell
unity projects create ForestGuild --path F:\WORK\_fg_tmp --editor-version 6000.6.6f1 --template com.unity.template.universal-2d --with-pipeline --no-cloud --non-interactive
```

РћР¶РёРґР°РЅРёРµ: РєРѕРґ 0, `F:\WORK\_fg_tmp\ForestGuild\ProjectSettings\ProjectVersion.txt` СЃРѕРґРµСЂР¶РёС‚ `6000.6.6f1`.

- [ ] **Step 2: РџРµСЂРµРЅРµСЃС‚Рё СЃРѕРґРµСЂР¶РёРјРѕРµ РІ СЂРµРїРѕР·РёС‚РѕСЂРёР№**

```powershell
$src="F:\WORK\_fg_tmp\ForestGuild"; $dst="F:\WORK\ForestGuild"
foreach ($d in @("Assets","Packages","ProjectSettings")) { Copy-Item "$src\$d" $dst -Recurse -Force }
Remove-Item "F:\WORK\_fg_tmp" -Recurse -Force
```

- [ ] **Step 3: РџСЂРѕРІРµСЂРёС‚СЊ, С‡С‚Рѕ РїСЂРѕРµРєС‚ РЅР° РјРµСЃС‚Рµ**

Run: `Test-Path F:\WORK\ForestGuild\ProjectSettings\ProjectVersion.txt; Get-ChildItem F:\WORK\ForestGuild\Assets -Name`
РћР¶РёРґР°РЅРёРµ: `True`, РІ `Assets` РµСЃС‚СЊ `Scenes`, `Settings` (РёР»Рё РїСѓСЃС‚Рѕ), РЅРµС‚ РѕС€РёР±РѕРє.

- [ ] **Step 4: Р”РѕРїРѕР»РЅРёС‚СЊ `.gitignore`**

Р”РѕР±Р°РІРёС‚СЊ РІ РєРѕРЅРµС† `.gitignore`:

```text
*.meta.tmp
/[Aa]ssets/[Ss]treamingAssets/Data/*.tmp
```

- [ ] **Step 5: РљРѕРјРјРёС‚**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat: РїСЂРѕРµРєС‚ Unity 6000.6.6f1 (URP 2D, UI Toolkit)"
```

---

### Task 2: РџРѕРґРЅСЏС‚СЊ СЂРµРґР°РєС‚РѕСЂ Рё СѓР±РµРґРёС‚СЊСЃСЏ, С‡С‚Рѕ РѕРЅ С‚РёРєР°РµС‚

**Files:** РЅРµС‚ (С‚РѕР»СЊРєРѕ СЃРѕСЃС‚РѕСЏРЅРёРµ СЂРµРґР°РєС‚РѕСЂР°).

**Interfaces:**
- Consumes: РїСЂРѕРµРєС‚ РёР· Р·Р°РґР°С‡Рё 1.
- Produces: Р·Р°РїСѓС‰РµРЅРЅС‹Р№ СЂРµРґР°РєС‚РѕСЂ СЃ РїР°РєРµС‚РѕРј Pipeline, Р°РІС‚РѕС‚РёРє РІРєР»СЋС‡С‘РЅ, `unity_status` РІРѕР·РІСЂР°С‰Р°РµС‚ `state: ready`.

- [ ] **Step 1: РџСЂРѕС‡РёС‚Р°С‚СЊ СЃРєРёР»Р»**

РџСЂРѕС‡РёС‚Р°С‚СЊ `.dsh/skills/unity-pipeline/SKILL.md` С†РµР»РёРєРѕРј (РїСЂР°РІРёР»Рѕ 11 РёР· `AGENTS.md`). Р‘РµР· РЅРµРіРѕ `recompile` Рё С‚РµСЃС‚С‹ РІРёСЃСЏС‚.

- [ ] **Step 2: РћС‚РєСЂС‹С‚СЊ РїСЂРѕРµРєС‚**

```powershell
unity open F:\WORK\ForestGuild --args -automated
```

- [x] **Step 3: РЈСЃС‚Р°РЅРѕРІРёС‚СЊ РїР°РєРµС‚ Pipeline Рё РІРєР»СЋС‡РёС‚СЊ Р°РІС‚РѕС‚РёРє** (СЃРґРµР»Р°РЅРѕ 2026-11-07)

РЈСЃС‚Р°РЅРѕРІРєР° вЂ” С‡РµСЂРµР· CLI:

```powershell
unity pipeline install --project-path F:\WORK\ForestGuild\FG
```

РђРІС‚РѕС‚РёРє вЂ” С‡РµСЂРµР· MCP-РёРЅСЃС‚СЂСѓРјРµРЅС‚С‹. **Р¤Р°РєС‚РёС‡РµСЃРєРѕРµ РёРјСЏ РєРѕРјР°РЅРґС‹ вЂ” `set_auto_tick`, РїР°СЂР°РјРµС‚СЂ вЂ” `--enabled`** (РїСЂРµР¶РЅРµРµ `set_autotick --enable` РїРµСЂРµРёРјРµРЅРѕРІР°РЅРѕ, CLI РѕР± СЌС‚РѕРј СЃРѕРѕР±С‰Р°РµС‚ РїСЂРё РѕС€РёР±РєРµ):

```
unity_command(command: "set_auto_tick", args: ["--enabled", "true"])
```

РћС‚РІРµС‚: `"Auto-tick already enabled (interval updated to 16ms)"`.

- [ ] **Step 4: РџСЂРѕРІРµСЂРёС‚СЊ СЃРѕСЃС‚РѕСЏРЅРёРµ**

`unity_status` в†’ `success: true`, `state: "ready"`. Р•СЃР»Рё `STATUS_NO_INSTANCES` вЂ” РїРѕРґРѕР¶РґР°С‚СЊ Рё РїРѕРІС‚РѕСЂРёС‚СЊ, РЅРµ РїРµСЂРµР·Р°РїСѓСЃРєР°СЏ СЂРµРґР°РєС‚РѕСЂ.

- [ ] **Step 5: Р—Р°С„РёРєСЃРёСЂРѕРІР°С‚СЊ С„Р°РєС‚ РІ РїР»Р°РЅРµ**

Р”РѕРїРёСЃР°С‚СЊ РІ РєРѕРЅРµС† С„Р°Р№Р»Р°: `- [x] Р РµРґР°РєС‚РѕСЂ Р·Р°РїСѓС‰РµРЅ, Р°РІС‚РѕС‚РёРє РІРєР»СЋС‡С‘РЅ (<РґР°С‚Р°>)`.

---

### Task 3: РљРѕРЅСЃРѕР»СЊРЅС‹Р№ harness `tools/sim` c СЏРґСЂРѕРј

**Files:**
- Create: `tools/sim/ForestGuild.Sim.csproj`
- Create: `tools/sim/Program.cs`
- Create: `tools/sim/CheckRunner.cs`
- Modify: `.gitignore` (РґРѕР±Р°РІРёС‚СЊ `tools/sim/bin/`, `tools/sim/obj/`)

**Interfaces:**
- Produces: `ForestGuild.Core` РєРѕРґ РєРѕРјРїРёР»РёСЂСѓРµС‚СЃСЏ РІ .NET; `dotnet run --project tools/sim -- --selfcheck` РІРѕР·РІСЂР°С‰Р°РµС‚ 0 РїСЂРё СѓСЃРїРµС…Рµ Рё 1 РїСЂРё РїСЂРѕРІР°Р»Рµ.

- [ ] **Step 1: РЎРѕР·РґР°С‚СЊ csproj, РїРѕРґРєР»СЋС‡Р°СЋС‰РёР№ СЏРґСЂРѕ**

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

`LangVersion 9` вЂ” РєР°Рє РІ Unity 6, С‡С‚РѕР±С‹ РєРѕРґ РЅРµ СЃРєРѕРјРїРёР»РёСЂРѕРІР°Р»СЃСЏ РІ РєРѕРЅСЃРѕР»Рё Рё РЅРµ СѓРїР°Р» РїРѕС‚РѕРј РІ СЂРµРґР°РєС‚РѕСЂРµ.

- [ ] **Step 2: РќР°РїРёСЃР°С‚СЊ РїР°РґР°СЋС‰РёР№ selfcheck**

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
        try { if (!body()) _failed.Add(name + ": РІРµСЂРЅСѓР» false"); }
        catch (Exception e) { _failed.Add(name + ": " + e.GetType().Name + " " + e.Message); }
        Console.WriteLine((_failed.Count == 0 ? "ok   " : "FAIL ") + name);
    }
    public bool Report()
    {
        if (_failed.Count == 0) { Console.WriteLine("РІСЃРµ РїСЂРѕРІРµСЂРєРё РїСЂРѕС€Р»Рё"); return true; }
        foreach (var f in _failed) Console.WriteLine("  вњ— " + f);
        return false;
    }
}
```

Р”РѕР±Р°РІРёС‚СЊ `<Compile Include="CheckRunner.cs" />` РІ csproj.

- [ ] **Step 3: Р—Р°РїСѓСЃС‚РёС‚СЊ вЂ” СѓР±РµРґРёС‚СЊСЃСЏ, С‡С‚Рѕ РїР°РґР°РµС‚**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: РѕС€РёР±РєР° РєРѕРјРїРёР»СЏС†РёРё `The type or namespace name 'Rng' could not be found`.

- [ ] **Step 4: РќР°РїРёСЃР°С‚СЊ РјРёРЅРёРјР°Р»СЊРЅС‹Р№ `Rng`**

`Assets/Scripts/Core/Sim/Rng.cs`:

```csharp
namespace ForestGuild.Core.Sim
{
    /// Р”РµС‚РµСЂРјРёРЅРёСЂРѕРІР°РЅРЅС‹Р№ РіРµРЅРµСЂР°С‚РѕСЂ: РѕРґРёРЅ Рё С‚РѕС‚ Р¶Рµ СЃРёРґ вЂ” РѕРґРЅР° Рё С‚Р° Р¶Рµ РёРіСЂР°.
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

- [ ] **Step 5: Р—Р°РїСѓСЃС‚РёС‚СЊ вЂ” СѓР±РµРґРёС‚СЊСЃСЏ, С‡С‚Рѕ РїСЂРѕС…РѕРґРёС‚**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: `ok   rng is deterministic`, `РІСЃРµ РїСЂРѕРІРµСЂРєРё РїСЂРѕС€Р»Рё`, РєРѕРґ 0.

- [ ] **Step 6: РљРѕРјРјРёС‚**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(sim): РєРѕРЅСЃРѕР»СЊРЅС‹Р№ harness Рё РґРµС‚РµСЂРјРёРЅРёСЂРѕРІР°РЅРЅС‹Р№ Rng"
```

---

### Task 4: CSV-СЃРїСЂР°РІРѕС‡РЅРёРєРё MVP

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
- Produces: 14 С„Р°Р№Р»РѕРІ СЃ Р·Р°РіРѕР»РѕРІРєР°РјРё, РєРѕС‚РѕСЂС‹Рµ РѕР¶РёРґР°РµС‚ `DataLoader` (Р·Р°РґР°С‡Р° 5). РљРѕР»РѕРЅРєРё вЂ” СЂРѕРІРЅРѕ РєР°Рє РЅРёР¶Рµ.

- [ ] **Step 1: `tuning.csv`**

```csv
id;value;note
schema_version;1;РІРµСЂСЃРёСЏ СЃС…РµРјС‹ С‚Р°Р±Р»РёС†
hours_per_day;6;СЂР°Р±РѕС‡РёС… С‡Р°СЃРѕРІ РІ СЃСѓС‚РєР°С…
slots_per_person;4;СЃР»РѕС‚РѕРІ РіСЂСѓР·Р° РЅР° С‡РµР»РѕРІРµРєР°
stamina_per_person;4;РІС‹РЅРѕСЃР»РёРІРѕСЃС‚Рё РЅР° С‡РµР»РѕРІРµРєР°
encounter_per_hour;0.25;С€Р°РЅСЃ РІСЃС‚СЂРµС‡Рё Р·Р° С‡Р°СЃ РЅР° РµРґРёРЅРёС†Сѓ РїР»РѕС‚РЅРѕСЃС‚Рё
find_base;0.12;Р±Р°Р·Р° С€Р°РЅСЃР° РЅР°С…РѕРґРєРё
find_per_skill;0.06;РїСЂРёР±Р°РІРєР° Р·Р° СѓСЂРѕРІРµРЅСЊ РЅР°РІС‹РєР°
xp_to_nest;5;РѕРїС‹С‚Р° РґРѕ РіРЅРµР·РґР°
nest_beasts_cap;2;С‚РІР°СЂРµР№ РІ РіРЅРµР·РґРµ РјР°РєСЃРёРјСѓРј
nest_spawn_days;2;РґРЅРµР№ РЅР° РѕРґРЅСѓ С‚РІР°СЂСЊ РІ РіРЅРµР·РґРµ
migration_per_night;1;РјРёРіСЂР°С†РёР№ РІ Р·РѕРЅСѓ Р·Р° РЅРѕС‡СЊ
heart_influx_days;3;РґРЅРµР№ РјРµР¶РґСѓ РїСЂРёС‚РѕРєРѕРј РёР· СЃРµСЂРґС†Р°
crystal_life_days;2;РґРЅРµР№ Р¶РёР·РЅРё РєСЂРёСЃС‚Р°Р»Р»Р° РЅР° Р·РµРјР»Рµ
palisade_hp;10;РїСЂРѕС‡РЅРѕСЃС‚СЊ С‡Р°СЃС‚РѕРєРѕР»Р°
wave_day;21;РґРµРЅСЊ РїРµСЂРІРѕР№ РІРѕР»РЅС‹
xp_per_day;1;РѕРїС‹С‚Р° Р·Р° РґРµРЅСЊ СЂР°Р±РѕС‚С‹
xp_per_fight;1;Р±РѕРЅСѓСЃ РѕРїС‹С‚Р° Р·Р° Р±РѕР№
xp_per_depth;1;Р±РѕРЅСѓСЃ РѕРїС‹С‚Р° Р·Р° РїРѕСЏСЃ
level_xp;10|30|70|150;РѕРїС‹С‚Р° РЅР° РїРµСЂРµС…РѕРґС‹ 1-2,2-3,3-4,4-5
```

- [ ] **Step 2: `items.csv`**

```csv
id;name;category;slots;value;tags
herb_fire_leaf;РћРіРЅРµРЅРЅР°СЏ РєСЂР°РїРёРІР°;material;0.5;3;fire
herb_ash_lichen;РџРµРїРµР»СЊРЅС‹Р№ Р»РёС€Р°Р№;material;0.5;4;
herb_glass_stem;РЎС‚РµРєР»СЏРЅРЅРёС†Р°;material;0.5;3;
ore_obsidian;РћР±СЃРёРґРёР°РЅРѕРІР°СЏ СЂСѓРґР°;material;2;8;heavy
sulfur_nodule;РЎРµСЂРЅС‹Р№ РЅР°С‚С‘Рє;material;1;5;fire
crystal_fire;РљСЂРёСЃС‚Р°Р»Р» РѕРіРЅСЏ;material;1;25;crystal
moss_common;РћР±С‹С‡РЅС‹Р№ РјРѕС…;material;0.5;1;
bone_plate;РљРѕСЃС‚СЏРЅР°СЏ РїР»Р°СЃС‚РёРЅР°;trophy;1;12;bone
hide_rough;Р“СЂСѓР±Р°СЏ С€РєСѓСЂР°;trophy;1;6;hide
```

- [ ] **Step 3: `zones.csv` + `zone_resources.csv`**

```csv
id;name;depth;travel_hours;crystals_per_day;forage_per_day;neighbours;env_rule;map_x;map_y
verge_meadow;РћРїСѓС€РєР°;edge;1;1;2;verge_springs|verge_thicket;none;0.50;0.52
verge_springs;Р’СѓР»РєР°РЅРёС‡РµСЃРєРёРµ РєР»СЋС‡Рё;edge;1.5;1;2;verge_meadow|verge_marsh|belt_stone;heat;0.28;0.38
verge_marsh;РўРѕРїСЊ;edge;1.5;1;2;verge_springs|verge_thicket;mud;0.16;0.62
verge_thicket;Р§Р°С‰Р°;edge;1;1;2;verge_meadow|verge_marsh|belt_stone;thicket;0.74;0.40
belt_stone;РљР°РјРµРЅРЅС‹Р№ Р»РѕРі;belt;3;2;3;verge_springs|verge_thicket|heart_root;none;0.58;0.20
heart_root;РЎРµСЂРґС†Рµ Р»РµСЃР°;heart;6;4;4;belt_stone;none;0.86;0.08
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
beast_spark;РСЃРєСЂРѕРјС‘С‚;2;3;1;1
beast_bone;РљРѕСЃС‚СЏРЅРѕР№ Р±РµРіСѓРЅ;3;2;1;1
beast_ash;РџРµРїРµР»СЊРЅС‹Р№ РІРѕСЂ;1;4;1;0
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
gathering;РЎРѕР±РёСЂР°С‚РµР»СЊСЃС‚РІРѕ;field;5
hunting;РћС…РѕС‚Р°;field;5
butchering;Р Р°Р·РґРµР»РєР°;field;5
smithing;РљСѓР·РЅРµС‡РЅРѕРµ РґРµР»Рѕ;craft;5
alchemy;РђР»С…РёРјРёСЏ;craft;5
tanning;РљРѕР¶РµРІРµРЅРЅРѕРµ РґРµР»Рѕ;craft;5
```

```csv
id;name;fight_bonus;find_bonus;butcher_bonus;carry_bonus
brave;РҐСЂР°Р±СЂС‹Р№;1;0;0;0
keen;Р—РѕСЂРєРёР№;0;0.05;0;0
steady;РўРІС‘СЂРґР°СЏ СЂСѓРєР°;0;0;1;0
strong;РљСЂРµРїРєРёР№;0;0;0;1
patient;РўРµСЂРїРµР»РёРІС‹Р№;0;0.03;1;0
```

```csv
id;name;skills_allowed;requires_trait;start_skills
gatherer;РЎР±РѕСЂС‰РёРє;gathering|butchering;patient;gathering:2|butchering:1
hunter;РћС…РѕС‚РЅРёРє;hunting|butchering;brave;hunting:2|butchering:1
```

```csv
id;name;profession;traits;skills_override
brunni;Р‘СЂСѓРЅРЅРё;hunter;brave|strong;hunting:4|butchering:3
skadi;РЎРєР°РґРё;hunter;brave;hunting:3|butchering:2
hilda;РҐРёР»СЊРґР°;hunter;brave|keen;hunting:2|butchering:2
orn;РћСЂРЅ;hunter;strong;hunting:2|butchering:1
leya;Р›РµСЏ;gatherer;keen|patient;gathering:3|butchering:2
tove;РўРѕРІРµ;gatherer;patient;gathering:2|butchering:2
run;Р СѓРЅ;gatherer;steady;gathering:2|butchering:1
valda;Р’Р°Р»СЊРґР°;gatherer;patient;gathering:1|butchering:0
```

Р’РЅРёРјР°РЅРёРµ: **id вЂ” С‚РѕР»СЊРєРѕ ASCII** (`run`, `valda`), СЂСѓСЃСЃРєРёРµ РёРјРµРЅР° вЂ” РІ РєРѕР»РѕРЅРєРµ `name`.

- [ ] **Step 6: `stations.csv`, `recipes.csv`, `recipe_quality.csv`**

```csv
id;name;skill_id;people_slots
smithy;РљСѓР·РЅСЏ;smithing;1
workshop;РњР°СЃС‚РµСЂСЃРєР°СЏ;alchemy;1
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

- [ ] **Step 7: РџСЂРѕРІРµСЂРёС‚СЊ, С‡С‚Рѕ С„Р°Р№Р»С‹ С‡РёС‚Р°СЋС‚СЃСЏ Рё РІР°Р»РёРґРЅС‹ РїРѕ С„РѕСЂРјРµ**

```powershell
Get-ChildItem F:\WORK\ForestGuild\Assets\StreamingAssets\Data\*.csv |
  ForEach-Object { "{0}: {1} СЃС‚СЂРѕРє" -f $_.Name, ((Get-Content $_.FullName).Count) }
```

РћР¶РёРґР°РЅРёРµ: 14 С„Р°Р№Р»РѕРІ, Сѓ РєР°Р¶РґРѕРіРѕ РјРёРЅРёРјСѓРј 2 СЃС‚СЂРѕРєРё, РЅРµС‚ РїСѓСЃС‚С‹С….

- [ ] **Step 8: РљРѕРјРјРёС‚**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(data): 14 CSV-СЃРїСЂР°РІРѕС‡РЅРёРєР° MVP"
```

---

### Task 5: Р—Р°РіСЂСѓР·С‡РёРє Рё РІР°Р»РёРґР°С‚РѕСЂ С‚Р°Р±Р»РёС†

**Files:**
- Create: `Assets/Scripts/Core/Data/DataException.cs`
- Create: `Assets/Scripts/Core/Data/CsvReader.cs`
- Create: `Assets/Scripts/Core/Data/Table.cs`
- Create: `Assets/Scripts/Core/Data/Rows.cs` (С‚РёРїС‹ СЃС‚СЂРѕРє)
- Create: `Assets/Scripts/Core/Data/DataLoader.cs`
- Modify: `tools/sim/Program.cs` (РґРѕР±Р°РІРёС‚СЊ РїСЂРѕРІРµСЂРєРё Р·Р°РіСЂСѓР·РєРё СЂРµР°Р»СЊРЅС‹С… С‚Р°Р±Р»РёС†)

**Interfaces:**
- Produces:
  - `Tables DataLoader.Load(string dataDir)` вЂ” С‡РёС‚Р°РµС‚ РІСЃРµ 14 С„Р°Р№Р»РѕРІ, РІР°Р»РёРґРёСЂСѓРµС‚, Р±СЂРѕСЃР°РµС‚ `DataException`.
  - `Table<T>.Get(string id)`, `.All`, `.TryGet(string, out T)`.
  - `Tables.Tuning`, `Tables.Zones`, `Tables.Items`, `Tables.ZoneResources`, `Tables.Beasts`, `Tables.ZoneBeasts`, `Tables.Drops`, `Tables.Skills`, `Tables.Traits`, `Tables.Professions`, `Tables.Roster`, `Tables.Stations`, `Tables.Recipes`, `Tables.RecipeQuality`.
  - `Tuning.GetFloat(string key)`, `GetInt(string key)`, `GetIntList(string key)`.

- [ ] **Step 1: РќР°РїРёСЃР°С‚СЊ РїР°РґР°СЋС‰СѓСЋ РїСЂРѕРІРµСЂРєСѓ**

Р’ `Program.cs` РїРµСЂРµРґ `var ok = failures.Report();` РґРѕР±Р°РІРёС‚СЊ:

```csharp
failures.Check("tables load", () =>
{
    var t = ForestGuild.Core.Data.DataLoader.Load(DataDir());
    return t.Zones.All.Count == 6 && t.Roster.All.Count == 8;
});
failures.Check("unknown zone in neighbours fails", () =>
{
    var dir = CopyDataWithMutation("zones.csv", "verge_meadow;РћРїСѓС€РєР°;edge;1;1;2;nowhere;none;0.5;0.52");
    try { ForestGuild.Core.Data.DataLoader.Load(dir); return false; }
    catch (ForestGuild.Core.Data.DataException) { return true; }
});
```

Рё С…РµР»РїРµСЂС‹ РІ РєРѕРЅС†Рµ `Program`:

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

(`using System.IO; using System.Linq; using System.Collections.Generic;` вЂ” РґРѕР±Р°РІРёС‚СЊ.)

- [ ] **Step 2: Р—Р°РїСѓСЃС‚РёС‚СЊ вЂ” СѓР±РµРґРёС‚СЊСЃСЏ, С‡С‚Рѕ РїР°РґР°РµС‚**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: `FAIL tables load ... 'DataLoader' could not be found` (РѕС€РёР±РєР° РєРѕРјРїРёР»СЏС†РёРё).

- [ ] **Step 3: РќР°РїРёСЃР°С‚СЊ `DataException`, `CsvReader`, `Table<T>`**

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
    /// Р§РёС‚Р°РµС‚ CSV: ';', UTF-8, '#' вЂ” РєРѕРјРјРµРЅС‚Р°СЂРёР№, РїРµСЂРІР°СЏ РЅРµРїСѓСЃС‚Р°СЏ СЃС‚СЂРѕРєР° вЂ” Р·Р°РіРѕР»РѕРІРѕРє.
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
                        "РєРѕР»РѕРЅРѕРє " + cells.Length + ", РѕР¶РёРґР°Р»РѕСЃСЊ " + header.Length);
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var c = 0; c < header.Length; c++) row[header[c]] = cells[c].Trim();
                rows.Add(row);
            }
            if (header == null) throw new DataException(Path.GetFileName(path), "РЅРµС‚ Р·Р°РіРѕР»РѕРІРєР°");
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
                if (_byId.ContainsKey(id)) throw new DataException("table", "РґСѓР±Р»РёРєР°С‚ id: " + id);
                _byId.Add(id, all[i]);
            }
        }

        public T Get(string id)
        {
            if (!_byId.TryGetValue(id, out var value)) throw new DataException("table", "РЅРµС‚ id: " + id);
            return value;
        }

        public bool TryGet(string id, out T value) { return _byId.TryGetValue(id, out value); }
    }
}
```

- [ ] **Step 4: РќР°РїРёСЃР°С‚СЊ С‚РёРїС‹ СЃС‚СЂРѕРє**

`Rows.cs` вЂ” РІСЃРµ С‚РёРїС‹ РїСѓР±Р»РёС‡РЅС‹Рµ, РїРѕР»СЏ-СЃРІРѕР№СЃС‚РІР° С‚РѕР»СЊРєРѕ РґР»СЏ С‡С‚РµРЅРёСЏ:

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

- [ ] **Step 5: РќР°РїРёСЃР°С‚СЊ `DataLoader`**

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
                throw new DataException("tuning.csv", "schema_version " + t.Tuning.GetInt("schema_version") + ", РѕР¶РёРґР°Р»РѕСЃСЊ " + SchemaVersion);

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
                        throw new DataException("zones.csv", "Р·РѕРЅР° " + z.Id + " СЃСЃС‹Р»Р°РµС‚СЃСЏ РЅР° РЅРµРёР·РІРµСЃС‚РЅРѕРіРѕ СЃРѕСЃРµРґР° " + nb);

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
                    throw new DataException("table", "РѕР¶РёРґР°Р»РѕСЃСЊ id:С‡РёСЃР»Рѕ, РїРѕР»СѓС‡РµРЅРѕ " + pair);
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

РҐРµР»РїРµСЂС‹ `Get`, `I`, `F`, `SplitList` (СЃРїРёСЃРѕРє `a|b`, РїР°СЂС‹ `a:1|b:2`):

```csharp
private static string Get(Dictionary<string, string> r, string path, string col)
{
    if (!r.TryGetValue(col, out var v) || string.IsNullOrEmpty(v))
        throw new DataException(Path.GetFileName(path), "РїСѓСЃС‚Р°СЏ РєРѕР»РѕРЅРєР° " + col + " РІ СЃС‚СЂРѕРєРµ " + string.Join(";", r.Values));
    return v;
}
private static int I(Dictionary<string, string> r, string path, string col)
{
    if (!int.TryParse(Get(r, path, col), System.Globalization.NumberStyles.Integer,
        System.Globalization.CultureInfo.InvariantCulture, out var v))
        throw new DataException(Path.GetFileName(path), "РЅРµ С‡РёСЃР»Рѕ: " + col + "=" + Get(r, path, col));
    return v;
}
private static float F(Dictionary<string, string> r, string path, string col)
{
    if (!float.TryParse(Get(r, path, col), System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture, out var v))
        throw new DataException(Path.GetFileName(path), "РЅРµ С‡РёСЃР»Рѕ: " + col + "=" + Get(r, path, col));
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
    public string Get(string key) => _raw.TryGetValue(key, out var v) ? v : throw new DataException("tuning.csv", "РЅРµС‚ РєР»СЋС‡Р° " + key);
    public int GetInt(string key) => int.Parse(Get(key), System.Globalization.CultureInfo.InvariantCulture);
    public float GetFloat(string key) => float.Parse(Get(key), System.Globalization.CultureInfo.InvariantCulture);
    public int[] GetIntList(string key) => Get(key).Split('|').Select(s => int.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
}
```

- [ ] **Step 6: Р”РѕР±Р°РІРёС‚СЊ РїСЂРѕРІРµСЂРєРё РІРЅРµС€РЅРёС… РєР»СЋС‡РµР№**

Р’ `Load`, РїРѕСЃР»Рµ С‡С‚РµРЅРёСЏ РІСЃРµС… С‚Р°Р±Р»РёС†:

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

`RequireRefs` вЂ” РѕР±РѕР±С‰С‘РЅРЅР°СЏ, Р±РµР· `dynamic`:

```csharp
private static void RequireRefs<T>(IEnumerable<string> ids, Table<T> table, string file, string col)
{
    foreach (var id in ids)
        if (!table.TryGet(id, out _))
            throw new DataException(file, "РЅРµРёР·РІРµСЃС‚РЅР°СЏ СЃСЃС‹Р»РєР° " + col + "=" + id);
}
```

- [ ] **Step 7: Р—Р°РїСѓСЃС‚РёС‚СЊ РїСЂРѕРІРµСЂРєРё**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: `ok   tables load`, `ok   unknown zone in neighbours fails`, РєРѕРґ 0.

- [ ] **Step 8: РљРѕРјРјРёС‚**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(core): Р·Р°РіСЂСѓР·С‡РёРє CSV СЃ РІР°Р»РёРґР°С†РёРµР№ РІРЅРµС€РЅРёС… РєР»СЋС‡РµР№"
```

---

### Task 6: РњРёСЂ Рё РЅРѕС‡РЅР°СЏ СЃРёРјСѓР»СЏС†РёСЏ

**Files:**
- Create: `Assets/Scripts/Core/Sim/World.cs`
- Create: `Assets/Scripts/Core/Sim/NightSim.cs`
- Modify: `tools/sim/Program.cs` (РїСЂРѕРІРµСЂРєРё РїСЂР°РІРёР» РЅРѕС‡Рё)

**Interfaces:**
- Produces:
  - `World World.Create(Tables tables)` вЂ” Р·РѕРЅС‹ РёР· `zone_beasts.initial_count`, РєСЂРёСЃС‚Р°Р»Р»С‹ 0, РєРѕСЂРј 0, `NestXp 0`, `Day 1`.
  - `ZoneState` СЃ РїРѕР»СЏРјРё `ZoneId`, `Beasts` (`Dictionary<string,int>`), `Crystals`, `Forage`, `NestXp`, `Nest` (`NestState`), `Scouted`.
  - `NestState { bool Exists; int Beasts; int DaysSinceSpawn; }`
  - `NightReport NightSim.AdvanceNight(World world, Tables tables, Rng rng)` вЂ” РѕР±РЅРѕРІР»СЏРµС‚ РјРёСЂ, РІРѕР·РІСЂР°С‰Р°РµС‚ `NightReport` СЃРѕ `List<NightEvent>` Рё `List<string> Lines`.
  - `World.BeastCount(string zoneId)`.

- [ ] **Step 1: РќР°РїРёСЃР°С‚СЊ РїР°РґР°СЋС‰РёРµ РїСЂРѕРІРµСЂРєРё**

Р’ `Program.cs`:

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
    NightSim.AdvanceNight(w, t, new Rng(1));   // РєРѕСЂРј РµСЃС‚СЊ, РєСЂРёСЃС‚Р°Р»Р» С†РµР»
    z.Forage = 0; z.Crystals = 3;
    NightSim.AdvanceNight(w, t, new Rng(1));   // РєРѕСЂРј РєРѕРЅС‡РёР»СЃСЏ вЂ” РµСЃС‚ РєСЂРёСЃС‚Р°Р»Р»
    return z.NestXp > 0;
});
```

- [ ] **Step 2: Р—Р°РїСѓСЃС‚РёС‚СЊ вЂ” СѓР±РµРґРёС‚СЊСЃСЏ, С‡С‚Рѕ РїР°РґР°РµС‚**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: РѕС€РёР±РєР° РєРѕРјРїРёР»СЏС†РёРё `'World' could not be found`.

- [ ] **Step 3: РќР°РїРёСЃР°С‚СЊ `World`**

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

- [ ] **Step 4: РќР°РїРёСЃР°С‚СЊ `NightSim`**

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

            // 1. РџСЂРёС…РѕРґ РµРґС‹.
            foreach (var z in world.Zones.Values)
            {
                var def = tables.Zones.Get(z.ZoneId);
                z.Forage = def.ForagePerDay;
                z.Crystals += def.CrystalsPerDay;
            }

            // 2. РљРѕСЂРјР»РµРЅРёРµ: РєРѕСЂРј в†’ С‚СЂСѓРї в†’ РєСЂРёСЃС‚Р°Р»Р» (С‚СЂСѓРїРѕРІ РІ С€Р°РіРµ 1 РЅРµС‚).
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

            // 3. РњРёРіСЂР°С†РёСЏ РіРѕР»РѕРґРЅС‹С… РЅР°СЂСѓР¶Сѓ, РЅРµ Р±РѕР»СЊС€Рµ migration_per_night РЅР° Р·РѕРЅСѓ.
            var limit = t.GetInt("migration_per_night");
            foreach (var pair in hungry)
            {
                var z = world.Zones[pair.Key];
                var outward = OutwardNeighbour(tables, pair.Key);
                if (outward == null) continue;
                var move = Math.Min(Math.Min(pair.Value, limit), world.BeastCount(pair.Key));
                for (var i = 0; i < move; i++) MoveOneBeast(world, pair.Key, outward);
                if (move > 0) report.Add($"{tables.Zones.Get(pair.Key).Name}: {move} С‚РІР°СЂ. СѓС…РѕРґРёС‚ РІ {tables.Zones.Get(outward).Name}");
            }

            // 4. Р“РЅРµР·РґРѕ РёР· РѕРїС‹С‚Р°.
            var xpToNest = t.GetInt("xp_to_nest");
            foreach (var z in world.Zones.Values)
            {
                if (z.Nest.Exists || z.NestXp < xpToNest) continue;
                z.NestXp -= xpToNest;
                z.Nest.Exists = true; z.Nest.Beasts = 0; z.Nest.DaysSinceSpawn = 0;
                report.Add($"{tables.Zones.Get(z.ZoneId).Name}: РїРѕСЏРІРёР»РѕСЃСЊ РіРЅРµР·РґРѕ");
            }

            // 5. Р“РЅРµР·РґРѕ РїР»РѕРґРёС‚.
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
                        report.Add($"{tables.Zones.Get(z.ZoneId).Name}: РіРЅРµР·РґРѕ СЂР°СЃС‚РёС‚ С‚РІР°СЂСЊ");
                    }
                }
            }

            // 6. РљСЂРёСЃС‚Р°Р»Р»С‹, РєРѕС‚РѕСЂС‹Рµ РЅРёРєС‚Рѕ РЅРµ РІР·СЏР», РІРїРёС‚С‹РІР°СЋС‚СЃСЏ.
            var life = t.GetInt("crystal_life_days");
            foreach (var z in world.Zones.Values)
            {
                var def = tables.Zones.Get(z.ZoneId);
                var capCrystals = def.CrystalsPerDay * life;
                if (z.Crystals > capCrystals) z.Crystals = capCrystals;
            }

            world.Day++;
            report.Add($"РќР°СЃС‚СѓРїРёР» РґРµРЅСЊ {world.Day}");
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

        /// Р“РѕР»РѕРґРЅР°СЏ С‚РІР°СЂСЊ РёРґС‘С‚ РЅР°СЂСѓР¶Сѓ: Рє РґРµСЂРµРІРЅРµ. Р С‘Р±СЂР° Р±РµСЂС‘Рј РёР· zones.csv,
        /// В«РЅР°СЂСѓР¶СѓВ» = Р·РѕРЅР° СЃ РјРµРЅСЊС€РµР№ РіР»СѓР±РёРЅРѕР№; РѕРєСЂР°РёРЅР° СЃРјРѕС‚СЂРёС‚ РЅР° РґРµСЂРµРІРЅСЋ.
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

- [ ] **Step 5: Р—Р°РїСѓСЃС‚РёС‚СЊ РїСЂРѕРІРµСЂРєРё**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: С‚СЂРё РїСЂРѕРІРµСЂРєРё РЅРѕС‡Рё Рё РїСЂРµРґС‹РґСѓС‰РёРµ вЂ” `ok`, РєРѕРґ 0.

- [ ] **Step 6: РљРѕРјРјРёС‚**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(core): РјРёСЂ Рё РЅРѕС‡РЅР°СЏ СЃРёРјСѓР»СЏС†РёСЏ Р»РµСЃР°"
```

---

### Task 7: РЇРґСЂРѕ вЂ” РЅР°Р·РЅР°С‡РµРЅРёСЏ Рё Р°РІС‚РѕСЂРµР·РѕР»РІ РІС‹Р»Р°Р·РєРё

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
  - `Person.SkillLevel(Person p, string skillId)` (РІ `Data/People.cs`).

- [ ] **Step 1: РќР°РїРёСЃР°С‚СЊ РїР°РґР°СЋС‰СѓСЋ РїСЂРѕРІРµСЂРєСѓ**

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

- [ ] **Step 2: Р—Р°РїСѓСЃС‚РёС‚СЊ вЂ” СѓР±РµРґРёС‚СЊСЃСЏ, С‡С‚Рѕ РїР°РґР°РµС‚**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: `'PartyAssignment' could not be found`.

- [ ] **Step 3: РќР°РїРёСЃР°С‚СЊ `DayPlan` Рё РЅР°РІС‹РєРё С‡РµР»РѕРІРµРєР°**

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

- [ ] **Step 4: РќР°РїРёСЃР°С‚СЊ `ExpeditionSim`**

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
            if (workHours <= 0) { result.Log.Add("Р”РѕСЂРѕРіР° СЃСЉРµРґР°РµС‚ РІРµСЃСЊ РґРµРЅСЊ: РѕС‚СЂСЏРґ РЅРµ РґРѕС€С‘Р»"); return result; }

            var density = Density(world, zoneState);
            var intensity = party.Behavior == Behavior.Avoid ? 0.5 : party.Behavior == Behavior.Clean ? 2.5 : 1.0;
            if (party.PersonIds.Count > 2) intensity *= 1.2;
            if (zoneState.AlertDays > 0) intensity *= 1.2;

            var slotsPerPerson = t.GetInt("slots_per_person");
            var free = party.PersonIds.Count * slotsPerPerson;

            for (var hour = 1; hour <= workHours; hour++)
            {
                result.HoursUsed = hour;
                if (result.Stamina <= 0) { result.Log.Add($"{zone.Name}: РѕС‚СЂСЏРґ РІС‹Р±РёР»СЃСЏ РёР· СЃРёР»"); break; }

                // РќР°С…РѕРґРєРё.
                foreach (var personId in party.PersonIds)
                {
                    var person = tables.Roster.Get(personId);
                    var skill = People.SkillLevel(tables, person, "gathering");
                    var chance = t.GetFloat("find_base") + t.GetFloat("find_per_skill") * skill;
                    if (!rng.Chance(chance)) continue;

                    var pick = PickResource(tables, zone.Id, rng);
                    if (pick == null) continue;
                    var item = tables.Items.Get(pick);
                    if (free < item.Slots) { result.Log.Add($"{person.Name}: РјРµС€РѕРє РїРѕР»РѕРЅ, {item.Name} Р±СЂРѕС€РµРЅРѕ"); continue; }
                    free -= item.Slots;
                    AddFound(result, pick);
                }

                // Р’СЃС‚СЂРµС‡Р°.
                var encounter = t.GetFloat("encounter_per_hour") * density * intensity;
                if (!rng.Chance(encounter)) continue;
                result.Encountered = true;
                result.Stamina -= 1;
                if (rng.Chance(0.5)) { result.Log.Add($"{zone.Name}: С‚РІР°СЂСЊ СЂСЏРґРѕРј, РѕС‚СЂСЏРґ РѕС‚РѕС€С‘Р»"); continue; }
                if (rng.Chance(0.5)) { result.Log.Add($"{zone.Name}: Р±РѕР№ вЂ” РїРѕР±РµРґР°, С‚СѓС€Р° Р±СЂРѕС€РµРЅР°"); continue; }
                result.Wounded = true;
                result.Log.Add($"{zone.Name}: Р±РѕР№ вЂ” СЂР°РЅРµРЅРёРµ, РѕС‚СЂСЏРґ РІРѕР·РІСЂР°С‰Р°РµС‚СЃСЏ");
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

- [ ] **Step 5: Р—Р°РїСѓСЃС‚РёС‚СЊ РїСЂРѕРІРµСЂРєРё**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: РІСЃРµ РїСЂРѕРІРµСЂРєРё `ok`, РєРѕРґ 0.

- [ ] **Step 6: РљРѕРјРјРёС‚**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(core): РЅР°Р·РЅР°С‡РµРЅРёСЏ Рё Р°РІС‚РѕСЂРµР·РѕР»РІ РІС‹Р»Р°Р·РєРё РїРѕ С‡Р°СЃР°Рј"
```

---

### Task 8: РЎС†РµРЅР°, UIDocument Рё РєР°СЂС‚Р° Р»РµСЃР°

**Files:**
- Create: `Assets/UI/Map.uxml`
- Create: `Assets/UI/Map.uss`
- Create: `Assets/Scripts/Game/MapScreen.cs`
- Create: `Assets/Scripts/Game/MapController.cs`
- Create: `Assets/Scripts/Game/ZoneNodeView.cs`
- Create: `Assets/Scripts/Game/EdgeLine.cs`
- Create: `Assets/Scenes/Main.unity` (Р¶РёРІРѕР№ СЂРµРґР°РєС‚РѕСЂ)

**Interfaces:**
- Consumes: `Tables`, `World`, `World.Create`.
- Produces: СЃС†РµРЅР° `Main` СЃ СѓР·Р»Р°РјРё Р·РѕРЅ Рё СЃРІСЏР·СЏРјРё; `MapScreen.Tables`, `MapScreen.World` РґРѕСЃС‚СѓРїРЅС‹ РґСЂСѓРіРёРј РєРѕРјРїРѕРЅРµРЅС‚Р°Рј.

- [ ] **Step 1: РќР°РїРёСЃР°С‚СЊ `Map.uxml`**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:u="UnityEditor.UIElements">
  <Style src="Map.uss" />
  <ui:VisualElement name="root" class="root">
    <ui:VisualElement name="map" class="map" />
    <ui:VisualElement name="left-panel" class="panel left">
      <ui:Label text="Р–РРўР•Р›Р" class="panel-title" />
      <ui:ScrollView name="roster" class="roster" />
      <ui:Button name="send-button" text="Р’ Р›Р•РЎ" class="action" />
    </ui:VisualElement>
    <ui:VisualElement name="right-panel" class="panel right">
      <ui:Label name="zone-name" text="вЂ”" class="panel-title" />
      <ui:Label name="zone-info" text="" class="zone-info" />
      <ui:Label name="zone-beasts" text="" class="zone-info" />
    </ui:VisualElement>
    <ui:VisualElement name="bottom-bar" class="bottom">
      <ui:Label name="day-label" text="Р”РµРЅСЊ 1" class="day-label" />
      <ui:Button name="end-day" text="РљРћРќР•Р¦ Р”РќРЇ" class="action primary" />
    </ui:VisualElement>
    <ui:VisualElement name="report" class="report hidden" />
  </ui:VisualElement>
</ui:UXML>
```

- [ ] **Step 2: РќР°РїРёСЃР°С‚СЊ `Map.uss`**

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

- [ ] **Step 3: РќР°РїРёСЃР°С‚СЊ `EdgeLine` Рё `ZoneNodeView`**

`EdgeLine.cs` вЂ” Р»РёРЅРёСЏ РјРµР¶РґСѓ СѓР·Р»Р°РјРё:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// Р›РёРЅРёСЏ СЃРІСЏР·Рё Р·РѕРЅ: РґРІР° СѓР·Р»Р° РІ РєРѕРѕСЂРґРёРЅР°С‚Р°С… РєР°СЂС‚С‹.
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

- [ ] **Step 4: РќР°РїРёСЃР°С‚СЊ `MapController` вЂ” РїРѕСЃС‚СЂРѕРµРЅРёРµ РєР°СЂС‚С‹ РёР· С‚Р°Р±Р»РёС†**

```csharp
using System.Collections.Generic;
using ForestGuild.Core.Data;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// РЎС‚СЂРѕРёС‚ РєР°СЂС‚Сѓ: СѓР·Р»С‹ РїРѕ map_x/map_y, СЂС‘Р±СЂР° РїРѕ neighbours. РљРѕРѕСЂРґРёРЅР°С‚С‹ РЅРѕСЂРјР°Р»РёР·РѕРІР°РЅС‹.
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
                    if (string.CompareOrdinal(z.Id, nb) >= 0) continue; // СЂРµР±СЂРѕ РѕРґРёРЅ СЂР°Р·
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

- [ ] **Step 5: РќР°РїРёСЃР°С‚СЊ `MapScreen`**

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
            _dayLabel.text = "Р”РµРЅСЊ " + World.Day;
        }

        private void ShowZone(string zoneId)
        {
            var zone = Tables.Zones.Get(zoneId);
            var state = World.Zones[zoneId];
            _zoneName.text = zone.Name;
            _zoneInfo.text = $"Р“Р»СѓР±РёРЅР°: {zone.Depth}, РґРѕСЂРѕРіР° {zone.TravelHours:0.#} С‡\n" +
                             $"РљСЂРёСЃС‚Р°Р»Р»С‹: {state.Crystals}, РєРѕСЂРј: {state.Forage}";
            _zoneBeasts.text = state.Beasts.Count == 0
                ? "РўРІР°СЂРµР№ РЅРµ РІРёРґРЅРѕ"
                : string.Join("\n", state.Beasts.Select(kv => $"{Tables.Beasts.Get(kv.Key).Name}: {kv.Value}"));
        }
    }
}
```

(`using System.Linq;` РґРѕР±Р°РІРёС‚СЊ.)

- [ ] **Step 6: РЎРѕР·РґР°С‚СЊ СЃС†РµРЅСѓ Р¶РёРІС‹Рј СЂРµРґР°РєС‚РѕСЂРѕРј**

Р§РµСЂРµР· `unity_eval` (РѕРґРЅРѕСЃС‚СЂРѕС‡РЅРёРє РЅРµ РїРѕРґРѕР№РґС‘С‚ вЂ” РёСЃРїРѕР»СЊР·РѕРІР°С‚СЊ `run_script` РёР· `unity-pipeline`, С„Р°Р№Р» РІРЅРµ `Assets/`):

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

РћР¶РёРґР°РЅРёРµ: `Assets/Scenes/Main.unity` СЃСѓС‰РµСЃС‚РІСѓРµС‚, РІ РєРѕРЅСЃРѕР»Рё СЂРµРґР°РєС‚РѕСЂР° РЅРµС‚ РѕС€РёР±РѕРє.

- [ ] **Step 7: РџСЂРѕРІРµСЂРёС‚СЊ РІРёР·СѓР°Р»СЊРЅРѕ**

РћС‚РєСЂС‹С‚СЊ `Main`, РІРѕР№С‚Рё РІ Play Mode, СЃРґРµР»Р°С‚СЊ СЃРєСЂРёРЅС€РѕС‚ С‡РµСЂРµР· `unity_command screenshot` (РёР»Рё `look`), СѓР±РµРґРёС‚СЊСЃСЏ: РІРёРґРЅС‹ 6 СѓР·Р»РѕРІ, СЃРІСЏР·Рё, РїР°РЅРµР»Рё, В«Р”РµРЅСЊ 1В».

- [ ] **Step 8: РљРѕРјРјРёС‚**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(ui): РєР°СЂС‚Р° Р»РµСЃР° РёР· С‚Р°Р±Р»РёС† (UI Toolkit, СѓР·Р»С‹ Рё СЃРІСЏР·Рё)"
```

---

### Task 9: Р РѕСЃС‚РµСЂ, РЅР°Р·РЅР°С‡РµРЅРёРµ РѕС‚СЂСЏРґРѕРІ Рё РєРѕРЅРµС† РґРЅСЏ

**Files:**
- Modify: `Assets/UI/Map.uxml` (РєРЅРѕРїРєР° В«СЃС„РѕСЂРјРёСЂРѕРІР°С‚СЊ РѕС‚СЂСЏРґВ» РІ СѓР·Р»Рµ Р·РѕРЅС‹)
- Create: `Assets/Scripts/Game/RosterPanel.cs`
- Create: `Assets/Scripts/Game/DayFlow.cs`

**Interfaces:**
- Consumes: `MapScreen.Tables`, `MapScreen.World`, `DayPlan`, `ExpeditionSim`, `NightSim`.
- Produces: СЂР°Р±РѕС‡РёР№ РґРµРЅСЊ 1: РѕС‚СЂСЏРґ РѕС‚РїСЂР°РІР»РµРЅ, В«РљРћРќР•Р¦ Р”РќРЇВ» в†’ РѕС‚С‡С‘С‚.

- [ ] **Step 1: РќР°РїРёСЃР°С‚СЊ РїР°РґР°СЋС‰СѓСЋ РїСЂРѕРІРµСЂРєСѓ РІ СЏРґСЂРµ**

Р’ `Program.cs`:

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

- [ ] **Step 2: Р—Р°РїСѓСЃС‚РёС‚СЊ вЂ” СѓР±РµРґРёС‚СЊСЃСЏ, С‡С‚Рѕ РїР°РґР°РµС‚**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: `'DayFlow' could not be found`.

- [ ] **Step 3: РќР°РїРёСЃР°С‚СЊ `DayFlow` РІ СЏРґСЂРµ (РЅРµ РІ `Game/`)**

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

    /// РћРґРёРЅ РґРµРЅСЊ: РІС‹Р»Р°Р·РєРё в†’ РЅРѕС‡СЊ. Р­С‚Рѕ РµРґРёРЅСЃС‚РІРµРЅРЅРѕРµ РјРµСЃС‚Рѕ, РіРґРµ РґРµРЅСЊ В«РёРґС‘С‚В».
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
                    report.Lines.Add($"РџСЂРёРЅРµСЃРµРЅРѕ: {_tables.Items.Get(found.Key).Name} Г—{found.Value}");
            }
            report.Night = NightSim.AdvanceNight(_world, _tables, _rng);
            report.Lines.AddRange(report.Night.Lines);
            return report;
        }
    }
}
```

- [ ] **Step 4: Р—Р°РїСѓСЃС‚РёС‚СЊ РїСЂРѕРІРµСЂРєСѓ**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: `ok   day plan sends people to zone`, РєРѕРґ 0.

- [ ] **Step 5: РќР°РїРёСЃР°С‚СЊ `RosterPanel` (UI)**

```csharp
using System.Collections.Generic;
using System.Linq;
using ForestGuild.Core.Data;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// РЎРїРёСЃРѕРє Р¶РёС‚РµР»РµР№ СЃ РІС‹Р±РѕСЂРѕРј: РєР»РёРє вЂ” РІР·СЏС‚СЊ/РѕС‚РїСѓСЃС‚РёС‚СЊ.
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
                var button = new Button { text = $"{person.Name} В· {PersonLine(person)}", name = "person-" + person.Id };
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

Р”РѕР±Р°РІРёС‚СЊ РІ `.uss`: `.selected { border-color: rgb(240, 190, 90); border-width: 3px; }`

- [ ] **Step 6: РЎРІСЏР·Р°С‚СЊ РІСЃС‘ РІ `MapScreen`**

Р”РѕРїРёСЃР°С‚СЊ РІ `MapScreen`:

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
    if (Map.Selected == null) { ShowMessage("РЎРЅР°С‡Р°Р»Р° РІС‹Р±РµСЂРёС‚Рµ Р·РѕРЅСѓ"); return; }
    if (_roster.Selected.Count == 0) { ShowMessage("Р’С‹Р±РµСЂРёС‚Рµ С…РѕС‚СЏ Р±С‹ РѕРґРЅРѕРіРѕ Р¶РёС‚РµР»СЏ"); return; }
    var party = new PartyAssignment { ZoneId = Map.Selected };
    party.PersonIds.AddRange(_roster.Selected);
    _plan.Parties.Add(party);
    ShowMessage($"РћС‚СЂСЏРґ РёР· {party.PersonIds.Count} РёРґС‘С‚ РІ {Tables.Zones.Get(Map.Selected).Name}");
    _roster.Selected.Clear();
}

private void EndDay()
{
    var report = _flow.RunDay(_plan);
    _plan = new DayPlan();
    _dayLabel.text = "Р”РµРЅСЊ " + World.Day;
    ShowReport(report);
    RefreshMapState();
}
```

`ShowMessage`/`ShowReport`/`RefreshMapState` вЂ” С‚РѕР¶Рµ РґРѕРїРёСЃР°С‚СЊ РІ `MapScreen`:

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
    _report.Add(new Label("Р”РµРЅСЊ " + World.Day));
    foreach (var line in report.Lines) _report.Add(new Label("В· " + line));
    _report.RemoveFromClassList("hidden");
}

private void RefreshMapState()
{
    if (Map.Selected != null) ShowZone(Map.Selected);
}
```

- [ ] **Step 7: РџСЂРѕРІРµСЂРёС‚СЊ РІ Play Mode**

РЎРєСЂРёРЅС€РѕС‚: РѕС‚СЂСЏРґ РѕС‚РїСЂР°РІР»РµРЅ, РЅР°Р¶Р°С‚РёРµ В«РљРћРќР•Р¦ Р”РќРЇВ» РґР°С‘С‚ РѕС‚С‡С‘С‚ СЃ СЃС‚СЂРѕРєР°РјРё Рѕ РЅР°С…РѕРґРєР°С… Рё РЅРѕС‡Рё, РґРµРЅСЊ СЃС‚Р°РЅРѕРІРёС‚СЃСЏ 2.

- [ ] **Step 8: РљРѕРјРјРёС‚**

```powershell
cd F:\WORK\ForestGuild; git add -A; git commit -m "feat(game): СЂРѕСЃС‚РµСЂ, РѕС‚РїСЂР°РІРєР° РѕС‚СЂСЏРґР° Рё РєРѕРЅРµС† РґРЅСЏ СЃ РЅРѕС‡РЅС‹Рј РѕС‚С‡С‘С‚РѕРј"
```

---

### Task 10: Р”РѕРєСѓРјРµРЅС‚Р°С†РёСЏ Рё РїСЂРёС‘РјРєР° С€Р°РіР°

**Files:**
- Modify: `docs/architecture.md` (РєРѕР»РѕРЅРєРё `map_x/map_y` РІ `zones.csv`, С„Р°РєС‚РёС‡РµСЃРєРёР№ СЃРїРёСЃРѕРє С„Р°Р№Р»РѕРІ)
- Create: `docs/current/2026-11-07-ui-slice/result.md`

**Interfaces:** РЅРµС‚.

- [ ] **Step 1: РћР±РЅРѕРІРёС‚СЊ `docs/architecture.md`**

Р’ С‚Р°Р±Р»РёС†Сѓ СЃРїСЂР°РІРѕС‡РЅРёРєРѕРІ РґРѕР±Р°РІРёС‚СЊ `map_x; map_y` РІ СЃС‚СЂРѕРєСѓ `zones.csv` Рё РґРѕРїРёСЃР°С‚СЊ СЂР°Р·РґРµР»:

```markdown
## 10. РЁР°Рі 1: С‡С‚Рѕ СѓР¶Рµ СЂР°Р±РѕС‚Р°РµС‚

- `tools/sim --selfcheck` вЂ” РїСЂРѕРІРµСЂРєРё СЏРґСЂР° Р±РµР· СЂРµРґР°РєС‚РѕСЂР°.
- РЎС†РµРЅР° `Assets/Scenes/Main.unity`: РєР°СЂС‚Р° РёР· `zones.csv`, РїР°РЅРµР»СЊ Р·РѕРЅС‹, СЂРѕСЃС‚РµСЂ, В«РєРѕРЅРµС† РґРЅСЏВ».
- РќРѕС‡СЊ: РєРѕСЂРјР»РµРЅРёРµ (РєРѕСЂРј в†’ С‚СЂСѓРї в†’ РєСЂРёСЃС‚Р°Р»Р»), РјРёРіСЂР°С†РёСЏ РЅР°СЂСѓР¶Сѓ, РіРЅРµР·РґРѕ РїРѕ РѕРїС‹С‚Сѓ, РїР»РѕРґ РіРЅРµР·РґР°.
- Р’С‹Р»Р°Р·РєР°: Р°РІС‚РѕСЂРµР·РѕР»РІ РїРѕ С‡Р°СЃР°Рј (РЅР°С…РѕРґРєРё, РІСЃС‚СЂРµС‡Рё 50/50 РѕС‚С…РѕРґ/Р±РѕР№), Р±РµР· РїР°СѓР·.
```

- [ ] **Step 2: РќР°РїРёСЃР°С‚СЊ `result.md`**

```markdown
# РЁР°Рі 1 вЂ” РєР°СЂРєР°СЃ, РєР°СЂС‚Р°, РїРµСЂРІС‹Р№ РґРµРЅСЊ

Р”Р°С‚Р°: <РґР°С‚Р°>. РЎС‚Р°С‚СѓСЃ: РїСЂРёРЅСЏС‚Рѕ.

## РЎРґРµР»Р°РЅРѕ
- Unity 6000.6.6f1 (URP 2D), РїСЂРѕРµРєС‚ РІ РєРѕСЂРЅРµ СЂРµРїРѕР·РёС‚РѕСЂРёСЏ.
- 14 CSV-СЃРїСЂР°РІРѕС‡РЅРёРєРѕРІ, Р·Р°РіСЂСѓР·С‡РёРє СЃ РІР°Р»РёРґР°С†РёРµР№ РІРЅРµС€РЅРёС… РєР»СЋС‡РµР№.
- РЇРґСЂРѕ: `Rng`, `World`, `NightSim`, `DayPlan`, `ExpeditionSim`, `DayFlow`.
- UI Toolkit: РєР°СЂС‚Р° (СѓР·Р»С‹, СЂС‘Р±СЂР°), РїР°РЅРµР»СЊ Р·РѕРЅС‹, СЂРѕСЃС‚РµСЂ, РєРЅРѕРїРєР° РґРЅСЏ, РѕС‚С‡С‘С‚.
- `tools/sim --selfcheck`: N РїСЂРѕРІРµСЂРѕРє.

## Р§РµРј РїСЂРѕРІРµСЂРµРЅРѕ
- `dotnet run ... --selfcheck` в†’ РІСЃРµ РїСЂРѕРІРµСЂРєРё ok, РєРѕРґ 0 (РІС‹РІРѕРґ РїСЂРёР»РѕР¶РёС‚СЊ).
- Play Mode: СЃРєСЂРёРЅС€РѕС‚ РєР°СЂС‚С‹ Рё СЃРєСЂРёРЅС€РѕС‚ РѕС‚С‡С‘С‚Р° РґРЅСЏ 2.
- `grep` РїРѕ `Core` РЅРµ РЅР°С…РѕРґРёС‚ `UnityEngine`.

## Р§С‚Рѕ РЅРµ РІРѕС€Р»Рѕ
РџР°СѓР·С‹ РІ РІС‹Р»Р°Р·РєРµ, С‚СѓС€Р°/СЂР°Р·РґРµР»РєР°, СЂР°РЅРµРЅС‹Р№, РїРѕР»РЅРѕС†РµРЅРЅС‹Р№ Р±РѕР№, РІРѕР»РЅР° РЅР° 21-Р№ РґРµРЅСЊ.
```

- [ ] **Step 3: РџСЂРѕРІРµСЂРёС‚СЊ, С‡С‚Рѕ СЏРґСЂРѕ Р±РµР· Unity**

```powershell
cd F:\WORK\ForestGuild; Get-ChildItem Assets\Scripts\Core -Recurse -Filter *.cs |
  Select-String -Pattern "using UnityEngine" | Out-String
```

РћР¶РёРґР°РЅРёРµ: РїСѓСЃС‚Рѕ. Р•СЃР»Рё С‡С‚Рѕ-С‚Рѕ РЅР°С€Р»РѕСЃСЊ вЂ” СѓР±СЂР°С‚СЊ.

- [ ] **Step 4: Р¤РёРЅР°Р»СЊРЅС‹Р№ РїСЂРѕРіРѕРЅ**

Run: `dotnet run --project F:\WORK\ForestGuild\tools\sim -- --selfcheck`
РћР¶РёРґР°РЅРёРµ: РєРѕРґ 0, РІСЃРµ `ok`.

- [ ] **Step 5: РљРѕРјРјРёС‚ Рё РїРµСЂРµРЅРѕСЃ РІ Р°СЂС…РёРІ**

```powershell
cd F:\WORK\ForestGuild
git add -A
git commit -m "docs: СЂРµР·СѓР»СЊС‚Р°С‚ С€Р°РіР° 1 Рё РѕР±РЅРѕРІР»РµРЅРёРµ Р°СЂС…РёС‚РµРєС‚СѓСЂС‹"
```

РџР°РїРєСѓ `docs/current/2026-11-07-ui-slice` РїРµСЂРµРЅРµСЃС‚Рё РІ `docs/archive/` **РїРѕСЃР»Рµ** РїСЂРёС‘РјРєРё РїРѕР»СЊР·РѕРІР°С‚РµР»РµРј, РѕС‚РґРµР»СЊРЅС‹Рј РєРѕРјРјРёС‚РѕРј.
