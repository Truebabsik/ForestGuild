"""Хильда+Орн против стаи болотников: 1, 2 и 3.

Показывает, как стая меняет бой: исход, длина, распределение ударов по людям,
кто падает первым.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

PARTY = ["hilda", "orn"]
BEAST = "beast_bogling"


def run(count, n=4000, seed=2026):
    world = bs.World()
    stats = {"win": 0, "win_with_wound": 0, "lost": 0, "broke_off": 0,
             "retreat_after_wound": 0}
    rounds = [0] * 12
    hits = {p: [0] * 10 for p in PARTY}
    amb_p = amb_b = 0
    for i in range(n):
        rng = random.Random(seed + i)
        fs = [bs.Fighter(world, p, rng=rng) for p in PARTY]
        beasts = [bs.Beast(world, BEAST) for _ in range(count)]
        res = bs.resolve(world, fs, beasts, rng)
        stats[res["outcome"]] += 1
        rounds[min(11, res["rounds"])] += 1
        amb_p += 1 if res["ambush_people"] else 0
        amb_b += 1 if res["ambush"] else 0
        for p in PARTY:
            hits[p][min(9, res["hits"][p])] += 1
    return stats, rounds, hits, amb_p / n, amb_b / n, n


if __name__ == "__main__":
    for count in (1, 2, 3):
        stats, rounds, hits, ap, ab, n = run(count)
        total = sum(stats.values())
        win = (stats["win"] + stats["win_with_wound"]) / n
        avg_rounds = sum(r * c for r, c in enumerate(rounds)) / n
        print(f"\n=== {count} болотник(а) — исход: победа {win * 100:5.1f}% "
              f"(с раной {stats['win_with_wound'] / n * 100:5.1f}%), "
              f"пор. {stats['lost'] / n * 100:5.1f}%, раундов {avg_rounds:.2f}")
        print(f"    застали мы {ap * 100:.1f}%, застала стая {ab * 100:.1f}%")
        print("    ударов получила Хильда: " + " ".join(
            f"{h}:{hits['hilda'][h] / n * 100:.0f}%" for h in range(6) if hits["hilda"][h]))
        print("    ударов получил  Орн:    " + " ".join(
            f"{h}:{hits['orn'][h] / n * 100:.0f}%" for h in range(6) if hits["orn"][h]))
        print("    длительность: " + " ".join(
            f"{r}:{rounds[r] / n * 100:.0f}%" for r in range(1, 9) if rounds[r]))
