using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Досье зоны (ui.md §4.7): полноэкранный оверлей, не фаза суток.
    /// Динамическое сверху (настроение, слухи), статика снизу (лор, находки, среда).
    /// </summary>
    public sealed class DossierScreen
    {
        private readonly VisualElement _panel;
        private readonly VisualElement _art;
        private readonly VisualElement _text;
        private readonly Button _gatherButton;

        private Tables _tables;
        private World _world;
        private string _zoneId;

        public event Action Closed;
        public event Action<string> GatherRequested;

        public DossierScreen(VisualElement root)
        {
            _panel = root.Q<VisualElement>("dossier");
            _art = root.Q<VisualElement>("dossier-art");
            _text = root.Q<VisualElement>("dossier-text");
            _gatherButton = root.Q<Button>("dossier-gather");

            root.Q<Button>("dossier-back").clicked += Close;
            _gatherButton.clicked += () =>
            {
                var zoneId = _zoneId;
                Close();
                if (zoneId != null) GatherRequested?.Invoke(zoneId);
            };
        }

        public bool IsOpen => _panel != null && !_panel.ClassListContains("hidden");

        public void Open(string zoneId, Tables tables, World world, bool canGather)
        {
            _zoneId = zoneId;
            _tables = tables;
            _world = world;

            _panel.RemoveFromClassList("hidden");
            _gatherButton.SetEnabled(canGather);
            _gatherButton.text = canGather ? "К СБОРКЕ ЭТОЙ ЗОНЫ" : "К СБОРКЕ — ТОЛЬКО УТРОМ";

            Build();
        }

        public void Close()
        {
            _panel.AddToClassList("hidden");
            Closed?.Invoke();
        }

        private void Build()
        {
            var zone = _tables.Zones.Get(_zoneId);
            var state = _world.Zones[_zoneId];

            _art.Clear();
            foreach (var old in _art.GetClasses())
            {
                if (old.StartsWith("zone-art-")) _art.RemoveFromClassList(old);
            }
            _art.AddToClassList("zone-art-" + _zoneId);

            _text.Clear();
            _text.Add(Text("zone-title", zone.Name));
            _text.Add(Text("zone-meta",
                zone.Depth + " · дорога " + zone.TravelHours.ToString("0.#") + " ч · сейчас: "
                + ZoneKnowledge.Mood(_world, _zoneId)));
            _text.Add(Text("zone-meta",
                "соседи: " + string.Join(", ", NeighbourNames(zone))));

            if (!state.Scouted)
            {
                _text.Add(Text("zone-text", "Сюда ещё не ходили. Известны только дорога и соседи — "
                    + "пока кто-нибудь не вернётся, о зоне нечего рассказать."));
                return;
            }

            _text.Add(Text("zone-text", Lore("look")));

            _text.Add(Text("zone-section-title", "КТО ВОДИТСЯ"));
            _text.Add(Text("zone-text", Lore("beasts")));

            _text.Add(Text("zone-section-title", "ЧТО НАЙТИ"));
            _text.Add(Text("zone-text", Lore("finds")));

            _text.Add(Text("zone-section-title", "СРЕДА"));
            _text.Add(Text("zone-text", EnvRule(zone.EnvRule)));

            _text.Add(Text("zone-section-title", "ОСОБОЕ"));
            _text.Add(Text("zone-text", Lore("special")));

            if (state.Crystals > 0)
            {
                _text.Add(Text("zone-text",
                    "На земле что-то блестит. Унесёшь — зона обеднеет, и твари пойдут ближе к деревне."));
            }

            _text.Add(Text("zone-section-title", "СЛУХИ"));
            if (state.Sightings.Count == 0)
            {
                _text.Add(Text("zone-text", "Пока ничего не рассказывали."));
            }
            else
            {
                var from = Math.Max(0, state.Sightings.Count - 4);
                for (var i = from; i < state.Sightings.Count; i++)
                {
                    var sighting = state.Sightings[i];
                    _text.Add(Text("zone-text", "день " + sighting.Day + " — " + sighting.Text));
                }
            }
        }

        private string EnvRule(string rule)
        {
            switch (rule)
            {
                case "heat": return "Жарко и душно: силы тают быстрее, вода обжигает.";
                case "mud": return "Вязко: груз тянет, обратный путь дольше.";
                case "thicket": return "Тесно: идти по одному, встречи вплотную.";
                default: return "Место как место — ничего особенного.";
            }
        }

        private IEnumerable<string> NeighbourNames(Zone zone)
        {
            foreach (var id in zone.Neighbours) yield return _tables.Zones.Get(id).Name;
        }

        private string Lore(string field)
        {
            return _tables.ZoneLore.TryGet(_zoneId + "|" + field, out var row) ? row.Text : "—";
        }

        private static Label Text(string className, string value)
        {
            return Ui.Text(className, value);
        }
    }
}
