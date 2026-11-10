"""Разведка ширины: hp твари и броня людей, при которых h2 попадает в 50%.

Модель точная (без RNG): охотник бьёт `per_hit`, тварь бьёт `attack` каждый раунд;
первая ступень — броня экипировки, закалка — доля урона.
"""


def outcome(level, hp_b, attack=3.0, beast_armor=1.0, human_armor=1.0, hardening=0.0,
            base_hp=6.0, hp_per_level=3.0, weapon=2.0, skill_share=0.5, brave=1.0):
    hp_p = base_hp + hp_per_level * level
    per_hit = weapon * 0.3 + brave + level * (1 + skill_share)
    dmg = max(0.5, per_hit - beast_armor)
    take = max(0.0, max(1.0, attack - human_armor) - hardening)
    rounds_p = hp_p / take if take else 99
    rounds_b = hp_b / dmg
    return rounds_b, rounds_p


def scan():
    print("hp_твари; h1; h2; h2 раундов(тварь/чел); h3")
    for hp_b in (12, 14, 16, 18, 20, 22, 24, 26):
        cells = []
        for lvl in (1, 2, 3):
            rp, rk = outcome(lvl, hp_b)
            cells.append(f"{rp / rk:.2f}" if lvl == 2 else f"{rp / rk:.2f}")
        print(f"{hp_b}; " + " | ".join(cells))


if __name__ == "__main__":
    scan()
