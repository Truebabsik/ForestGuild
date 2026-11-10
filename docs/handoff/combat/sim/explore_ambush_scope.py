"""Нужны ли бесплатные удары ВСЕМ при засаде, и нужна ли стае атака каждого зверя.

Четыре варианта, применяемые к уже собранному движку:
  full  — как сейчас: засада бьёт всех, стая бьёт каждым зверем (база сравнения);
  one   — засада бьёт только одного (первого, кто попал), стая — как сейчас;
  pack1 — засада всех, но стая наносит ОДИН суммарный удар (по среднему зверю);
  both  — засада одним + стая одним ударом.

Смотрим на отрядах разного размера: это единственный способ поймать
«множитель на размер отряда», ради которого затеян замер.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

BEAST = "beast_bogling"
PARTIES = (
    ("один 2 ур.", ["hilda"]),
    ("Хильда+Орн (2+2)", ["hilda", "orn"]),
    ("Хильда+Орн+Брунни", ["hilda", "orn", "brunni"]),
    ("четверо", ["brunni", "skadi", "hilda", "orn"]),
)


def fight(members, count, mode, n=3000, seed=2026):
    world = bs.World()
    if mode == "one":
        world.t["ambush_all_hit"] = 0.0
    if mode in ("pack1", "both"):
        world.t["pack_single_strike"] = 1.0
    if mode == "both":
        world.t["ambush_all_hit"] = 0.0
    wins = 0
    for i in range(n):
        rng = random.Random(seed + i)
        fs = [bs.Fighter(world, p, rng=rng) for p in members]
        beasts = [bs.Beast(world, BEAST) for _ in range(count)]
        res = bs.resolve(world, fs, beasts, rng)
        wins += res["outcome"] in ("win", "win_with_wound")
    return wins / n


if __name__ == "__main__":
    print(f"{'отряд':22s} {'твар':>4s}  " + "  ".join(f"{m:>6s}" for m in
                                                     ("full", "one", "pack1", "both")))
    for name, members in PARTIES:
        for count in (1, 2):
            cells = [f"{fight(members, count, m) * 100:5.1f}%" for m in
                     ("full", "one", "pack1", "both")]
            print(f"{name:22s} {count:>4d}  " + "  ".join(cells))
