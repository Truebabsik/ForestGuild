using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Карта из данных: узлы по map_x/map_y, рёбра по neighbours, деревня у своих ворот.
    /// Координаты нормализованы 0..1, поэтому карта переставляется правкой CSV.
    /// </summary>
    public sealed class MapController
    {
        private const float NodeSize = 120f;
        private const float NodeIconHalf = 48f;
        private const float VillageSize = 130f;
        private const float VillageHalf = 65f;

        private readonly VisualElement _map;
        private readonly Dictionary<string, ZoneNodeView> _nodes = new Dictionary<string, ZoneNodeView>();
        private readonly List<EdgeLine> _edges = new List<EdgeLine>();
        private readonly Dictionary<string, Vector2> _positions = new Dictionary<string, Vector2>();
        private Tables _tables;

        private VisualElement _villageNode;
        private Label _villageState;
        private EdgeLine _villageEdge;
        private Vector2 _villagePosition;

        public string Selected { get; private set; }
        public bool VillageSelected { get; private set; }

        public event Action<string> ZoneSelected;
        public event Action VillageClicked;

        public MapController(VisualElement map)
        {
            _map = map;
            _map.RegisterCallback<GeometryChangedEvent>(_ => Layout());
        }

        public void Build(Tables tables)
        {
            _tables = tables;
            _map.Clear();
            _nodes.Clear();
            _edges.Clear();
            _positions.Clear();
            VillageSelected = false;

            _villageNode = new VisualElement();
            _villageNode.AddToClassList("village-node");
            var villageIcon = new VisualElement();
            villageIcon.AddToClassList("village-icon");
            _villageNode.Add(villageIcon);
            var villageName = new Label("Деревня");
            villageName.AddToClassList("village-name");
            _villageNode.Add(villageName);
            _villageState = new Label("");
            _villageState.AddToClassList("village-state");
            _villageNode.Add(_villageState);
            _villageNode.RegisterCallback<ClickEvent>(_ =>
            {
                SelectVillage();
                VillageClicked?.Invoke();
            });
            _map.Add(_villageNode);

            foreach (var zone in tables.Zones.All)
            {
                var node = new ZoneNodeView(zone.Id, zone.Name);
                node.Clicked += id =>
                {
                    Select(id);
                    ZoneSelected?.Invoke(id);
                };
                _nodes[zone.Id] = node;
                _map.Add(node);
            }

            foreach (var zone in tables.Zones.All)
            {
                foreach (var neighbour in zone.Neighbours)
                {
                    if (string.CompareOrdinal(zone.Id, neighbour) >= 0) continue;
                    var line = new EdgeLine();
                    _edges.Add(line);
                    _map.Insert(0, line);
                }
            }

            _villageEdge = new EdgeLine();
            _map.Insert(0, _villageEdge);

            Layout();
        }

        public void RefreshStates(World world)
        {
            if (_tables == null) return;

            foreach (var zone in _tables.Zones.All)
            {
                var beasts = world != null ? world.BeastCount(zone.Id) : 0;
                var nest = world != null && world.Zones[zone.Id].Nest.Exists;
                _nodes[zone.Id].ShowState(beasts, nest);
            }

            if (world != null) _villageState.text = world.PalisadeHp + "/" + world.PalisadeMax;
        }

        private void Layout()
        {
            if (_tables == null) return;

            var width = _map.resolvedStyle.width;
            var height = _map.resolvedStyle.height;
            if (float.IsNaN(width) || float.IsNaN(height) || width < 10f || height < 10f) return;

            foreach (var zone in _tables.Zones.All)
            {
                var x = zone.MapX * width;
                var y = zone.MapY * height;
                _positions[zone.Id] = new Vector2(x, y);

                var node = _nodes[zone.Id];
                node.style.left = x - NodeSize * 0.5f;
                node.style.top = y - NodeIconHalf;
            }

            _villagePosition = new Vector2(
                _tables.Tuning.GetFloat("village_map_x") * width,
                _tables.Tuning.GetFloat("village_map_y") * height);

            _villageNode.style.left = _villagePosition.x - VillageHalf;
            _villageNode.style.top = _villagePosition.y - NodeIconHalf;

            var edgeIndex = 0;
            foreach (var zone in _tables.Zones.All)
            {
                foreach (var neighbour in zone.Neighbours)
                {
                    if (string.CompareOrdinal(zone.Id, neighbour) >= 0) continue;
                    if (!_positions.TryGetValue(zone.Id, out var a)) continue;
                    if (!_positions.TryGetValue(neighbour, out var b)) continue;

                    _edges[edgeIndex].A = a;
                    _edges[edgeIndex].B = b;
                    _edges[edgeIndex].MarkDirtyRepaint();
                    edgeIndex++;
                }
            }

            if (_positions.TryGetValue(_tables.Tuning.Get("village_zone"), out var gate))
            {
                _villageEdge.A = _villagePosition;
                _villageEdge.B = gate;
                _villageEdge.MarkDirtyRepaint();
            }
        }

        private void Select(string zoneId)
        {
            foreach (var pair in _nodes) pair.Value.RemoveFromClassList("selected");
            if (_villageNode != null) _villageNode.RemoveFromClassList("selected");
            VillageSelected = false;

            if (_nodes.TryGetValue(zoneId, out var node)) node.AddToClassList("selected");
            Selected = zoneId;
        }

        private void SelectVillage()
        {
            foreach (var pair in _nodes) pair.Value.RemoveFromClassList("selected");
            if (_villageNode != null) _villageNode.AddToClassList("selected");
            Selected = null;
            VillageSelected = true;
        }
    }
}
