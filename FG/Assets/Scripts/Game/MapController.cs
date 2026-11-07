using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Карта из данных: узлы по map_x/map_y, рёбра по neighbours.
    /// Координаты нормализованы 0..1, поэтому карта переставляется правкой CSV.
    /// </summary>
    public sealed class MapController
    {
        private const float NodeSize = 104f;

        private readonly VisualElement _map;
        private readonly Dictionary<string, ZoneNodeView> _nodes = new Dictionary<string, ZoneNodeView>();
        private readonly List<EdgeLine> _edges = new List<EdgeLine>();
        private readonly Dictionary<string, Vector2> _positions = new Dictionary<string, Vector2>();
        private Tables _tables;

        public string Selected { get; private set; }
        public event Action<string> ZoneSelected;

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

            RefreshStates(null);
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
                node.style.top = y - NodeSize * 0.5f;
            }

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
        }

        private void Select(string zoneId)
        {
            foreach (var pair in _nodes) pair.Value.RemoveFromClassList("selected");
            if (_nodes.TryGetValue(zoneId, out var node)) node.AddToClassList("selected");
            Selected = zoneId;
        }
    }
}
