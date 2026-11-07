---
name: blender-headless
description: Use when a task touches Blender for this project — an FBX round trip into Unity, importing or exporting a model, measuring its bytes or world size, checking a rig or its animation clips, unpacking packed textures, or a headless bpy run that reported success but wrote nothing to disk.
---

# Blender headless

Blender здесь **инструмент скрипта, а не окно**. Всё идёт через `blender.exe --background`:
интерфейса нет, значит нет ни контекста `bpy.ops`, ни выделения, ни режима редактирования.
Отсюда два следствия, которые ловят чаще всего: **оператор молча падает или работает не над тем
объектом**, и **провал записи виден только по файлу на диске**.

Скилл отвечает на «как запускать, что чем мерить и где врёт». Решения конвейера — в
`unity-model-optimize` (вес, масштаб, текстуры), приёмка в проект — `unity-model-catalogue`,
библиотека текстур — `unity-texture-import`.

## Инварианты проекта — нарушение ломает молча

| Инвариант | Где записан |
|---|---|
| Геометрию не трогаем: у генератора бюджет 50k, конвейер жмёт **только текстуры** (`--max-tris 0`) | ADR-0012, `AGENTS.md` п. 9 |
| Размер задаётся явно и считается в ростах персонажа; эталон — гном 1.30 м | ADR-0006 |
| Признак успеха записи — **файл на диске**, а не возврат вызова и не строка в логе | ADR-0013 |
| Исходники в `Resources/models/origins/` не трогаются; текстуры — из библиотеки вне `Assets/` | ADR-0009, `AGENTS.md` п. 8 |
| Каталог `docs/models.md` и лист моделей обновляются тем же коммитом, что и модель | `AGENTS.md` п. 10, `unity-model-catalogue` |

## Запуск

```powershell
$bl = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"   # 5.2.2 LTS, проверено на этой машине
& $bl --background --factory-startup --python-exit-code 1 --python <script.py> -- `
    --src Resources/models/origins/<имя>.fbx --out Assets/Models/Props/<Имя>
```

Флаги, каждый из которых что-то ловит:

| Флаг | Зачем |
|---|---|
| `--background` | без него откроется окно. В проекте GUI-режима нет ни у одного сценария |
| `--factory-startup` | игнорирует пользовательские настройки и аддоны; без него результат неповторим между машинами |
| `--python-exit-code 1` | **без него падение скрипта даёт код возврата 0.** Проверено: `RuntimeError` в скрипте → с флагом `exit=1`, без флага `exit=0` при том же трейсбеке в логе |
| `--` | всё после него — это `sys.argv` скрипта; Blender его не разбирает |

**`--factory-startup` не очищает файл.** Он игнорирует настройки, но грузит стартовую сцену:
проверено — `bpy.context.scene.objects` = `['Cube', 'Light', 'Camera']`. Скрипт обязан очистить
сцену сам: `bpy.ops.wm.read_factory_settings(use_empty=True)` первой строкой (так делает
`optimize_model.py`).

`raise SystemExit("FATAL: …")` даёт код 1 и без флага — этим конвейер и пользуется, — но любой
**другой** трейсбек без `--python-exit-code 1` уходит в лог с нулевым кодом. `props_pipeline.ps1`
читает `$LASTEXITCODE` и грепает лог по `FATAL|Traceback`, поэтому сейчас спасается только грепом.

## Скелет скрипта

```python
import os
import sys

import bpy

argv = sys.argv
args = argv[argv.index("--") + 1:] if "--" in argv else []   # без "--" в argv индекса нет


def log(*a):
    print("[PROP]", *a, flush=True)          # без flush лог теряется при падении


def arg(name, default=None):                 # --height 1.3  ->  arg("--height")
    return args[args.index(name) + 1] if name in args else default


bpy.ops.wm.read_factory_settings(use_empty=True)   # иначе в сцене Cube, Light, Camera
bpy.ops.import_scene.fbx(filepath=SRC)

if not os.path.exists(written):
    raise SystemExit(f"FATAL: {written} was not written")   # ADR-0013: падать громко
```

Правила, которые уже оплачены в этом проекте:

- **Логировать с `flush=True`** и печатать то, что потом ищут в логе: путь, размер файла,
  число треугольников, `world dims`.
- **Падать, а не «предупреждать».** Молчаливый провал записи картинки выглядел как успех семи
  моделей сразу (ADR-0013).
- Возвращать результат машинно-читаемо: одна строка-маркер вида `@@JSON@@{...}` позволяет
  вызывающему разобрать итог без разбора человеческого текста.

## Ловушки Blender 5.2

Проверено на 5.2.2 LTS этой машины, если не сказано иное.

| Ловушка | Как есть |
|---|---|
| **`img.save()` в headless молча не пишет файл** | Пишет только `img.save(filepath=p)`. `img.filepath_raw = p` + `img.save()` и `img.filepath = p` + `img.save()` файла не создают и ничего не выбрасывают. ADR-0013 |
| **`img.scale()` молча не сжимает вшитые текстуры** | Проверено на `Dwarf_low.fbx` 18.10.2026: `img.scale(1024, 1024)` на упакованной карте 4096² вернул управление, `img.size` даже показал 1024, а `img.pixels` остались 4096² (67 108 864 float), и `foreach_set` в новый образ упал с `expected sequence size 4194304, got 67108864`. На «1024-й» карте в файле вышло 13.5 МБ. Уменьшать самому: `pixels.foreach_get` → `reshape(size, f, size, f, 4).mean(axis=(1, 3))` → новый образ. Размер и магические байты проверять на диске, не в `img.size` |
| **Удаление действий не сбрасывает позу** | Проверено 18.10.2026 на ригованном гноме: после `bpy.data.actions.remove()` кости остались в последней анимированной позе, и «rest»-деформация дала подошву на `z=0.9212` вместо 0 (габарит 2.21 м вместо 1.30). Перед замером и экспортом обнулять `pose.bones`: `location`, `rotation_quaternion`/`rotation_euler`, `scale` — затем `view_layer.update()` |
| **`bpy.data.images.load` не понимает относительный путь** | Путь разрешается от файла `.blend`, которого в headless нет: `RuntimeError: Cannot read 'Assets/…'`. Пути к только что записанным картам передавать только абсолютными (`os.path.abspath`) |
| **`action.fcurves` больше нет** | Проверено: `hasattr(act, "fcurves")` → `False`, `hasattr(act, "slots")` → `True`. F-кривые живут в **channelbag**: `from bpy_extras import anim_utils`, затем `cb = anim_utils.action_ensure_channelbag_for_slot(act, slot)` и `cb.fcurves.ensure(path, index=i, group_name=...)` |
| **`obj.dimensions` — размер локального bbox, без поворота** | Импортёр FBX компенсирует конверсию осей поворотом на 90°, и локальный Z перестаёт быть высотой. Габарит считать как `matrix_world @ bound_box`. ADR-0006 |
| **`bound_box` устаревает у только что созданного объекта** | Перед чтением — `bpy.context.view_layer.update()` |
| **Новый материал уже имеет узлы** | Проверено: у `bpy.data.materials.new()` в `node_tree` сразу `['BSDF_PRINCIPLED', 'OUTPUT_MATERIAL']`; `use_nodes = True` — deprecated |
| **`bpy.ops.*` в background нет контекста** | Оператор либо бросает `RuntimeError: context is incorrect`, либо работает над не тем объектом. Оборачивать: `with bpy.context.temp_override(object=ob, active_object=ob, selected_objects=[ob]):` и проверять результат `== {"FINISHED"}` |
| **Узел нельзя искать по имени** | Имена локализованы. Искать по типу: `next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")` |
| **Enum под-сообщает допустимые значения** | Присваивать в `try/except TypeError` — сообщение перечисляет валидные значения |
| **Освобождённые данные возвращают мусор** | После применения модификатора или удаления ID старый Python-объект недействителен. Собрать имена заранее и перечитать из `bpy.data` |
| **Меш с модификаторами читается отдельно** | `eo = ob.evaluated_get(depsgraph)`, `me = eo.to_mesh()`, обязательно `eo.to_mesh_clear()` после |
| **`mesh.use_auto_smooth` удалён (4.1+)** | `bpy.ops.object.shade_auto_smooth(angle=rad)` или модификатор «Smooth by Angle». Конвейер проекта уже так и делает |
| **Экспорт для Unity, scale 100 на корне** | Это конверсия единиц, а не ошибка модели. В Unity `localScale` **умножать**, не присваивать: присваивание `2f` затирает сотню и модель «исчезает» |

## Обмен FBX с Unity

Дефолты экспортёра FBX, **снятые с этой машины** (`export_scene.fbx.get_rna_type()`), потому что
на них легко положиться неправильно:

| Параметр | Дефолт 5.2.2 | Что это значит для нас |
|---|---|---|
| `bake_anim_use_all_actions` | `True` | по умолчанию выгружаются **все** actions. Если кто-то поставил `False` (так делает, например, чужой Unity-пресет), клипы молча теряются — а именно так один раз и вышло с ригом Тана |
| `bake_anim_use_nla_strips` | `True` | то же про NLA |
| `apply_scale_options` | `FBX_SCALE_NONE` | единицы не пекутся, отсюда ×100 на корне |
| `axis_forward` / `axis_up` | `-Z` / `Y` | совпадает с ожиданием Unity; менять не нужно |
| `add_leaf_bones` | `True` | для **ригованной** модели лишние листья. Ставить `False` |
| `mesh_smooth_type` | `OFF` | группы сглаживания не пишутся. Для скелета — `"FACE"` |
| `use_armature_deform_only` | `False` | в FBX уедут и control-кости |
| `use_triangles` | `False` | для стабильных тангенсов при запекании — `True` |
| `embed_textures` | `False` | **так и надо**: текстуры кладём рядом, в `<имя>/Textures/`, Unity ищет их именно там |
| `path_mode` | `AUTO` | `'COPY'` создаёт дубли в `<имя>.fbm` и удваивает вес. Использовать `'RELATIVE'` |

**`bake_space_transform=True` (Apply Transform) снимает и −90° по X, и scale 100 на корне — но
только для статики.** С арматурой его использовать нельзя: bind-поза поедет. Риг Тана из этого
правила — не исключение.

Минимум для скелета: `add_leaf_bones=False`, `mesh_smooth_type="FACE"`,
`use_armature_deform_only=True`, `bake_anim_use_all_actions=True`.
Минимум для статики (то, что делает `optimize_model.py`): `embed_textures=False`,
`path_mode="RELATIVE"`, `use_selection=True`.

**Проверка после экспорта — по числам, не по логу:** файл существует и непуст; число костей
совпадает с исходным; пересчёт actions до и после перегона даёт одно и то же.

## Проверки, которых у проекта раньше не было

Это не замена `unity-model-catalogue`, а то, что мерить в Blender до Unity.

**Парные грани в одной плоскости (z-fighting) — ближайшим расстоянием тел.** Проект уже ловил
**симптом** — моргающие клинья арки (ADR-0030), — и там причиной оказались не копланарные грани,
а пересекающиеся тела: у повёрнутых боксов две грани сходятся под острым углом, и линия
пересечения уезжает по глубине. Поэтому зазор между гранями ничего не говорит о пересечении тел.
Мерить стоит двумя способами: `BVHTree.FromPolygons` + `find_nearest_range` — для копланарных
пар, обстрел лучами — для пересечений (ADR-0030: 24 000 лучей, 219 → 26 пар с зазором меньше 5 см).
Проверено на двух нарочно совмещённых квадах 1 × 1 м, порог 1 мм:

| Зазор между гранями | Результат |
|---:|---|
| 0 мм | Z-FIGHT |
| 0.5 мм | Z-FIGHT |
| 1 мм | на границе порога |
| 5 мм | чисто |
| 20 мм | вне радиуса зонда |

Ловушка метода, которую надо знать: запрос от **центра** грани пропускает вытянутые перекрытия,
где центр одной грани лежит вне другой.

**Прочее, что стоит мерить и что обычно и есть причина:**
- **Отсоединённые куски** — остров, который ничем не касается остальных в пределах допуска.
  Визуальная версия того же — контактный лист с раскраской по объектам: разъехавшийся кусок
  сразу виден цветом.
- **Незамкнутость и вывернутые нормали** — открытые рёбра, рёбра с более чем двумя гранями,
  замкнутый кусок с нормалями внутрь.
- **Веса скина**: вершины без весов, больше 4 влияний, ненормализованные, веса на не-деформирующих
  костях. Незавешенная вершина просто не двигается, и это не видно в покое.
- **`ground_z ≠ 0`** — проп висит или утоплен; низ модели обязан быть на нуле.

## Куда идти дальше

- Вес, масштаб, децимация, упаковка карт — `.dsh/skills/unity-model-optimize`.
- Приёмка модели в проект: каталог, лист, коммит — `.dsh/skills/unity-model-catalogue`.
- Текстуры библиотеки → URP Lit — `.dsh/skills/unity-texture-import`.
- Батч по списку моделей — `.dsh/tools/props_pipeline.ps1`.
- Живой Unity Editor — `.dsh/skills/unity-pipeline` (Blender тут не участвует).

## Происхождение

Собрано 18.10.2026 из адаптаций чужого материала (оба под MIT) и проверено на 5.2.2 этой машины:
`EL4CTEO/blender-skills` (`blender-core` — контракт запуска, таблица миграции 4.x → 5.2,
`bpy-patterns`; `blender-export-pipelines/references/unity.md` — FBX-пресет и оси;
`blender-asset-qa` — перечень проверок; `blender-animation` — channelbags),
`MartinRapcan/blender-claude-skill` (разделы про headless и `temp_override`) и записи проекта
(ADR-0006, ADR-0012, ADR-0013, ADR-0030). Чужой код не копировался: скилл — выжимка правил,
числа взяты из замеров проекта или из прогонов, описанных выше.
