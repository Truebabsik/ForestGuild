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
        public ExpeditionScreen Expedition;

        private UIDocument _document;
        private DayFlow _flow;
        private DayPlan _plan = new DayPlan();
        private Rng _rng;
        private Phase _phase = Phase.StartOfDay;
        private DayReport _dayReport;
        private Action _paneAction;

        private HudView _hud;
        private Label _hudLabel;
        private VisualElement _squadsElement;
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
        private VisualElement _menu;
        private VisualElement _menuItems;
        private bool _scenarioMuster;
        private Episode _episode;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            var root = _document.rootVisualElement;

            try
            {
                var dataDir = System.IO.Path.Combine(Application.streamingAssetsPath, "Data");
                Tables = DataLoader.Load(dataDir);
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

            _hudLabel = root.Q<Label>("hud");
            _squadsElement = root.Q<VisualElement>("squads");

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

            Board = null;
            Village = new VillageScreen(root);
            Village.Closed += () => Board?.Rebuild();

            Muster = new MusterScreen(root);
            Muster.Closed += () =>
            {
                Board?.Rebuild();

                if (_scenarioMuster)
                {
                    _scenarioMuster = false;
                    ShowMenu();
                }
            };

            Dossier = new DossierScreen(root);
            Dossier.GatherRequested += zoneId =>
            {
                if (Board == null) return;
                if (Board.Active == null) Board.Active = World.Squads[0];
                Board.Active.ZoneId = zoneId;
                Board.Rebuild();
            };
            Evening = new EveningScreen(root);
            Evening.Slept += GoToNight;

            Expedition = new ExpeditionScreen(root);
            Expedition.Changed += () => _hud?.Update(_phase);
            Expedition.Finished += results =>
            {
                if (_episode != null && _episode.EndsInMenu)
                {
                    ShowMenu();
                    return;
                }

                _flow.FinishExpeditions(_dayReport, results);
                GoToEvening();
            };

            StartDay = new StartOfDayScreen(root);
            StartDay.Continued += EnterPlanning;

            root.Q<Button>("sign-button").clicked += OpenOrder;

            _menu = root.Q<VisualElement>("menu");
            _menuItems = root.Q<VisualElement>("menu-items");
            BuildMenu();
            ShowMenu();
        }

        // ---- стартовое меню ----

        /// Меню — список сценариев из CSV: добавить эпизод = добавить строку, без кода.
        private void BuildMenu()
        {
            _menuItems.Clear();

            foreach (var scenario in EpisodeBuilder.All(Tables))
            {
                var id = scenario.Id;
                var button = new Button(() => StartScenario(id));
                button.text = scenario.Title;
                button.AddToClassList("action");
                button.AddToClassList("menu-button");
                if (scenario.StartScreen == "campaign") button.AddToClassList("primary");
                _menuItems.Add(button);
            }
        }

        private void ShowMenu()
        {
            _menu.RemoveFromClassList("hidden");
            _leftPanel.AddToClassList("hidden");
            _bottomBar.AddToClassList("hidden");
            _pane.AddToClassList("hidden");
            StartDay.Close();
            Evening.Close();
            Expedition?.Close();
            Muster.Close();
            Village.Close();
            Dossier.Close();
            _scenarioMuster = false;
        }

        /// Любой запуск мира — это эпизод по сценарию: игра и тесты идут одним путём.
        private void StartScenario(string id)
        {
            _episode = EpisodeBuilder.Build(Tables, id);
            World = _episode.World;
            _rng = _episode.Rng;
            _flow = new DayFlow(World, Tables, _rng);
            _plan = new DayPlan();
            _dayReport = null;

            CreateBoardAndHud();
            Map.RefreshStates(World);

            _menu.AddToClassList("hidden");

            switch (_episode.Scenario.StartScreen)
            {
                case "muster":
                    SetPhase(Phase.Morning);
                    _leftPanel.AddToClassList("hidden");
                    _bottomBar.AddToClassList("hidden");
                    _scenarioMuster = true;
                    Muster.Open(World.Squads[0], Tables, World, _plan);
                    break;

                case "expedition":
                    SetPhase(Phase.Day);
                    _leftPanel.AddToClassList("hidden");
                    _bottomBar.AddToClassList("hidden");
                    BuildParties(World, _plan);
                    Expedition.Open(Tables, World, _plan, _rng);
                    break;

                default:
                    BeginDay();
                    break;
            }
        }

        private void CreateBoardAndHud()
        {
            _hud = new HudView(_hudLabel, Tables, World);
            Board = new MorningBoard(Tables, World, _squadsElement, _warnings);
            Board.Active = World.Squads[0];
            Board.EditRequested += squad => Muster.Open(squad, Tables, World, _plan);
            Board.Rebuild();
        }

        // ---- фазы ----

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            var planning = phase == Phase.Morning;

            _leftPanel.EnableInClassList("hidden", !planning);
            _bottomBar.EnableInClassList("hidden", !planning);
            _pane.EnableInClassList("hidden", phase != Phase.Night);
            _modal.AddToClassList("hidden");
            if (phase != Phase.Day) Expedition?.Close();

            if (!planning)
            {
                Muster.Close();
                Dossier.Close();
                Village.Close();
            }

            if (phase != Phase.Evening) Evening.Close();
            if (phase != Phase.StartOfDay) StartDay.Close();

            _hud?.Update(_phase);
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
            _modalLines.Clear();
            BuildParties();

            foreach (var squad in World.Squads)
            {
                if (squad.Members.Count == 0 || string.IsNullOrEmpty(squad.ZoneId)) continue;

                var zone = Tables.Zones.Get(squad.ZoneId);
                var briefing = Briefing.Compute(World, Tables, squad.Members, squad.ZoneId, squad.Behavior);

                AddModalLine("ОТРЯД «" + squad.Name + "» → " + zone.Name + " · " + BehaviorName(squad.Behavior));

                var portraits = new VisualElement();
                portraits.AddToClassList("modal-portraits");
                foreach (var personId in squad.Members) portraits.Add(Ui.Portrait(personId));
                _modalLines.Add(portraits);

                AddModalLine("   " + string.Join(" · ", MemberNames(squad)));
                AddModalLine("   дорога " + briefing.TravelMinutes + " мин"
                    + (briefing.WorkMinutes > 0
                        ? ", в лесу " + briefing.WorkMinutes + " мин"
                        : ", работы не будет — день уйдёт на дорогу"));

                foreach (var warning in briefing.Warnings) AddModalLine("   ⚠ " + warning, "warning-line");
            }

            if (_plan.Parties.Count == 0) AddModalLine("В лес никто не идёт.");

            AddModalLine("");
            AddModalLine("В ДЕРЕВНЕ: починка " + _plan.VillageWorkers.Count
                + " · постели " + _plan.Beds.Count
                + " · заготовка " + _plan.Foragers.Count);

            _modalTitle.text = "ДЕНЬ " + World.Day + " · ПРИКАЗ";
            _modal.RemoveFromClassList("hidden");
        }

        /// Отряды-пресеты → партии дня. Одно место, откуда берут и приказ, и симуляция похода.
        private void BuildParties()
        {
            BuildParties(World, _plan);
        }

        private static void BuildParties(World world, DayPlan plan)
        {
            plan.Parties.Clear();

            foreach (var squad in world.Squads)
            {
                if (squad.Members.Count == 0 || string.IsNullOrEmpty(squad.ZoneId)) continue;

                var party = new PartyAssignment
                {
                    ZoneId = squad.ZoneId,
                    Name = squad.Name,
                    Behavior = squad.Behavior,
                    Orders = squad.Orders,
                };
                party.PersonIds.AddRange(squad.Members);
                plan.Parties.Add(party);
            }
        }

        private void AddModalLine(string text, string extraClass = null)
        {
            var label = new Label(text);
            label.AddToClassList("pane-line");
            if (extraClass != null) label.AddToClassList(extraClass);
            _modalLines.Add(label);
        }

        private void ConfirmOrder()
        {
            _modal.AddToClassList("hidden");

            _dayReport = _flow.BeginDay(_plan);

            SetPhase(Phase.Day);
            Expedition.Open(Tables, World, _plan, _rng);
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

            World.RemoveUnavailableFromSquads();

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
