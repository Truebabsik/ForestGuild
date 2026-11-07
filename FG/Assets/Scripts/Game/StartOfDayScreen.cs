using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Начало дня: номер дня и короткий статус по жителям. Дальше — экран планирования.
    /// </summary>
    public sealed class StartOfDayScreen
    {
        private readonly VisualElement _panel;
        private readonly Label _title;
        private readonly VisualElement _lines;

        public event System.Action Continued;

        public StartOfDayScreen(VisualElement root)
        {
            _panel = root.Q<VisualElement>("startday");
            _title = root.Q<Label>("startday-title");
            _lines = root.Q<VisualElement>("startday-lines");

            root.Q<Button>("startday-go").clicked += () =>
            {
                _panel.AddToClassList("hidden");
                Continued?.Invoke();
            };
        }

        public bool IsOpen => _panel != null && !_panel.ClassListContains("hidden");

        public void Open(World world, Tables tables)
        {
            _panel.RemoveFromClassList("hidden");
            _title.text = "ДЕНЬ " + world.Day;

            _lines.Clear();
            _lines.Add(Line(StatusLine(world, tables)));

            foreach (var person in tables.Roster.All)
            {
                _lines.Add(Line(PersonLine(world, person)));
            }
        }

        public void Close()
        {
            _panel.AddToClassList("hidden");
        }

        private string StatusLine(World world, Tables tables)
        {
            var wounded = 0;
            var dead = 0;

            foreach (var person in tables.Roster.All)
            {
                var condition = world.ConditionOf(person.Id);
                if (condition == PersonCondition.Wounded) wounded++;
                if (condition == PersonCondition.Dead) dead++;
            }

            if (wounded == 0 && dead == 0) return "Все живы и здоровы.";
            if (dead == 0) return "Раненых: " + wounded + ". Постелей: " + tables.Tuning.GetInt("beds_count") + ".";
            return "Раненых: " + wounded + ". Погибших: " + dead + ".";
        }

        private string PersonLine(World world, Person person)
        {
            var condition = world.ConditionOf(person.Id);
            if (condition == PersonCondition.Dead) return person.Name + " — погиб";

            var squad = world.SquadOf(person.Id);
            var where = squad != null ? "в отряде «" + squad.Name + "»" : "свободен";

            if (condition == PersonCondition.Wounded) return person.Name + " — ранен, лечится";

            return person.Name + " — здоров, " + where;
        }

        private static Label Line(string text)
        {
            return new Label("· " + text);
        }
    }
}
