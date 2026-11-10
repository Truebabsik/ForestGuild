"""Штраф ранения на поход: грузоподъёмность и скорость.

После боя считаем по отряду:
  - сколько слотов груза осталось (4 на человека, × множитель по состоянию),
  - с какой скоростью отряд пойдёт домой (1.0 — обычная, по самому медленному),
  - хватает ли слотов на то, что уже лежит.

Состояния по HP: цел (>=50%), ранен (<50%), тяжело ранен (<25%), выбит (0).
Три скорости независимы: раненый идёт сам, тяжелораненый тоже идёт сам (медленнее),
выбитого несут — это отдельная скорость носилок, не путать с тяжёлой ранею.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

TRIALS = (
    ("один 2 ур.", ["hilda"]),
    ("один 2 ур. + сборщик", ["hilda", "leya"]),
    ("двое 2 ур.", ["hilda", "orn"]),
    ("двое 2 ур. + сборщик", ["hilda", "orn", "leya"]),
    ("Брунни 4 + Скади 3", ["brunni", "skadi"]),
    ("Брунни 4 + Скади 3 + сборщик", ["brunni", "skadi", "leya"]),
)


def state(frac, t):
    if frac <= 0:
        return "out"
    if frac < t["hurt_threshold"]:
        return "hurt"
    if frac < t["wound_threshold"]:
        return "wound"
    return "ok"


def sample(members, count, n=4000, seed=2026, beast="beast_bogling"):
    """Один прогон боёв; после боя снимаем, что осталось — дальше считаем повторно."""
    world = bs.World()
    t = world.t
    rows = []
    for i in range(n):
        rng = random.Random(seed + i)
        fs = [bs.Fighter(world, p, rng=rng) for p in members]
        beasts = [bs.Beast(world, beast) for _ in range(count)]
        res = bs.resolve(world, fs, beasts, rng)
        kills = sum(1 for b in beasts if b.hp <= 0)
        fracs = [max(0.0, f.hp) / f.tough_max for f in fs]
        rows.append({"fracs": fracs, "kills": kills, "outcome": res["outcome"]})
    return t, rows


def evaluate(t, rows):
    """Единственная оценка последствий: всё, что нужно для таблиц, считается здесь."""
    n = len(rows)
    k = len(rows[0]["fracs"])
    per = t["carry_slots_per_person"]
    agg = dict(ok=0, wound=0, hurt=0, out=0, raw_slots=0.0, usable_slots=0.0, speed=0.0,
               carry_1=0, has_slots_2=0, deficit=0, wiped=0,
               slow_by_wound=0, slow_by_hurt=0, slow_by_out=0, slow_half=0)
    for row in rows:
        slots = 0.0
        slowest = 1.0
        slowest_src = "ok"
        outs = 0
        for frac in row["fracs"]:
            st = state(frac, t)
            agg[st] += 1
            if st == "out":
                outs += 1
                if t["carry_out_speed_mult"] < slowest:
                    slowest, slowest_src = t["carry_out_speed_mult"], "out"
                continue
            if st == "hurt":
                slots += per * t["hurt_carry_mult"]
                if t["hurt_speed_mult"] < slowest:
                    slowest, slowest_src = t["hurt_speed_mult"], "hurt"
            elif st == "wound":
                slots += per * t["wound_carry_mult"]
                if t["wound_speed_mult"] < slowest:
                    slowest, slowest_src = t["wound_speed_mult"], "wound"
            else:
                slots += per
        raw = slots - outs * t["carry_out_slots"]
        if outs == k:
            # тащить некому: отряд домой не возвращается своими силами
            slowest, slowest_src = 0.0, "wiped"
            agg["wiped"] += 1
        agg["deficit"] += 1 if raw < 0 else 0
        agg["raw_slots"] += raw
        agg["usable_slots"] += max(0.0, raw)
        agg["speed"] += slowest
        agg["slow_by_wound"] += 1 if slowest_src == "wound" else 0
        agg["slow_by_hurt"] += 1 if slowest_src == "hurt" else 0
        agg["slow_by_out"] += 1 if slowest_src == "out" else 0
        agg["slow_half"] += 1 if slowest < 0.5 else 0
        if slowest_src != "wiped":
            agg["carry_1"] += 1 if (row["kills"] >= 1 and raw >= 3) else 0
            agg["has_slots_2"] += 1 if raw >= 6 else 0
    return {
        "ok": agg["ok"] / n / k, "wound": agg["wound"] / n / k,
        "hurt": agg["hurt"] / n / k, "out": agg["out"] / n / k,
        "slots": agg["raw_slots"] / n, "speed": agg["speed"] / n,
        "carry_1": agg["carry_1"] / n, "has_slots_2": agg["has_slots_2"] / n,
        "wiped": agg["wiped"] / n, "deficit": agg["deficit"] / n,
        "slow_by_wound": agg["slow_by_wound"] / n,
        "slow_by_hurt": agg["slow_by_hurt"] / n, "slow_by_out": agg["slow_by_out"] / n,
        "slow_half": agg["slow_half"] / n,
        "usable_slots": agg["usable_slots"] / n,
    }


if __name__ == "__main__":
    name_w = max([len("отряд")] + [len(n) for n, _ in TRIALS]) + 2
    for count in (1, 2):
        print(f"\n=== {count} болотник(а) ===")
        print("  скорость: ранен {:.2f}, тяжело ранен {:.2f}, носилки {:.2f}".format(
            bs.World().t["wound_speed_mult"], bs.World().t["hurt_speed_mult"],
            bs.World().t["carry_out_speed_mult"]))
        print(f"  {'отряд':{name_w}s} {'цел':>5s} {'ранен':>6s} {'тяж':>5s} {'выб':>5s} "
              f"{'скорость':>9s} {'медл. ран':>10s} {'медл. тяж':>9s} {'медл. нос.':>10s} "
              f"{'слотов':>7s} {'дефицит':>8s} {'ту́ша 1':>8s} {'ещё 1':>7s}")
        for name, members in TRIALS:
            t, rows = sample(members, count)
            s = evaluate(t, rows)
            print(f"  {name:{name_w}s} {s['ok']:5.2f} {s['wound']:6.2f} {s['hurt']:5.2f} "
                  f"{s['out']:5.2f} {s['speed'] * 100:8.0f}% "
                  f"{s['slow_by_wound'] * 100:9.0f}% {s['slow_by_hurt'] * 100:8.0f}% "
                  f"{s['slow_by_out'] * 100:9.0f}% "
                  f"{s['usable_slots']:7.2f} {s['deficit'] * 100:7.0f}% "
                  f"{s['carry_1'] * 100:7.0f}% {s['has_slots_2'] * 100:6.0f}%")
