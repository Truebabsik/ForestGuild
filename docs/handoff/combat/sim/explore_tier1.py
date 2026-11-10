"""Твари 1 уровня: кривая по голым уровням охотника и роли.

Проверяем четыре твари первого пояса — вор (засадник), осколок (ровный с броней),
стеклянный червь (толстый без брони), болотник (медленный с бронёй) — и смотрим,
разводится ли «разная тварь → разный ответ».
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

LEVELS = (1, 2, 3)
BEASTS = ("beast_ash", "beast_shard", "beast_glassworm", "beast_bogling")


def curve(beast_id, n=4000, seed=2026, skills=()):
    world = bs.World()
    out = []
    for level in LEVELS:
        world.add_probe("q", level)
        wins = amb_people = amb_beast = 0
        rounds = 0
        for i in range(n):
            rng = random.Random(seed + i)
            f = bs.Fighter(world, "q", {"level": level, "skills": list(skills)}, rng=rng)
            b = bs.Beast(world, beast_id)
            res = bs.resolve(world, [f], b, rng)
            wins += res["outcome"] in ("win", "win_with_wound")
            amb_people += 1 if res["ambush_people"] else 0
            amb_beast += 1 if res["ambush"] else 0
            rounds += res["rounds"]
        out.append((wins / n, amb_people / n, amb_beast / n, rounds / n))
    return out


if __name__ == "__main__":
    world = bs.World()
    print(f"{'тварь':18s} {'hp':>3s} {'урн':>6s} {'бр':>3s} {'ск':>3s}   "
          + "  ".join(f"h{l}" for l in LEVELS) + "    застали/застала (h2)   бой h2")
    for bid in BEASTS:
        row = world.beasts[bid]
        r = curve(bid)
        cells = "  ".join(f"{v[0] * 100:5.1f}%" for v in r)
        mid = r[1]
        print(f"{bid:18s} {row['hp']:>3s} {row['attack_min']}..{row['attack_max']} "
              f"{row['armor']:>3s} {row['speed']:>3s}   {cells}   "
              f"{mid[1] * 100:5.1f}% / {mid[2] * 100:5.1f}%   {mid[3]:.2f}")

    print("\nотряды стартового ростера против каждой твари 1 уровня:")
    parties = (
        ("Хильда 2 + Орн 2", ["hilda", "orn"]),
        ("Брунни 4 + Скади 3", ["brunni", "skadi"]),
        ("Лея+Тове (сборщики)", ["leya", "tove"]),
    )
    for name, members in parties:
        cells = []
        for bid in BEASTS:
            cells.append(f"{bs.batch(world, members, bid)['wins'] * 100:5.1f}%")
        print(f"  {name:22s} " + "  ".join(cells))
