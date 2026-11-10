using ForestGuild.Core.Sim;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>Мелкие фабрики UI Toolkit: метки с классом и портрет жителя.</summary>
    public static class Ui
    {
        public static Label Text(string className, string value)
        {
            var label = new Label(value);
            if (!string.IsNullOrEmpty(className)) label.AddToClassList(className);
            return label;
        }

        /// <summary>
        /// Состояние человека словами — единственная форма HP, которую видит игрок.
        /// Одно слово на весь интерфейс: карточка боя, доска утра, Сбор, вечер.
        /// </summary>
        public static string HpWord(World world, string personId)
        {
            var condition = world.ConditionOf(personId);
            if (condition == PersonCondition.Dead) return "погиб";
            // Ранение — это выбитый: HP уже 0, даже если состояние пришло отдельно от боя.
            if (condition == PersonCondition.Wounded) return "на ногах не стоит";

            if (!world.People.TryGetValue(personId, out var state)) return "в порядке";
            return Battle.HpWord(state.Hp, state.HpMax);
        }

        public static VisualElement Portrait(string personId, bool small = false)
        {
            var portrait = new VisualElement();
            portrait.AddToClassList("portrait");
            if (small) portrait.AddToClassList("portrait-small");
            portrait.AddToClassList("portrait-" + personId);
            return portrait;
        }
    }
}
