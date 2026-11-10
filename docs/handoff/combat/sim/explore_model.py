"""Разведка боевой модели: какая формула вообще может дать кривую владельца.

Сравнивает «одна проверка за раунд» с «несколько проверок за раунд» (много бойцов
или многократные удары), чтобы понять, откуда в модели берётся ступенька
h1 → h2 → h3 и почему она узкая.
"""

import random


def fight(hp_p, atk_p, hp_b, atk_b, max_rounds=6, rolls=1, n=5000, seed=1):
    """Дуэль: человек бьёт `rolls` раз, тварь — один. Возвращает долю побед."""
    rng = random.Random(seed)
    wins = 0
    for _ in range(n):
        ph, bh = hp_p, hp_b
        for _ in range(max_rounds):
            for _ in range(rolls):
                if rng.random() < atk_p:
                    bh -= 1
            if bh <= 0:
                wins += 1
                break
            if rng.random() < atk_b:
                ph -= 1
            if ph <= 0:
                break
    return wins / n


def ladder():
    print("rolls;hp_p;hp_b;atk_p(1);atk_p(2);atk_p(3);atk_p(4)")
    for rolls in (1, 2, 3, 4):
        for hp_p in (2, 3, 4, 6):
            for hp_b in (3, 4, 6):
                row = []
                for a in (0.35, 0.45, 0.55, 0.65):
                    row.append(fight(hp_p, a, hp_b, 0.5, rolls=rolls))
                print(f"{rolls};{hp_p};{hp_b};" + ";".join(f"{x * 100:5.1f}%" for x in row))


def wide(n_lvl=5, n=4000, max_rounds=6, seed=3):
    """Лестница: каждая пара (человек, тварь) — отдельный бой, ищем широкую кривую."""
    rng = random.Random(seed)
    print("\nчеловек hp/atk × тварь hp/atk → доля побед")
    for hp_p, atk_p in [(2, 0.35), (2, 0.5), (3, 0.4), (3, 0.5), (4, 0.4), (4, 0.5), (6, 0.45)]:
        line = []
        for hp_b, atk_b in [(2, 0.4), (3, 0.4), (3, 0.5), (4, 0.5), (6, 0.5)]:
            wins = 0
            r = random.Random(seed)
            for _ in range(n):
                ph, bh = hp_p, hp_b
                for _ in range(max_rounds):
                    if r.random() < atk_p:
                        bh -= 1
                    if bh <= 0:
                        wins += 1
                        break
                    if r.random() < atk_b:
                        ph -= 1
                    if ph <= 0:
                        break
            line.append(f"{wins / n * 100:5.1f}%")
        print(f"чел {hp_p}hp/{atk_p} → " + " ".join(line))


if __name__ == "__main__":
    ladder()
    wide()
