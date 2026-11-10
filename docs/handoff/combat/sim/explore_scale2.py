"""Крупный масштаб: пояса тварей и роль подготовки (оружие/закалка/уровень).

Базовый набор из explore_scale: дубина (6..18), точность 0.35 + 0.05/ур.,
урон +4.5/ур., урон твари (9..15) − 3.

Проверяем «не идти на следующий уровень без подготовки»:
  тот же охотник, но с топором (18..36) или с копьём (12..27),
  против твари с бронёй 3/6/9/12.
"""

import random

ROUNDS = 8
MIN_DAMAGE = 1.0
BASE_HP, HP_PER_LEVEL = 18.0, 9.0
HARDEN_ARMOR, HARDEN_FROM = 0.1, 3
BEAST_ATK = (9, 15)
HIT0, HIT_STEP = 0.35, 0.05
WOUND_STEP = 4.5

WEAPONS = {
    "кулаки": (3, 9),
    "нож": (6, 15),
    "дубина": (6, 18),
    "копьё": (12, 27),
    "топор": (18, 36),
}


def hardening(lvl):
    return 0.0 if lvl < HARDEN_FROM else HARDEN_ARMOR * min(2, lvl - HARDEN_FROM + 1)


def fight(rng, lvl, hp_b, armor_b, weapon, harden=None):
    wmin, wmax = weapon
    hp_p = BASE_HP + HP_PER_LEVEL * lvl
    hit_p = min(0.95, HIT0 + HIT_STEP * (lvl - 1))
    bonus = (lvl - 1) * WOUND_STEP
    hard = hardening(lvl) if harden is None else harden
    for _ in range(ROUNDS):
        if rng.random() < hit_p:
            hp_b -= max(MIN_DAMAGE, rng.uniform(wmin, wmax) + bonus - armor_b)
            if hp_b <= 0:
                return True
        hp_p -= max(MIN_DAMAGE, rng.uniform(*BEAST_ATK) - 3.0) * (1 - hard)
        if hp_p <= 0:
            return False
    return False


def rate(lvl, hp_b, armor_b, weapon, harden=None, n=3000, seed=31):
    rng = random.Random(seed)
    return sum(fight(rng, lvl, hp_b, armor_b, weapon, harden) for _ in range(n)) / n


print("== Кривая 1×1, дубина, тварь 1 ур. (hp 30, броня 0) ==")
r = [rate(l, 30, 0, WEAPONS["дубина"]) for l in range(1, 6)]
print("  " + " ".join(f"h{l}={r[l - 1] * 100:.1f}%" for l in range(1, 6)))

print("\n== Пояса: тварь с бронёй, боец своего уровня со стартовым (дубина) ==")
print("| броня | hp 24 | hp 30 | hp 36 | hp 45 |")
print("|---|---|---|---|---|")
for armor_b in (0, 3, 6, 9, 12):
    row = [rate(1, hp, armor_b, WEAPONS["дубина"]) for hp in (24, 30, 36, 45)]
    print(f"| {armor_b} | " + " | ".join(f"{v * 100:5.1f}%" for v in row) + " |")

print("\n== Тварь 2-3 ур. (hp 36, броня 6): что решает — оружие, уровень, закалка ==")
print("| боец | кулаки | нож | дубина | копьё | топор |")
print("|---|---|---|---|---|---|")
for lvl in (1, 2, 3, 4):
    row = [rate(lvl, 36, 6, w) for w in WEAPONS.values()]
    print(f"| {lvl} ур. | " + " | ".join(f"{v * 100:5.1f}%" for v in row) + " |")

print("\n== Закалка: тварь 2 ур. 1→3 лвл, дубина ==")
for lvl, hard in ((1, 0.0), (1, 0.1), (1, 0.2), (2, 0.0), (2, 0.1)):
    print(f"  {lvl} ур., закалка {hard}: {rate(lvl, 36, 6, WEAPONS['дубина'], harden=hard) * 100:5.1f}%")
