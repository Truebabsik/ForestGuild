"""Засада отдельно: кто застаёт кого, и что это стоит каждой стороне.

Печатает по каждому уровню охотника против Пепельного вора:
  - сколько боёв охотник застал тварь (атака из засады группы),
  - сколько боёв тварь застала охотника (атака из засады монстра),
  - сколько раз никто не застал,
  - и как исход делится между этими тремя случаями.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402


def run(level, skills=(), n=6000, seed=2026, beast_id="beast_ash"):
    world = bs.World()
    world.add_probe("q", level)
    buckets = {"people": [], "beast": [], "none": []}
    chance = None
    for i in range(n):
        rng = random.Random(seed + i)
        f = bs.Fighter(world, "q", {"level": level, "skills": list(skills)}, rng=rng)
        b = bs.Beast(world, beast_id)
        if chance is None:
            chance = bs.detect_chance(world, [f], b)
        res = bs.resolve(world, [f], b, rng)
        key = "people" if res["ambush_people"] else ("beast" if res["ambush"] else "none")
        buckets[key].append(res["outcome"] in ("win", "win_with_wound"))
    out = {}
    for key, arr in buckets.items():
        out[key] = (len(arr) / n, sum(arr) / len(arr) if arr else 0.0)
    return out, chance


if __name__ == "__main__":
    print("шансы обнаружения: группа / тварь (Пепельный вор)")
    world = bs.World()
    for lvl in (1, 2, 3, 4, 5):
        world.add_probe("q", lvl)
        f = bs.Fighter(world, "q", {"level": lvl})
        b = bs.Beast(world, "beast_ash")
        p, m = bs.detect_chance(world, [f], b)
        print(f"  h{lvl}: группа {p * 100:4.1f}%   тварь {m * 100:4.1f}%")

    print()
    print("исход по тому, кто застал")
    print(f"{'':4s}{'застала группа':>28s}{'застала тварь':>26s}{'не застал никто':>24s}")
    print(f"{'ур':>4s}{'доля':>9s}{'победа':>10s}{'доля':>9s}{'победа':>10s}{'доля':>9s}{'победа':>9s}")
    for lvl in (1, 2, 3, 4, 5):
        res, _ = run(lvl)
        cells = ""
        for key in ("people", "beast", "none"):
            share, winrate = res[key]
            cells += f"{share * 100:8.1f}%{winrate * 100:9.1f}%"
        print(f"{lvl:4d}{cells}")

    print()
    print("навык «Из засады» на 3 уровне:")
    a, _ = run(3)
    b_, _ = run(3, ["skill_ambusher"])
    for key in ("people", "beast", "none"):
        print(f"  {key:7s} без навыка {a[key][0] * 100:5.1f}%  ->  с навыком {b_[key][0] * 100:5.1f}%")
