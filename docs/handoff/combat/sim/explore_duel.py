"""Разбор одного боя: отряд против болотника.

Печатает по 4000 боёв: исходы, длину боя, кто сколько ударов получил,
как часто включается штраф за ранение, и 3 конкретных боя построчно.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

PARTY = ["hilda", "orn"]
BEAST = "beast_bogling"


def summary(n=4000, seed=2026):
    world = bs.World()
    stats = {"win": 0, "win_with_wound": 0, "lost": 0, "broke_off": 0,
             "retreat_after_wound": 0}
    rounds = [0] * 10
    hits = {"hilda": [0] * 10, "orn": [0] * 10}
    amb_p = amb_b = 0
    for i in range(n):
        rng = random.Random(seed + i)
        fs = [bs.Fighter(world, p, rng=rng) for p in PARTY]
        b = bs.Beast(world, BEAST)
        res = bs.resolve(world, fs, b, rng)
        stats[res["outcome"]] += 1
        rounds[min(9, res["rounds"])] += 1
        amb_p += 1 if res["ambush_people"] else 0
        amb_b += 1 if res["ambush"] else 0
        for p in PARTY:
            hits[p][min(9, res["hits"][p])] += 1
    print(f"исходы за {n} боёв: победа {stats['win'] / n * 100:.1f}% "
          f"(с раной {stats['win_with_wound'] / n * 100:.1f}%), "
          f"поражение {stats['lost'] / n * 100:.1f}%, "
          f"отход {stats['broke_off'] / n * 100:.1f}%")
    print(f"застали мы {amb_p / n * 100:.1f}%   застала тварь {amb_b / n * 100:.1f}%")

    print("\nдлина боя (раунды):")
    for r in range(1, 10):
        if rounds[r]:
            print(f"  {r}: {rounds[r] / n * 100:5.1f}%  " + "█" * int(rounds[r] / n * 100))

    for p in PARTY:
        print(f"\nударов, полученных {p}:")
        for h in range(10):
            if hits[p][h]:
                print(f"  {h}: {hits[p][h] / n * 100:5.1f}%  " + "█" * int(hits[p][h] / n * 100))

    world2 = bs.World()
    print("\nпараметры участников:")
    for p in PARTY:
        f = bs.Fighter(world2, p)
        print(f"  {p}: ур.{f.level} hp {f.tough_max:.0f} урон {f.weapon_min:.0f}..{f.weapon_max:.0f}"
              f"+{(f.level - 1) * 6:.0f} точность {f.tuning_hit:.2f} броня {f.armor:.0f}")
    bb = bs.Beast(world2, BEAST)
    print(f"  {BEAST}: hp {bb.hp:.0f} урон {bb.attack_min:.0f}..{bb.attack_max:.0f} "
          f"броня {bb.armor:.0f} скорость {bb.speed:.0f}")


if __name__ == "__main__":
    summary()
