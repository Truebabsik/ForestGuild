"""Собиратель в отряде: польза от уклонения против его вклада в бой.

Сравниваем отряды охотников с собирателем и без него. Собиратель 2 уровня:
урон режется вдвое (gatherer_combat_factor), точность как у охотника 2 ур.,
уклоняется от каждого удара с шансом gatherer_dodge (0.25).
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402

TRIALS = (
    ("один 2 ур.", ["hilda"]),
    ("один 2 ур. + сборщик", ["hilda", "leya"]),
    ("двое 2 ур.", ["hilda", "orn"]),
    ("двое 2 ур. + сборщик", ["hilda", "orn", "leya"]),
    ("Брунни 4 + Скади 3", ["brunni", "skadi"]),
    ("Брунни 4 + Скади 3 + сборщик", ["brunni", "skadi", "leya"]),
)


def run(members, count, dodge=0.25, n=4000, seed=2026, beast="beast_bogling"):
    world = bs.World()
    world.t["gatherer_dodge"] = dodge
    wins = wounded = lost = 0
    gatherer_out = dodges = 0
    for i in range(n):
        rng = random.Random(seed + i)
        fs = [bs.Fighter(world, p, rng=rng) for p in members]
        beasts = [bs.Beast(world, beast) for _ in range(count)]
        res = bs.resolve(world, fs, beasts, rng)
        wins += res["outcome"] in ("win", "win_with_wound")
        wounded += res["outcome"] == "win_with_wound"
        lost += res["outcome"] == "lost"
        if "leya" in members:
            gatherer_out += 1 if "leya" in res["wounded"] else 0
            dodges += fs[-1].dodged if members[-1] == "leya" else 0
    return wins / n, wounded / n, lost / n, gatherer_out / n, dodges / n


if __name__ == "__main__":
    for count in (1, 2):
        print(f"\n=== {count} болотник(а) ===")
        print(f"{'отряд':26s} {'победа':>7s} {'с раной':>8s} {'пор.':>6s}")
        for name, members in TRIALS:
            w, wo, lo, gout, dod = run(members, count)
            extra = ""
            if "leya" in members:
                extra = f"   сборщик выбит {gout * 100:4.1f}%, уклонов {dod:.2f}"
            print(f"{name:26s} {w * 100:6.1f}% {wo * 100:7.1f}% {lo * 100:5.1f}%{extra}")

    print("\nвклад уклонения (двое 2 ур. + сборщик, 2 болотника):")
    for dodge in (0.0, 0.25, 0.5):
        w, wo, lo, gout, dod = run(["hilda", "orn", "leya"], 2, dodge=dodge)
        print(f"  шанс уклонения {dodge * 100:4.0f}%: победа {w * 100:5.1f}%, "
              f"с раной {wo * 100:4.1f}%, выбит {gout * 100:4.1f}%")
