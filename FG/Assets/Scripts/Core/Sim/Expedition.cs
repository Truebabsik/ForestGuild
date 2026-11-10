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

        /// <summary>О каком предмете речь (для полного мешка).</summary>
        public string ItemId;

        /// <summary>Какая тварь (для арта встречи и туши).</summary>
        public string BeastId;

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
        private readonly int _workMinutes;

        /// Шаг похода: 10 минут. Дорога и бой списываются как есть, шаг — целыми тиками.
        private const int TickMinutes = 10;

        private float _freeSlots;
        private bool _leaving;

        /// Последняя встреченная тварь: из неё потом достаётся добыча.
        private string _lastBeastId;

        /// Кто именно встретился: одна тварь или стая до трёх (ADR-0041).
        private List<string> _pack = new List<string>();

        /// Бой выигран, но сначала надо решить, что делать с выбитым.
        private bool _carcassAfterWound;

        public ExpeditionDecision Pending { get; private set; }
        public bool Finished { get; private set; }
        public int WorkMinutes => _workMinutes;
        public int MinutesLeft => Math.Max(0, _workMinutes - Result.MinutesUsed);

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
            _workMinutes = Math.Max(0, tuning.GetInt("daylight_minutes") - _zone.TravelMinutes * 2);

            Result = new ExpeditionResult
            {
                ZoneId = party.ZoneId,
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

        /// Один шаг похода (10 минут). false — поход закончился.
        public bool Advance()
        {
            if (Finished) return false;
            if (Pending != null) return true;

            if (_workMinutes <= 0)
            {
                Finish("дорога съедает весь день, работы не было");
                return false;
            }

            if (MinutesLeft < TickMinutes || _leaving)
            {
                Finish("отряд повернул домой");
                return false;
            }

            SpendMinutes(TickMinutes);

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
                var perHour = _tables.Tuning.GetFloat("find_base")
                    + _tables.Tuning.GetFloat("find_per_skill") * skill
                    + People.TraitSumDouble(_tables, person, trait => trait.FindBonus);
                var chance = perHour * TickMinutes / 60.0;

                if (!_rng.Chance(chance)) continue;

                var itemId = PickResource();
                if (itemId == null) continue;

                var item = _tables.Items.Get(itemId);

                if (_freeSlots < item.Slots)
                {
                    if (!MakeRoomOrAsk(person, item)) continue;
                }

                _freeSlots -= item.Slots;
                AddFound(itemId);
                Result.Log.Add(person.Name + " нашёл: " + item.Name);
            }
        }

        /// Мешок полон: либо решаем наказом, либо спрашиваем игрока.
        /// true — место освободилось, можно брать; false — предмет брошен.
        private bool MakeRoomOrAsk(Person person, Item item)
        {
            var orders = Party.Orders ?? new StandingOrders();

            if (orders.BagFull == BagOrder.ReplaceCheapest)
            {
                if (TryMakeRoom(item, true)) return true;

                Result.Log.Add(person.Name + ": мешок полон, " + item.Name + " брошено");
                return false;
            }

            if (orders.BagFull == BagOrder.Skip)
            {
                Result.Log.Add(person.Name + ": мешок полон, " + item.Name + " брошено");
                return false;
            }

            var cheapest = Cheapest();
            var decision = new ExpeditionDecision
            {
                Kind = DecisionKind.BagFull,
                Title = "МЕШОК ПОЛОН",
                Body = person.Name + " нашёл: " + item.Name + ". Места нет — решай, что важнее.",
                ItemId = item.Id,
                PersonId = person.Id,
            };

            if (cheapest.HasValue && cheapest.Value.Value < item.Value)
            {
                decision.Options.Add(new ExpeditionOption
                {
                    Id = "replace",
                    Label = "взять вместо " + _tables.Items.Get(cheapest.Value.Key).Name,
                    Chance = 1.0,
                });
            }

            decision.Options.Add(new ExpeditionOption
            {
                Id = "skip",
                Label = "не брать",
                Chance = 1.0,
            });

            Pending = decision;
            return false;
        }

        private KeyValuePair<string, int>? Cheapest()
        {
            KeyValuePair<string, int>? best = null;

            foreach (var found in Result.Found)
            {
                var value = _tables.Items.Get(found.Key).Value;
                if (best.HasValue && value >= _tables.Items.Get(best.Value.Key).Value) continue;
                best = found;
            }

            return best;
        }

        /// Наказ «мешок»: если велено менять, выбрасываем самую дешёвую находку ради новой.
        private bool TryMakeRoom(Item incoming, bool force = false)
        {
            var orders = Party.Orders ?? new StandingOrders();
            if (!force && orders.BagFull != BagOrder.ReplaceCheapest) return false;

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

        private void ResolveBagFull(string option, ExpeditionDecision decision)
        {
            var item = _tables.Items.Get(decision.ItemId);

            if (option == "replace" && TryMakeRoom(item, true))
            {
                _freeSlots -= item.Slots;
                AddFound(item.Id);
                Result.Log.Add("взяли " + item.Name);
                return;
            }

            Result.Log.Add("мешок полон, " + item.Name + " брошено");
        }

        private void Meet()
        {
            var beasts = _world.BeastCount(_zone.Id);
            if (beasts <= 0) return;

            var intensity = Intensity();
            var perMinute = _tables.Tuning.GetFloat("encounter_per_hour") / 60.0 * beasts * intensity;
            if (!_rng.Chance(perMinute * TickMinutes)) return;

            Result.Encountered = true;

            _pack = PickPack();
            var beast = _tables.Beasts.Get(_lastBeastId).Name;
            if (_pack.Count > 1) beast += " (стая из " + _pack.Count + ")";

            var orders = Party.Orders ?? new StandingOrders();
            var fightChance = Battle.Estimate(_tables, _world, Party.PersonIds, _pack);

            var decision = new ExpeditionDecision
            {
                Kind = DecisionKind.Encounter,
                Title = "ВСТРЕЧА: " + beast,
                Body = "Проскочить — тихо, если повезёт. Отойти — час долой. Бой — туша или рана.",
                BeastId = _lastBeastId,
            };

            decision.Options.Add(new ExpeditionOption
            {
                Id = "hide",
                Label = "спрятаться",
                Chance = 0.55,
                Note = RiskWord(0.55),
            });

            decision.Options.Add(new ExpeditionOption
            {
                Id = "retreat",
                Label = "отойти",
                Chance = 0.70,
                Note = RiskWord(0.70),
            });

            decision.Options.Add(new ExpeditionOption
            {
                Id = "fight",
                Label = "бой",
                Chance = fightChance,
                Note = RiskWord(fightChance),
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

                case DecisionKind.BagFull:
                    ResolveBagFull(option.Id, decision);
                    break;
            }
        }

        private void ResolveEncounter(string option, bool success)
        {
            if (option == "hide")
            {
                Result.Log.Add(success ? _zone.Name + ": прошли мимо, тварь не заметила"
                    : _zone.Name + ": спрятаться не вышло — бой");
                if (!success) RunBattle();
                return;
            }

            if (option == "retreat")
            {
                if (success)
                {
                    SpendMinutes(60);
                    Result.Log.Add(_zone.Name + ": отошли, потеряли час");
                    LoseSomeLoot();
                }
                else
                {
                    Result.Log.Add(_zone.Name + ": отойти не вышло — бой");
                    RunBattle();
                }

                return;
            }

            RunBattle();
        }

        /// Короткий бой: раунды до победы или до того, как все выбиты (ADR-0041).
        private void RunBattle()
        {
            var battle = Battle.Resolve(_tables, _world, Party.PersonIds, _pack, _rng);
            SpendMinutes(battle.Minutes);

            foreach (var line in battle.Log) Result.Log.Add(_zone.Name + ": " + line);

            foreach (var fighter in battle.Fighters)
            {
                if (!_world.People.TryGetValue(fighter.PersonId, out var state)) continue;
                state.Hp = Math.Max(0, fighter.Hp);
                state.HpMax = fighter.HpMax;
            }

            var downed = battle.Downed;
            foreach (var personId in downed) Works.Wound(_world, personId);

            _lastBeastId = battle.Beast.Id;

            if (!battle.Won)
            {
                Wound(downed.Count > 0 ? downed[0] : null);
                return;
            }

            if (downed.Count > 0)
            {
                _carcassAfterWound = true;
                Wound(downed[0]);
                return;
            }

            FightWon();
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
                BeastId = _lastBeastId,
            };

            decision.Options.Add(new ExpeditionOption
            {
                Id = "haul", Label = "утащить целиком", Chance = 1.0,
                Note = "3 слота",
            });
            decision.Options.Add(new ExpeditionOption
            {
                Id = "butcher", Label = "освежевать", Chance = ButcherChance(),
                Note = BestButcher(),
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
                TakeDrops(success: true);
                return;
            }

            if (option == "butcher")
            {
                SpendMinutes(60);
                if (success)
                {
                    Result.Log.Add("освежевали");
                    TakeDrops(success: true);
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

        /// Что нашлось в туше: роллы из `drops.csv` по убитому зверю и навыку разделки.
        private void TakeDrops(bool success)
        {
            if (!success) return;

            var drops = RollDrops(_tables, _lastBeastId, BestButcherLevel(), _rng);
            if (drops.Count == 0)
            {
                Result.Log.Add("в туше ничего стоящего");
                return;
            }

            foreach (var drop in drops)
            {
                var item = _tables.Items.Get(drop.Key);
                if (_freeSlots < item.Slots)
                {
                    Result.Log.Add("в туше: " + item.Name + " — не влезло");
                    continue;
                }

                _freeSlots -= item.Slots;
                AddFound(drop.Key);
                Result.Log.Add("в туше: " + item.Name);
            }
        }

        /// Ролл добычи зверя. Публичный — чтобы проверять данные и вкус без боя.
        public static List<KeyValuePair<string, int>> RollDrops(Tables tables, string beastId, int skill, Rng rng)
        {
            var found = new List<KeyValuePair<string, int>>();
            if (string.IsNullOrEmpty(beastId)) return found;

            foreach (var drop in tables.Drops.All)
            {
                if (drop.BeastId != beastId) continue;
                if (skill < drop.MinSkill) continue;
                if (!rng.Chance(drop.Chance)) continue;

                var index = found.FindIndex(x => x.Key == drop.ItemId);
                if (index >= 0) found[index] = new KeyValuePair<string, int>(drop.ItemId, found[index].Value + 1);
                else found.Add(new KeyValuePair<string, int>(drop.ItemId, 1));
            }

            return found;
        }

        private int BestButcherLevel()
        {
            var best = 0;
            foreach (var personId in Party.PersonIds)
            {
                var level = People.SkillLevel(_tables, _tables.Roster.Get(personId), "butchering");
                if (level > best) best = level;
            }

            return best;
        }

        /// Минуты в лесу: единственное место, где растёт `MinutesUsed`.
        private void SpendMinutes(int minutes)
        {
            Result.MinutesUsed += minutes;
        }

        /// Слова вместо процентов: игрок прикидывает шансы, а не читает таблицу.
        public static string RiskWord(double chance)
        {
            if (chance >= 0.85) return "почти наверняка";
            if (chance >= 0.70) return "надёжно";
            if (chance >= 0.50) return "как повезёт";
            return "рискованно";
        }

        /// Кто в отряде лучше всех владеет ножом — его имя и мастерство идут на кнопку.
        private string BestButcher()
        {
            string bestId = null;
            var best = -1;

            foreach (var personId in Party.PersonIds)
            {
                var person = _tables.Roster.Get(personId);
                var level = People.SkillLevel(_tables, person, "butchering");

                if (level <= best) continue;
                best = level;
                bestId = personId;
            }

            if (bestId == null) return null;

            return _tables.Roster.Get(bestId).Name + " · " + MasteryWord(best);
        }

        /// Степень мастерства словами — по уровню навыка.
        public static string MasteryWord(int level)
        {
            switch (level)
            {
                case 5: return "виртуоз";
                case 4: return "мастер";
                case 3: return "умелый";
                case 2: return "ученик";
                default: return "новичок";
            }
        }

        private void Wound(string victimId = null)
        {
            var victim = string.IsNullOrEmpty(victimId)
                ? Party.PersonIds[_rng.Next(Party.PersonIds.Count)]
                : victimId;
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
                SpendMinutes(60);
                Result.Log.Add("раненого несут в деревню");
                _leaving = true;
            }
            else
            {
                Works.Kill(_world, personId);
                Result.Log.Add("оставили " + _tables.Roster.Get(personId).Name + " — не дойдёт");
                _leaving = true;
            }

            // Бой был выигран, но сначала пришлось решать судьбу выбитого — туша ещё на месте.
            if (_carcassAfterWound)
            {
                _carcassAfterWound = false;
                FightWon();
            }
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

        /// Кто именно встретился — одна тварь или стая до трёх (ADR-0041).
        /// Берём фактических тварей зоны, а не статичный список видов: иначе зашедшая
        /// мимо своей зоны тварь превращается в безымянную «тварь» без дропа.
        private List<string> PickPack()
        {
            var live = _state.Beasts.Where(x => x.Value > 0).OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
            var pack = new List<string>();

            var available = live.Sum(x => x.Value);
            if (available <= 0) return pack;

            var tuning = _tables.CombatTuning;
            var max = Math.Min(tuning.GetInt("pack_max"), available);
            var size = 1;

            var weights = new List<KeyValuePair<int, int>>
            {
                new KeyValuePair<int, int>(1, tuning.GetInt("pack_weight_1")),
                new KeyValuePair<int, int>(2, tuning.GetInt("pack_weight_2")),
                new KeyValuePair<int, int>(3, tuning.GetInt("pack_weight_3")),
            };

            var totalWeight = weights.Where(x => x.Key <= max).Sum(x => x.Value);
            if (totalWeight > 0)
            {
                var roll = _rng.Next(totalWeight);
                foreach (var pair in weights)
                {
                    if (pair.Key > max) continue;
                    roll -= pair.Value;
                    if (roll < 0)
                    {
                        size = pair.Key;
                        break;
                    }
                }
            }

            var remaining = new Dictionary<string, int>(_state.Beasts);
            for (var i = 0; i < size; i++)
            {
                var pool = remaining.Where(x => x.Value > 0).OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
                if (pool.Count == 0) break;

                var total = pool.Sum(x => x.Value);
                var roll = _rng.Next(total);

                foreach (var entry in pool)
                {
                    roll -= entry.Value;
                    if (roll >= 0) continue;

                    pack.Add(entry.Key);
                    remaining[entry.Key] = entry.Value - 1;
                    break;
                }
            }

            if (pack.Count == 0) pack.Add(live[0].Key);
            _lastBeastId = pack[0];

            // Не даём взять больше, чем в зоне.
            if (pack.Count > available) pack.RemoveRange(available, pack.Count - available);
            return pack;
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
