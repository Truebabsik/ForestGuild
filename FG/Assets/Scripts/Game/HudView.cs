using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>Верхняя строка HUD (ui.md §8): день, фаза, частокол, до волны, склад, здоровы.</summary>
    public sealed class HudView
    {
        private readonly Label _label;
        private readonly Tables _tables;
        private readonly World _world;

        public HudView(Label label, Tables tables, World world)
        {
            _label = label;
            _tables = tables;
            _world = world;
        }

        public void Update(Phase phase)
        {
            var waveIn = _world.NextWaveDay - _world.Day;
            if (waveIn < 0) waveIn = 0;

            var shownDay = phase == Phase.Night ? _world.Day - 1 : _world.Day;

            _label.text = "ДЕНЬ " + shownDay + " · " + PhaseName(phase)
                + "    |    частокол " + _world.PalisadeHp + "/" + _world.PalisadeMax
                + "    |    до волны: " + waveIn + " дн."
                + "    |    " + StoreLine()
                + "    |    здоровы " + HealthyCount() + "/" + _tables.Roster.All.Count;
        }

        /// До трёх позиций по количеству, короткими именами из items.csv.
        private string StoreLine()
        {
            var entries = new List<KeyValuePair<string, int>>();
            foreach (var pair in _world.Store)
            {
                if (pair.Value > 0) entries.Add(pair);
            }

            if (entries.Count == 0) return "склад пуст";

            entries.Sort((a, b) => b.Value.CompareTo(a.Value));

            var parts = new List<string>();
            for (var i = 0; i < entries.Count && i < 3; i++)
            {
                var key = entries[i].Key;
                var label = key == Works.ForageStoreKey
                    ? "корм"
                    : _tables.Items.TryGet(key, out var item)
                        ? (string.IsNullOrEmpty(item.Short) ? item.Name : item.Short)
                        : key;

                parts.Add(label + " " + entries[i].Value);
            }

            if (entries.Count > 3) parts.Add("ещё " + (entries.Count - 3));

            return "склад: " + string.Join(" · ", parts);
        }

        private int HealthyCount()
        {
            var count = 0;
            foreach (var person in _tables.Roster.All)
            {
                if (_world.ConditionOf(person.Id) == PersonCondition.Healthy) count++;
            }

            return count;
        }

        private static string PhaseName(Phase phase)
        {
            switch (phase)
            {
                case Phase.Day: return "ДЕНЬ";
                case Phase.Evening: return "ВЕЧЕР";
                case Phase.Night: return "НОЧЬ";
                default: return "УТРО";
            }
        }
    }
}
