"""Проверка устойчивости: кривая по другому сиду и по отключённой засаде.

Цель достигнута на seed 2026. Проверяем, что это не подгонка под один сид,
и что засада не является тем единственным, что держит h2.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

GOAL = [(0.0, 0.10), (0.45, 0.55), (0.75, 0.85), (0.88, 1.0), (0.88, 1.0)]


def curve(seed, ambush=True, n=4000, hp_step=0.0):
    world = bs.World()
    out = []
    for level in (1, 2, 3, 4, 5):
        world.add_probe("q", level)
        if not ambush:
            rng_world = None
        wins = 0
        for i in range(n):
            rng = random.Random(seed + i)
            f = bs.Fighter(world, "q", {"level": level}, rng=rng)
            b = bs.Beast(world, "beast_ash", {"hp": float(world.beasts["beast_ash"]["hp"])
                                              * (1 + hp_step * (level - 1))})
            if not ambush:
                b.speed = 0.0
            res = bs.resolve(world, [f], b, rng)
            wins += res["outcome"] in ("win", "win_with_wound")
        out.append(wins / n)
    return out


def show(label, r):
    marks = " ".join(("ok" if lo <= v <= hi else "MISS") for v, (lo, hi) in zip(r, GOAL))
    print(f"{label:28s} " + " ".join(f"{v * 100:5.1f}%" for v in r) + f"  [{marks}]")


if __name__ == "__main__":
    print("уровень                      " + " ".join(f"{'h' + str(i + 1):>6s}" for i in range(5)))
    show("n=4000, seed 2026", curve(2026, n=4000))
    show("n=4000, seed 7", curve(7, n=4000))
    show("n=4000, seed 999", curve(999, n=4000))
    show("n=15000, seed 2026", curve(2026, n=15000))
    show("n=4000, без засады", curve(2026, ambush=False))
    show("n=4000, HP твари +10%", curve(2026, hp_step=0.10))
    show("n=4000, HP твари -10%", curve(2026, hp_step=-0.10))
