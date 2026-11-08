using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Экран похода (ui.md §5): часы идут сами, на развилке — пауза и карточка выбора.
    /// Наказы отряда снимают паузу: развилку решает политика (в ядре).
    /// </summary>
    public sealed class ExpeditionScreen
    {
        private const int TickMs = 700;

        private readonly VisualElement _panel;
        private readonly Label _title;
        private readonly Label _hour;
        private readonly ScrollView _log;
        private readonly VisualElement _card;
        private readonly Label _decisionTitle;
        private readonly Label _decisionBody;
        private readonly VisualElement _options;
        private readonly Button _pause;

        private readonly VisualElement _monsterModal;
        private readonly VisualElement _monsterArt;
        private readonly Label _monsterTitle;
        private readonly Label _monsterBody;
        private readonly VisualElement _monsterOptions;

        private Tables _tables;
        private World _world;
        private DayPlan _plan;
        private Rng _rng;

        private readonly Queue<PartyAssignment> _queue = new Queue<PartyAssignment>();
        private readonly List<ExpeditionResult> _results = new List<ExpeditionResult>();

        private Expedition _current;
        private IVisualElementScheduledItem _tick;
        private bool _paused;

        public event Action<List<ExpeditionResult>> Finished;

        public ExpeditionScreen(VisualElement root)
        {
            _panel = root.Q<VisualElement>("expedition");
            _title = root.Q<Label>("expedition-title");
            _hour = root.Q<Label>("expedition-hour");
            _log = root.Q<ScrollView>("expedition-log");
            _card = root.Q<VisualElement>("expedition-card");
            _decisionTitle = root.Q<Label>("decision-title");
            _decisionBody = root.Q<Label>("decision-body");
            _options = root.Q<VisualElement>("decision-options");
            _pause = root.Q<Button>("expedition-pause");

            _monsterModal = root.Q<VisualElement>("monster-modal");
            _monsterArt = root.Q<VisualElement>("monster-art");
            _monsterTitle = root.Q<Label>("monster-title");
            _monsterBody = root.Q<Label>("monster-body");
            _monsterOptions = root.Q<VisualElement>("monster-options");

            _pause.clicked += TogglePause;
            root.Q<Button>("expedition-skip").clicked += SkipAll;
        }

        public bool IsOpen => _panel != null && !_panel.ClassListContains("hidden");

        public void Open(Tables tables, World world, DayPlan plan, Rng rng)
        {
            _tables = tables;
            _world = world;
            _plan = plan;
            _rng = rng;

            _queue.Clear();
            _results.Clear();
            foreach (var party in plan.Parties) _queue.Enqueue(party);

            _log.Clear();
            HideDecisions();
            _paused = false;
            _pause.text = "ПАУЗА";
            _panel.RemoveFromClassList("hidden");

            _tick = _panel.schedule.Execute(Tick).Every(TickMs);
            StartNext();
        }

        public void Close()
        {
            _tick?.Pause();
            _panel.AddToClassList("hidden");
        }

        private void TogglePause()
        {
            _paused = !_paused;
            _pause.text = _paused ? "ПРОДОЛЖИТЬ" : "ПАУЗА";
        }

        // ---- шаги похода ----

        private void StartNext()
        {
            if (_queue.Count == 0)
            {
                Close();
                Finished?.Invoke(_results);
                return;
            }

            var party = _queue.Dequeue();
            _current = new Expedition(_world, _tables, party, _rng);

            _title.text = "ПОХОД · " + _tables.Zones.Get(party.ZoneId).Name;
            Append("— отряд «" + PartyName(party) + "»: " + ZoneLine(party));
            UpdateHour();
        }

        private void Tick()
        {
            if (_paused || _current == null) return;
            if (_current.HasDecision) return;

            if (_current.Finished)
            {
                _results.Add(_current.Result);
                StartNext();
                return;
            }

            var before = _current.Result.Log.Count;
            _current.Advance();
            AppendNew(before);
            UpdateHour();

            if (_current.HasDecision)
            {
                ShowDecision(_current.Pending);
                return;
            }

            if (_current.Finished)
            {
                _results.Add(_current.Result);
                StartNext();
            }
        }

        private void AppendNew(int fromIndex)
        {
            for (var i = fromIndex; i < _current.Result.Log.Count; i++)
            {
                Append(_current.Result.Log[i]);
            }
        }

        private void Append(string line)
        {
            var label = new Label("· " + line);
            label.AddToClassList("pane-line");
            _log.Add(label);
            _log.scrollOffset = new UnityEngine.Vector2(0, float.MaxValue);
        }

        private void UpdateHour()
        {
            if (_current == null) return;
            _hour.text = "час " + _current.Hour + " из " + _current.WorkHours;
        }

        // ---- карточка развилки ----

        private void ShowDecision(ExpeditionDecision decision)
        {
            _tick.Pause();

            HideDecisions();

            if (UsesMonsterArt(decision)) ShowMonsterDecision(decision);
            else ShowInlineDecision(decision);
        }

        /// Встреча и туша показываются модалом с артом твари; раненый и мешок — нижней карточкой.
        private bool UsesMonsterArt(ExpeditionDecision decision)
        {
            if (decision.Kind != DecisionKind.Encounter && decision.Kind != DecisionKind.Carcass) return false;
            return !string.IsNullOrEmpty(decision.BeastId);
        }

        private void ShowMonsterDecision(ExpeditionDecision decision)
        {
            var beast = _tables.Beasts.Get(decision.BeastId);
            var state = decision.Kind == DecisionKind.Encounter ? "search" : "dead";

            _monsterArt.ClearClassList();
            _monsterArt.AddToClassList("monster-art");

            if (!string.IsNullOrEmpty(beast.Art))
                _monsterArt.AddToClassList("monster-art-" + beast.Art + "-" + state);

            _monsterTitle.text = decision.Title;
            _monsterBody.text = decision.Body;

            BuildOptions(_monsterOptions, decision);
            _monsterModal.RemoveFromClassList("hidden");
        }

        private void ShowInlineDecision(ExpeditionDecision decision)
        {
            _decisionTitle.text = decision.Title;
            _decisionBody.text = decision.Body;

            BuildOptions(_options, decision);
            _card.RemoveFromClassList("hidden");
        }

        private void BuildOptions(VisualElement container, ExpeditionDecision decision)
        {
            container.Clear();

            foreach (var option in decision.Options)
            {
                var id = option.Id;
                var button = new Button(() => Choose(id));

                button.text = option.Label
                    + (string.IsNullOrEmpty(option.Note) ? "" : " — " + option.Note);

                button.AddToClassList("decision-option");
                container.Add(button);
            }
        }

        private void HideDecisions()
        {
            _card.AddToClassList("hidden");
            _monsterModal.AddToClassList("hidden");
        }

        private void Choose(string optionId)
        {
            HideDecisions();
            var before = _current.Result.Log.Count;
            _current.Decide(optionId);
            AppendNew(before);

            if (_current.Finished)
            {
                _results.Add(_current.Result);
                StartNext();
                return;
            }

            // Решение могло родить следующее (победа в бою → туша): показываем сразу.
            if (_current.HasDecision)
            {
                ShowDecision(_current.Pending);
                return;
            }

            _tick.Resume();
        }

        private string PartyName(PartyAssignment party)
        {
            return string.IsNullOrEmpty(party.Name) ? party.PersonIds.Count.ToString() : party.Name;
        }

        // ---- домотать ----

        private void SkipAll()
        {
            _tick?.Pause();
            HideDecisions();

            if (_current != null && !_current.Finished)
            {
                for (var guard = 0; guard < 500 && !_current.Finished; guard++)
                {
                    if (_current.HasDecision) _current.Decide(ExpeditionSim.Default(_current.Pending));
                    else _current.Advance();
                }

                _results.Add(_current.Result);
                _current = null;
            }

            while (_queue.Count > 0)
            {
                _results.Add(ExpeditionSim.Resolve(_world, _tables, _queue.Dequeue(), _rng));
            }

            Close();
            Finished?.Invoke(_results);
        }

        private string ZoneLine(PartyAssignment party)
        {
            var zone = _tables.Zones.Get(party.ZoneId);
            var names = new List<string>();
            foreach (var personId in party.PersonIds) names.Add(_tables.Roster.Get(personId).Name);

            return zone.Name + " · " + string.Join(", ", names);
        }
    }
}
