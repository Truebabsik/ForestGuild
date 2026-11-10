using System;
using System.Collections.Generic;
using ForestGuild.Core.Sim;
using ForestGuild.Core.Data;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Экран планирования (ui.md §4.1): карточки отрядов-пресетов и предупреждения.
    /// Работы в деревне живут в отдельном окне (`VillageScreen`), люди — в Сборе.
    /// </summary>
    public sealed class MorningBoard
    {
        private readonly Tables _tables;
        private readonly World _world;
        private readonly VisualElement _squads;
        private readonly Label _warnings;

        public Squad Active;
        public event Action<Squad> EditRequested;

        public MorningBoard(Tables tables, World world, VisualElement squads, Label warnings)
        {
            _tables = tables;
            _world = world;
            _squads = squads;
            _warnings = warnings;
        }

        public void Rebuild()
        {
            BuildSquads();
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
                });
                zoneButton.text = string.IsNullOrEmpty(squad.ZoneId)
                    ? "зона: не выбрана ▾"
                    : "зона: " + _tables.Zones.Get(squad.ZoneId).Name + " ▾";
                zoneButton.AddToClassList("action");
                head.Add(zoneButton);
                card.Add(head);

                var body = new VisualElement();
                body.AddToClassList("squad-body");

                var left = new VisualElement();
                left.AddToClassList("squad-left");

                if (squad.Members.Count == 0)
                {
                    left.Add(Line("squad-members", "никого — жми «настроить…»"));
                }

                left.Add(Line("squad-line",
                    string.IsNullOrEmpty(squad.ZoneId)
                        ? "зона не выбрана — нажми кнопку зоны"
                        : briefing.WorkMinutes > 0
                            ? "дорога " + briefing.TravelMinutes + " мин · в лесу " + briefing.WorkMinutes + " мин"
                            : "дорога " + briefing.TravelMinutes + " мин · работы не будет"));

                if (squad.Members.Count > 0)
                {
                    var words = new List<string>();
                    foreach (var personId in squad.Members)
                        words.Add(_tables.Roster.Get(personId).Name + " — " + Ui.HpWord(_tables, _world, personId));

                    left.Add(Line("squad-line", string.Join(", ", words)));
                }

                var actions = new VisualElement();
                actions.AddToClassList("squad-actions");

                var behavior = new Button(() =>
                {
                    Active = squad;
                    squad.Behavior = squad.Behavior == Behavior.Gather ? Behavior.Avoid
                        : squad.Behavior == Behavior.Avoid ? Behavior.Clean
                        : Behavior.Gather;
                    Rebuild();
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
                    portraits.Add(Ui.Portrait(personId));
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

            var anyMembers = _world.Squads[0].Members.Count + _world.Squads[1].Members.Count > 0;
            if (!anyMembers) lines.Add("Никто не идёт в лес");

            _warnings.text = string.Join("  ·  ", lines);
        }

        private static Label Line(string className, string text)
        {
            return Ui.Text(className, text);
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
