using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Экран дня: карта, ростер, панель зоны, кнопка «конец дня».
    /// Вся логика — в ядре; здесь только показ и ввод.
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

        private Label _zoneName;
        private Label _zoneInfo;
        private Label _zoneBeasts;
        private Label _zoneParties;
        private Label _dayLabel;
        private Label _queueLabel;
        private Label _reportTitle;
        private VisualElement _report;
        private VisualElement _reportLines;

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

            _zoneName = root.Q<Label>("zone-name");
            _zoneInfo = root.Q<Label>("zone-info");
            _zoneBeasts = root.Q<Label>("zone-beasts");
            _zoneParties = root.Q<Label>("zone-parties");
            _dayLabel = root.Q<Label>("day-label");
            _queueLabel = root.Q<Label>("queue-label");
            _report = root.Q<VisualElement>("report");
            _reportTitle = root.Q<Label>("report-title");
            _reportLines = root.Q<VisualElement>("report-lines");

            _roster = new RosterPanel(root.Q<VisualElement>("roster"));
            _roster.Build(Tables);

            root.Q<Button>("send-button").clicked += SendParty;
            root.Q<Button>("end-day").clicked += EndDay;
            root.Q<Button>("report-close").clicked += () => _report.AddToClassList("hidden");

            Map.RefreshStates(World);
            _dayLabel.text = "День " + World.Day;
            RefreshQueue();
        }

        private void SendParty()
        {
            if (Map.Selected == null)
            {
                ShowMessage("Сначала выберите зону на карте");
                return;
            }

            if (_roster.Selected.Count == 0)
            {
                ShowMessage("Выберите хотя бы одного жителя");
                return;
            }

            var party = new PartyAssignment { ZoneId = Map.Selected };
            party.PersonIds.AddRange(_roster.Selected);
            _plan.Parties.Add(party);

            var zone = Tables.Zones.Get(Map.Selected);
            ShowMessage("Отряд из " + party.PersonIds.Count + " идёт в " + zone.Name);
            _roster.Clear();
            RefreshQueue();
        }

        private void EndDay()
        {
            var flow = new DayFlow(World, Tables, _rng);
            var report = flow.RunDay(_plan);
            _plan = new DayPlan();

            _dayLabel.text = "День " + World.Day;
            Map.RefreshStates(World);
            if (Map.Selected != null) ShowZone(Map.Selected);
            RefreshQueue();

            ShowReport(report);
        }

        private void ShowZone(string zoneId)
        {
            var zone = Tables.Zones.Get(zoneId);
            var state = World.Zones[zoneId];

            _zoneName.text = zone.Name;
            _zoneInfo.text = "Глубина: " + zone.Depth + "\n"
                + "Дорога: " + zone.TravelHours.ToString("0.#") + " ч в одну сторону\n"
                + "Кристаллы: " + state.Crystals + " · корм: " + state.Forage;

            if (state.Beasts.Count == 0)
            {
                _zoneBeasts.text = "Тварей не видно";
            }
            else
            {
                var lines = new List<string>();
                foreach (var pair in state.Beasts)
                {
                    lines.Add(Tables.Beasts.Get(pair.Key).Name + ": " + pair.Value);
                }
                _zoneBeasts.text = string.Join("\n", lines);
            }

            var planned = 0;
            foreach (var party in _plan.Parties)
            {
                if (party.ZoneId == zoneId) planned++;
            }

            _zoneParties.text = planned > 0
                ? "Отрядов отправлено: " + planned
                : "Отряд не отправлен";
        }

        private void RefreshQueue()
        {
            var total = 0;
            foreach (var party in _plan.Parties) total += party.PersonIds.Count;

            _queueLabel.text = _plan.Parties.Count == 0
                ? "План: в лес никто не идёт"
                : "План: " + _plan.Parties.Count + " отряд(ов), " + total + " человек(а)";
        }

        private void ShowMessage(string text)
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
