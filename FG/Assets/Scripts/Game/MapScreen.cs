using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Экран дня: карта, ростер, план дня, досье зоны, «конец дня».
    /// Вся логика — в ядре; здесь только показ и ввод. Точные числа зоны скрыты (design.md §6.5):
    /// обычный режим показывает слова и слухи, тумблер «числа» — отладку.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MapScreen : MonoBehaviour
    {
        public Tables Tables;
        public World World;
        public MapController Map;

        private UIDocument _document;
        private RosterPanel _roster;
        private DayPlan _plan = new DayPlan();
        private Rng _rng;
        private Behavior _behavior = Behavior.Gather;
        private bool _debug;

        private Label _dayLabel;
        private Label _queueLabel;
        private Label _reportTitle;
        private VisualElement _report;
        private VisualElement _reportLines;
        private VisualElement _parties;
        private ScrollView _zoneCard;
        private Label _toast;
        private IVisualElementScheduledItem _toastHide;
        private Button _behaviorGather;
        private Button _behaviorAvoid;
        private Button _behaviorClean;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            var root = _document.rootVisualElement;

            try
            {
                var dataDir = System.IO.Path.Combine(Application.streamingAssetsPath, "Data");
                Tables = DataLoader.Load(dataDir);
                World = World.Create(Tables);
                _rng = new Rng(12345);
            }
            catch (Exception e)
            {
                var error = new Label("ОШИБКА ДАННЫХ: " + e.Message);
                error.style.color = new Color(1f, 0.45f, 0.35f);
                error.style.fontSize = 16;
                error.style.whiteSpace = WhiteSpace.Normal;
                root.Add(error);
                Debug.LogError(e);
                enabled = false;
                return;
            }

            Map = new MapController(root.Q<VisualElement>("map"));
            Map.Build(Tables);
            Map.ZoneSelected += ShowZone;
            Map.VillageClicked += ShowVillage;

            _dayLabel = root.Q<Label>("day-label");
            _queueLabel = root.Q<Label>("queue-label");
            _report = root.Q<VisualElement>("report");
            _reportTitle = root.Q<Label>("report-title");
            _reportLines = root.Q<VisualElement>("report-lines");
            _parties = root.Q<VisualElement>("parties");
            _zoneCard = root.Q<ScrollView>("zone-card");
            _toast = root.Q<Label>("toast");

            _roster = new RosterPanel(root.Q<VisualElement>("roster"));
            _roster.Build(Tables);

            _behaviorGather = root.Q<Button>("behavior-gather");
            _behaviorAvoid = root.Q<Button>("behavior-avoid");
            _behaviorClean = root.Q<Button>("behavior-clean");
            _behaviorGather.clicked += () => SetBehavior(Behavior.Gather);
            _behaviorAvoid.clicked += () => SetBehavior(Behavior.Avoid);
            _behaviorClean.clicked += () => SetBehavior(Behavior.Clean);
            SetBehavior(Behavior.Gather);

            var debugToggle = root.Q<Toggle>("debug-toggle");
            if (debugToggle != null)
            {
                debugToggle.value = false;
                debugToggle.RegisterValueChangedCallback(e =>
                {
                    _debug = e.newValue;
                    if (Map.Selected != null) ShowZone(Map.Selected);
                    else if (Map.VillageSelected) ShowVillage();
                });
            }

            root.Q<Button>("send-button").clicked += SendParty;
            root.Q<Button>("village-button").clicked += SendToVillage;
            root.Q<Button>("end-day").clicked += EndDay;
            root.Q<Button>("report-close").clicked += () => _report.AddToClassList("hidden");

            Map.RefreshStates(World);
            _dayLabel.text = "День " + World.Day;
            ShowHint();
            RefreshPlan();
        }

        private void SetBehavior(Behavior behavior)
        {
            _behavior = behavior;
            _behaviorGather.EnableInClassList("selected", behavior == Behavior.Gather);
            _behaviorAvoid.EnableInClassList("selected", behavior == Behavior.Avoid);
            _behaviorClean.EnableInClassList("selected", behavior == Behavior.Clean);
        }

        private void SendParty()
        {
            if (Map.Selected == null)
            {
                ShowMessage("Сначала выберите зону на карте");
                return;
            }

            if (_plan.Parties.Count >= DayPlan.MaxParties)
            {
                ShowMessage("Больше " + DayPlan.MaxParties + " отрядов деревня не поднимет");
                return;
            }

            if (_roster.Selected.Count == 0)
            {
                ShowMessage("Выберите хотя бы одного жителя");
                return;
            }

            var party = new PartyAssignment { ZoneId = Map.Selected, Behavior = _behavior };
            party.PersonIds.AddRange(_roster.Selected);
            _plan.Parties.Add(party);

            var zone = Tables.Zones.Get(Map.Selected);
            ShowMessage("Отряд из " + party.PersonIds.Count + " идёт в " + zone.Name
                + " · " + BehaviorName(_behavior));
            _roster.Clear();
            RefreshPlan();
        }

        private void SendToVillage()
        {
            if (_roster.Selected.Count == 0)
            {
                ShowMessage("Выберите хотя бы одного жителя");
                return;
            }

            foreach (var personId in _roster.Selected) _plan.VillageWorkers.Add(personId);
            ShowMessage("В деревне остаются: " + _roster.Selected.Count + " чел.");
            _roster.Clear();
            RefreshPlan();
        }

        private void RemoveParty(int index)
        {
            if (index < 0 || index >= _plan.Parties.Count) return;
            _plan.Parties.RemoveAt(index);
            RefreshPlan();
        }

        private void RefreshPlan()
        {
            _roster.SetBusy(_plan.IsBusy);
            _parties.Clear();

            for (var i = 0; i < _plan.Parties.Count; i++)
            {
                var party = _plan.Parties[i];
                var index = i;
                var row = new VisualElement();
                row.AddToClassList("party-row");

                var names = new List<string>();
                foreach (var personId in party.PersonIds) names.Add(Tables.Roster.Get(personId).Name);

                var text = new Label(Tables.Zones.Get(party.ZoneId).Name + " · " + BehaviorName(party.Behavior)
                    + " · " + string.Join(", ", names));
                text.AddToClassList("party-text");
                row.Add(text);

                var remove = new Button(() => RemoveParty(index));
                remove.text = "×";
                remove.AddToClassList("party-remove");
                row.Add(remove);

                _parties.Add(row);
            }

            if (_plan.VillageWorkers.Count > 0)
            {
                var names = new List<string>();
                foreach (var personId in _plan.VillageWorkers) names.Add(Tables.Roster.Get(personId).Name);

                var row = new VisualElement();
                row.AddToClassList("party-row");
                var text = new Label("Деревня · частокол · " + string.Join(", ", names));
                text.AddToClassList("party-text");
                row.Add(text);

                var clear = new Button(() =>
                {
                    _plan.VillageWorkers.Clear();
                    RefreshPlan();
                });
                clear.text = "×";
                clear.AddToClassList("party-remove");
                row.Add(clear);
                _parties.Add(row);
            }

            var workers = 0;
            foreach (var party in _plan.Parties) workers += party.PersonIds.Count;
            workers += _plan.VillageWorkers.Count;

            _queueLabel.text = _plan.Parties.Count == 0 && _plan.VillageWorkers.Count == 0
                ? "План пуст: весь день деревня без дела"
                : "План: отрядов " + _plan.Parties.Count + " из " + DayPlan.MaxParties
                  + ", занято " + workers + " из " + Tables.Roster.All.Count;

            if (Map.Selected != null) ShowZone(Map.Selected, false);
        }

        private string BehaviorName(Behavior behavior)
        {
            switch (behavior)
            {
                case Behavior.Avoid: return "избегание";
                case Behavior.Clean: return "чистка";
                default: return "сбор";
            }
        }

        private void EndDay()
        {
            var flow = new DayFlow(World, Tables, _rng);
            var report = flow.RunDay(_plan);
            _plan = new DayPlan();

            _dayLabel.text = "День " + World.Day;
            Map.RefreshStates(World);
            if (Map.Selected != null) ShowZone(Map.Selected);
            if (Map.VillageSelected) ShowVillage();
            RefreshPlan();

            ShowReport(report);
        }

        private void ShowZone(string zoneId)
        {
            ShowZone(zoneId, true);
        }

        /// <summary>Досье зоны: картинка, нарратив, кто водится, находки, слухи.</summary>
        private void ShowZone(string zoneId, bool scrollToTop)
        {
            var zone = Tables.Zones.Get(zoneId);
            var state = World.Zones[zoneId];
            _zoneCard.Clear();

            var art = new VisualElement();
            art.AddToClassList("zone-art");
            art.AddToClassList("zone-art-" + zoneId);
            _zoneCard.Add(art);

            _zoneCard.Add(Text("zone-title", zone.Name));
            _zoneCard.Add(Text("zone-meta", zone.Depth + " · дорога " + zone.TravelHours.ToString("0.#")
                + " ч · сейчас: " + ZoneKnowledge.Mood(World, zoneId)));

            if (!state.Scouted)
            {
                _zoneCard.Add(Text("zone-text", "Сюда ещё не ходили. Известны только дорога и соседи — "
                    + string.Join(", ", NeighbourNames(zone)) + "."));
                if (_debug) _zoneCard.Add(Text("zone-text", DebugLine(zoneId)));
                return;
            }

            _zoneCard.Add(Text("zone-text", Lore(zoneId, "look")));
            AddSection("КТО ВОДИТСЯ", Lore(zoneId, "beasts"));
            AddSection("ЧТО НАЙТИ", Lore(zoneId, "finds"));
            AddSection("ОСОБОЕ", Lore(zoneId, "special"));

            _zoneCard.Add(Text("zone-section-title", "СЛУХИ"));
            if (state.Sightings.Count == 0)
            {
                _zoneCard.Add(Text("zone-text", "Пока ничего не рассказывали."));
            }
            else
            {
                var from = Math.Max(0, state.Sightings.Count - 3);
                for (var i = from; i < state.Sightings.Count; i++)
                {
                    var sighting = state.Sightings[i];
                    _zoneCard.Add(Text("zone-text", "день " + sighting.Day + " — " + sighting.Text));
                }
            }

            if (_debug) _zoneCard.Add(Text("zone-text", DebugLine(zoneId)));

            if (scrollToTop) _zoneCard.scrollOffset = Vector2.zero;
        }

        private void ShowVillage()
        {
            _zoneCard.Clear();

            var art = new VisualElement();
            art.AddToClassList("zone-art");
            art.AddToClassList("zone-art-verge_meadow");
            _zoneCard.Add(art);

            _zoneCard.Add(Text("zone-title", "Деревня"));
            _zoneCard.Add(Text("zone-meta", "частокол " + World.PalisadeHp + " из " + World.PalisadeMax));
            _zoneCard.Add(Text("zone-text", "Пока стена держит, лес сюда не заходит. "
                + "Один человеко-день даёт два очка прочности."));
            _zoneCard.Add(Text("zone-text", "Жителей: " + Tables.Roster.All.Count
                + ". В деревне сейчас: " + _plan.VillageWorkers.Count + "."));

            if (_debug)
            {
                _zoneCard.Add(Text("zone-text", "отладка: прочность " + World.PalisadeHp + "/" + World.PalisadeMax
                    + ", день " + World.Day));
            }
        }

        private void ShowHint()
        {
            _zoneCard.Clear();
            _zoneCard.Add(Text("zone-title", "Выберите зону"));
            _zoneCard.Add(Text("zone-text", "Клик по узлу на карте открывает досье: что там за место, "
                + "кто водится, что находили и что рассказывают."));
        }

        private void AddSection(string title, string body)
        {
            _zoneCard.Add(Text("zone-section-title", title));
            _zoneCard.Add(Text("zone-text", body));
        }

        private string Lore(string zoneId, string field)
        {
            return Tables.ZoneLore.TryGet(zoneId + "|" + field, out var row)
                ? row.Text
                : "—";
        }

        private IEnumerable<string> NeighbourNames(Zone zone)
        {
            foreach (var id in zone.Neighbours) yield return Tables.Zones.Get(id).Name;
        }

        private string DebugLine(string zoneId)
        {
            var state = World.Zones[zoneId];
            var beasts = new List<string>();
            foreach (var pair in state.Beasts) beasts.Add(Tables.Beasts.Get(pair.Key).Name + " " + pair.Value);

            return "отладка: твари " + (beasts.Count == 0 ? "нет" : string.Join(", ", beasts))
                + " · кристаллы " + state.Crystals
                + " · корм " + state.Forage
                + " · опыт гнезда " + state.NestXp
                + " · гнездо " + (state.Nest.Exists ? state.Nest.Beasts + "/" + Tables.Tuning.GetInt("nest_beasts_cap") : "нет")
                + " · тревога " + state.AlertDays;
        }

        private Label Text(string className, string value)
        {
            var label = new Label(value);
            label.AddToClassList(className);
            return label;
        }

        private void ShowMessage(string text)
        {
            if (_toast == null)
            {
                ShowReportTitleOnly(text);
                return;
            }

            _toast.text = text;
            _toast.RemoveFromClassList("hidden");
            _toastHide?.Pause();
            _toastHide = _toast.schedule.Execute(() => _toast.AddToClassList("hidden")).StartingIn(3000);
        }

        private void ShowReportTitleOnly(string text)
        {
            _reportTitle.text = "Сообщение";
            _reportLines.Clear();
            _reportLines.Add(new Label(text));
            _report.RemoveFromClassList("hidden");
        }

        private void ShowReport(DayReport report)
        {
            _reportTitle.text = "Итоги дня " + (World.Day - 1);
            _reportLines.Clear();
            foreach (var line in report.Lines)
            {
                _reportLines.Add(new Label("· " + line));
            }

            if (report.Lines.Count == 0)
            {
                _reportLines.Add(new Label("· день прошёл тихо"));
            }

            _report.RemoveFromClassList("hidden");
        }
    }
}
