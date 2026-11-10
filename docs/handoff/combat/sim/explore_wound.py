"""Вклад штрафа за ранение (<50% HP ×0.75, <25% HP ×0.5) в кривую 1×1.

Сравнивает прогон с правилом и прогон, где оба порога выставлены в 0 (штраф не
достижим). Второй вариант — не «старая механика», а именно «без этого правила»:
остальные формул те же.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

GOAL = [(0.0, 0.10), (0.45, 0.55), (0.75, 0.85), (0.88, 1.0), (0.88, 1.0)]


def curve(seed, penalty=True, n=4000):
    world = bs.World()
    if not penalty:
        world.t["wound_threshold"] = 0.0
        world.t["hurt_threshold"] = 0.0
    out = []
    for level in (1, 2, 3, 4, 5):
        world.add_probe("q", level)
        wins = 0
        for i in range(n):
            rng = random.Random(seed + i)
            f = bs.Fighter(world, "q", {"level": level}, rng=rng)
            b = bs.Beast(world, "beast_ash")
            res = bs.resolve(world, [f], b, rng)
            wins += res["outcome"] in ("win", "win_with_wound")
        out.append(wins / n)
    return out


def show(label, r):
    marks = " ".join(("ok" if lo <= v <= hi else "MISS") for v, (lo, hi) in zip(r, GOAL))
    print(f"{label:22s} " + " ".join(f"{v * 100:5.1f}%" for v in r) + f"  [{marks}]")


if __name__ == "__main__":
    print("уровень               " + " ".join(f"{'h' + str(i + 1):>6s}" for i in range(5)))
    show("со штрафом", curve(2026, penalty=True))
    show("без штрафа", curve(2026, penalty=False))
    show("штраф, seed 7", curve(7, penalty=True))
    show("штраф, seed 999", curve(999, penalty=True))
