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
