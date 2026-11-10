"""Бюджет похода в минутах: сколько светлого времени остаётся на сбор.

Световой день — `daylight_minutes` (600). Дорога в одну сторону — `travel_minutes`
зоны. Отряд выходит на рассвете, бой стоит 5 минут за раунд.

Ключевая величина — остаток на работу: сколько минут отряд может провести на месте
и всё ещё выйти до темноты. Возврат идёт со скоростью самого медленного, поэтому
каждое ранение отнимает бюджет у всей группы.

Запуск: python3 explore_day_budget.py
"""

import csv
import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

DATA = Path(__file__).resolve().parent.parent / "data"
TUNING = Path(__file__).resolve().parent.parent / "data" / "tuning.csv"

MINUTES_PER_ROUND_DEFAULT = 5

TRIALS = (
    ("один 2 ур.", ["hilda"]),
    ("двое 2 ур.", ["hilda", "orn"]),
    ("двое 2 ур. + сборщик", ["hilda", "orn", "leya"]),
    ("Брунни 4 + Скади 3", ["brunni", "skadi"]),
)


def read_tuning():
    return {r["id"]: r["value"] for r in
            csv.DictReader(TUNING.open(encoding="utf-8"), delimiter=";")}


def load_zones():
    return list(csv.DictReader((DATA / "zones.csv").open(encoding="utf-8"), delimiter=";"))


def beast_count_in_zone(zone_id):
    rows = list(csv.DictReader((DATA / "zone_beasts.csv").open(encoding="utf-8"),
                              delimiter=";"))
    return sum(int(r["initial_count"]) for r in rows if r["zone_id"] == zone_id)


def speed_of(t, fracs):
    """Скорость похода по самому медленному; 0 — идти некому."""
    if all(frac <= 0 for frac in fracs):
        return 0.0
    slowest = 1.0
    for frac in fracs:
        if frac <= 0:
            slowest = min(slowest, t["carry_out_speed_mult"])
        elif frac < t["hurt_threshold"]:
            slowest = min(slowest, t["hurt_speed_mult"])
        elif frac < t["wound_threshold"]:
            slowest = min(slowest, t["wound_speed_mult"])
    return slowest


def budget(t, travel_min, fracs, rounds, daylight):
    """Минуты: сколько остаётся на работу. Отрицательное — до темноты не успеть."""
    per_round = t.get("minutes_per_round", MINUTES_PER_ROUND_DEFAULT)
    speed = speed_of(t, fracs)
    if speed <= 0:
        return None
    back = travel_min / speed
    spent = travel_min + back + rounds * per_round
    return daylight - spent


def sample(members, count, beast, n=4000, seed=2026):
    world = bs.World()
    out = []
    for i in range(n):
        rng = random.Random(seed + i)
        fs = [bs.Fighter(world, p, rng=rng) for p in members]
        beasts = [bs.Beast(world, beast) for _ in range(count)]
        res = bs.resolve(world, fs, beasts, rng)
        out.append({"fracs": [max(0.0, f.hp) / f.tough_max for f in fs],
                    "rounds": res["rounds"], "outcome": res["outcome"]})
    return world.t, out


def summarise(t, rows, travel_min, daylight):
    vals = []
    no_return = 0
    for row in rows:
        b = budget(t, travel_min, row["fracs"], row["rounds"], daylight)
        if b is None:
            no_return += 1
        else:
            vals.append(b)
    n = len(rows)
    vals.sort()
    if not vals:
        return {"no_return": 1.0, "median": 0.0, "p10": 0.0, "negative": 1.0}
    return {
        "no_return": no_return / n,
        "median": vals[len(vals) // 2],
        "p10": vals[int(len(vals) * 0.1)],
        "negative": sum(1 for v in vals if v < 0) / n,
    }


if __name__ == "__main__":
    tn = read_tuning()
    daylight = int(tn["daylight_minutes"])
    zones = load_zones()
    # скорость берём из боевой настройки: походные ручки живут там же
    t0 = bs.World().t

    assert speed_of(t0, [1.0]) == 1.0, "здоровый идёт как обычно"
    assert speed_of(t0, [0.5]) == 1.0, "ровно 50% — ещё не ранен (порог строгий)"
    assert speed_of(t0, [0.49]) == t0["wound_speed_mult"], "ниже 50% — ранен"
    assert speed_of(t0, [0.24]) == t0["hurt_speed_mult"], "ниже 25% — тяжело ранен"
    assert speed_of(t0, [0.0, 1.0]) == t0["carry_out_speed_mult"], "несут выбитого"
    assert speed_of(t0, [0.0, 0.0]) == 0.0, "все выбиты — идти некому"
    print("Контроль состояний: ok\n")

    print(f"Световой день {daylight} мин ({daylight // 60} ч), бой "
          f"{int(t0.get('minutes_per_round', MINUTES_PER_ROUND_DEFAULT))} мин/раунд\n")

    print("Остаток на работу до боя (здоровый отряд, скорость 1.0):")
    print(f"  {'зона':22s} {'км':>5s} {'туда':>6s} {'обратно':>8s} {'дорога':>7s} "
          f"{'на сбор':>8s}")
    for z in zones:
        tm = int(z["travel_minutes"])
        road = tm * 2
        print(f"  {z['name']:22s} {float(z['distance_km']):5.1f} {tm:6d} {tm:8d} "
              f"{road:7d} {daylight - road:8d}")

    print("\nБюджет по состояниям отряда (без боя, только дорога; порог строгий <):")
    states = (
        ("здоров", [1.0]),
        ("ранен (0.49)", [0.49]),
        ("тяж (0.24)", [0.24]),
        ("несут выбитого", [0.0, 1.0]),
        ("два раненых", [0.49, 0.49]),
    )
    head = f"  {'зона':22s}"
    for name, _ in states:
        head += f" {name:>17s}"
    print(head)
    for z in zones:
        tm = int(z["travel_minutes"])
        line = f"  {z['name']:22s}"
        for _, fracs in states:
            b = budget(t0, tm, fracs, 0, daylight)
            line += f" {b:16.0f}м" if b is not None else f" {'нет':>17s}"
        print(line)

    print("\nПосле боя: медиана остатка на сбор (в минутах), по зверю на зону:")
    for beast, count in (("beast_ash", 1), ("beast_bogling", 2)):
        print(f"\n=== {beast} × {count} ===")
        print(f"  {'зона':22s} {'отряд':26s} {'не верн':>8s} {'<0 мин':>7s} "
              f"{'медиана':>8s} {'P10':>6s}")
        for z in zones:
            cnt = beast_count_in_zone(z["id"])
            tag = "нет своих" if cnt == 0 else f"{cnt} тв."
            for name, members in TRIALS:
                t, rows = sample(members, count, beast)
                s = summarise(t, rows, int(z["travel_minutes"]), daylight)
                print(f"  {z['name'] + ' (' + tag + ')':22s} {name:26s} "
                      f"{s['no_return'] * 100:7.0f}% {s['negative'] * 100:6.0f}% "
                      f"{s['median']:8.0f} {s['p10']:6.0f}")
