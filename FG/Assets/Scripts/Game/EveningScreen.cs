using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Вечер (ui.md §6): итог дня, прирост склада с дельтой, постели для раненых.
    /// Добыча уже в складе — вечер её показывает и распределяет постели.
    /// </summary>
    public sealed class EveningScreen
    {
        private readonly VisualElement _panel;
        private readonly Label _title;
        private readonly VisualElement _lines;
        private readonly VisualElement _wounded;

        private Tables _tables;
        private World _world;
        private DayPlan _plan;
        private DayReport _report;

        public event Action Slept;

        public EveningScreen(VisualElement root)
        {
            _panel = root.Q<VisualElement>("evening");
            _title = root.Q<Label>("evening-title");
            _lines = root.Q<VisualElement>("evening-lines");
            _wounded = root.Q<VisualElement>("evening-wounded");

            root.Q<Button>("evening-sleep").clicked += () =>
            {
                _panel.AddToClassList("hidden");
                Slept?.Invoke();
            };
        }

        public bool IsOpen => _panel != null && !_panel.ClassListContains("hidden");

        public void Open(DayReport report, DayPlan plan, World world, Tables tables)
        {
            _report = report;
            _plan = plan;
            _world = world;
            _tables = tables;

            _panel.RemoveFromClassList("hidden");
            _title.text = "ДЕНЬ " + world.Day + " · ВЕЧЕР · разбор";
            BuildLines();
            BuildBeds();
        }

        public void Close()
        {
            _panel.AddToClassList("hidden");
        }

        private void BuildLines()
        {
            _lines.Clear();

            _lines.Add(Line("Отрядов в лесу: " + (_report?.Parties.Count ?? 0)));

            if (_report != null && _report.Loot.Count > 0)
            {
                foreach (var pair in _report.Loot)
                {
                    var item = _tables.Items.Get(pair.Key);
                    _world.Store.TryGetValue(pair.Key, out var stored);
                    _lines.Add(Line("Склад: " + item.Name + " +" + pair.Value
                        + "  (было " + (stored - pair.Value) + ", стало " + stored + ")"));
                }
            }
            else
            {
                _lines.Add(Line("Добычи нет — день вернул только дорогу"));
            }

            if (_world.Store.TryGetValue(Works.ForageStoreKey, out var forage) && forage > 0)
            {
                _lines.Add(Line("Подножный корм в складе: " + forage));
            }

            var wounded = WoundedList();
            if (wounded.Count == 0)
            {
                _lines.Add(Line("Все вернулись целыми"));
            }
            else
            {
                // Имена и слово о состоянии, а не id: «Раненые: Хильда — на ногах не стоит».
                var parts = new List<string>();
                foreach (var personId in wounded)
                    parts.Add(_tables.Roster.Get(personId).Name + " — " + Ui.HpWord(_tables, _world, personId));

                _lines.Add(Line("Раненые: " + string.Join(", ", parts)));
                if (LeftWithoutBed() > 0)
                {
                    _lines.Add(Line("Беспостельные не встанут утром. Постелей всего "
                        + _tables.Tuning.GetInt("beds_count") + "."));
                }
            }

            _lines.Add(Line("Частокол: " + _world.PalisadeHp + "/" + _world.PalisadeMax
                + " · стену чинят руками, не добром из мешка"));
        }

        private void BuildBeds()
        {
            _wounded.Clear();

            var wounded = WoundedList();
            if (wounded.Count == 0)
            {
                var none = new Label("нести некого");
                none.AddToClassList("bed-note");
                _wounded.Add(none);
                return;
            }

            foreach (var personId in wounded)
            {
                var row = new VisualElement();
                row.AddToClassList("bed-row");

                var taken = _plan.Beds.Contains(personId);
                if (taken) row.AddToClassList("taken");

                var left = new VisualElement();
                left.AddToClassList("person-info");

                var name = new Label(_tables.Roster.Get(personId).Name);
                name.AddToClassList("bed-name");
                left.Add(name);

                var note = new Label(taken ? "на постели — встанет утром" : "ждёт постели");
                note.AddToClassList("bed-note");
                left.Add(note);
                row.Add(left);

                var id = personId;
                var button = new Button(() =>
                {
                    if (_plan.Beds.Contains(id))
                    {
                        _plan.RemoveBed(id);
                    }
                    else
                    {
                        if (!_plan.TryAddBed(_tables, _world, id)) return;
                    }

                    BuildLines();
                    BuildBeds();
                });
                button.text = taken ? "снять" : "на постель";
                button.AddToClassList("bed-button");
                row.Add(button);

                _wounded.Add(row);
            }
        }

        private List<string> WoundedList()
        {
            var list = new List<string>();
            foreach (var person in _tables.Roster.All)
            {
                if (_world.ConditionOf(person.Id) == PersonCondition.Wounded) list.Add(person.Id);
            }

            return list;
        }

        private int LeftWithoutBed()
        {
            var left = 0;
            foreach (var personId in WoundedList())
            {
                if (!_plan.Beds.Contains(personId)) left++;
            }

            return left;
        }

        private static Label Line(string text)
        {
            return new Label("· " + text);
        }
    }
}
