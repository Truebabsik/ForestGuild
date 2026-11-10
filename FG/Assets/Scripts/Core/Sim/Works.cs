using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    /// <summary>
    /// Работы в деревне (ui.md §4.6): починка частокола, постели, заготовка.
    /// Все они — сток рабочей силы, а не ресурсов.
    /// </summary>
    public static class Works
    {
        public const string ForageStoreKey = "forage";

        public static int RepairPalisade(Tables tables, World world, int workers)
        {
            if (workers <= 0) return 0;

            var perWorker = tables.Tuning.GetInt("repair_per_worker");
            var before = world.PalisadeHp;
            world.PalisadeHp = Math.Min(world.PalisadeMax, world.PalisadeHp + workers * perWorker);
            return world.PalisadeHp - before;
        }

        public static int Forage(Tables tables, World world, int workers)
        {
            if (workers <= 0) return 0;

            var perWorker = tables.Tuning.GetInt("forage_per_worker");
            world.Store.TryGetValue(ForageStoreKey, out var current);
            world.Store[ForageStoreKey] = current + workers * perWorker;
            return workers;
        }

        /// Положить на постель: к утру человек восстанавливается сильнее обычного сна.
        public static bool PutToBed(World world, string personId)
        {
            if (!world.People.TryGetValue(personId, out var state)) return false;
            if (state.Condition == PersonCondition.Dead) return false;

            state.InBedDays = 1;
            return true;
        }

        /// Утро: ночь уже прошла, здесь только гасим счётчик постели.
        public static List<string> Recover(World world)
        {
            var healed = new List<string>();

            foreach (var pair in world.People)
            {
                var state = pair.Value;
                if (state.Condition == PersonCondition.Dead) continue;
                if (state.InBedDays <= 0) continue;

                state.InBedDays--;
                if (state.InBedDays == 0 && state.Condition == PersonCondition.Wounded && state.Hp > 0)
                {
                    state.Condition = PersonCondition.Healthy;
                    healed.Add(pair.Key);
                }
            }

            return healed;
        }

        public static void Wound(World world, string personId)
        {
            if (!world.People.TryGetValue(personId, out var state)) return;
            if (state.Condition == PersonCondition.Dead) return;

            state.Condition = PersonCondition.Wounded;
            state.InBedDays = 0;
        }

        public static void Kill(World world, string personId)
        {
            if (!world.People.TryGetValue(personId, out var state)) return;
            state.Condition = PersonCondition.Dead;
        }

    }
}
