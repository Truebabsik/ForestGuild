"""Состояние отряда после стычки — главная метрика, а не победа.

Собиратель не бьёт и только спасается (уклоняется с шансом gatherer_dodge).
Считаем по каждому исходу:
  - сколько людей выбито (HP <= 0),
  - сколько осталось «ранен» (<50% HP) и «тяжело ранен» (<25%),
  - суммарный остаток HP отряда,
  - сколько ночей лечить выбитых (rest_per_night = 3).
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


def run(members, count, n=4000, seed=2026, beast="beast_bogling"):
    world = bs.World()
    t = world.t
    stats = dict(win=0, lost=0, broke=0, out_total=0, wounded=0, hurt=0,
                 hp_share=0.0, cure_nights=0, any_out=0)
    for i in range(n):
        rng = random.Random(seed + i)
        fs = [bs.Fighter(world, p, rng=rng) for p in members]
        beasts = [bs.Beast(world, beast) for _ in range(count)]
        res = bs.resolve(world, fs, beasts, rng)
        if res["outcome"] in ("win", "win_with_wound"):
            stats["win"] += 1
        elif res["outcome"] == "lost":
            stats["lost"] += 1
        else:
            stats["broke"] += 1
        outs = wounded = hurt = 0
        share = 0.0
        for f in fs:
            frac = max(0.0, f.hp) / f.tough_max
            share += frac
            if f.out:
                outs += 1
                # выбывший лечится от нуля до полного: полный HP / 3 ночи
                stats["cure_nights"] += f.tough_max / t["rest_per_night"]
            elif frac < t["hurt_threshold"]:
                hurt += 1
            elif frac < t["wound_threshold"]:
                wounded += 1
        stats["out_total"] += outs
        stats["any_out"] += 1 if outs else 0
        stats["wounded"] += wounded
        stats["hurt"] += hurt
        stats["hp_share"] += share / len(fs)
    for k in ("out_total", "wounded", "hurt", "any_out", "cure_nights"):
        stats[k] /= n
    stats["hp_share"] /= n
    return stats


if __name__ == "__main__":
    for count in (1, 2):
        print(f"\n=== {count} болотник(а) ===")
        print(f"{'отряд':26s} {'победа':>7s} {'пор.':>6s} {'выбито':>7s} "
              f"{'хоть 1 выбит':>12s} {'ранен':>6s} {'тяж.ран':>8s} {'HP отр.':>8s} {'ночей':>6s}")
        for name, members in TRIALS:
            s = run(members, count)
            print(f"{name:26s} {s['win'] / 40:6.1f}% {s['lost'] / 40:5.1f}% "
                  f"{s['out_total']:7.2f} {s['any_out'] * 100:11.1f}% "
                  f"{s['wounded']:6.2f} {s['hurt']:8.2f} {s['hp_share'] * 100:7.0f}% "
                  f"{s['cure_nights']:6.1f}")
