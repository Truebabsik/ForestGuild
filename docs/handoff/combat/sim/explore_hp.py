"""Как HP Пепельного вора двигает кривую при включённом штрафе за ранение.

В отличие от урона, HP при ударе по твари не меняет длину боя — значит штраф за
ранение не будет самогаситься. Ищем окно, где h2 и h3 одновременно в коридоре.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402


def curve(hp, seed=2026, n=6000):
    world = bs.World()
    out = []
    rounds = []
    for level in (1, 2, 3, 4, 5):
        world.add_probe("q", level)
        wins = total_rounds = 0
        for i in range(n):
            rng = random.Random(seed + i)
            f = bs.Fighter(world, "q", {"level": level}, rng=rng)
            b = bs.Beast(world, "beast_ash", {"hp": hp})
            res = bs.resolve(world, [f], b, rng)
            wins += res["outcome"] in ("win", "win_with_wound")
            total_rounds += res["rounds"]
        out.append(wins / n)
        rounds.append(total_rounds / n)
    return out, rounds


if __name__ == "__main__":
    print("hp вора   h1     h2     h3     h4     h5    h2 ok  h3 ok   бой h2")
    for hp in (30, 29, 28, 27, 26, 25, 24, 22, 20):
        r, rd = curve(hp)
        h2ok = 0.45 <= r[1] <= 0.55
        h3ok = 0.75 <= r[2] <= 0.85
        print(f"{hp:5d}   " + " ".join(f"{v * 100:5.1f}%" for v in r)
              + f"   {'yes' if h2ok else 'no ':4s}  {'yes' if h3ok else 'no':3s}   {rd[1]:.2f}")
