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
        private readonly VisualElement _tabs;
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
            _tabs = root.Q<VisualElement>("muster-squads");
            _orders = root.Q<VisualElement>("muster-orders");
            _title = root.Q<Label>("muster-title");
            _summary = root.Q<Label>("muster-summary");

            root.Q<Button>("muster-done").clicked += Close;
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

            _title.text = "СБОР ОТРЯДА «" + _squad.Name + "»";

            BuildSquadTabs();
            BuildFree();
            BuildTray();
            BuildOrders();
            BuildSummary();
        }

        /// Оба отряда собираются здесь: переключатель между ними.
        private void BuildSquadTabs()
        {
            _tabs.Clear();

            foreach (var squad in _world.Squads)
            {
                var captured = squad;
                var button = new Button(() =>
                {
                    _squad = captured;
                    Rebuild();
                });

                button.text = "ОТРЯД «" + squad.Name + "» · " + squad.Members.Count
                    + "/" + _tables.Tuning.GetInt("party_max");
                button.AddToClassList("squad-tab");
                if (ReferenceEquals(squad, _squad)) button.AddToClassList("selected");
                _tabs.Add(button);
            }
        }

        private void BuildFree()
        {
            _free.Clear();

            AddGuildBlock("hunter", "ГИЛЬДИЯ ОХОТНИКОВ");
            AddGuildBlock("gatherer", "ГИЛЬДИЯ СОБИРАТЕЛЕЙ");
        }

        /// Блок гильдии: рамка, заголовок и плитки тех, кто свободен.
        private void AddGuildBlock(string professionId, string title)
        {
            var block = new VisualElement();
            block.AddToClassList("guild-block");

            var header = new Label(title);
            header.AddToClassList("guild-title");
            block.Add(header);

            var tiles = new VisualElement();
            tiles.AddToClassList("guild-tiles");
            block.Add(tiles);

            var any = false;
            foreach (var person in _tables.Roster.All)
            {
                if (person.Profession != professionId) continue;
                if (_squad.Members.Contains(person.Id)) continue;

                any = true;
                tiles.Add(MakeTile(person));
            }

            if (!any)
            {
                var empty = new Label("все заняты или в отряде");
                empty.AddToClassList("person-note");
                tiles.Add(empty);
            }

            _free.Add(block);
        }

        /// Плитка свободного человека — того же размера, что и место в лотке.
        private VisualElement MakeTile(Person person)
        {
            var personId = person.Id;
            var condition = _world.ConditionOf(personId);
            var where = Where(personId);
            var busy = where != null;

            var tile = new VisualElement();
            tile.AddToClassList("guild-tile");
            tile.AddToClassList(StatusClass(condition));
            if (busy) tile.AddToClassList("busy");

            tile.Add(Ui.Portrait(personId));

            var info = new VisualElement();
            info.AddToClassList("person-info");

            var name = new Label(person.Name);
            name.AddToClassList("person-name");
            info.Add(name);

            var profession = _tables.Professions.Get(person.Profession);
            var primary = profession.SkillsAllowed[0];
            var skill = new Label(_tables.Skills.Get(primary).Name + " "
                + People.SkillLevel(_tables, person, primary));
            skill.AddToClassList("person-note");
            info.Add(skill);

            info.Add(HpLabel(_tables, _world, personId));

            if (busy)
            {
                var note = new Label(where);
                note.AddToClassList("person-note");
                info.Add(note);
            }
            else
            {
                var traits = Traits(person);
                if (traits.Length > 0)
                {
                    var traitLine = new Label(traits);
                    traitLine.AddToClassList("person-note");
                    info.Add(traitLine);
                }
            }

            tile.Add(info);

            tile.RegisterCallback<ClickEvent>(_ =>
            {
                if (!busy)
                {
                    AddMember(personId);
                    return;
                }

                TakeFromOtherSquad(personId);
            });

            if (!busy) EnableDrag(tile, personId);
            return tile;
        }

        private void BuildTray()
        {
            _tray.Clear();

            var max = _tables.Tuning.GetInt("party_max");
            for (var i = 0; i < max; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("tray-slot");

                if (i < _squad.Members.Count)
                {
                    var personId = _squad.Members[i];
                    var person = _tables.Roster.Get(personId);

                    slot.Add(Ui.Portrait(personId));

                    var info = new VisualElement();
                    info.AddToClassList("person-info");

                    var name = new Label(person.Name);
                    name.AddToClassList("person-name");
                    info.Add(name);

                    var profession = _tables.Professions.Get(person.Profession);
                    var primarySkill = profession.SkillsAllowed[0];
                    var note = new Label(_tables.Skills.Get(primarySkill).Name + " "
                        + People.SkillLevel(_tables, person, primarySkill));
                    note.AddToClassList("person-note");
                    info.Add(note);

                    info.Add(HpLabel(_tables, _world, personId));

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
                        info.Add(role);
                    }

                    slot.Add(info);

                    var captured = personId;
                    var remove = new Button(() =>
                    {
                        if (!_squad.Members.Remove(captured)) return;
                        Rebuild();
                    });
                    remove.text = "×";
                    remove.AddToClassList("tray-remove");
                    slot.Add(remove);
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

        /// В Сборе — только слова о составе, без слотов, процентов и дороги (ui.md §4.3).
        private void BuildSummary()
        {
            var briefing = Briefing.Compute(_world, _tables, _squad.Members, _squad.ZoneId, _squad.Behavior);
            _summary.text = string.Join(" · ", briefing.Warnings);
        }

        // ---- люди ----

        /// <summary>Состояние человека словами: то же слово, что в карточке боя и на доске утра.</summary>
        private static Label HpLabel(Tables tables, World world, string personId)
        {
            var word = Ui.HpWord(tables, world, personId);
            var label = new Label(word);
            label.AddToClassList("person-hp");
            if (word != "в порядке") label.AddToClassList("person-hp-hurt");
            return label;
        }

        private static string StatusClass(PersonCondition condition)        {
            switch (condition)
            {
                case PersonCondition.Wounded: return "status-wounded";
                case PersonCondition.Dead: return "status-dead";
                default: return "status-ok";
            }
        }

        private string SkillLine(Person person)
        {
            var profession = _tables.Professions.Get(person.Profession);
            var parts = new List<string>();

            foreach (var skillId in profession.SkillsAllowed)
            {
                parts.Add(_tables.Skills.Get(skillId).Name + " " + People.SkillLevel(_tables, person, skillId));
            }

            return string.Join(" · ", parts);
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
            return Assignment.Where(_world, _plan, _squad, personId);
        }

        private void AddMember(string personId)
        {
            if (_world.TryAddMember(_tables, _squad, personId)) Rebuild();
        }

        /// Человек из второго отряда: забираем сюда, если есть место.
        private void TakeFromOtherSquad(string personId)
        {
            if (_world.TryTakeFromOtherSquad(_tables, _squad, personId)) Rebuild();
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
