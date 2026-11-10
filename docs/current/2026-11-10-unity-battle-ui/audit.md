# Аудит документов и кода (2026-11-10)

Два субагента проверили документы и код **только чтением**; результаты проверены по исходникам и
исправлено то, что подтвердилось. Ниже — что исправлено, чем доказано и что осталось решением владельца.

## Проверено фактами

- `dotnet run --project tools/sim -- --selfcheck` → **79 проверок, код 0** (было 74).
- `dotnet build tools/sim` → 0 предупреждений, 0 ошибок.
- Unity `recompile` → completed, 0 ошибок.
- Play Mode после правок: встреча (`ВСТРЕЧА: Осколочник`), карточка боя (`БОЙ: Осколочник`,
  `art=shard-attack`), HP словами на доске (`Брунни — тяжело ранен, Скади — ранен`).

## Исправлено в коде (с проверками)

| Что | Было | Стало | Чем доказано |
|---|---|---|---|
| Пустой отряд в `Expedition.Wound` | `PersonIds[Next(0)]` → исключение | `Finish("отряд пуст — раненых нет")` | проверка «expedition with an empty party finishes» |
| Пустая стая в `Battle.MakePack` | `beastIds[0]` → `IndexOutOfRange` | `DataException("Battle", …)` | проверка «battle rejects an empty pack» |
| `LoseSomeLoot` | брошенный предмет не возвращал слоты → «мешок полон» без причины | `_freeSlots += item.Slots` | чтение `Find`/`LoseSomeLoot`, слоты возвращаются |
| Слово о HP | границы 0.25/0.5/0.8 зашиты в `Battle` | из `combat_tuning.csv` (`hurt_threshold`, `wound_threshold`, `scratches_threshold`) | проверка «hp word boundaries come from the csv» |
| Оценка риска при наказе | 400 боёв считались, даже когда карточка не нужна | только при `EncounterOrder.Ask` | `Meet` |
| «Домотать» | игнорировал наказы, всегда отступал | `ExpeditionSim.Default(decision, orders)` — те же правила, что в карточке | проверка «sim follows the standing orders» |
| Прогноз приказа | `Briefing` считал интенсивность по-своему (4 человека — ×1,2 вместо ×1,44) | одна функция `Expedition.EncounterIntensity` | проверка «briefing intensity matches the expedition» |
| Починка/заготовка | `+2` и `+1` в коде и в тексте UI | ключи `repair_per_worker` / `forage_per_worker`, текст из значения | данные + `Works`, `VillageScreen` |
| Миграция ночью | кто уйдёт — решал порядок `Dictionary` | сортировка по ключу, как в `PickPack` | `NightSim.MoveOneBeast` |
| Сценарий «сразу поход» + `continue` | `_dayReport == null` → NRE | `BeginDay` перед `FinishExpeditions` | `MapScreen` |
| Аргументы `tools/sim` | опечатка (`--selfchek`) молча гоняла проверки | неизвестный аргумент → код 2 | `Main` |
| Мёртвый код | счётчики боя, `ambushBonus`, `BattleBeast.Name/Level`, `Density`, `FightChance` | удалены | сборка без предупреждений |

Проверок стало 79: добавлены «battle rejects an empty pack», «expedition with an empty party finishes»,
«sim follows the standing orders», «briefing intensity matches the expedition», «hp word boundaries
come from the csv».

## Исправлено в документах

- **Число проверок** 66 → 79: `AGENTS.md`, `docs/architecture.md`, `docs/decisions.md`.
- **`AGENTS.md`**: «поход по часам» → «по минутам»; убран несуществующий `Start_editor.bat`.
- **`docs/design.md`**: §8.1 «Час как тик» переписан на минуты (600, шаг 10, формула 10 %/час × твари);
  имена зон («Серая Топь», «Стеклянная роща»); «5 видов тварей» → 8 с фактическим списком; арт `_attack`
  больше не «не используется»; точка 21 дня — 14/10/0 «не откалибровано»; штрафы похода и −3 слота
  помечены «в срезе не реализовано»; «снаряжения нет» → фиксировано за человеком; черты — добавлен
  `patient`, отмечено, что в бою черты не работают; свежевание — 60 минут; пояс — одна локация.
- **`docs/ui.md`**: заголовок §14 «сверка 2026-11-10»; HP словами в §4.4/§5; §4.5 и ASCII-макеты — минуты;
  хот-релоада таблиц нет (вместо тоста на `R`); «25 % × плотность» → «0,10/час × число тварей»;
  отладочный тумблер убран; при полном мешке — карточка `BagFull`; `List<Event>`/`LogLines` помечены
  планом; макеты зон — новые имена.
- **`docs/decisions.md`**: ADR-0041 — h2 51,2 вместо «калибровка отложена»; ADR-0043 — походные штрафы
  честно помечены нереализованными (долг T8); ADR-0044 — 43,1 % / 60,2 %.
- **`docs/architecture.md`**: дерево `Data/` и таблица §4 дополнены шестью боевыми таблицами и двумя
  сценарными; «проверяется тестом» → компиляцией без Unity; примеры проверок — реальные.
- **`zone_lore.csv`**: «шесть часов» → «четыре часа» (240 минут по `zones.csv`).
- **Результаты задач**: `combat-integration/result.md` — UI боя уже сделан; `unity-battle-ui/result.md` —
  T1–T5 сделаны, T4/T5 убраны из «осталось».

## Осталось: требует решения владельца

1. **T8 — походные штрафы и −3 слота.** Ключи `wound_speed_mult`, `hurt_speed_mult`, `carry_out_slots`,
   `carry_out_speed_mult` лежат в CSV без читателей. Либо реализовать (не баланс, а правило), либо
   удалить ключи и убрать обещание из ADR-0043. Сейчас помечено как долг.
2. **Роль охотника и черты в бою.** `HunterRole` — только подпись в Сборе; `fight_bonus` не читается.
   Либо реализовать («охраняю» даёт +1 к бою и −20 % к засаде), либо убрать из `ui.md` §4.5/§5.
3. **Мёртвые ключи/колонки CSV** (около 25): `pack_single_strike`, `gatherer_combat_factor`,
   `fight_stamina`, `carry_slots_per_person`, `beast_combat.damage_spread/level`, `beasts.strength/speed`,
   `zone_beasts.weight`, `xp_*`, `heart_influx_days`, `skills_combat.param/branch` и др. Решение:
   удалить или реализовать — сейчас CSV выглядит источником правды, которым не является.
4. **Мёртвый слой UI/ядра**: `DayReport.Lines`/`NightReport.Lines`, `IsOpen` ×6, `MorningBoard.Changed`,
   `DossierScreen.Closed`, `TakeDrops(bool)` — удалить пакетно (около 300 строк).
5. **Методы длиннее 60 строк**: `DataLoader.Load` (214), `Program.Main` (1265), `MapScreen.Awake` (96),
   `Battle.ResolveFighters` (104) — разбить, когда тронем эти файлы.
6. **Мелкие расхождения**: `alert_bonus` есть в коде, но не в валидаторе/документе; `depth` зоны
   (`edge`) уходит в UI как есть; `profession.SkillsAllowed[0]` вместо `hunting` в подписи; постели
   применяются дважды (идемпотентно, но лишний вызов); класс `.battle-body` и арты `fang`/`mole`
   в USS — сироты.

## Что проверено и в порядке

74 существующие проверки остались зелёными; детерминизм (`Rng`, сортировка ключей), Core без
`UnityEngine`, загрузчик с валидацией ссылок, UXML-имена против `Q<>`, все картинки в документах
существуют, карточка боя и слова о HP работают в Play Mode.

## Сделано после аудита (2026-11-10)

Правки по этому списку — `docs/current/2026-11-10-audit-fixes/` (`plan.md`, `result.md`):

- T8: походные штрафы и −3 слота работают (скорость дороги, слоты по состоянию, выброс добычи при
  нехватке ёмкости);
- живые `food_need` (кормление) и `zone_beasts.weight` (выбор вида в стае);
- удалены случайные ключи/колонки CSV (5 ключей `combat_tuning`, `damage_spread`, `beasts.strength/speed`,
  `skills_combat.param`); задел следующего среза помечен в `architecture.md` §4;
- `alert_bonus` в валидаторе, глубина зоны словами, навык `hunting` в подписях, постели один раз,
  класс `pane-line` в вечере.

Проверок стало **81**. Ф4 (мёртвый слой кода) — остался в `result.md` как следующая работа.

