using System;
using System.Collections.Generic;
using System.Linq;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    public enum DecisionKind { Encounter, Carcass, Wounded, BagFull }

    public sealed class ExpeditionOption
    {
        public string Id;
        public string Label;
        public double Chance;
        public string Note;
    }

    public sealed class ExpeditionDecision
    {
        public DecisionKind Kind;
        public string Title;
        public string Body;

        /// <summary>Кого касается решение (для раненого).</summary>
        public string PersonId;

        public readonly List<ExpeditionOption> Options = new List<ExpeditionOption>();

        public ExpeditionOption Option(string id)
        {
            foreach (var option in Options)
            {
                if (option.Id == id) return option;
            }

            return null;
        }
    }

    /// <summary>
    /// Поход по часам (ui.md §5): каждый час — находка и встреча, на развилках — пауза.
    /// Наказ отряда (StandingOrders) снимает паузу: развилку решает политика, а не игрок.
    /// </summary>
    public sealed class Expedition
    {
        public readonly PartyAssignment Party;
        public readonly ExpeditionResult Result;

        private readonly World _world;
        private readonly Tables _tables;
        private readonly Rng _rng;
        private readonly ZoneState _state;
        private readonly Zone _zone;
        private readonly int _workHours;

        private float _freeSlots;
        private int _hour;
        private bool _leaving;

        public ExpeditionDecision Pending { get; private set; }
        public bool Finished { get; private set; }
        public int Hour => _hour;
        public int WorkHours => _workHours;

        public Expedition(World world, Tables tables, PartyAssignment party, Rng rng)
        {
            _world = world;
            _tables = tables;
            _rng = rng;
            Party = party;

            _zone = tables.Zones.Get(party.ZoneId);
            _state = world.Zones[party.ZoneId];
            _state.Scouted = true;

            var tuning = tables.Tuning;
            _workHours = Math.Max(0, tuning.GetInt("hours_per_day") - (int)Math.Ceiling(_zone.TravelHours * 2));

            Result = new ExpeditionResult
            {
                ZoneId = party.ZoneId,
                Stamina = party.PersonIds.Count * tuning.GetInt("stamina_per_person"),
            };

            foreach (var personId in party.PersonIds)
            {
                var person = tables.Roster.Get(personId);
                _freeSlots += tuning.GetInt("slots_per_person")
                    + People.TraitSum(tables, person, trait => trait.CarryBonus);
            }

            Result.Log.Add(_zone.Name + ": отряд вышел из деревни");
        }

        public bool HasDecision => Pending != null;

        /// Один час. Возвращает false, если поход закончился.
        public bool Advance()
        {
            if (Finished) return false;
            if (Pending != null) return true;

            if (_workHours <= 0)
            {
                Finish("дорога съедает весь день, работы не было");
                return false;
            }

            if (_hour >= _workHours || _leaving)
            {
                Finish("отряд повернул домой");
                return false;
            }

            if (Result.Stamina <= 0)
            {
                _leaving = true;
                Result.Log.Add(_zone.Name + ": отряд выбился из сил");
                return true;
            }

            _hour++;
            Result.HoursUsed = _hour;
            Result.Stamina--;

            Find();
            if (Pending != null) return true;

            Meet();
            return true;
        }

        /// Игрок (или наказ) выбирает вариант. null — нечего решать.
        public void Decide(string optionId)
        {
            if (Pending == null) return;

            var decision = Pending;
            Pending = null;

            var option = decision.Option(optionId) ?? decision.Options[0];
            var success = _rng.Chance(option.Chance);
            Resolve(decision, option, success);
        }

        // ---- шаги ----

        private void Find()
        {
            foreach (var personId in Party.PersonIds)
            {
                var person = _tables.Roster.Get(personId);
                var skill = People.SkillLevel(_tables, person, "gathering");
                var chance = _tables.Tuning.GetFloat("find_base")
                    + _tables.Tuning.GetFloat("find_per_skill") * skill
                    + People.TraitSumDouble(_tables, person, trait => trait.FindBonus);

                if (!_rng.Chance(chance)) continue;

                var itemId = PickResource();
                if (itemId == null) continue;

                var item = _tables.Items.Get(itemId);

                if (_freeSlots < item.Slots)
                {
                    if (!TryMakeRoom(item))
                    {
                        Result.Log.Add(person.Name + ": мешок полон, " + item.Name + " брошено");
                        continue;
                    }
                }

                _freeSlots -= item.Slots;
                AddFound(itemId);
                Result.Log.Add(person.Name + " нашёл: " + item.Name);
            }
        }

        /// Наказ «мешок»: если велено менять, выбрасываем самую дешёвую находку ради новой.
        private bool TryMakeRoom(Item incoming)
        {
            var orders = Party.Orders ?? new StandingOrders();
            if (orders.BagFull != BagOrder.ReplaceCheapest) return false;

            var worstIndex = -1;
            var worstValue = int.MaxValue;

            for (var i = 0; i < Result.Found.Count; i++)
            {
                var value = _tables.Items.Get(Result.Found[i].Key).Value;
                if (value >= worstValue) continue;
                worstValue = value;
                worstIndex = i;
            }

            if (worstIndex < 0 || worstValue >= incoming.Value) return false;

            var droppedId = Result.Found[worstIndex].Key;
            if (Result.Found[worstIndex].Value > 1)
                Result.Found[worstIndex] = new KeyValuePair<string, int>(droppedId, Result.Found[worstIndex].Value - 1);
            else
                Result.Found.RemoveAt(worstIndex);

            _freeSlots += _tables.Items.Get(droppedId).Slots;
            Result.Log.Add("выбросили " + _tables.Items.Get(droppedId).Name + " ради " + incoming.Name);

            return _freeSlots >= incoming.Slots;
        }

        private void Meet()
        {
            var density = Density();
            var intensity = Intensity();
            if (!_rng.Chance(_tables.Tuning.GetFloat("encounter_per_hour") * density * intensity)) return;

            Result.Encountered = true;
            Result.Stamina = Math.Max(0, Result.Stamina - 1);

            var beast = PickBeast();
            var orders = Party.Orders ?? new StandingOrders();

            var decision = new ExpeditionDecision
            {
                Kind = DecisionKind.Encounter,
                Title = "СТРЕЧА: " + beast,
                Body = _zone.Name + ": тварь рядом. Дальше — как решит отряд.",
            };

            decision.Options.Add(new ExpeditionOption
            {
                Id = "hide",
                Label = "спрятаться",
                Chance = 0.55,
                Note = "проскочим — и никто не пострадает",
            });

            decision.Options.Add(new ExpeditionOption
            {
                Id = "retreat",
                Label = "отойти",
                Chance = 0.70,
                Note = "уходим, но теряем час и часть добычи",
            });

            decision.Options.Add(new ExpeditionOption
            {
                Id = "fight",
                Label = "бой",
                Chance = FightChance(),
                Note = "победа — туша и трофей, провал — ранение",
            });

            Pending = decision;

            if (orders.Encounter == EncounterOrder.Ask) return;

            var auto = orders.Encounter == EncounterOrder.Hide ? "hide"
                : orders.Encounter == EncounterOrder.Retreat ? "retreat"
                : "fight";
            Decide(auto);
        }

        private void Resolve(ExpeditionDecision decision, ExpeditionOption option, bool success)
        {
            switch (decision.Kind)
            {
                case DecisionKind.Encounter:
                    ResolveEncounter(option.Id, success);
                    break;

                case DecisionKind.Carcass:
                    ResolveCarcass(option.Id, success);
                    break;

                case DecisionKind.Wounded:
                    ResolveWounded(option.Id, decision.PersonId);
                    break;
            }
        }

        private void ResolveEncounter(string option, bool success)
        {
            if (option == "hide")
            {
                Result.Log.Add(success ? _zone.Name + ": прошли мимо, тварь не заметила"
                    : _zone.Name + ": спрятаться не вышло — бой");
                if (!success) Fight();
                return;
            }

            if (option == "retreat")
            {
                if (success)
                {
                    _hour++;
                    Result.Log.Add(_zone.Name + ": отошли, потеряли час");
                    LoseSomeLoot();
                }
                else
                {
                    Result.Log.Add(_zone.Name + ": отойти не вышло — бой");
                    Fight();
                }

                return;
            }

            if (success)
            {
                FightWon();
            }
            else
            {
                Wound();
            }
        }

        private void Fight()
        {
            if (_rng.Chance(FightChance())) FightWon();
            else Wound();
        }

        private void FightWon()
        {
            Result.Log.Add(_zone.Name + ": бой — победа");

            var orders = Party.Orders ?? new StandingOrders();
            var decision = new ExpeditionDecision
            {
                Kind = DecisionKind.Carcass,
                Title = "ТУША",
                Body = "Тварь убита. Что с ней делать?",
            };

            decision.Options.Add(new ExpeditionOption
            {
                Id = "haul", Label = "утащить целиком", Chance = 1.0,
                Note = "3 слота",
            });
            decision.Options.Add(new ExpeditionOption
            {
                Id = "butcher", Label = "освежевать", Chance = ButcherChance(),
                Note = "час работы",
            });
            decision.Options.Add(new ExpeditionOption
            {
                Id = "leave", Label = "бросить", Chance = 1.0,
                Note = "",
            });

            Pending = decision;

            if (orders.Carcass == CarcassOrder.Ask) return;

            Decide(orders.Carcass == CarcassOrder.Haul ? "haul"
                : orders.Carcass == CarcassOrder.Butcher ? "butcher" : "leave");
        }

        private void ResolveCarcass(string option, bool success)
        {
            var orders = Party.Orders ?? new StandingOrders();

            if (option == "haul")
            {
                if (_freeSlots < 3f)
                {
                    Result.Log.Add("туша не влезла в мешок — бросили");
                    _state.Forage++;
                    return;
                }

                _freeSlots -= 3f;
                Result.Log.Add("тушу утащили целиком");
                Result.Found.Add(new KeyValuePair<string, int>("crystal_fire", 1));
                return;
            }

            if (option == "butcher")
            {
                _hour++;
                if (success)
                {
                    AddFound("crystal_fire");
                    Result.Log.Add("освежевали: кристалл и части в мешке");
                }
                else
                {
                    Result.Log.Add("освежевать не вышло — испортили");
                }

                return;
            }

            _state.Forage++;
            Result.Log.Add("тушу бросили");
        }

        private void Wound()
        {
            var victim = Party.PersonIds[_rng.Next(Party.PersonIds.Count)];
            var person = _tables.Roster.Get(victim);

            var decision = new ExpeditionDecision
            {
                Kind = DecisionKind.Wounded,
                Title = "РАНЕН: " + person.Name,
                Body = "Идти сама не может. Нести — 3 слота и час дороги. Оставить — не дойдёт.",
                PersonId = victim,
            };

            decision.Options.Add(new ExpeditionOption
            {
                Id = "carry", Label = "нести", Chance = 1.0, Note = "3 слота, дорога дольше",
            });
            decision.Options.Add(new ExpeditionOption
            {
                Id = "leave", Label = "оставить", Chance = 1.0, Note = "смерть до утра",
            });

            Pending = decision;

            var orders = Party.Orders ?? new StandingOrders();
            if (orders.Wounded == WoundedOrder.Ask) return;

            Decide(orders.Wounded == WoundedOrder.Carry ? "carry" : "leave");
        }

        private void ResolveWounded(string option, string personId)
        {
            if (option == "carry")
            {
                Result.Wounded = true;
                Result.WoundedPersonId = personId;
                Result.Stamina = Math.Max(0, Result.Stamina - 1);
                Result.Log.Add("раненого несут в деревню");
                _leaving = true;
                return;
            }

            Works.Kill(_world, personId);
            Result.Log.Add("оставили " + _tables.Roster.Get(personId).Name + " — не дойдёт");
            _leaving = true;
        }

        // ---- итог ----

        private void Finish(string reason)
        {
            if (Finished) return;

            if (Result.Wounded && Result.WoundedPersonId != null)
            {
                Works.Wound(_world, Result.WoundedPersonId);
            }

            Result.Log.Add(reason);
            Finished = true;
        }

        private void LoseSomeLoot()
        {
            if (Result.Found.Count == 0) return;

            var last = Result.Found[Result.Found.Count - 1];
            if (last.Value <= 1) Result.Found.RemoveAt(Result.Found.Count - 1);
            else Result.Found[Result.Found.Count - 1] = new KeyValuePair<string, int>(last.Key, last.Value - 1);

            Result.Log.Add("потеряли часть добычи");
        }

        private void AddFound(string itemId)
        {
            for (var i = 0; i < Result.Found.Count; i++)
            {
                if (Result.Found[i].Key != itemId) continue;
                Result.Found[i] = new KeyValuePair<string, int>(itemId, Result.Found[i].Value + 1);
                return;
            }

            Result.Found.Add(new KeyValuePair<string, int>(itemId, 1));
        }

        // ---- шансы и выбор ----

        private double Density()
        {
            var food = Math.Max(1, _state.Forage + _state.Crystals);
            return (double)_world.BeastCount(_state.ZoneId) / food;
        }

        private double Intensity()
        {
            var intensity = Party.Behavior == Behavior.Avoid ? 0.5
                : Party.Behavior == Behavior.Clean ? 2.5 : 1.0;

            if (Party.PersonIds.Count > 2)
            {
                intensity *= Math.Pow(1.2, Party.PersonIds.Count - 2);
            }

            if (_state.AlertDays > 0) intensity *= 1.2;
            return intensity;
        }

        private double FightChance()
        {
            var strength = 0;
            foreach (var personId in Party.PersonIds)
            {
                var person = _tables.Roster.Get(personId);
                strength += People.SkillLevel(_tables, person, "hunting")
                    + People.TraitSum(_tables, person, trait => trait.FightBonus);
            }

            var chance = 0.35 + 0.10 * strength;
            return Math.Min(0.85, chance);
        }

        private double ButcherChance()
        {
            var best = 0;
            foreach (var personId in Party.PersonIds)
            {
                var person = _tables.Roster.Get(personId);
                var level = People.SkillLevel(_tables, person, "butchering")
                    + People.TraitSum(_tables, person, trait => trait.ButcherBonus);
                if (level > best) best = level;
            }

            return Math.Min(0.95, 0.35 + 0.12 * best);
        }

        private string PickBeast()
        {
            var pool = _tables.ZoneBeasts.All.Where(x => x.ZoneId == _zone.Id).ToList();
            if (pool.Count == 0) return "тварь";

            var total = pool.Sum(x => x.Weight);
            if (total <= 0) return "тварь";

            var roll = _rng.Next(total);
            foreach (var entry in pool)
            {
                roll -= entry.Weight;
                if (roll < 0) return _tables.Beasts.Get(entry.BeastId).Name;
            }

            return _tables.Beasts.Get(pool[pool.Count - 1].BeastId).Name;
        }

        private string PickResource()
        {
            var pool = _tables.ZoneResources.All.Where(x => x.ZoneId == _zone.Id).ToList();
            if (pool.Count == 0) return null;

            var total = pool.Sum(x => x.Abundance);
            if (total <= 0) return null;

            var roll = _rng.Next(total);
            foreach (var entry in pool)
            {
                roll -= entry.Abundance;
                if (roll < 0) return entry.ItemId;
            }

            return pool[pool.Count - 1].ItemId;
        }
    }
}
