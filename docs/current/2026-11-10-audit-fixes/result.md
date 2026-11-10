# Правки по аудиту — итог

Дата: 2026-11-10. План — `plan.md`. Вход — `docs/current/2026-11-10-unity-battle-ui/audit.md`.

## Сделано

### Ф1. Походные штрафы и −3 слота (T8)

`Expedition` теперь считает скорость отряда и ёмкость мешка по состоянию людей:

- дорога домой — `ceil(минуты / скорость)`, скорость — минимум по людям: 1.0 / `wound_speed_mult` (0.75) /
  `hurt_speed_mult` (0.5), несут — `carry_out_speed_mult` (0.5);
- слоты: `slots_per_person` + черта, у раненого ×`wound_carry_mult`, у тяжело раненого ×`hurt_carry_mult` (0),
  за каждого выбитого −`carry_out_slots`;
- когда ёмкость падает, лишняя добыча выбрасывается с строкой в лог; после боя и решения «нести» вызывается
  `SyncPenalties`.

Доказательства: проверки «wounded walk slower», «wounded carry fewer slots» (9 / 7 / 5 слотов для
здорового / раненого / тяжело раненого отряда); Play Mode `DriveBattle.Carry` — «slots 5 -> 2»,
лог «раненый замедляет отряд — дорога домой дольше».

### Ф2. Живые `food_need` и вес вида

- `NightSim.Feed` читает `beasts.food_need`: тварь ест столько порций, сколько записано; голод считается
  по недоеденным порциям.
- `Expedition.PickPack` выбирает вид с весом `zone_beasts.weight × численность`. Логика «стая одного
  вида» сохранена.

### Ф3. Удалены случайные остатки CSV

`combat_tuning`: `pack_single_strike`, `gatherer_combat_factor`, `gatherer_fights`, `fight_stamina`,
`carry_slots_per_person`. `beast_combat`: колонка `damage_spread`. `beasts`: `strength`, `speed` (дубль
`beast_combat.speed`). `skills_combat`: `param`. Код/`Rows`/`DataLoader`/`architecture.md` обновлены.
Ключи походных штрафов (`wound_*`, `hurt_*`, `carry_out_*`) наоборот **ожили** в Ф1.

Оставлен и помечен в `docs/architecture.md` §4 как «данные без кода (задел)»: `xp_*`, `level_xp`,
`heart_influx_days`, `items.category/tags`, `stations/recipes/recipe_quality`, `skills.kind/max_level`,
`professions.requires_trait`, `traits.fight_bonus`, `equipment.value`, `zones.distance_km`,
`scenarios.notes`, `skills_combat.branch`.

### Ф5. Мелкие исправления

- `alert_bonus` добавлен в валидатор эффектов `DataLoader` (раньше был в коде, но не в данных).
- `DossierScreen`: глубина зоны словами — опушка / пояс / сердце.
- Навык в Сборе и приказе — `hunting` (через `People.PrimarySkill`), а не первый из таблицы.
- Постели применяются один раз — в ночи; вечер может передумать.
- `EveningScreen` строки получили класс `pane-line`; мёртвый `.battle-body` из `Map.uss` удалён.
- `Expedition.TakeDrops` без бессмысленного `bool`.

### Ф4. Мёртвый слой — сделано

Удалены без читателей: `Works.IsAvailable`, `DayPlan.IsBusy`, `DayPlan.WorkingCount`, `DayPlan.MaxParties`,
`World.TotalCrystals`, `Tuning.GetIntList`, `Tuning.Has`, `MusterScreen.SkillLine`, `MorningBoard.Changed`,
`DossierScreen.Closed`; `Program.Main` разгружен — проверки вынесены в `RunChecks()`, неизвестный
аргумент даёт код 2.

Оставлено намеренно: `IsOpen` у экранов (используется пробами `FG/AgentScripts`), `Expedition.Pack`
(нужен карточке/пробам), `DayReport.Lines` / `NightReport.Lines` (на них висят три проверки ночи —
свести к `Entries` можно отдельным проходом), CSS и арт `fang`/`mole` (задел пояса, видов в данных нет),
`DataLoader.Load` 214 строк (разбить при следующем касании). Это записано, а не спрятано.

### Ф6. Проверка

- `--selfcheck`: **81 проверка, код 0**; сборка без предупреждений; Unity компилируется.
- `--days 21`: 14 тварей, **волна 12**, гнёзд 3, частокол 0/10 — точка сдвинулась из-за живого
  `food_need` (раньше кормление было по 1), записана в `design.md` и ждёт калибровки T7.
- Play Mode: бой, карточка боя, встреча, доска — без регрессий; ноша даёт −3 слота; досье показывает
  «опушка · дорога 60 мин» вместо `edge`.

## Осталось (хвосты)

- `DayReport.Lines` / `NightReport.Lines` — дубль `Entries`; свести при следующем касании ночи.
- CSS и арт `fang`/`mole` — сироты: виды в данные не заведены (арт на будущий пояс).
- Роль охотника (`HunterRole`) **убрана** решением владельца (ADR-0047): из кода, Сбора, CSS и документов.
- Баланс (h1/h3, болотник, точка 21 дня) — отдельная задача T6/T7.
