using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ForestGuild.Core.Data
{
    /// <summary>
    /// Глобальные числа из tuning.csv. Здесь живут только коэффициенты —
    /// сами формулы в коде (ADR-0001).
    /// </summary>
    public sealed class Tuning
    {
        private readonly Dictionary<string, string> _raw;

        public Tuning(Dictionary<string, string> raw)
        {
            _raw = raw;
        }

        public string Get(string key)
        {
            if (_raw.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value)) return value;
            throw new DataException("tuning.csv", "нет ключа " + key);
        }

        public int GetInt(string key)
        {
            return int.Parse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public float GetFloat(string key)
        {
            return float.Parse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public int[] GetIntList(string key)
        {
            return Get(key)
                .Split('|')
                .Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture))
                .ToArray();
        }

        public bool Has(string key)
        {
            return _raw.ContainsKey(key);
        }

        /// <summary>
        /// Копия с заменённым числом. Нужна проверкам, которым нужен «выключенный» параметр
        /// (например, кривая без засады), не трогая сам справочник.
        /// </summary>
        public Tuning With(string key, string value)
        {
            var raw = new Dictionary<string, string>(_raw) { [key] = value };
            return new Tuning(raw);
        }
    }
}
