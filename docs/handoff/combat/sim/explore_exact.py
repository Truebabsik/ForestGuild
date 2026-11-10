"""Точная разведка: можно ли в одной модели закрыть всю кривую владельца.

Модель: каждый раунд человек наносит `dmg_p` (детерминированно), тварь —
`dmg_b` с вероятностью `atk_b`; закалка гасит часть урона твари.
Ищем наборы (hp_p по уровням, dmg_p, dmg_b, atk_b), дающие
h1 ≤10%, h2≈50%, h3≈80%, h4 ≥90% при твари 1 уровня.
"""

import itertools
import random


def duel(hp_p, dmg_p, hp_b, dmg_b, atk_b, harden=0, rounds=8, rng=None):
    for _ in range(rounds):
        hp_b -= dmg_p
        if hp_b <= 0:
            return True
        if rng.random() < atk_b:
            hp_p -= max(1 - harden, 1) if harden else dmg_b
            if hp_p <= 0:
                return False
    return False


def win_rate(hp_p, dmg_p, hp_b, dmg_b, atk_b, harden=0, n=8000, seed=5):
    rng = random.Random(seed)
    return sum(duel(hp_p, dmg_p, hp_b, dmg_b, atk_b, harden, rng=rng) for _ in range(n)) / n


def search():
    best = []
    for hp_b in (3, 4, 5, 6):
        for dmg_b in (2, 3, 4):
            for atk_b in (0.3, 0.45, 0.6):
                for dmg_p in (1,):
                    for hp1, hp2, hp3 in itertools.product((2, 3, 4), (3, 4, 5), (4, 5, 6, 7)):
                        p1 = win_rate(hp1, dmg_p, hp_b, dmg_b, atk_b)
                        p2 = win_rate(hp2, dmg_p, hp_b, dmg_b, atk_b)
                        p3 = win_rate(hp3, dmg_p, hp_b, dmg_b, atk_b, harden=1)
                        if p1 <= 0.10 and 0.40 <= p2 <= 0.60 and 0.72 <= p3 <= 0.88:
                            best.append((abs(p2 - 0.5) + abs(p3 - 0.8), hp_b, dmg_b, atk_b,
                                         hp1, hp2, hp3, p1, p2, p3))
    best.sort()
    print("тварь hp/dmg/atk; чел hp 1/2/3; победы 1/2/3")
    for row in best[:15]:
        _, hb, db, ab, h1, h2, h3, p1, p2, p3 = row
        print(f"{hb}/{db}/{ab:.2f};{h1}/{h2}/{h3};{p1 * 100:5.1f}% {p2 * 100:5.1f}% {p3 * 100:5.1f}%")


if __name__ == "__main__":
    search()
