"""Гипотеза: исход решает ЗАСАДА.

Тварь 1 ур. быстрее любого одиночки (speed 4 против «чутья» охотника 2), значит первый
удар её. У h1–h2 это 8-11 HP из 27-36 при ударе твари 6-12 — и ещё один удар в каждом раунде.
Проверяем: тот же бой без засады.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402


def rate(level, n=4000, seed=11, ambush=True, beast="beast_ash", **kwargs):
    world = bs.World()
    world.add_probe("q", level)
    wins = 0
    for i in range(n):
        rng = random.Random(seed + i)
        f = bs.Fighter(world, "q", {"level": level}, rng=rng)
        b = bs.Beast(world, beast)
        if not ambush:
            b.speed = 0.0
        res = bs.resolve(world, [f], b, rng)
        wins += res["outcome"] in ("win", "win_with_wound")
    return wins / n


print("дубина, стёганка, закалка по уровню; тварь 1 ур.")
print("| уровень | с засадой | без засады |")
print("|---|---|---|")
for level in (1, 2, 3, 4, 5):
    print(f"| {level} | {rate(level) * 100:5.1f}% | {rate(level, ambush=False) * 100:5.1f}% |")

print()
print("для сравнения: h2 против твари 2 ур. (искромёт), засада")
print(f"  с засадой {rate(2, beast='beast_spark') * 100:5.1f}%, "
      f"без {rate(2, ambush=False, beast='beast_spark') * 100:5.1f}%")
