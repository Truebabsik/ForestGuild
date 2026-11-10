"""Броня как вычитание на крупных числах: полосы пробития и роль оружия.

Идея владельца: оружие бьёт в диапазоне (min..max), броня твари вычитается.
Копьё (4..9) против брони 8 — большинство ударов в ноль (только минимум),
тяжёлый зверь требует топора или отряда.

Здесь:
  строка   — броня твари (0..12)
  столбцы  — оружие, урон = max(min_damage, uniform(min,max) − armor)
Плюс: сколько ударов нужно на HP твари и как это меняется по уровням охотника.
"""

import random

MIN_DAMAGE = 1.0
ROUNDS = 8
WEAPONS = {
    "кулаки": (1, 3),
    "нож": (2, 5),
    "дубина": (3, 7),
    "копьё": (4, 9),
    "топор": (6, 12),
}


def mean_hit(wmin, wmax, armor, n=20000, seed=5):
    rng = random.Random(seed)
    return sum(max(MIN_DAMAGE, rng.uniform(wmin, wmax) - armor) for _ in range(n)) / n


def kill_prob(wmin, wmax, armor, n=20000, seed=5):
    """Доля ударов, которые реально доходят (урон > минимума)."""
    rng = random.Random(seed)
    real = sum(1 for _ in range(n) if max(MIN_DAMAGE, rng.uniform(wmin, wmax) - armor) > MIN_DAMAGE)
    return real / n


print("== Средний урон удара по броне (min_damage = 1) ==")
print("| броня | " + " | ".join(WEAPONS) + " |")
print("|---|" + "---|" * len(WEAPONS))
for armor in range(0, 13, 2):
    row = [mean_hit(a, b, armor) for a, b in WEAPONS.values()]
    print(f"| {armor} | " + " | ".join(f"{v:5.2f}" for v in row) + " |")

print("\n== Доля «настоящих» ударов (урон > 1) ==")
print("| броня | " + " | ".join(WEAPONS) + " |")
print("|---|" + "---|" * len(WEAPONS))
for armor in range(0, 13, 2):
    row = [kill_prob(a, b, armor) for a, b in WEAPONS.values()]
    print(f"| {armor} | " + " | ".join(f"{v * 100:5.1f}%" for v in row) + " |")

print("\n== Ударов до убийства (HP твари / средний урон) ==")
print("| HP твари | броня | " + " | ".join(WEAPONS) + " |")
print("|---|---|" + "---|" * len(WEAPONS))
for hp_beast, armor in [(20, 0), (20, 4), (30, 4), (30, 8), (40, 8), (40, 12)]:
    row = [f"{hp_beast / mean_hit(a, b, armor):5.1f}" for a, b in WEAPONS.values()]
    print(f"| {hp_beast} | {armor} | " + " | ".join(row) + " |")
