using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;
using ForestGuild.Core.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForestGuild.Game
{
    /// <summary>
    /// Сбор отряда (ui.md §4.3): отдельный экран, свободные люди слева, лоток справа.
    /// Драг — основной жест, клик-клик — обязательный запасной путь.
    /// </summary>
    public sealed class MusterScreen
    {
        private readonly VisualElement _root;
        private readonly VisualElement _panel;
        private readonly VisualElement _free;
        private readonly VisualElement _tray;
        private readonly VisualElement _orders;
        private readonly Label _title;
        private readonly Label _summary;

        private Tables _tables;
        private World _world;
        private DayPlan _plan;
        private Squad _squad;

        private string _dragId;
        private VisualElement _ghost;

        public event Action Closed;

        public MusterScreen(VisualElement root)
        {
            _root = root;
            _panel = root.Q<VisualElement>("muster");
            _free = root.Q<VisualElement>("muster-free");
            _tray = root.Q<VisualElement>("muster-tray");
            _orders = root.Q<VisualElement>("muster-orders");
            _title = root.Q<Label>("muster-title");
            _summary = root.Q<Label>("muster-summary");

            root.Q<Button>("muster-done").clicked += Close;
            root.Q<Button>("muster-clear").clicked += () =>
            {
                _squad?.Members.Clear();
                Rebuild();
            };
        }

        public bool IsOpen => _panel != null && !_panel.ClassListContains("hidden");

        public void Open(Squad squad, Tables tables, World world, DayPlan plan)
        {
            _squad = squad;
            _tables = tables;
            _world = world;
            _plan = plan;

            _panel.RemoveFromClassList("hidden");
            Rebuild();
        }

        public void Close()
        {
            EndDrag();
            _panel.AddToClassList("hidden");
            Closed?.Invoke();
        }

        // ---- сборка экрана ----

        private void Rebuild()
        {
            if (_squad == null) return;

            var zone = string.IsNullOrEmpty(_squad.ZoneId) ? "зона не выбрана" : _tables.Zones.Get(_squad.ZoneId).Name;
            _title.text = "СБОР ОТРЯДА «" + _squad.Name + "» · " + zone;

            BuildFree();
            BuildTray();
            BuildOrders();
            BuildSummary();
        }

        private void BuildFree()
        {
            _free.Clear();

            foreach (var person in _tables.Roster.All)
            {
                if (_squad.Members.Contains(person.Id)) continue;

                var where = Where(person.Id);
                var busy = where != null;

                var card = MakePersonCard(person, busy ? Note(person, where) : Note(person, null));
                if (busy) card.AddToClassList("busy");

                var id = person.Id;
                card.RegisterCallback<ClickEvent>(_ =>
                {
                    if (busy) return;
                    AddMember(id);
                });

                if (!busy) EnableDrag(card, id);
                _free.Add(card);
            }
        }

        private void BuildTray()
        {
            _tray.Clear();

            for (var i = 0; i < 3; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("tray-slot");

                if (i < _squad.Members.Count)
                {
                    var personId = _squad.Members[i];
                    var person = _tables.Roster.Get(personId);

                    var name = new Label(person.Name);
                    name.AddToClassList("person-name");
                    slot.Add(name);

                    var profession = _tables.Professions.Get(person.Profession);
                    var skill = profession.SkillsAllowed[0];
                    var note = new Label(profession.Name + " "
                        + People.SkillLevel(_tables, person, skill)
                        + " · " + Traits(person));
                    note.AddToClassList("person-note");
                    slot.Add(note);

                    if (profession.Id == "hunter")
                    {
                        var role = new Button(() =>
                        {
                            var current = _squad.RoleOf(personId);
                            _squad.SetRole(personId, current == HunterRole.Guard ? HunterRole.Gather
                                : current == HunterRole.Gather ? HunterRole.Track
                                : HunterRole.Guard);
                            Rebuild();
                        });
                        role.text = RoleName(_squad.RoleOf(personId));
                        role.AddToClassList("tray-role");
                        slot.Add(role);
                    }

                    var captured = personId;
                    slot.RegisterCallback<ClickEvent>(_ =>
                    {
                        if (_squad.Members.Remove(captured)) Rebuild();
                    });
                }
                else
                {
                    var empty = new Label("свободно");
                    empty.AddToClassList("person-note");
                    slot.Add(empty);
                }

                _tray.Add(slot);
            }
        }

        private void BuildOrders()
        {
            _orders.Clear();
            if (_squad == null) return;

            AddOrder("встреча", EncounterName(_squad.Orders.Encounter), () =>
            {
                _squad.Orders.Encounter = Next(_squad.Orders.Encounter);
            });

            AddOrder("туша", CarcassName(_squad.Orders.Carcass), () =>
            {
                _squad.Orders.Carcass = Next(_squad.Orders.Carcass);
            });

            AddOrder("раненый", WoundedName(_squad.Orders.Wounded), () =>
            {
                _squad.Orders.Wounded = Next(_squad.Orders.Wounded);
            });

            AddOrder("мешок", BagName(_squad.Orders.BagFull), () =>
            {
                _squad.Orders.BagFull = Next(_squad.Orders.BagFull);
            });
        }

        private void AddOrder(string title, string value, Action next)
        {
            var button = new Button(() =>
            {
                next();
                Rebuild();
            });
            button.text = title + ": " + value;
            button.AddToClassList("order-button");
            _orders.Add(button);
        }

        private void BuildSummary()
        {
            var briefing = Briefing.Compute(_world, _tables, _squad.Members, _squad.ZoneId, _squad.Behavior);

            var text = "дорога " + briefing.TravelHours.ToString("0.#") + " ч → работа " + briefing.WorkHours
                + " ч · слоты " + briefing.Slots
                + " · встречи ~" + Math.Round(briefing.EncounterPerHour * 100) + "%/час";

            if (briefing.Warnings.Count > 0) text += "   ⚠ " + string.Join(", ", briefing.Warnings);
            _summary.text = text;
        }

        // ---- люди ----

        private VisualElement MakePersonCard(Person person, string note)
        {
            var card = new VisualElement();
            card.AddToClassList("person-card");

            var name = new Label(person.Name + " · " + _tables.Professions.Get(person.Profession).Name);
            name.AddToClassList("person-name");
            card.Add(name);

            var noteLabel = new Label(note);
            noteLabel.AddToClassList("person-note");
            card.Add(noteLabel);

            return card;
        }

        private string Note(Person person, string where)
        {
            if (where != null) return where;

            var profession = _tables.Professions.Get(person.Profession);
            var level = People.SkillLevel(_tables, person, profession.SkillsAllowed[0]);
            return "навык " + level + " · " + Traits(person);
        }

        private string Traits(Person person)
        {
            if (person.Traits.Length == 0) return "без черт";

            var names = new List<string>();
            foreach (var traitId in person.Traits)
            {
                if (_tables.Traits.TryGet(traitId, out var trait)) names.Add(trait.Name);
            }

            return string.Join(", ", names);
        }

        /// Где человек занят: в другом отряде, на работе, на постели, ранен, мёртв.
        private string Where(string personId)
        {
            var condition = _world.ConditionOf(personId);
            if (condition == PersonCondition.Dead) return "погиб";
            if (condition == PersonCondition.Wounded) return "ранен, лечится";

            foreach (var squad in _world.Squads)
            {
                if (ReferenceEquals(squad, _squad)) continue;
                if (squad.Members.Contains(personId)) return "в отряде «" + squad.Name + "»";
            }

            if (_plan.VillageWorkers.Contains(personId)) return "на частоколе";
            if (_plan.Beds.Contains(personId)) return "на постели";
            if (_plan.Foragers.Contains(personId)) return "на заготовке";

            return null;
        }

        private void AddMember(string personId)
        {
            if (_squad.Members.Contains(personId)) return;
            if (_squad.Members.Count >= 3) return;
            if (_world.ConditionOf(personId) != PersonCondition.Healthy) return;

            _squad.Members.Add(personId);
            Rebuild();
        }

        // ---- драг ----

        private void EnableDrag(VisualElement card, string personId)
        {
            card.RegisterCallback<PointerDownEvent>(e =>
            {
                _dragId = personId;
                _ghost = new Label(_tables.Roster.Get(personId).Name);
                _ghost.AddToClassList("drag-ghost");
                _root.Add(_ghost);
                MoveGhost(e.position);

                card.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            card.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (_dragId != personId) return;
                MoveGhost(e.position);
                _tray.EnableInClassList("accepting", _tray.worldBound.Contains(e.position));
            });

            card.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_dragId != personId) return;

                var overTray = _tray.worldBound.Contains(e.position);
                EndDrag();
                if (overTray) AddMember(personId);

                card.ReleasePointer(e.pointerId);
            });
        }

        private void MoveGhost(Vector2 panelPosition)
        {
            if (_ghost == null) return;
            _ghost.style.left = panelPosition.x + 12;
            _ghost.style.top = panelPosition.y - 10;
        }

        private void EndDrag()
        {
            _dragId = null;
            _tray?.EnableInClassList("accepting", false);

            if (_ghost != null)
            {
                _ghost.RemoveFromHierarchy();
                _ghost = null;
            }
        }

        // ---- названия ----

        private static string RoleName(HunterRole role)
        {
            switch (role)
            {
                case HunterRole.Gather: return "собираю";
                case HunterRole.Track: return "иду по следу";
                default: return "охраняю";
            }
        }

        private static EncounterOrder Next(EncounterOrder value)
        {
            return value == EncounterOrder.Ask ? EncounterOrder.Hide
                : value == EncounterOrder.Hide ? EncounterOrder.Retreat
                : value == EncounterOrder.Retreat ? EncounterOrder.Fight
                : EncounterOrder.Ask;
        }

        private static CarcassOrder Next(CarcassOrder value)
        {
            return value == CarcassOrder.Ask ? CarcassOrder.Haul
                : value == CarcassOrder.Haul ? CarcassOrder.Butcher
                : value == CarcassOrder.Butcher ? CarcassOrder.Leave
                : CarcassOrder.Ask;
        }

        private static WoundedOrder Next(WoundedOrder value)
        {
            return value == WoundedOrder.Ask ? WoundedOrder.Carry
                : value == WoundedOrder.Carry ? WoundedOrder.Leave
                : WoundedOrder.Ask;
        }

        private static BagOrder Next(BagOrder value)
        {
            return value == BagOrder.Ask ? BagOrder.ReplaceCheapest
                : value == BagOrder.ReplaceCheapest ? BagOrder.Skip
                : BagOrder.Ask;
        }

        private static string EncounterName(EncounterOrder value)
        {
            switch (value)
            {
                case EncounterOrder.Hide: return "прятаться";
                case EncounterOrder.Retreat: return "отойти";
                case EncounterOrder.Fight: return "бой";
                default: return "спрашивать";
            }
        }

        private static string CarcassName(CarcassOrder value)
        {
            switch (value)
            {
                case CarcassOrder.Haul: return "утащить";
                case CarcassOrder.Butcher: return "освежевать";
                case CarcassOrder.Leave: return "бросить";
                default: return "спрашивать";
            }
        }

        private static string WoundedName(WoundedOrder value)
        {
            switch (value)
            {
                case WoundedOrder.Carry: return "нести";
                case WoundedOrder.Leave: return "оставить";
                default: return "спрашивать";
            }
        }

        private static string BagName(BagOrder value)
        {
            switch (value)
            {
                case BagOrder.ReplaceCheapest: return "заменить";
                case BagOrder.Skip: return "не брать";
                default: return "спрашивать";
            }
        }
    }
}
