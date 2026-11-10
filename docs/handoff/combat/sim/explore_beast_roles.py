"""Две роли твари через обнаружение: засадник и тяжеловес.

Проверяем, что `speed` и `armor` дают РАЗНЫХ тварей, а не два способа быть страшной:
  - засадник: высокая speed → часто бьёт первым; броня может быть низкой;
  - тяжеловес: низкая speed → его легко застать, но броня съедает обычный удар,
    а удар из засады идёт мимо брони — значит из засады его и надо брать.
"""

import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import battle_sim as bs  # noqa: E402


def trial(level, beast_over, skills=(), n=4000, seed=2026):
    world = bs.World()
    world.add_probe("q", level)
    buckets = {"people": [0, 0], "beast": [0, 0], "none": [0, 0]}
    for i in range(n):
        rng = random.Random(seed + i)
        f = bs.Fighter(world, "q", {"level": level, "skills": list(skills)}, rng=rng)
        b = bs.Beast(world, "beast_ash", beast_over)
        res = bs.resolve(world, [f], b, rng)
        key = "people" if res["ambush_people"] else ("beast" if res["ambush"] else "none")
        buckets[key][0] += 1
        buckets[key][1] += res["outcome"] in ("win", "win_with_wound")
    return buckets


def show(label, b):
    total = sum(v[0] for v in b.values())
    parts = []
    for key, name in (("people", "группа"), ("beast", "тварь"), ("none", "никто")):
        cnt, wins = b[key]
        parts.append(f"{name} {cnt / total * 100:4.1f}% → {wins / max(1, cnt) * 100:5.1f}%")
    overall = sum(v[1] for v in b.values()) / total * 100
    print(f"{label:26s} " + " | ".join(parts) + f" | итог {overall:5.1f}%")


if __name__ == "__main__":
    base = {"hp": 30, "attack_min": 9, "attack_max": 15}
    print("охотник 3 ур., дубина 6..18, стёганка 3, закалка 0.1\n")
    print("тварь: одна сила удара/HP, меняется только скорость и броня\n")
    for speed, armor, name in (
        (4, 0, "быстрый, без брони"),
        (4, 9, "быстрый, броня 9"),
        (1, 0, "медленный, без брони"),
        (1, 9, "медленный, броня 9"),
        (1, 12, "медленный, броня 12"),
    ):
        over = dict(base, armor=armor)
        world = bs.World()
        world.add_probe("q", 3)
        b = bs.Beast(world, "beast_ash", over)
        b.speed = float(speed)
        # speed читается из Beast.__init__ из строки, поэтому задаём напрямую
        buckets = {"people": [0, 0], "beast": [0, 0], "none": [0, 0]}
        n = 4000
        for i in range(n):
            rng = random.Random(2026 + i)
            f = bs.Fighter(world, "q", {"level": 3}, rng=rng)
            bb = bs.Beast(world, "beast_ash", over)
            bb.speed = float(speed)
            res = bs.resolve(world, [f], bb, rng)
            key = "people" if res["ambush_people"] else ("beast" if res["ambush"] else "none")
            buckets[key][0] += 1
            buckets[key][1] += res["outcome"] in ("win", "win_with_wound")
        show(f"spd {speed} arm {armor:2d} {name[:12]}", buckets)

    print("\nтот же медленный тяжеловес (броня 12), но охотник берёт «Из засады»:")
    world = bs.World()
    world.add_probe("q", 3)
    for skills, label in (([], "без навыка"), (["skill_ambusher"], "Из засады +15%")):
        buckets = {"people": [0, 0], "beast": [0, 0], "none": [0, 0]}
        n = 4000
        for i in range(n):
            rng = random.Random(2026 + i)
            f = bs.Fighter(world, "q", {"level": 3, "skills": skills}, rng=rng)
            bb = bs.Beast(world, "beast_ash", dict(base, armor=12))
            bb.speed = 1.0
            res = bs.resolve(world, [f], bb, rng)
            key = "people" if res["ambush_people"] else ("beast" if res["ambush"] else "none")
            buckets[key][0] += 1
            buckets[key][1] += res["outcome"] in ("win", "win_with_wound")
        show(label, buckets)
