using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    public enum Phase
    {
        Morning,
        Day,
        Evening,
        Night,
    }

    /// <summary>
    /// Каркас цикла суток (docs/ui.md §3): HUD + фазы Утро → День → Вечер → Ночь.
    /// Утро — доска приказов (§4). Правила — в ядре, здесь показ и ввод.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MapScreen : MonoBehaviour
    {
        public Tables Tables;
        public World World;
        public MapController Map;
        public MorningBoard Board;
        public MusterScreen Muster;
        public DossierScreen Dossier;
        public EveningScreen Evening;

        private UIDocument _document;
        private DayFlow _flow;
        private DayPlan _plan = new DayPlan();
        private Rng _rng;
        private bool _debug;
        private Phase _phase = Phase.Morning;
        private DayReport _dayReport;
        private Action _paneAction;

        private Label _hud;
        private Label _warnings;
        private Label _paneTitle;
        private VisualElement _pane;
        private ScrollView _paneLines;
        private Button _paneButton;
        private VisualElement _leftPanel;
        private VisualElement _rightPanel;
        private VisualElement _bottomBar;
        private VisualElement _modal;
        private Label _modalTitle;
        private VisualElement _modalLines;
        private ScrollView _zoneCard;
        private Label _toast;
        private IVisualElementScheduledItem _toastHide;

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
                _flow = new DayFlow(World, Tables, _rng);
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
            Map.ZoneSelected += zoneId =>
            {
                ShowZone(zoneId);
                Dossier?.Open(zoneId, Tables, World, _phase == Phase.Morning);
            };
            Map.VillageClicked += ShowVillage;

            _hud = root.Q<Label>("hud");
            _warnings = root.Q<Label>("warnings");
            _pane = root.Q<VisualElement>("pane");
            _paneTitle = root.Q<Label>("pane-title");
            _paneLines = root.Q<ScrollView>("pane-lines");
            _paneButton = root.Q<Button>("pane-button");
            _paneButton.clicked += () => _paneAction?.Invoke();
            _leftPanel = root.Q<VisualElement>("left-panel");
            _rightPanel = root.Q<VisualElement>("right-panel");
            _bottomBar = root.Q<VisualElement>("bottom-bar");
            _zoneCard = root.Q<ScrollView>("zone-card");
            _toast = root.Q<Label>("toast");

            _modal = root.Q<VisualElement>("modal");
            _modalTitle = root.Q<Label>("modal-title");
            _modalLines = root.Q<VisualElement>("modal-lines");
            root.Q<Button>("modal-cancel").clicked += () => _modal.AddToClassList("hidden");
            root.Q<Button>("modal-confirm").clicked += ConfirmOrder;

            Board = new MorningBoard(Tables, World, _plan,
                root.Q<VisualElement>("squads"),
                root.Q<VisualElement>("works"),
                _warnings);
            Board.Active = World.Squads[0];
            Board.Changed += ShowActiveSquadZone;

            Muster = new MusterScreen(root);
            Board.EditRequested += squad => Muster.Open(squad, Tables, World, _plan);
            Muster.Closed += () =>
            {
                Board.Rebuild();
                ShowActiveSquadZone();
            };

            Dossier = new DossierScreen(root);
            Dossier.GatherRequested += zoneId =>
            {
                if (Board.Active == null) Board.Active = World.Squads[0];
                Board.Active.ZoneId = zoneId;
                Board.Rebuild();
                ShowZone(zoneId);
            };

            Evening = new EveningScreen(root);
            Evening.Slept += GoToNight;

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

            root.Q<Button>("sign-button").clicked += OpenOrder;

            Map.RefreshStates(World);
            ShowHint();
            Board.Rebuild();
            ShowActiveSquadZone();
            SetPhase(Phase.Morning);
        }

        // ---- фазы ----

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            var morning = phase == Phase.Morning;

            _leftPanel.EnableInClassList("hidden", !morning);
            _rightPanel.EnableInClassList("hidden", !morning);
            _bottomBar.EnableInClassList("hidden", !morning);
            _pane.EnableInClassList("hidden", morning);
            _modal.AddToClassList("hidden");
            if (!morning)
            {
                Muster?.Close();
                Dossier?.Close();
            }

            if (phase != Phase.Evening) Evening?.Close();

            UpdateHud();
        }

        private void UpdateHud()
        {
            var waveIn = Tables.Tuning.GetInt("wave_day") - World.Day;
            if (waveIn < 0) waveIn = 0;

            var shownDay = _phase == Phase.Night ? World.Day - 1 : World.Day;
            _hud.text = "ДЕНЬ " + shownDay + " · " + PhaseName(_phase)
                + "    |    частокол " + World.PalisadeHp + "/" + World.PalisadeMax
                + "    |    до волны: " + waveIn + " дн.";
        }

        private string PhaseName(Phase phase)
        {
            switch (phase)
            {
                case Phase.Day: return "ДЕНЬ";
                case Phase.Evening: return "ВЕЧЕР";
                case Phase.Night: return "НОЧЬ";
                default: return "УТРО";
            }
        }

        // ---- приказ ----

        private void ShowActiveSquadZone()
        {
            if (Board.Active != null && !string.IsNullOrEmpty(Board.Active.ZoneId))
            {
                ShowZone(Board.Active.ZoneId);
            }
        }

        private void OpenOrder()
        {
            var lines = new List<string>();
            _plan.Parties.Clear();

            foreach (var squad in World.Squads)
            {
                if (squad.Members.Count == 0 || string.IsNullOrEmpty(squad.ZoneId)) continue;

                var party = new PartyAssignment { ZoneId = squad.ZoneId, Behavior = squad.Behavior };
                party.PersonIds.AddRange(squad.Members);
                _plan.Parties.Add(party);

                var zone = Tables.Zones.Get(squad.ZoneId);
                var briefing = Briefing.Compute(World, Tables, squad.Members, squad.ZoneId, squad.Behavior);

                lines.Add("ОТРЯД «" + squad.Name + "» → " + zone.Name + " · " + BehaviorName(squad.Behavior));
                lines.Add("   " + string.Join(" · ", MemberNames(squad)));
                lines.Add("   дорога " + briefing.TravelHours.ToString("0.#") + " ч"
                    + (briefing.WorkHours > 0
                        ? ", в лесу " + briefing.WorkHours + " ч"
                        : ", работы не будет — день уйдёт на дорогу"));

                foreach (var warning in briefing.Warnings) lines.Add("   ⚠ " + warning);
            }

            if (_plan.Parties.Count == 0)
            {
                lines.Add("В лес никто не идёт.");
            }

            lines.Add("");
            lines.Add("В ДЕРЕВНЕ: починка " + _plan.VillageWorkers.Count
                + " · постели " + _plan.Beds.Count
                + " · заготовка " + _plan.Foragers.Count);

            _modalTitle.text = "ПРИКАЗ НА ВЫЛАЗКУ";
            _modalLines.Clear();
            foreach (var line in lines)
            {
                var label = new Label(line);
                label.AddToClassList("pane-line");
                _modalLines.Add(label);
            }

            _modal.RemoveFromClassList("hidden");
        }

        private void ConfirmOrder()
        {
            _modal.AddToClassList("hidden");

            _dayReport = _flow.RunExpeditions(_plan);

            var lines = new List<string>(_dayReport.Lines);
            if (lines.Count == 0) lines.Add("День прошёл без событий");

            SetPhase(Phase.Day);
            ShowPane("ДЕНЬ " + World.Day + " · ПОХОД", lines, "К ВЕЧЕРУ →", GoToEvening);
        }

        private void GoToEvening()
        {
            SetPhase(Phase.Evening);
            Evening.Open(_dayReport, _plan, World, Tables);
        }

        private void GoToNight()
        {
            var night = _flow.RunNight(_plan);

            SetPhase(Phase.Night);
            ShowNight(night);

            Map.RefreshStates(World);
        }

        /// Ночь (ui.md §7): строки с именами зон, по клику — досье. Стрелок на карте нет.
        private void ShowNight(NightReport night)
        {
            _paneTitle.text = "НОЧЬ · лес живёт";
            _paneButton.text = "УТРО →";
            _paneAction = GoToMorning;

            _paneLines.Clear();
            foreach (var entry in night.Entries)
            {
                var label = new Label("· " + entry.Text);
                label.AddToClassList("pane-line");

                if (entry.ZoneId != null)
                {
                    label.AddToClassList("link");
                    var zoneId = entry.ZoneId;
                    label.RegisterCallback<ClickEvent>(_ =>
                        Dossier.Open(zoneId, Tables, World, false));
                }

                _paneLines.Add(label);
            }

            _paneLines.scrollOffset = Vector2.zero;
        }

        private void GoToMorning()
        {
            _plan = new DayPlan();
            _dayReport = null;

            foreach (var squad in World.Squads)
            {
                squad.Members.RemoveAll(id => World.ConditionOf(id) != PersonCondition.Healthy);
            }

            SetPhase(Phase.Morning);
            Map.RefreshStates(World);
            Board.Rebuild();
            ShowHint();
            if (_warnings != null) _warnings.text = "";
        }

        private IEnumerable<string> MemberNames(Squad squad)
        {
            foreach (var personId in squad.Members)
            {
                var person = Tables.Roster.Get(personId);
                var profession = Tables.Professions.Get(person.Profession);
                var level = People.SkillLevel(Tables, person, profession.SkillsAllowed[0]);
                yield return person.Name + " " + level;
            }
        }

        private List<string> EveningLines()
        {
            var lines = new List<string>();
            if (_dayReport == null) return lines;

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var wounded = new List<string>();

            foreach (var party in _dayReport.Parties)
            {
                if (party.Wounded && party.WoundedPersonId != null)
                    wounded.Add(Tables.Roster.Get(party.WoundedPersonId).Name);

                foreach (var found in party.Found)
                {
                    counts.TryGetValue(found.Key, out var current);
                    counts[found.Key] = current + found.Value;
                }
            }

            lines.Add("Отрядов в лесу: " + _dayReport.Parties.Count);

            if (counts.Count == 0)
            {
                lines.Add("Добыча: пусто");
            }
            else
            {
                foreach (var pair in counts)
                {
                    lines.Add("Склад: " + Tables.Items.Get(pair.Key).Name + " +" + pair.Value);
                }
            }

            lines.Add(wounded.Count == 0
                ? "Все вернулись целыми"
                : "Раненые: " + string.Join(", ", wounded));

            lines.Add("Частокол: " + World.PalisadeHp + "/" + World.PalisadeMax);
            return lines;
        }

        private void ShowPane(string title, List<string> lines, string buttonText, Action action)
        {
            _paneTitle.text = title;
            _paneButton.text = buttonText;
            _paneAction = action;

            _paneLines.Clear();
            foreach (var line in lines)
            {
                var label = new Label(line.Length == 0 ? " " : "· " + line);
                label.AddToClassList("pane-line");
                _paneLines.Add(label);
            }

            _paneLines.scrollOffset = Vector2.zero;
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

        // ---- досье ----

        private void ShowZone(string zoneId)
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
            _zoneCard.scrollOffset = Vector2.zero;
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
            _zoneCard.Add(Text("zone-text", "Жителей: " + Tables.Roster.All.Count));
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
            return Tables.ZoneLore.TryGet(zoneId + "|" + field, out var row) ? row.Text : "—";
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
                + " · гнездо " + (state.Nest.Exists
                    ? state.Nest.Beasts + "/" + Tables.Tuning.GetInt("nest_beasts_cap")
                    : "нет")
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
            if (_toast == null) return;

            _toast.text = text;
            _toast.RemoveFromClassList("hidden");
            _toastHide?.Pause();
            _toastHide = _toast.schedule.Execute(() => _toast.AddToClassList("hidden")).StartingIn(3000);
        }
    }
}
