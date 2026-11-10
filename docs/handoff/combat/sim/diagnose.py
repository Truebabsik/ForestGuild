"""Диагностика кривой: что реально обрывает бой у h1 и h2.

Не балансер — инструмент, чтобы не гадать, почему h2 = 26% при расчётных 50%.
Печатает распределения по всем N боям: сколько человек наносит ударов, на каком раунде
бой кончается. Раунд считается только свой у каждого боя — иначе счётчики складываются.
"""

import collections
import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from battle_sim import Beast, Fighter, World, resolve  # noqa: E402


def run(beast_id="beast_ash", levels=(1, 2, 3), n=3000, seed=5):
    """Каждый бой считается через resolve() — тем же кодом, что и отчёт.

    Своя копия цикла боя здесь была ошибкой: она показывала другую игру
    (без засады и без того же порядка бросков), и расхождение с отчётом
    выглядело как баг симулятора. Инструмент обязан звать resolve.
    """
    world = World()
    for level in levels:
        world.add_probe(f"p{level}", level)
    row = world.beasts[beast_id]
    print(f"тварь {beast_id}: hp {row['hp']}, урон {row['attack_min']}..{row['attack_max']}, "
          f"броня {row['armor']}, скорость {row['speed']}")
    for level in levels:
        f0 = Fighter(world, f"p{level}", {"level": level}, rng=random.Random(0))
        print(f"\nуровень {level}: hp {f0.tough_max:.0f}, точность {f0.tuning_hit:.2f}, "
              f"удар {f0.weapon_min:.0f}..{f0.weapon_max:.0f} + "
              f"{(level - 1) * world.t['wound_per_level']:.0f}")
        hits = collections.Counter()
        rounds = collections.Counter()
        outcomes = collections.Counter()
        for i in range(n):
            rng = random.Random(seed + i)
            f = Fighter(world, f"p{level}", {"level": level}, rng=rng)
            b = Beast(world, beast_id)
            res = resolve(world, [f], b, rng)
            hits[res["hits"]["p" + str(level)]] += 1
            rounds[res["rounds"]] += 1
            outcomes[res["outcome"]] += 1
        print("  исходы: " + ", ".join(f"{k}:{v / n * 100:.0f}%" for k, v in outcomes.most_common()))
        print("  ударов человека за бой: " + ", ".join(
            f"{k}:{v / n * 100:.0f}%" for k, v in sorted(hits.items())))
        print("  бой кончился на раунде: " + ", ".join(
            f"{k}:{v / n * 100:.0f}%" for k, v in sorted(rounds.items())))


if __name__ == "__main__":
    run()
