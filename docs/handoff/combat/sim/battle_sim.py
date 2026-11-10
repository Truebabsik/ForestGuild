"""Боевой прототип ForestGuild: точная реализация механики из mechanics.md.

Не замена tools/sim — балансер для калибровки, пока .NET SDK недоступен.
Детерминирован: один seed → один бой. Числа только из data-draft/*.csv.

Механика (вариант 2, крупный масштаб):
  раунд = каждый живой человек бросает точность; попал — наносит удар оружием
  из диапазона (damage_min..damage_max) плюс wound_per_level за каждый уровень
  выше первого; броня твари вычитается, min_damage остаётся в любом случае.
  Затем тварь бьёт первого невыбитого: удар из attack_min..attack_max минус
  броня экипировки, остаток гасится закалкой.

Запуск:
  python3 battle_sim.py             — отчёт (out/battle-report.md)
  python3 battle_sim.py --probe     — как HP твари двигает кривую
  python3 battle_sim.py --matrix    — закалка × уровни
  python3 battle_sim.py --belts     — опасность тварей по уровню
"""

import csv
import random
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
DRAFT = HERE.parent / "data-draft"
DATA = HERE.parent / "data"


def read_csv(path):
    with path.open(encoding="utf-8") as f:
        return list(csv.DictReader(f, delimiter=";"))


def load_tuning():
    return {row["id"]: float(row["value"]) for row in read_csv(DRAFT / "combat_tuning.csv")}


class World:
    def __init__(self):
        self.t = load_tuning()
        self.beasts = {r["beast_id"]: r for r in read_csv(DRAFT / "beast_combat.csv")}
        self.equip = {r["id"]: r for r in read_csv(DRAFT / "equipment.csv")}
        self.roster = {r["id"]: r for r in read_csv(DATA / "roster.csv")}
        self.combat = {r["person_id"]: r for r in read_csv(DRAFT / "roster_combat.csv")}
        self.skills = {r["id"]: r for r in read_csv(DRAFT / "skills_combat.csv")}

    def skill(self, person_id, skill="hunting"):
        for part in self.roster[person_id]["skills_override"].split("|"):
            key, value = part.split(":")
            if key == skill:
                return int(value)
        return 0

    def add_probe(self, person_id, level, weapon="weapon_club", armor="armor_padded",
                  profession="hunter"):
        self.roster[person_id] = {
            "id": person_id, "name": f"Проба {level}", "profession": profession,
            "traits": "", "skills_override": f"hunting:{level}",
        }
        self.combat[person_id] = {
            "person_id": person_id, "hardening": 0,
            "weapon_id": weapon, "armor_id": armor,
        }


class Fighter:
    """Человек в бою. Точность и сила удара растут раздельно."""

    def __init__(self, world, person_id, overrides=None, rng=None):
        overrides = overrides or {}
        row = world.combat[person_id]
        t = world.t
        self.id = person_id
        self.level = int(overrides["level"]) if "level" in overrides else world.skill(person_id)
        self.hunter = world.roster[person_id]["profession"] == "hunter"
        weapon = world.equip[overrides.get("weapon", row["weapon_id"])]
        armor = world.equip[overrides.get("armor", row["armor_id"])]
        self.hardening = float(overrides["hardening"]) if "hardening" in overrides \
            else self.hardening_of_level(world, self.level)

        self.weapon_min = float(weapon["damage_min"])
        self.weapon_max = float(weapon["damage_max"])
        self.rng = rng or random.Random()

        picked = overrides.get("skills", [])
        self.skill_effects = {}
        for sid in picked:
            row = world.skills[sid]
            self.skill_effects[row["effect"]] = (
                self.skill_effects.get(row["effect"], 0.0) + float(row["value"]))
        self.damage_bonus = self.skill_effects.get("damage_bonus", 0.0)
        self.hit_bonus = self.skill_effects.get("hit_bonus", 0.0)
        self.alert_bonus = self.skill_effects.get("alert_bonus", 0.0)
        self.ambush = bool(self.skill_effects.get("ambush", 0.0))
        self.armor_pierce = self.skill_effects.get("armor_pierce", 0.0)
        self.detect_bonus = self.skill_effects.get("detect_bonus", 0.0)

        self.tough_max = (t["base_hp"] + t["hp_per_level"] * self.level
                          + self.skill_effects.get("hp_bonus", 0.0)
                          + float(overrides.get("tough_bonus", 0)))
        self.hp = float(overrides.get("hp", self.tough_max))
        self.armor = float(armor["armor"])
        self.hits_taken = 0
        self.dodged = 0
        self.out = False
        self.tuning_hit = min(0.95, t["hit_base"] + t["hit_per_level"] * (self.level - 1)
                              + self.hit_bonus)

    @staticmethod
    def hardening_of_level(world, level):
        """Закалка = ступени × доля. Ступени открываются с `hardening_from_level`."""
        t = world.t
        if level < t["hardening_from_level"]:
            return 0.0
        steps = min(level - int(t["hardening_from_level"]) + 1, int(t["hardening_max"]))
        return steps * t["hardening_armor"]

    def hits(self, world):
        """Попал ли человек в этом раунде."""
        if not self.hunter and not world.t["gatherer_fights"]:
            return False
        return self.rng.random() < self.tuning_hit

    def damage_factor(self, world):
        """Штраф за ранение: <50% HP — «ранен», <25% — тяжело ранен."""
        t = world.t
        share = self.hp / self.tough_max
        if share < t["hurt_threshold"]:
            return t["hurt_damage_factor"]
        if share < t["wound_threshold"]:
            return t["wound_damage_factor"]
        return 1.0

    def deal(self, world, beast, surprise=False):
        """surprise — удар из засады: цель не успела прикрыться, броня не вычитается.

        Сила удара — обычный бросок: засада меняет **кто бьёт первым и чем это стоит
        цели**, а не превращает удар в гарантированный максимум.
        """
        t = world.t
        strength = (self.rng.uniform(self.weapon_min, self.weapon_max)
                    + max(0, self.level - 1) * t["wound_per_level"] + self.damage_bonus)
        armor = 0.0 if surprise and t["ambush_no_armor"] else max(
            0.0, beast.armor - self.armor_pierce)
        factor = 1.0 if self.hunter else t["gatherer_combat_factor"]
        factor *= self.damage_factor(world)
        if surprise:
            factor *= 1.0 + t["ambush_bonus"]
        return max(t["min_damage"], strength - armor) * factor

    def take(self, world, beast, surprise=False):
        """surprise — тварь застала отряд: бьёт обычным ударом, но мимо брони."""
        t = world.t
        if not self.hunter and self.rng.random() < t["gatherer_dodge"]:
            # сборщик не бьётся, а уходит с линии удара
            self.dodged += 1
            return 0.0
        swing = self.rng.uniform(beast.attack_min, beast.attack_max)
        if surprise and t["ambush_no_armor"]:
            hurt = swing
        else:
            hurt = max(t["min_damage"], swing - self.armor) * (1.0 - self.hardening)
        if surprise:
            hurt *= 1.0 + t["ambush_bonus"]
        self.hp -= hurt
        self.hits_taken += 1
        if self.hp <= 0:
            self.out = True
        return hurt


class Beast:
    def __init__(self, world, beast_id, overrides=None):
        overrides = overrides or {}
        row = world.beasts[beast_id]
        self.id = beast_id
        self.level = int(row["level"])
        self.hp = float(overrides.get("hp", row["hp"]))
        self.attack_min = float(overrides.get("attack_min", row["attack_min"]))
        self.attack_max = float(overrides.get("attack_max", row["attack_max"]))
        self.armor = float(overrides.get("armor", row["armor"]))
        self.speed = float(row["speed"])


def party_alert(fighters):
    alerts = [(2.0 if f.hunter else 1.0) + f.alert_bonus for f in fighters if not f.out]
    return sum(alerts) / len(alerts) if alerts else 0.0


def detect_chance(world, fighters, beast):
    """Шанс, что сторона заметит другую первой.

    Люди: база + за каждую единицу «чутья» выше опорной. Тварь: её `speed` против
    того же опорного чутья (у стаи берётся самая быстрая). Обе стороны бросают
    независимо; кто выиграл — тот и застал.
    """
    t = world.t
    beasts = beast if isinstance(beast, (list, tuple)) else [beast]
    top_speed = max(b.speed for b in beasts)
    alert = party_alert(fighters)
    bonus = max((f.detect_bonus for f in fighters if not f.out), default=0.0)
    people = (t["detect_base"] + t["detect_per_alert"] * (alert - t["detect_ref_alert"])
              + bonus)
    beast_chance = t["detect_base"] + t["detect_per_alert"] * (top_speed - t["detect_ref_alert"])
    return max(0.0, min(1.0, people)), max(0.0, min(1.0, beast_chance))


def detect(world, fighters, beast, rng):
    """Кто обнаружил первым: 'people', 'beast' или None (заметили одновременно)."""
    people, beast_chance = detect_chance(world, fighters, beast)
    p = rng.random() < people
    b = rng.random() < beast_chance
    if p and not b:
        return "people"
    if b and not p:
        return "beast"
    return None


def resolve(world, fighters, beast, rng, trace_penalty=False):
    """Порядок: люди бьют — твари отвечают. `beast` — одна тварь или их список (стая).

    Засада — это обнаружение: обе стороны бросают шанс заметить друг друга первой,
    побеждает тот, у кого бросок удачнее. Выигравшая сторона бьёт первой своим
    обычным ударом без вычета брони цели, а при `ambush_skip_reply` — ещё и лишает
    застигнутую сторону ответного удара в первом раунде.

    Стая идёт по порядку: люди распределяют удары по передней живой твари, затем
    каждая живая тварь бьёт первого живого человека.
    """
    t = world.t
    beasts = beast if isinstance(beast, (list, tuple)) else [beast]
    rounds = 0
    out_order = []
    penalty_rounds = {"wound": 0, "hurt": 0}
    side = detect(world, fighters, beasts, rng)
    beast_first = side == "beast"
    human_first = side == "people"
    skip_reply = bool(t["ambush_skip_reply"])

    def beasts_alive():
        return [b for b in beasts if b.hp > 0]

    def pick_target():
        """Цель удара: при `target_random` — случайный живой, иначе первый живой."""
        alive = [f for f in fighters if not f.out]
        if not alive:
            return None
        if t["target_random"]:
            return alive[rng.randrange(len(alive))]
        return alive[0]

    def strike_people(beasts_now, surprise=False):
        """Удар стаи по людям: бьёт одна тварь — самая быстрая из живых.

        Стая не суммирует удары: нападают передние, остальные давят массой
        (их надо убивать дольше), но за раунд прилетает один удар.
        """
        target = pick_target()
        if target is None or not beasts_now:
            return
        leader = max(beasts_now, key=lambda b: b.speed)
        target.take(world, leader, surprise=surprise)
        if target.out:
            out_order.append(target.id)

    def beast_turn(surprise=False):
        if not [f for f in fighters if not f.out]:
            return True
        strike_people(beasts_alive(), surprise=surprise)
        return False

    if human_first:
        hit_any = False
        for f in fighters:
            if hit_any and not t["ambush_all_hit"]:
                break
            if not f.out and f.hits(world):
                alive = beasts_alive()
                if alive:
                    alive[0].hp -= f.deal(world, alive[0], surprise=True)
                    hit_any = True

    while rounds < t["max_rounds"] and beasts_alive() and not all(f.out for f in fighters):
        rounds += 1
        if rounds == 1 and beast_first:
            if t["ambush_all_hit"]:
                beast_turn(surprise=True)
            else:
                alive = beasts_alive()
                target = pick_target()
                if alive and target is not None:
                    target.take(world, alive[0], surprise=True)
                    if target.out:
                        out_order.append(target.id)
            if skip_reply:
                # тварь застала отряд: люди теряют свой ход в этом раунде
                continue

        for f in fighters:
            if trace_penalty and not f.out:
                factor = f.damage_factor(world)
                if factor < 1.0:
                    penalty_rounds["hurt" if factor <= world.t["hurt_damage_factor"]
                                   else "wound"] += 1
            if not f.out and f.hits(world):
                alive = beasts_alive()
                if alive:
                    alive[0].hp -= f.deal(world, alive[0])

        if not beasts_alive() or all(f.out for f in fighters):
            break

        if rounds == 1 and human_first and skip_reply:
            # отряд застал тварь: её ответ в первом раунде не происходит
            continue

        beast_turn()
        if all(f.out for f in fighters):
            break
    broken = bool(beasts_alive()) and not all(f.out for f in fighters)

    won = not beasts_alive()
    if won and out_order:
        outcome = "win_with_wound"
    elif won:
        outcome = "win"
    elif all(f.out for f in fighters):
        outcome = "lost"
    else:
        outcome = "retreat_after_wound" if out_order else "broke_off"

    return {
        "outcome": outcome,
        "rounds": rounds,
        "wounded": out_order,
        "hp_left": {f.id: round(max(0.0, f.hp), 2) for f in fighters},
        "hits": {f.id: f.hits_taken for f in fighters},
        "beast_hp": round(max(0.0, sum(b.hp for b in beasts)), 2),
        "ambush": beast_first,
        "ambush_people": human_first,
        "broken": broken,
        "penalty_rounds": penalty_rounds,
    }


def batch(world, person_ids, beast_id, n=4000, seed=2026, hp_profile=None,
          beast_overrides=None, hardening=None, levels=None, with_penalty=False, skills=None):
    rng = random.Random(seed)
    stats = {k: 0 for k in ("win", "win_with_wound", "lost", "retreat_after_wound", "broke_off")}
    rounds = ambushes = 0
    rounds_wounded = rounds_hurt = penalised = people_ambush = 0
    # Бойцы создаются внутри цикла от удара к удару ничего не переносится (свежий бой).
    for _ in range(n):
        fs = []
        for pid in person_ids:
            ov = dict((hp_profile or {}).get(pid, {}))
            if hardening is not None:
                ov["hardening"] = hardening
            if levels is not None:
                ov["level"] = levels
            if skills:
                ov["skills"] = skills
            fs.append(Fighter(world, pid, ov, rng=rng))
        b = Beast(world, beast_id, beast_overrides)
        if with_penalty:
            res = resolve(world, fs, b, rng, trace_penalty=True)
            rounds_wounded += res["penalty_rounds"]["wound"]
            rounds_hurt += res["penalty_rounds"]["hurt"]
            penalised += 1 if (res["penalty_rounds"]["wound"]
                               or res["penalty_rounds"]["hurt"]) else 0
        else:
            res = resolve(world, fs, b, rng)
        stats[res["outcome"]] += 1
        rounds += res["rounds"]
        ambushes += 1 if res["ambush"] else 0
        people_ambush += 1 if res["ambush_people"] else 0
    return {
        "wins": (stats["win"] + stats["win_with_wound"]) / n,
        "win_clean": stats["win"] / n,
        "win_wound": stats["win_with_wound"] / n,
        "lost": stats["lost"] / n,
        "retreat": (stats["retreat_after_wound"] + stats["broke_off"]) / n,
        "avg_rounds": rounds / n,
        "ambush": ambushes / n,
        "ambush_people": people_ambush / n,
        "rounds_wounded": rounds_wounded / n,
        "rounds_hurt": rounds_hurt / n,
        "fights_penalised": penalised / n,
    }


def estimate(world, person_ids, beast_id, n=400, seed=7):
    """Скрытая оценка победы для слова риска: игроку показывается только слово."""
    return batch(world, person_ids, beast_id, n=n, seed=seed)["wins"]


def risk_word(chance):
    if chance >= 0.85:
        return "почти наверняка"
    if chance >= 0.70:
        return "надёжно"
    if chance >= 0.50:
        return "как повезло"
    return "рискованно"


def hp_word(fighter):
    share = fighter.hp / fighter.tough_max
    if fighter.out:
        return "на ногах не стоит"
    if share < 0.25:
        return "тяжело ранен"
    if share < 0.5:
        return "ранен"
    if share < 0.8:
        return "в ссадинах"
    return "в порядке"


def pct(x):
    return f"{x * 100:5.1f}%"


def goals_for(lvl):
    return {1: (0.0, 0.10), 2: (0.45, 0.55), 3: (0.75, 0.85),
            4: (0.88, 1.0), 5: (0.88, 1.0)}[lvl]


def probe():
    """Как HP твари двигает кривую (броня 0, дубина, стартовая стёганка)."""
    world = World()
    print("| hp твари | h1 | h2 | h3 | h4 | h5 |")
    print("|---|---|---|---|---|---|")
    for beast_hp in (18, 24, 30, 36, 42, 48, 60):
        row = []
        for lvl in (1, 2, 3, 4, 5):
            world.add_probe(f"p{lvl}", lvl)
            row.append(batch(world, [f"p{lvl}"], "beast_ash", n=1200,
                             beast_overrides={"hp": beast_hp})["wins"])
        print(f"| {beast_hp} | " + " | ".join(pct(v) for v in row) + " |")


def matrix():
    """Матрица: закалка ступенями по столбцам, уровни по строкам.

    Закалка задаётся явно (а не выводится из уровня) — иначе столбцы не различались бы.
    """
    world = World()
    print("| lvl | закалка 0 | 0.1 | 0.2 |")
    print("|---|---|---|---|")
    for lvl in (1, 2, 3, 4, 5):
        row = []
        for hard in (0.0, 0.1, 0.2):
            world.add_probe(f"z{lvl}", lvl, armor="armor_padded")
            row.append(batch(world, [f"z{lvl}"], "beast_ash", n=1500, hardening=hard)["wins"])
        print(f"| {lvl} | " + " | ".join(pct(v) for v in row) + " |")


def belts():
    """Опасность твари любого уровня против любого уровня охотника."""
    world = World()
    ids = list(world.beasts)
    print("| охотник | " + " | ".join(f"{b} (ур. {world.beasts[b]['level']})" for b in ids) + " |")
    print("|---|" + "---|" * len(ids))
    for lvl in (1, 2, 3, 4, 5):
        world.add_probe(f"b{lvl}", lvl)
        row = [batch(world, [f"b{lvl}"], b, n=1500)["wins"] for b in ids]
        print(f"| {lvl} | " + " | ".join(pct(v) for v in row) + " |")


def main():
    if "--probe" in sys.argv:
        probe()
        return
    if "--matrix" in sys.argv:
        matrix()
        return
    if "--belts" in sys.argv:
        belts()
        return

    world = World()
    out = HERE / "out"
    out.mkdir(exist_ok=True)
    lines = []
    add = lines.append

    add("# Боевой прогон — черновик\n")
    add("Механика: `mechanics.md` (вариант 2: отдельный бросок точности, удар из диапазона оружия, "
        "броня — вычитание). Числа: `data-draft/*.csv`. Детерминированно, seed 2026.")
    add("Закалка: ступень с 3 уровня hunting, доля `hardening_armor` за ступень (h3 0.1, h4–5 0.2).\n")

    add("## 1. Один охотник против твари 1 уровня (Пепельный вор)\n")
    add("| Охотник | Закалка | Точность | Победа | Чистая | С раной | Поражение | Отход | Раундов | "
        "Застали мы | Застала тварь |")
    add("|---|---|---|---|---|---|---|---|---|---|")
    curve = {}
    for lvl in range(1, 6):
        pid = f"h{lvl}"
        world.add_probe(pid, lvl)
        r = batch(world, [pid], "beast_ash", levels=lvl)
        curve[lvl] = r
        f = Fighter(world, pid)
        add(f"| {lvl} | {f.hardening:.2f} | {f.tuning_hit:.2f} | {pct(r['wins'])} | "
            f"{pct(r['win_clean'])} | {pct(r['win_wound'])} | {pct(r['lost'])} | "
            f"{pct(r['retreat'])} | {r['avg_rounds']:.2f} | {pct(r['ambush_people'])} | "
            f"{pct(r['ambush'])} |")
    add("")

    add("## 2. Цель владельца против факта (1×1)\n")
    add("| Уровень | Цель | Факт | Вывод |")
    add("|---|---|---|---|")
    labels = {1: "≤10%", 2: "45–55%", 3: "75–85%", 4: "≥90%", 5: "≥90%"}
    for lvl in range(1, 6):
        lo, hi = goals_for(lvl)
        fact = curve[lvl]["wins"]
        add(f"| {lvl} | {labels[lvl]} | {pct(fact)} | "
            f"{'сходится' if lo <= fact <= hi else 'не сходится'} |")
    add("")

    add("## 3. Скрытая оценка и слово риска (1×1)\n")
    add("| Охотник | Оценка | Слово |")
    add("|---|---|---|")
    for lvl in range(1, 6):
        p = estimate(world, [f"h{lvl}"], "beast_ash")
        add(f"| {lvl} | {pct(p)} | {risk_word(p)} |")
    add("")

    add("## 4. Отряды стартового ростера (Пепельный вор, наказ «бей»)\n")
    add("| Отряд | Победа | Чистая | С раной | Поражение | Отход | Раундов |")
    add("|---|---|---|---|---|---|---|")
    parties = [
        ("Хильда 2 + Орн 2", ["hilda", "orn"]),
        ("Брунни 4 + Скади 3", ["brunni", "skadi"]),
        ("Брунни+Скади+Хильда+Орн", ["brunni", "skadi", "hilda", "orn"]),
        ("Хильда+Орн+Лея", ["hilda", "orn", "leya"]),
        ("Лея+Тове (сборщики)", ["leya", "tove"]),
    ]
    for name, members in parties:
        r = batch(world, members, "beast_ash")
        add(f"| {name} | {pct(r['wins'])} | {pct(r['win_clean'])} | {pct(r['win_wound'])} | "
            f"{pct(r['lost'])} | {pct(r['retreat'])} | {r['avg_rounds']:.2f} |")
    add("")

    add("## 5. Охотник 3 уровня по всем тварям\n")
    add("| Тварь | Ур. | Победа | Поражение | Отход | Раундов | Засада |")
    add("|---|---|---|---|---|---|---|")
    world.add_probe("h3f", 3)
    for beast_id in world.beasts:
        r = batch(world, ["h3f"], beast_id)
        lvl = world.beasts[beast_id]["level"]
        add(f"| {beast_id} | {lvl} | {pct(r['wins'])} | {pct(r['lost'])} | {pct(r['retreat'])} | "
            f"{r['avg_rounds']:.2f} | {pct(r['ambush'])} |")
    add("")

    add("## 6. Порог подготовки: тварь 3 уровня (броня 6) разным оружием\n")
    add("| Охотник | дубина | копьё | топор |")
    add("|---|---|---|---|")
    for lvl, weapon in ((2, "spear"), (3, "spear"), (3, "axe")):
        pass
    for lvl in (2, 3, 4):
        row = []
        for w in ("weapon_club", "weapon_spear", "weapon_axe"):
            pid = f"w{lvl}{w[-3:]}"
            world.add_probe(pid, lvl, weapon=w)
            row.append(batch(world, [pid], "beast_bone")["wins"])
        add(f"| {lvl} | " + " | ".join(pct(v) for v in row) + " |")
    add("")

    add("## 7. Отряды против твари 3 уровня\n")
    add("| Отряд | Победа | Поражение | Отход |")
    add("|---|---|---|---|")
    squads = [
        ("один 3 ур., дубина", ["b3"]),
        ("двое 2 ур., дубины", ["s2a", "s2b"]),
        ("двое 3 ур., дубины", ["s3a", "s3b"]),
        ("3 ур. с копьём + 2 ур.", ["sp3", "b2"]),
    ]
    world.add_probe("b3", 3)
    world.add_probe("b2", 2)
    world.add_probe("s2a", 2)
    world.add_probe("s2b", 2)
    world.add_probe("s3a", 3)
    world.add_probe("s3b", 3)
    world.add_probe("sp3", 3, weapon="weapon_spear")
    for name, members in squads:
        r = batch(world, members, "beast_bone")
        add(f"| {name} | {pct(r['wins'])} | {pct(r['lost'])} | {pct(r['retreat'])} |")
    add("")

    add("## 8. Ранение режет урон: < 50% HP — ×0.75, < 25% — ×0.5\n")
    add("| Отряд | Победа (здоров) | Победа (50% HP) | Победа (25% HP) |")
    add("|---|---|---|---|")
    for name, members in [("Хильда+Орн", ["hilda", "orn"]), ("Брунни+Скади", ["brunni", "skadi"])]:
        healthy = batch(world, members, "beast_ash")
        cells = [pct(healthy["wins"])]
        for share in (0.5, 0.25):
            hp = {pid: {"hp": share * (world.t["base_hp"] + world.t["hp_per_level"]
                                       * world.skill(pid))} for pid in members}
            cells.append(pct(batch(world, members, "beast_ash", hp_profile=hp)["wins"]))
        add(f"| {name} | " + " | ".join(cells) + " |")
    add("")

    add("## 9. Как часто штраф реально включается (1×1)\n")
    add("| Охотник | Бой | Бой под штрафом | Раундов с ×0.75 | Раундов с ×0.5 | "
        "Средний бой |")
    add("|---|---|---|---|---|---|")
    for lvl, beast_id in ((1, "beast_ash"), (2, "beast_ash"), (3, "beast_ash"),
                          (3, "beast_bone"), (4, "beast_fang")):
        pid = f"g{lvl}{beast_id[-4:]}"
        world.add_probe(pid, lvl)
        r = batch(world, [pid], beast_id, levels=lvl, with_penalty=True)
        add(f"| {lvl} | {beast_id} | {pct(r['fights_penalised'])} | "
            f"{r['rounds_wounded']:.2f} | {r['rounds_hurt']:.2f} | {r['avg_rounds']:.2f} |")
    add("")

    text = "\n".join(lines)
    (out / "battle-report.md").write_text(text, encoding="utf-8")
    print(text)


if __name__ == "__main__":
    main()
