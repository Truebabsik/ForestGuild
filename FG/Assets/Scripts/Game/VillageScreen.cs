using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Деревня: работы руками (ui.md §4.6) в отдельном окне по клику на узел деревни.
    /// Три стока рабочей силы: починка частокола, постели, заготовка.
    /// </summary>
    public sealed class VillageScreen
    {
        private readonly VisualElement _panel;
        private readonly Label _title;
        private readonly Label _palisade;
        private readonly VisualElement _works;
        private readonly VisualElement _people;

        private Tables _tables;
        private World _world;
        private DayPlan _plan;

        public event System.Action Closed;

        public VillageScreen(VisualElement root)
        {
            _panel = root.Q<VisualElement>("village");
            _title = root.Q<Label>("village-title");
            _palisade = root.Q<Label>("village-palisade");
            _works = root.Q<VisualElement>("village-works");
            _people = root.Q<VisualElement>("village-people");

            root.Q<Button>("village-close").clicked += Close;
        }

        public bool IsOpen => _panel != null && !_panel.ClassListContains("hidden");

        public void Open(Tables tables, World world, DayPlan plan)
        {
            _tables = tables;
            _world = world;
            _plan = plan;

            _panel.RemoveFromClassList("hidden");
            Rebuild();
        }

        public void Close()
        {
            _panel.AddToClassList("hidden");
            Closed?.Invoke();
        }

        private void Rebuild()
        {
            _title.text = "ДЕРЕВНЯ · работы на день " + _world.Day;
            _palisade.text = "Частокол: " + _world.PalisadeHp + "/" + _world.PalisadeMax
                + " · починка даёт +" + _tables.Tuning.GetInt("repair_per_worker")
                + " за человека · стену чинят руками, не добром из мешка";

            BuildWorks();
            BuildPeople();
        }

        private void BuildWorks()
        {
            _works.Clear();

            AddWork("починка частокола", _plan.VillageWorkers, FirstFree);
            AddWork("постели", _plan.Beds, FirstWounded, id => _plan.TryAddBed(_tables, _world, id));
            AddWork("заготовка", _plan.Foragers, FirstFree);
        }

        private void AddWork(string title, List<string> pool, System.Func<string> pick,
            System.Func<string, bool> add = null)
        {
            var row = new VisualElement();
            row.AddToClassList("work-row");

            var label = new Label(title);
            label.AddToClassList("work-label");
            row.Add(label);

            var count = new Label(pool.Count.ToString());
            count.AddToClassList("work-count");
            row.Add(count);

            var minus = new Button(() =>
            {
                if (pool.Count > 0) pool.RemoveAt(pool.Count - 1);
                Rebuild();
            });
            minus.text = "−";
            minus.AddToClassList("work-button");
            row.Add(minus);

            var plus = new Button(() =>
            {
                var personId = pick();
                if (personId == null) return;

                if (add != null)
                {
                    if (!add(personId)) return;
                }
                else
                {
                    pool.Add(personId);
                }

                Rebuild();
            });
            plus.text = "+";
            plus.AddToClassList("work-button");
            row.Add(plus);

            var names = new Label(Names(pool));
            names.AddToClassList("bed-note");
            row.Add(names);

            _works.Add(row);
        }

        private void BuildPeople()
        {
            _people.Clear();

            foreach (var person in _tables.Roster.All)
            {
                var where = Assignment.Where(_world, _plan, null, person.Id);
                var line = new Label(person.Name + " — " + (where ?? "свободен"));
                line.AddToClassList(where == null ? "person-note" : "bed-note");
                _people.Add(line);
            }
        }

        private string Names(List<string> pool)
        {
            if (pool.Count == 0) return "";

            var names = new List<string>();
            foreach (var personId in pool) names.Add(_tables.Roster.Get(personId).Name);
            return "· " + string.Join(", ", names);
        }

        private string FirstFree()
        {
            foreach (var person in _tables.Roster.All)
            {
                if (Assignment.IsFree(_world, _plan, null, person.Id)) return person.Id;
            }

            return null;
        }

        private string FirstWounded()
        {
            foreach (var person in _tables.Roster.All)
            {
                if (_world.ConditionOf(person.Id) != PersonCondition.Wounded) continue;
                if (_plan.Beds.Contains(person.Id)) continue;
                return person.Id;
            }

            return null;
        }
    }
}
