using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    /// <summary>Сухой прогон приказа (ui.md §4.5, §9): всё, что UI показывает до отправки.</summary>
    public sealed class SquadBriefing
    {
        public int TravelMinutes;
        public int WorkMinutes;
        public int Slots;
        public double EncounterPerMinute;
        public readonly List<string> Warnings = new List<string>();
    }

    public static class Briefing
    {
        public static SquadBriefing Compute(World world, Tables tables,
            IReadOnlyList<string> members, string zoneId, Behavior behavior)
        {
            var tuning = tables.Tuning;
            var briefing = new SquadBriefing();

            // Зона выбирается на экране планирования, а не в Сборе: здесь про неё молчим.
            if (string.IsNullOrEmpty(zoneId)) return briefing;

            var zone = tables.Zones.Get(zoneId);
            briefing.TravelMinutes = zone.TravelMinutes;
            briefing.WorkMinutes = Math.Max(0, tuning.GetInt("daylight_minutes") - zone.TravelMinutes * 2);

            var slots = 0;
            var brave = false;

            foreach (var personId in members)
            {
                var person = tables.Roster.Get(personId);
                slots += tuning.GetInt("slots_per_person")
                    + People.TraitSum(tables, person, trait => trait.CarryBonus);

                if (Array.IndexOf(person.Traits, "brave") >= 0) brave = true;
            }

            briefing.Slots = slots;

            if (members.Count == 0)
            {
                briefing.Warnings.Add("Отряд пуст — в лес идти некому");
            }

            if (members.Count == 1)
            {
                briefing.Warnings.Add("Один в лесу: прикрыть некому");
            }

            if (members.Count == 3)
            {
                briefing.Warnings.Add("Втроём в лесу шумно — твари замечают раньше");
            }

            if (members.Count > 3)
            {
                briefing.Warnings.Add("Вчетвером в лесу очень шумно — твари идут на звук");
            }

            if (behavior == Behavior.Clean && !brave)
            {
                briefing.Warnings.Add("На тварей без храброго охотника не ходят");
            }

            if (briefing.WorkMinutes == 0)
            {
                briefing.Warnings.Add("До места и обратно — весь день, работать некогда");
            }

            // Встречи считаются по числу тварей в зоне, а не по «богатству» (ADR-0042).
            var beasts = world.BeastCount(zoneId);
            var alertDays = world.Zones.TryGetValue(zoneId, out var zoneState) ? zoneState.AlertDays : 0;

            // Тот же множитель, что и в самом походе: прогноз не должен расходиться с боем.
            var intensity = Expedition.EncounterIntensity(behavior, members.Count, alertDays);

            briefing.EncounterPerMinute = tuning.GetFloat("encounter_per_hour") / 60.0 * beasts * intensity;
            return briefing;
        }
    }
}
