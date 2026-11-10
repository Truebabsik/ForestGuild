# Справочники как есть

Копии CSV лежат в `data/`. Ниже — те же таблицы глазами, чтобы читать без парсера.
Разделитель — `;`, кодировка UTF-8 без BOM.

## Твари — `beasts.csv`

| id | name | strength | speed | food_need | nest_capable | art |
|---|---|---|---|---|---|---|
| beast_spark | Искромёт | 2 | 3 | 1 | 1 | spark |
| beast_bone | Костяной бегун | 3 | 2 | 1 | 1 | bone |
| beast_ash | Пепельный вор | 1 | 4 | 1 | 0 | ash |
| beast_angler | Тинный удильщик | 2 | 1 | 2 | 0 | angler |
| beast_climber | Теневой древолаз | 2 | 3 | 1 | 0 | climber |

## Дроп с тварей — `drops.csv`

| beast_id | item_id | chance | min_skill |
|---|---|---|---|
| beast_spark | sulfur_nodule | 0.5 | 0 |
| beast_spark | bone_plate | 0.2 | 2 |
| beast_spark | crystal_fire | 0.5 | 0 |
| beast_bone | bone_plate | 0.8 | 0 |
| beast_bone | hide_rough | 0.4 | 1 |
| beast_bone | crystal_fire | 0.5 | 0 |
| beast_ash | crystal_fire | 0.6 | 0 |
| beast_angler | hide_rough | 0.5 | 0 |
| beast_angler | crystal_fire | 0.4 | 0 |
| beast_climber | hide_rough | 0.6 | 1 |
| beast_climber | bone_plate | 0.3 | 0 |
| beast_climber | crystal_fire | 0.4 | 0 |

## Предметы — `items.csv`

| id | name | category | slots | value | tags | short |
|---|---|---|---|---|---|---|
| herb_fire_leaf | Огненная крапива | material | 0.5 | 3 | fire | Крапива |
| herb_ash_lichen | Пепельный лишай | material | 0.5 | 4 |  | Лишай |
| herb_glass_stem | Стеклянница | material | 0.5 | 3 |  | Стеклянница |
| ore_obsidian | Обсидиановая руда | material | 2 | 8 | heavy | Обсидиан |
| sulfur_nodule | Серный натёк | material | 1 | 5 | fire | Сера |
| crystal_fire | Кристалл огня | material | 1 | 25 | crystal | Кристалл |
| moss_common | Обычный мох | material | 0.5 | 1 |  | Мох |
| mushroom_ash_cap | Пепельный груздь | material | 0.5 | 2 | food | Груздь |
| resin_pine | Сосновая смола | material | 0.5 | 6 | fire | Смола |
| peat_block | Торфяная плита | material | 1 | 4 | fire | Торф |
| bog_iron | Болотное железо | material | 1.5 | 9 | heavy | Железо |
| timber_stout | Крепкое бревно | material | 2 | 10 | heavy | Бревно |
| thorn_bundle | Терновый пучок | material | 0.5 | 4 | thorn | Терновник |
| bone_plate | Костяная пластина | trophy | 1 | 12 | bone | Кость |
| hide_rough | Грубая шкура | trophy | 1 | 6 | hide | Шкура |

## Профессии — `professions.csv`

| id | name | skills_allowed | requires_trait | start_skills |
|---|---|---|---|---|
| gatherer | Сборщик | gathering\|butchering | patient | gathering:2\|butchering:1 |
| hunter | Охотник | hunting\|butchering | brave | hunting:2\|butchering:1 |

## Качество рецептов — `recipe_quality.csv`

| recipe_id | min_skill | output_id | qty | chance |
|---|---|---|---|---|
| knife_obsidian | 2 | bone_plate | 1 | 1.0 |
| knife_obsidian | 4 | bone_plate | 2 | 0.5 |
| salve_ash | 1 | moss_common | 1 | 1.0 |

## Рецепты — `recipes.csv`

| id | station | skill_id | skill_req | hours | inputs | outputs |
|---|---|---|---|---|---|---|
| knife_obsidian | smithy | smithing | 2 | 2 | ore_obsidian:1\|sulfur_nodule:1 | bone_plate:1 |
| salve_ash | workshop | alchemy | 1 | 1 | herb_ash_lichen:2 | moss_common:1 |

## Жители — `roster.csv`

| id | name | profession | traits | skills_override |
|---|---|---|---|---|
| brunni | Брунни | hunter | brave\|strong | hunting:4\|butchering:3 |
| skadi | Скади | hunter | brave | hunting:3\|butchering:2 |
| hilda | Хильда | hunter | brave\|keen | hunting:2\|butchering:2 |
| orn | Орн | hunter | strong | hunting:2\|butchering:1 |
| leya | Лея | gatherer | keen\|patient | gathering:3\|butchering:2 |
| tove | Тове | gatherer | patient | gathering:2\|butchering:2 |
| run | Рун | gatherer | steady | gathering:2\|butchering:1 |
| valda | Вальда | gatherer | patient | gathering:1\|butchering:0 |

## Отряды сценариев — `scenario_parties.csv`

| scenario_id | squad | zone | behavior | encounter_order | members |
|---|---|---|---|---|---|
| muster | A |  | gather | ask | brunni\|leya |
| expedition | A | verge_springs | gather | ask | brunni\|skadi |
| expedition | B | verge_thicket | clean | ask | hilda\|orn |

## Сценарии (эпизоды) — `scenarios.csv`

| id | title | seed | day | start_screen | on_finish | notes |
|---|---|---|---|---|---|---|
| campaign | НАЧАТЬ ИГРУ | 12345 | 1 | campaign | continue | обычный цикл с первого дня |
| muster | СБОР ОТРЯДА (ТЕСТИРОВАНИЕ) | 101 | 1 | muster | menu | только экран Сбора, мир отдельный |
| expedition | ПОХОД В ЛЕСУ (ТЕСТИРОВАНИЕ) | 4242 | 1 | expedition | menu | два отряда сразу в лесу |

## Навыки — `skills.csv`

| id | name | kind | max_level |
|---|---|---|---|
| gathering | Собирательство | field | 5 |
| hunting | Охота | field | 5 |
| butchering | Разделка | field | 5 |
| smithing | Кузнечное дело | craft | 5 |
| alchemy | Алхимия | craft | 5 |
| tanning | Кожевенное дело | craft | 5 |

## Станции деревни — `stations.csv`

| id | name | skill_id | people_slots |
|---|---|---|---|
| smithy | Кузня | smithing | 1 |
| workshop | Мастерская | alchemy | 1 |

## Черты — `traits.csv`

| id | name | fight_bonus | find_bonus | butcher_bonus | carry_bonus |
|---|---|---|---|---|---|
| brave | Храбрый | 1 | 0 | 0 | 0 |
| keen | Зоркий | 0 | 0.05 | 0 | 0 |
| steady | Твёрдая рука | 0 | 0 | 1 | 0 |
| strong | Крепкий | 0 | 0 | 0 | 1 |
| patient | Терпеливый | 0 | 0.03 | 1 | 0 |

## Настройки баланса — `tuning.csv`

| id | value | note |
|---|---|---|
| schema_version | 1 | версия схемы таблиц |
| daylight_minutes | 600 | сколько светло в сутках — бюджет похода в минутах |
| slots_per_person | 4 | слотов груза на человека |
| encounter_per_hour | 0.25 | шанс встречи за час на единицу плотности (легаси) |
| find_base | 0.12 | база шанса находки |
| find_per_skill | 0.06 | прибавка за уровень навыка |
| xp_to_nest | 10 | опыта до гнезда |
| nest_beasts_cap | 2 | тварей в гнезде максимум |
| nest_spawn_days | 2 | дней на одну тварь в гнезде |
| migration_per_night | 1 | миграций в зону за ночь |
| heart_influx_days | 3 | дней между притоком из сердца |
| crystal_life_days | 2 | дней жизни кристалла на земле |
| palisade_hp | 10 | прочность частокола |
| wave_day | 21 | день первой волны |
| xp_per_day | 1 | опыта за день работы |
| xp_per_fight | 1 | бонус опыта за бой |
| xp_per_depth | 1 | бонус опыта за пояс |
| level_xp | 10\|30\|70\|150 | опыта на переходы 1-2,2-3,3-4,4-5 |
| village_zone | verge_meadow | зона, к которой примыкает деревня (точка отсчёта «наружу») |
| village_map_x | 0.50 | положение деревни на карте по X (0..1) |
| village_map_y | 0.92 | положение деревни на карте по Y (0..1) |
| beds_count | 2 | сколько раненых можно положить на постели |
| party_max | 4 | сколько бойцов в отряде |
| wave_interval_days | 14 | через сколько дней после волны приходит следующая |
| hunter_kill_chance | 0.6 | шанс охотника в деревне снять тварь на подходе |
| wave_beast_damage | 2 | урон частоколу от одной дошедшей твари |
| wave_casualty_divisor | 4 | сколько урона сверх стены убивает одного жителя |

## Кто водится в зонах — `zone_beasts.csv`

| zone_id | beast_id | weight | initial_count |
|---|---|---|---|
| verge_springs | beast_spark | 3 | 1 |
| verge_springs | beast_bone | 1 | 0 |
| verge_marsh | beast_ash | 2 | 0 |
| verge_marsh | beast_angler | 2 | 1 |
| verge_thicket | beast_bone | 2 | 1 |
| verge_thicket | beast_ash | 1 | 0 |
| verge_thicket | beast_climber | 2 | 1 |
| belt_stone | beast_bone | 2 | 1 |
| belt_stone | beast_spark | 1 | 2 |

## Тексты досье зон — `zone_lore.csv`

| zone_id | field | text |
|---|---|---|
| verge_meadow | look | Край леса, каким его видит деревня. За спиной частокол, впереди стена стволов, между ними — полоса старой вырубки: пни в рост человека, трава по пояс, утренний туман, который не расходится до полудня. Пахнет сырой корой, мхом и дымом от кузни. Тихо так, что слышно, как капает с листьев. |
| verge_meadow | beasts | Своих тварей тут нет: им нечем поживиться. По следам видно, что заходят чужие — ночью кто-то ворошит валежник, утром на пнях остаются царапины. Окликнешь — уходит. Опасность здесь не в звере, а в том, что человек расслабляется. |
| verge_meadow | finds | Мох по камням и пням, пепельный лишай на валунах, пепельные грузди у корней, сосновая смола на старых затесах. Обсидиановая руда попадается редко — её выносит водой с пояса, и она лежит в глине у ручья. |
| verge_meadow | special | Первая зона и первый урок: полчаса дороги — и ты в лесу. Сюда ходят новички и те, кто не хочет рисковать. Отсюда видно, как живёт лес, и не слышно, как он дышит. |
| verge_springs | look | Земля дышит жаром: из трещин сочится оранжевый свет, над ключами дрожит воздух. Дышать тяжело, вода обжигает даже сквозь кожу. |
| verge_springs | beasts | Искромёты — быстрые ящерицы, что плюются огнём. Костяные бегуны в броне из наростов. |
| verge_springs | finds | Огненная крапива, что жжётся сквозь ткань · пепельный лишай · стеклянница · обсидиановая руда · серные натёки. |
| verge_springs | special | Всё добытое здесь нагрето: портит зелья и выдаёт отряд на обратном пути. |
| verge_marsh | look | Болото не стоит на месте: вода чёрная, но дышит — то поднимется, то уйдёт под кочку. Туман держится низко, по колено, и в нём видно только следующий шаг. Пахнет тиной, ржавчиной и чем-то сладковатым. |
| verge_marsh | beasts | Пепельные воры тащат всё, что блестит, и смелеют стаей. А в трясине лежит тинный удильщик: под водой не видно ни тела, ни глаз — только огонёк приманки над чёрной водой. |
| verge_marsh | finds | Стеклянница по кочкам, пепельный лишай на корягах. Под водой — торф, а в торфе болотное железо, чёрное и тяжёлое. Оступаться дорого. |
| verge_marsh | special | Груз вязнет: обратный путь дольше, а мешок тяжелее, чем кажется. Место кормит, но не даром. |
| verge_thicket | look | Стволы стоят вплотную, как частокол, только выше. Свет идёт полосами, и в полосах ничего не видно. Воздух густой, тёплый, без ветра. Слышно, как что-то идёт, но не слышно — куда. |
| verge_thicket | beasts | Костяные бегуны — их не слышно, пока не подойдёшь. Броня гасит удар, нужен тяжёлый. А теневой древолаз сидит на стволе выше головы и падает сверху, когда проходят под ним. |
| verge_thicket | finds | Мох, огненная крапива у просек, крепкий строевой лес и терновник, что растёт только в тени. Под корнями — кости старых тварей. |
| verge_thicket | special | Гнёзда встают у корней, и их не видно, пока не наступишь. В чаще не обойти — только пройти. |
| belt_stone | look | Овраг с обнажённой породой, и в ней блестят жилы. Снизу тянет холодом и сыростью. |
| belt_stone | beasts | Костяные бегуны покрупнее окраинных, искромёты. Есть и то, что никто не видел вблизи. |
| belt_stone | finds | Обсидиановая руда, серные натёки и кристаллы, что сочатся прямо из жил. |
| belt_stone | special | Дальше Каменного лога не ходят: оттуда возвращаются не все. |
| heart_root | look | Одно дерево выше всех, и от него к земле идут кристаллы, как корни. |
| heart_root | beasts | Никто не возвращался со списком. |
| heart_root | finds | Кристаллы. |
| heart_root | special | Дорога шесть часов в одну сторону. Пока туда не ходят. |

## Что находят в зонах — `zone_resources.csv`

| zone_id | item_id | abundance |
|---|---|---|
| verge_meadow | moss_common | 3 |
| verge_meadow | herb_ash_lichen | 2 |
| verge_meadow | mushroom_ash_cap | 2 |
| verge_meadow | resin_pine | 1 |
| verge_springs | herb_fire_leaf | 3 |
| verge_springs | herb_ash_lichen | 1 |
| verge_springs | ore_obsidian | 2 |
| verge_springs | sulfur_nodule | 1 |
| verge_marsh | herb_glass_stem | 2 |
| verge_marsh | herb_ash_lichen | 1 |
| verge_marsh | peat_block | 2 |
| verge_marsh | bog_iron | 1 |
| verge_thicket | moss_common | 3 |
| verge_thicket | herb_fire_leaf | 1 |
| verge_thicket | timber_stout | 1 |
| verge_thicket | thorn_bundle | 2 |
| belt_stone | ore_obsidian | 3 |
| belt_stone | sulfur_nodule | 2 |
| belt_stone | crystal_fire | 1 |

## Зоны — `zones.csv`

| id | name | depth | travel_minutes | distance_km | crystals_per_day | forage_per_day | neighbours | env_rule | map_x | map_y |
|---|---|---|---|---|---|---|---|---|---|---|
| verge_meadow | Опушка | edge | 30 | 1.5 | 1 | 2 | verge_springs\|verge_thicket | none | 0.50 | 0.52 |
| verge_springs | Вулканические ключи | edge | 60 | 3 | 1 | 2 | verge_meadow\|verge_marsh\|belt_stone | heat | 0.28 | 0.38 |
| verge_marsh | Серая Топь | edge | 90 | 4.5 | 1 | 2 | verge_springs\|verge_thicket | mud | 0.16 | 0.62 |
| verge_thicket | Стеклянная роща | edge | 60 | 3 | 1 | 2 | verge_meadow\|verge_marsh\|belt_stone | thicket | 0.74 | 0.40 |
| belt_stone | Каменный лог | belt | 150 | 7.5 | 2 | 2 | verge_springs\|verge_thicket\|heart_root | none | 0.58 | 0.20 |
| heart_root | Сердце леса | heart | 240 | 12 | 4 | 4 | belt_stone | none | 0.86 | 0.08 |

`travel_minutes` — каноническое поле. `distance_km` — производная от 3 км/ч, для карты и лора.
