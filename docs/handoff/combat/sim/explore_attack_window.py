"""Где окно по урону вора: ищем attack, при котором ОБА коридора (h2 и h3) держатся.

Кривая от attack двигается целиком, поэтому окно может отсутствовать — печатаем
это прямо, а не пытаемся подобрать.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402


def curve(amin, amax, seed=2026, n=6000):
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


if __name__ == "__main__":
    print("attack   h1     h2     h3     h4     h5    h2 ok  h3 ok")
    for amin, amax in ((9, 15), (9, 14), (8, 15), (9, 13), (8, 14)):
        r = curve(amin, amax)
        h2ok = 0.45 <= r[1] <= 0.55
        h3ok = 0.75 <= r[2] <= 0.85
        print(f"{amin}..{amax}  " + " ".join(f"{v * 100:5.1f}%" for v in r)
              + f"   {'yes' if h2ok else 'no ':4s}  {'yes' if h3ok else 'no'}")
