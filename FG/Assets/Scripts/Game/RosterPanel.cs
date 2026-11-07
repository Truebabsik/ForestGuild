using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>Список жителей: клик — взять в отряд или отпустить.</summary>
    public sealed class RosterPanel
    {
        private readonly VisualElement _root;
        private readonly Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
        private Tables _tables;

        public readonly List<string> Selected = new List<string>();

        public RosterPanel(VisualElement root)
        {
            _root = root;
        }

        public void Build(Tables tables)
        {
            _tables = tables;
            _root.Clear();
            _buttons.Clear();
            Selected.Clear();

            foreach (var person in tables.Roster.All)
            {
                var id = person.Id;
                var button = new Button { text = Line(person) };
                button.clicked += () => Toggle(id);
                _buttons[id] = button;
                _root.Add(button);
            }
        }

        public void Clear()
        {
            Selected.Clear();
            foreach (var pair in _buttons) pair.Value.RemoveFromClassList("selected");
        }

        /// Занятые люди недоступны: один человек — одно дело в день.
        public void SetBusy(Func<string, bool> isBusy)
        {
            foreach (var pair in _buttons)
            {
                var busy = isBusy(pair.Key);
                pair.Value.EnableInClassList("busy", busy);
                pair.Value.SetEnabled(!busy);
                if (busy) Selected.Remove(pair.Key);
            }
        }

        private string Line(Person person)
        {
            var profession = _tables.Professions.Get(person.Profession);
            var mainSkill = profession.SkillsAllowed.Length > 0 ? profession.SkillsAllowed[0] : null;
            var level = mainSkill != null ? People.SkillLevel(_tables, person, mainSkill) : 0;
            return person.Name + " · " + profession.Name + " " + level;
        }

        private void Toggle(string personId)
        {
            var button = _buttons[personId];
            if (Selected.Remove(personId))
            {
                button.RemoveFromClassList("selected");
                return;
            }

            Selected.Add(personId);
            button.AddToClassList("selected");
        }
    }
}
