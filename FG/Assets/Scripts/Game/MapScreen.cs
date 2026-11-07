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
        StartOfDay,
        Morning,
        Day,
        Evening,
        Night,
    }

    /// <summary>
    /// Цикл суток (docs/ui.md §3): окно начала дня → экран планирования → день → вечер → ночь.
    /// Здесь только фазы и связка экранов; показ — в отдельных View/Screen-классах.
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
        public StartOfDayScreen StartDay;
        public VillageScreen Village;

        private UIDocument _document;
        private DayFlow _flow;
        private DayPlan _plan = new DayPlan();
        private Rng _rng;
        private Phase _phase = Phase.StartOfDay;
        private DayReport _dayReport;
        private Action _paneAction;

        private HudView _hud;
        private Label _warnings;
        private Label _paneTitle;
        private VisualElement _pane;
        private ScrollView _paneLines;
        private Button _paneButton;
        private VisualElement _leftPanel;
        private VisualElement _bottomBar;
        private VisualElement _modal;
        private Label _modalTitle;
        private VisualElement _modalLines;

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
            Map.ZoneSelected += zoneId => Dossier.Open(zoneId, Tables, World, _phase == Phase.Morning);
            Map.VillageClicked += () => Village.Open(Tables, World, _plan);

            _hud = new HudView(root.Q<Label>("hud"), Tables, World);

            _warnings = root.Q<Label>("warnings");
            _pane = root.Q<VisualElement>("pane");
            _paneTitle = root.Q<Label>("pane-title");
            _paneLines = root.Q<ScrollView>("pane-lines");
            _paneButton = root.Q<Button>("pane-button");
            _paneButton.clicked += () => _paneAction?.Invoke();
            _leftPanel = root.Q<VisualElement>("left-panel");
            _bottomBar = root.Q<VisualElement>("bottom-bar");

            _modal = root.Q<VisualElement>("modal");
            _modalTitle = root.Q<Label>("modal-title");
            _modalLines = root.Q<VisualElement>("modal-lines");
            root.Q<Button>("modal-cancel").clicked += () => _modal.AddToClassList("hidden");
            root.Q<Button>("modal-confirm").clicked += ConfirmOrder;

            Board = new MorningBoard(Tables, World,
                root.Q<VisualElement>("squads"),
                _warnings);
            Board.Active = World.Squads[0];

            Village = new VillageScreen(root);
            Village.Closed += () => Board.Rebuild();

            Muster = new MusterScreen(root);
            Board.EditRequested += squad => Muster.Open(squad, Tables, World, _plan);
            Muster.Closed += () => Board.Rebuild();

            Dossier = new DossierScreen(root);
            Dossier.GatherRequested += zoneId =>
            {
                if (Board.Active == null) Board.Active = World.Squads[0];
                Board.Active.ZoneId = zoneId;
                Board.Rebuild();
            };

            Evening = new EveningScreen(root);
            Evening.Slept += GoToNight;

            StartDay = new StartOfDayScreen(root);
            StartDay.Continued += EnterPlanning;

            root.Q<Button>("sign-button").clicked += OpenOrder;

            Map.RefreshStates(World);
            Board.Rebuild();
            BeginDay();
        }

        // ---- фазы ----

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            var planning = phase == Phase.Morning;

            _leftPanel.EnableInClassList("hidden", !planning);
            _bottomBar.EnableInClassList("hidden", !planning);
            _pane.EnableInClassList("hidden", phase != Phase.Day && phase != Phase.Night);
            _modal.AddToClassList("hidden");

            if (!planning)
            {
                Muster.Close();
                Dossier.Close();
                Village.Close();
            }

            if (phase != Phase.Evening) Evening.Close();
            if (phase != Phase.StartOfDay) StartDay.Close();

            _hud.Update(_phase);
        }

        /// Окно начала дня: номер дня и кто в каком состоянии.
        private void BeginDay()
        {
            SetPhase(Phase.StartOfDay);
            StartDay.Open(World, Tables);
        }

        private void EnterPlanning()
        {
            SetPhase(Phase.Morning);
            Board.Rebuild();
        }

        // ---- приказ ----

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

            if (_plan.Parties.Count == 0) lines.Add("В лес никто не идёт.");

            lines.Add("");
            lines.Add("В ДЕРЕВНЕ: починка " + _plan.VillageWorkers.Count
                + " · постели " + _plan.Beds.Count
                + " · заготовка " + _plan.Foragers.Count);

            _modalTitle.text = "ДЕНЬ " + World.Day + " · ПРИКАЗ";
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
                    label.RegisterCallback<ClickEvent>(_ => Dossier.Open(zoneId, Tables, World, false));
                }

                _paneLines.Add(label);
            }

            if (night.Wave != null)
            {
                var title = new Label("ВОЛНА");
                title.AddToClassList("wave-title");
                _paneLines.Add(title);

                foreach (var line in night.Wave.Lines)
                {
                    var label = new Label("· " + line);
                    label.AddToClassList("wave-line");
                    _paneLines.Add(label);
                }
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

            Map.RefreshStates(World);
            Board.Rebuild();
            if (_warnings != null) _warnings.text = "";

            BeginDay();
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
    }
}
