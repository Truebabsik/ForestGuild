using System;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>Узел зоны: иконка в круге, под ней имя и состояние леса.</summary>
    public sealed class ZoneNodeView : VisualElement
    {
        private readonly VisualElement _icon;
        private readonly Label _state;

        public string ZoneId { get; }
        public event Action<string> Clicked;

        public ZoneNodeView(string zoneId, string title)
        {
            ZoneId = zoneId;
            AddToClassList("zone-node");

            _icon = new VisualElement();
            _icon.AddToClassList("zone-icon");
            _icon.AddToClassList("icon-" + zoneId);
            Add(_icon);

            var name = new Label(title);
            name.AddToClassList("zone-node-name");
            Add(name);

            _state = new Label("");
            _state.AddToClassList("zone-node-state");
            Add(_state);

            RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(ZoneId));
        }

        public void ShowState(int beasts, bool nest)
        {
            if (nest) _state.text = "гнездо";
            else if (beasts == 0) _state.text = "тихо";
            else _state.text = beasts + " твар.";

            EnableInClassList("danger", nest || beasts > 2);
        }
    }
}
