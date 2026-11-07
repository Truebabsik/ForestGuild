using System;
using System.Collections.Generic;
using ForestGuild.Core.Data;

namespace ForestGuild.Core.Sim
{
    /// <summary>Сухой прогон приказа (ui.md §4.5, §9): всё, что UI показывает до отправки.</summary>
    public sealed class SquadBriefing
    {
        public float TravelHours;
        public int WorkHours;
        public int Slots;
        public double EncounterPerHour;
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
            briefing.TravelHours = zone.TravelHours;
            briefing.WorkHours = Math.Max(0, tuning.GetInt("hours_per_day") - (int)Math.Ceiling(zone.TravelHours * 2));

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

            if (briefing.WorkHours == 0)
            {
                briefing.Warnings.Add("До места и обратно — весь день, работать некогда");
            }

            var density = 0.0;
            if (world.Zones.TryGetValue(zoneId, out var zoneState))
            {
                var food = Math.Max(1, zoneState.Forage + zoneState.Crystals);
                density = (double)world.BeastCount(zoneId) / food;
            }

            var intensity = behavior == Behavior.Avoid ? 0.5 : behavior == Behavior.Clean ? 2.5 : 1.0;
            if (members.Count > 2) intensity *= 1.2;

            briefing.EncounterPerHour = tuning.GetFloat("encounter_per_hour") * density * intensity;
            return briefing;
        }
    }
}
