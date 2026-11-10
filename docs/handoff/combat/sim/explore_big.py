"""Вариант 2 на крупных числах: броня — вычитание в диапазоне 0..12.

Модель (проверяем форму, не финальные числа):
  урон по твари = max(min_damage, uniform(wmin,wmax) + wound(lvl) − armor твари)
  урон по человеку = max(min_damage, uniform(bmin,bmax) − armor человека) × (1 − закалка)
  раунд: люди бьют (каждый — бросок точности), потом тварь бьёт одного.

Ищем набор, где один охотник со стартовым снаряжением даёт цель владельца:
  h1 ≤ 10%, h2 45–55%, h3 75–85%, h4 ≥ 88%.
Плюс проверяем: тварь с бронёй 8 должна требовать другого оружия.
"""

import itertools
import random

BASE_HP, HP_PER_LEVEL = 6.0, 3.0
ROUNDS = 8
MIN_DAMAGE = 1.0
HARDEN_ARMOR, HARDEN_FROM = 0.1, 3


def hardening(lvl):
    return 0.0 if lvl < HARDEN_FROM else HARDEN_ARMOR * min(2, lvl - HARDEN_FROM + 1)


def fight(rng, lvl, weapon, beast, hit0, hit_step, wound_step):
    wmin, wmax = weapon
    bmin, bmax, barmor, hp_b = beast
    hp_p = BASE_HP + HP_PER_LEVEL * lvl
    hit_p = min(0.95, hit0 + hit_step * (lvl - 1))
    bonus = (lvl - 1) * wound_step
    for _ in range(ROUNDS):
        if rng.random() < hit_p:
            hp_b -= max(MIN_DAMAGE, rng.uniform(wmin, wmax) + bonus - barmor)
            if hp_b <= 0:
                return True
        hp_p -= max(MIN_DAMAGE, rng.uniform(bmin, bmax) - 1.0) * (1 - hardening(lvl))
        if hp_p <= 0:
            return False
    return False


def curve(weapon, beast, hit0, hit_step, wound_step, n=1500, seed=53):
    rng = random.Random(seed)
    return [sum(fight(rng, l, weapon, beast, hit0, hit_step, wound_step)
                for _ in range(n)) / n for l in range(1, 6)]


def score(r):
    want = [0.05, 0.50, 0.80, 0.94, 0.97]
    return sum(abs(a - b) for a, b in zip(r, want))


if __name__ == "__main__":
    WEAPON = (2, 6)            # дубина, стартовое оружие охотника
    BEAST_ATK = (3, 5)         # удар твари — тоже диапазон, броня человека вычитается
    rows = []
    for beast_hp, beast_armor, hit0, hit_step, wound_step in itertools.product(
            [6, 8, 10, 12], [0, 1, 2], [0.20, 0.25, 0.30, 0.35],
            [0.06, 0.08, 0.10], [0.5, 1.0, 1.5]):
        beast = (*BEAST_ATK, beast_armor, beast_hp)
        r = curve(WEAPON, beast, hit0, hit_step, wound_step)
        ok = r[0] <= 0.10 and 0.45 <= r[1] <= 0.55 and 0.75 <= r[2] <= 0.85 and r[3] >= 0.88
        rows.append((score(r), ok, (beast_hp, beast_armor, hit0, hit_step, wound_step), r))
    rows.sort(key=lambda x: x[0])
    print("попаданий:", sum(1 for x in rows if x[1]))
    for s, ok, p, r in rows[:12]:
        print(f"{'ЦЕЛЬ' if ok else '    '} hpB={p[0]} armB={p[1]} hit0={p[2]} hitStep={p[3]}"
              f" woundStep={p[4]} -> " + " ".join(f"{x * 100:5.1f}%" for x in r)
              + f"  откл {s:.3f}")
