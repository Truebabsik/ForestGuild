using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Карточка зоны в правой панели (короткая выжимка) и заглушки «выберите зону» / «деревня».
    /// Полное досье живёт в `DossierScreen` (ui.md §4.7).
    /// </summary>
    public sealed class ZoneCardView
    {
        private readonly ScrollView _card;
        private readonly Tables _tables;
        private readonly World _world;

        public bool Debug;

        public ZoneCardView(ScrollView card, Tables tables, World world)
        {
            _card = card;
            _tables = tables;
            _world = world;
        }

        public void ShowHint()
        {
            _card.Clear();
            _card.Add(Text("zone-title", "Выберите зону"));
            _card.Add(Text("zone-text", "Клик по узлу на карте открывает досье: что там за место, "
                + "кто водится, что находили и что рассказывают."));
        }

        public void ShowZone(string zoneId)
        {
            var zone = _tables.Zones.Get(zoneId);
            var state = _world.Zones[zoneId];
            _card.Clear();

            var art = Art(zoneId);
            _card.Add(art);

            _card.Add(Text("zone-title", zone.Name));
            _card.Add(Text("zone-meta", zone.Depth + " · дорога " + zone.TravelHours.ToString("0.#")
                + " ч · сейчас: " + ZoneKnowledge.Mood(_world, zoneId)));

            if (!state.Scouted)
            {
                _card.Add(Text("zone-text", "Сюда ещё не ходили. Известны только дорога и соседи — "
                    + string.Join(", ", NeighbourNames(zone)) + "."));
                if (Debug) _card.Add(Text("zone-text", DebugLine(zoneId)));
                return;
            }

            _card.Add(Text("zone-text", Lore(zoneId, "look")));
            AddSection("КТО ВОДИТСЯ", Lore(zoneId, "beasts"));
            AddSection("ЧТО НАЙТИ", Lore(zoneId, "finds"));
            AddSection("ОСОБОЕ", Lore(zoneId, "special"));

            _card.Add(Text("zone-section-title", "СЛУХИ"));
            if (state.Sightings.Count == 0)
            {
                _card.Add(Text("zone-text", "Пока ничего не рассказывали."));
            }
            else
            {
                var from = System.Math.Max(0, state.Sightings.Count - 3);
                for (var i = from; i < state.Sightings.Count; i++)
                {
                    var sighting = state.Sightings[i];
                    _card.Add(Text("zone-text", "день " + sighting.Day + " — " + sighting.Text));
                }
            }

            if (Debug) _card.Add(Text("zone-text", DebugLine(zoneId)));
            _card.scrollOffset = UnityEngine.Vector2.zero;
        }

        public void ShowVillage()
        {
            _card.Clear();
            _card.Add(Art("verge_meadow"));
            _card.Add(Text("zone-title", "Деревня"));
            _card.Add(Text("zone-meta", "частокол " + _world.PalisadeHp + " из " + _world.PalisadeMax));
            _card.Add(Text("zone-text", "Пока стена держит, лес сюда не заходит. "
                + "Один человеко-день даёт два очка прочности."));
            _card.Add(Text("zone-text", "Жителей: " + _tables.Roster.All.Count));
        }

        private VisualElement Art(string zoneId)
        {
            var art = new VisualElement();
            art.AddToClassList("zone-art");
            art.AddToClassList("zone-art-" + zoneId);
            return art;
        }

        private void AddSection(string title, string body)
        {
            _card.Add(Text("zone-section-title", title));
            _card.Add(Text("zone-text", body));
        }

        private string Lore(string zoneId, string field)
        {
            return _tables.ZoneLore.TryGet(zoneId + "|" + field, out var row) ? row.Text : "—";
        }

        private IEnumerable<string> NeighbourNames(Zone zone)
        {
            foreach (var id in zone.Neighbours) yield return _tables.Zones.Get(id).Name;
        }

        private string DebugLine(string zoneId)
        {
            var state = _world.Zones[zoneId];
            var beasts = new List<string>();
            foreach (var pair in state.Beasts) beasts.Add(_tables.Beasts.Get(pair.Key).Name + " " + pair.Value);

            return "отладка: твари " + (beasts.Count == 0 ? "нет" : string.Join(", ", beasts))
                + " · кристаллы " + state.Crystals
                + " · корм " + state.Forage
                + " · опыт гнезда " + state.NestXp
                + " · гнездо " + (state.Nest.Exists
                    ? state.Nest.Beasts + "/" + _tables.Tuning.GetInt("nest_beasts_cap")
                    : "нет")
                + " · тревога " + state.AlertDays;
        }

        private static Label Text(string className, string value)
        {
            var label = new Label(value);
            label.AddToClassList(className);
            return label;
        }
    }
}
