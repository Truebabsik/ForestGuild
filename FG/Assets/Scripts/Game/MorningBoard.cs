using System;
using System.Collections.Generic;
using ForestGuild.Core.Sim;
using ForestGuild.Core.Data;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Доска приказов (ui.md §4.1): карточки отрядов-пресетов, работы в деревне, люди, предупреждения.
    /// Считает не здесь: цифры берутся из `Briefing`, работы — из `DayPlan`.
    /// </summary>
    public sealed class MorningBoard
    {
        private readonly Tables _tables;
        private readonly World _world;
        private readonly DayPlan _plan;
        private readonly VisualElement _squads;
        private readonly VisualElement _works;
        private readonly Label _warnings;

        public Squad Active;
        public event Action Changed;
        public event Action<Squad> EditRequested;

        public MorningBoard(Tables tables, World world, DayPlan plan,
            VisualElement squads, VisualElement works, Label warnings)
        {
            _tables = tables;
            _world = world;
            _plan = plan;
            _squads = squads;
            _works = works;
            _warnings = warnings;
        }

        public void Rebuild()
        {
            BuildSquads();
            BuildWorks();
            BuildWarnings();
        }

        // ---- отряды ----

        private void BuildSquads()
        {
            _squads.Clear();

            foreach (var squad in _world.Squads)
            {
                var card = new VisualElement();
                card.AddToClassList("squad-card");
                if (ReferenceEquals(squad, Active)) card.AddToClassList("active");

                var head = new VisualElement();
                head.AddToClassList("squad-head");
                var name = new Label("ОТРЯД «" + squad.Name + "»");
                name.AddToClassList("squad-name");
                head.Add(name);

                var briefing = Briefing.Compute(_world, _tables, squad.Members, squad.ZoneId, squad.Behavior);

                var zoneButton = new Button(() =>
                {
                    Active = squad;
                    CycleZone(squad);
                    Rebuild();
                    Changed?.Invoke();
                });
                zoneButton.text = string.IsNullOrEmpty(squad.ZoneId)
                    ? "зона: не выбрана ▾"
                    : "зона: " + _tables.Zones.Get(squad.ZoneId).Name + " ▾";
                zoneButton.AddToClassList("action");
                head.Add(zoneButton);
                card.Add(head);

                var members = squad.Members.Count == 0
                    ? "никого — жми «настроить…»"
                    : "";

                var body = new VisualElement();
                body.AddToClassList("squad-body");

                var left = new VisualElement();
                left.AddToClassList("squad-left");

                if (members.Length > 0) left.Add(Line("squad-members", members));

                left.Add(Line("squad-line",
                    "дорога " + briefing.TravelHours.ToString("0.#") + " ч · работа " + briefing.WorkHours
                    + " ч · слоты " + briefing.Slots
                    + " · встречи " + Math.Round(briefing.EncounterPerHour * 100) + "%/час"));

                var actions = new VisualElement();
                actions.AddToClassList("squad-actions");

                var behavior = new Button(() =>
                {
                    Active = squad;
                    squad.Behavior = squad.Behavior == Behavior.Gather ? Behavior.Avoid
                        : squad.Behavior == Behavior.Avoid ? Behavior.Clean
                        : Behavior.Gather;
                    Rebuild();
                    Changed?.Invoke();
                });
                behavior.text = "намерение: " + BehaviorName(squad.Behavior);
                behavior.AddToClassList("action");
                actions.Add(behavior);

                var edit = new Button(() =>
                {
                    Active = squad;
                    EditRequested?.Invoke(squad);
                });
                edit.text = "настроить…";
                edit.AddToClassList("action");
                actions.Add(edit);

                left.Add(actions);
                body.Add(left);

                var portraits = new VisualElement();
                portraits.AddToClassList("squad-portraits");
                foreach (var personId in squad.Members)
                {
                    var portrait = new VisualElement();
                    portrait.AddToClassList("portrait");
                    portrait.AddToClassList("portrait-" + personId);
                    portraits.Add(portrait);
                }

                body.Add(portraits);
                card.Add(body);

                var captured = squad;
                card.RegisterCallback<ClickEvent>(_ =>
                {
                    if (ReferenceEquals(Active, captured)) return;
                    Active = captured;
                    Rebuild();
                });

                _squads.Add(card);
            }
        }

        private void CycleZone(Squad squad)
        {
            var zones = _tables.Zones.All;
            if (string.IsNullOrEmpty(squad.ZoneId))
            {
                squad.ZoneId = zones[0].Id;
                return;
            }

            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i].Id != squad.ZoneId) continue;
                squad.ZoneId = zones[(i + 1) % zones.Count].Id;
                return;
            }

            squad.ZoneId = zones[0].Id;
        }

        // ---- работы ----

        private void BuildWorks()
        {
            _works.Clear();
            AddWork("починка частокола", _plan.VillageWorkers, () => FirstFree(), true);
            AddWork("постели", _plan.Beds, () => FirstWounded(), false);
            AddWork("заготовка", _plan.Foragers, () => FirstFree(), true);
        }

        private void AddWork(string title, List<string> pool, Func<string> pick, bool canAdd)
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
                Changed?.Invoke();
            });
            minus.text = "−";
            minus.AddToClassList("work-button");
            row.Add(minus);

            var plus = new Button(() =>
            {
                if (!canAdd) return;
                var personId = pick();
                if (personId == null)
                {
                    Changed?.Invoke();
                    return;
                }

                pool.Add(personId);
                Rebuild();
                Changed?.Invoke();
            });
            plus.text = "+";
            plus.AddToClassList("work-button");
            row.Add(plus);

            _works.Add(row);
        }

        private string FirstFree()
        {
            foreach (var person in _tables.Roster.All)
            {
                if (!IsFree(person.Id)) continue;
                return person.Id;
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

        public bool IsFree(string personId)
        {
            if (_world.ConditionOf(personId) != PersonCondition.Healthy) return false;
            if (_plan.IsBusy(personId)) return false;

            foreach (var squad in _world.Squads)
            {
                if (squad.Members.Contains(personId)) return false;
            }

            return true;
        }

        // ---- предупреждения ----

        private void BuildWarnings()
        {
            var lines = new List<string>();

            foreach (var squad in _world.Squads)
            {
                if (squad.Members.Count == 0) continue;

                var briefing = Briefing.Compute(_world, _tables, squad.Members, squad.ZoneId, squad.Behavior);
                foreach (var warning in briefing.Warnings) lines.Add("«" + squad.Name + "»: " + warning);
            }

            if (_world.Squads[0].Members.Count == 0 && _world.Squads[1].Members.Count == 0)
                lines.Add("Никто не идёт в лес");

            _warnings.text = lines.Count == 0 ? "" : string.Join("  ·  ", lines);
        }

        private Label Line(string className, string text)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static string BehaviorName(Behavior behavior)
        {
            switch (behavior)
            {
                case Behavior.Avoid: return "избегание";
                case Behavior.Clean: return "чистка";
                default: return "сбор";
            }
        }
    }
}
