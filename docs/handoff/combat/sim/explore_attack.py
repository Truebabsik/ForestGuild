"""Как урон Пепельного вора двигает кривую при включённом штрафе за ранение.

Штраф §4.1 в силе: <50% HP → ×0.75, <25% → ×0.5. Двигаем только attack_min/attack_max
вора, всё остальное — как в контракте.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

GOAL = [(0.0, 0.10), (0.45, 0.55), (0.75, 0.85), (0.88, 1.0), (0.88, 1.0)]


def curve(seed, amin, amax, n=4000):
    world = bs.World()
    out = []
    for level in (1, 2, 3, 4, 5):
        world.add_probe("q", level)
        wins = 0
        for i in range(n):
            rng = random.Random(seed + i)
            f = bs.Fighter(world, "q", {"level": level}, rng=rng)
            b = bs.Beast(world, "beast_ash", {"attack_min": amin, "attack_max": amax})
            res = bs.resolve(world, [f], b, rng)
            wins += res["outcome"] in ("win", "win_with_wound")
        out.append(wins / n)
    return out


def show(label, r):
    marks = " ".join(("ok" if lo <= v <= hi else "MISS") for v, (lo, hi) in zip(r, GOAL))
    print(f"{label:20s} " + " ".join(f"{v * 100:5.1f}%" for v in r) + f"  [{marks}]")


if __name__ == "__main__":
    print("attack вора          " + " ".join(f"{'h' + str(i + 1):>6s}" for i in range(5)))
    for amin, amax in ((9, 15), (8, 14), (8, 13), (7, 13), (7, 12), (6, 12), (9, 13), (9, 12)):
        show(f"{amin}..{amax}", curve(2026, amin, amax))
