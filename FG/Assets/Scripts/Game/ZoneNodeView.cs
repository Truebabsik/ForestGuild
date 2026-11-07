using System;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>Кружок зоны на карте: имя и состояние леса.</summary>
    public sealed class ZoneNodeView : VisualElement
    {
        private readonly Label _name;
        private readonly Label _state;

        public string ZoneId { get; }
        public event Action<string> Clicked;

        public ZoneNodeView(string zoneId, string title)
        {
            ZoneId = zoneId;
            AddToClassList("zone-node");

            _name = new Label(title);
            _name.AddToClassList("zone-node-name");
            Add(_name);

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
