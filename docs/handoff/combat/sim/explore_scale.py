"""Крупный масштаб (×3): калибровка варианта 2 на новых числах.

Оружие, броня и HP твари — крупные (как решил владелец). Броня твари — вычитание,
и она задаёт ПОРОГ: оружие с максимумом ниже брони твари бесполезно (всё в min_damage).
Поэтому броня твари = полоса «нужно такое-то оружие», а не произвольное число.

Ищем:
  1. базу кривой 1×1 (h1 ≤ 10%, h2 45–55%, h3 75–85%, h4 ≥ 88%)
  2. положение «поясов»: какая тварь на какой уровень охотника
"""

import itertools
import random

ROUNDS = 8
MIN_DAMAGE = 1.0
BASE_HP, HP_PER_LEVEL = 18.0, 9.0
HARDEN_ARMOR, HARDEN_FROM = 0.1, 3
WEAPON_CLUB = (6, 18)        # дубина ×3 (было 2..6)
BEAST_ATK = (9, 15)          # урон твари ×3 (было 3..5)


def hardening(lvl):
    return 0.0 if lvl < HARDEN_FROM else HARDEN_ARMOR * min(2, lvl - HARDEN_FROM + 1)


def fight(rng, lvl, hp_b, armor_b, hit0, hit_step, wound_step, weapon=WEAPON_CLUB):
    wmin, wmax = weapon
    hp_p = BASE_HP + HP_PER_LEVEL * lvl
    hit_p = min(0.95, hit0 + hit_step * (lvl - 1))
    bonus = (lvl - 1) * wound_step
    for _ in range(ROUNDS):
        if rng.random() < hit_p:
            hp_b -= max(MIN_DAMAGE, rng.uniform(wmin, wmax) + bonus - armor_b)
            if hp_b <= 0:
                return True
        hp_p -= max(MIN_DAMAGE, rng.uniform(*BEAST_ATK) - 3.0) * (1 - hardening(lvl))
        if hp_p <= 0:
            return False
    return False


def curve(hp_b, armor_b, hit0, hit_step, wound_step, n=1500, seed=53):
    rng = random.Random(seed)
    return [sum(fight(rng, l, hp_b, armor_b, hit0, hit_step, wound_step)
                for _ in range(n)) / n for l in range(1, 6)]


def score(r):
    want = [0.05, 0.50, 0.80, 0.94, 0.97]
    return sum(abs(a - b) for a, b in zip(r, want))


if __name__ == "__main__":
    rows = []
    for hp_b, armor_b, hit0, hit_step, wound_step in itertools.product(
            [24, 30, 36, 42], [0, 3, 6], [0.30, 0.35, 0.40],
            [0.05, 0.07, 0.09], [3.0, 4.5]):
        r = curve(hp_b, armor_b, hit0, hit_step, wound_step)
        ok = r[0] <= 0.10 and 0.45 <= r[1] <= 0.55 and 0.75 <= r[2] <= 0.85 and r[3] >= 0.88
        rows.append((score(r), ok, (hp_b, armor_b, hit0, hit_step, wound_step), r))
    rows.sort(key=lambda x: x[0])
    print("попаданий:", sum(1 for x in rows if x[1]))
    for s, ok, p, r in rows[:12]:
        print(f"{'ЦЕЛЬ' if ok else '    '} hpB={p[0]} armB={p[1]} hit0={p[2]} hitStep={p[3]}"
              f" woundStep={p[4]} -> " + " ".join(f"{x * 100:5.1f}%" for x in r)
              + f"  откл {s:.3f}")
