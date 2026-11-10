"""Сравнение двух правил засады.

  variant A — засада только бьёт первой без учёта брони (ambush_skip_reply = 0);
  variant B — засада ещё и лишает застигнутую сторону ответного удара в 1-м раунде.

Печатаем кривую по уровням и отдельно — по ролям твари (засадник / тяжеловес).
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

GOAL = [(0.0, 0.10), (0.45, 0.55), (0.75, 0.85), (0.88, 1.0), (0.88, 1.0)]


def curve(skip_reply, n=5000, seed=2026, beast_over=None, skills=None, levels=(1, 2, 3, 4, 5)):
    world = bs.World()
    world.t["ambush_skip_reply"] = float(skip_reply)
    out = []
    for level in levels:
        world.add_probe("q", level)
        wins = 0
        for i in range(n):
            rng = random.Random(seed + i)
            f = bs.Fighter(world, "q", {"level": level, "skills": skills or []}, rng=rng)
            b = bs.Beast(world, "beast_ash", beast_over)
            res = bs.resolve(world, [f], b, rng)
            wins += res["outcome"] in ("win", "win_with_wound")
        out.append(wins / n)
    return out


def show(label, r):
    marks = " ".join(("ok" if lo <= v <= hi else "MISS") for v, (lo, hi) in zip(r, GOAL))
    print(f"{label:30s} " + " ".join(f"{v * 100:5.1f}%" for v in r) + f"  [{marks}]")


if __name__ == "__main__":
    print("уровень                          " + " ".join(f"{'h' + str(i + 1):>6s}" for i in range(5)))
    print("\n[A] засада только бьёт первой без брони (текущее):")
    show("  кривая A", curve(0))
    print("\n[B] засада лишает ответа в 1-м раунде:")
    show("  кривая B", curve(1))

    print("\nроли твари (охотник 3 ур.), A vs B:")
    roles = (
        ("засадник spd4 arm0", {"hp": 30, "armor": 0, "speed": 4}),
        ("тяжеловес spd1 arm12", {"hp": 45, "armor": 12, "speed": 1}),
    )
    for name, over in roles:
        beast_over = {k: v for k, v in over.items() if k != "speed"}
        res = {}
        for skip in (0, 1):
            world = bs.World()
            world.t["ambush_skip_reply"] = float(skip)
            world.add_probe("q", 3)
            n, wins = 4000, 0
            for i in range(n):
                rng = random.Random(2026 + i)
                f = bs.Fighter(world, "q", {"level": 3}, rng=rng)
                b = bs.Beast(world, "beast_ash", beast_over)
                b.speed = float(over["speed"])
                r = bs.resolve(world, [f], b, rng)
                wins += r["outcome"] in ("win", "win_with_wound")
            res[skip] = wins / n
        print(f"  {name:24s} A {res[0] * 100:5.1f}%   B {res[1] * 100:5.1f}%")

    print("\nнавык «Из засады» (+15% обнаружения) на 3 уровне:")
    a = curve(0, skills=["skill_ambusher"], levels=(3,))
    b = curve(1, skills=["skill_ambusher"], levels=(3,))
    print(f"  A без навыка {curve(0, levels=(3,))[0] * 100:5.1f}%  -> с навыком {a[0] * 100:5.1f}%")
    print(f"  B без навыка {curve(1, levels=(3,))[0] * 100:5.1f}%  -> с навыком {b[0] * 100:5.1f}%")
