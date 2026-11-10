"""Вклад боевых навыков охотника по уровням.

Правило: на каждый левел-ап — один боевой навык из пула своего уровня.
База — «чистый охотник» без навыков. Считаем, как каждый навык сдвигает победу
против Пепельного вора (1×1, дубина, стёганка), и насколько уровень целиком
(все навыки ниже — взятые по одному в пуле с максимальной пользой).
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402


def win(level, skills, seed=2026, n=5000, beast_id="beast_ash", beast_over=None):
    world = bs.World()
    world.add_probe("q", level)
    wins = 0
    for i in range(n):
        rng = random.Random(seed + i)
        f = bs.Fighter(world, "q", {"level": level, "skills": skills}, rng=rng)
        b = bs.Beast(world, beast_id, beast_over)
        res = bs.resolve(world, [f], b, rng)
        wins += res["outcome"] in ("win", "win_with_wound")
    return wins / n


if __name__ == "__main__":
    world = bs.World()
    base = {lvl: win(lvl, []) for lvl in (2, 3, 4, 5)}
    print("база без навыков:  " + "  ".join(f"h{l} {v * 100:.1f}%" for l, v in base.items()))
    print()
    print(f"{'навык':22s} {'ур':>2s}  {'победа':>6s}  {'дельта':>7s}")
    for sid, row in world.skills.items():
        lvl = int(row["level"])
        p = win(lvl, [sid])
        print(f"{row['name']:22s} {lvl:2d}  {p * 100:5.1f}%  {(p - base[lvl]) * 100:+6.1f}")
