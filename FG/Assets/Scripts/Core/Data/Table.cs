using System;
using System.Collections.Generic;

namespace ForestGuild.Core.Data
{
    /// <summary>
    /// Таблица строк с доступом по id. Дубликат id — ошибка данных, а не «последний победил».
    /// </summary>
    public sealed class Table<T>
    {
        private readonly Dictionary<string, T> _byId;

        public Table(IReadOnlyList<T> all, Func<T, string> idOf)
        {
            All = all;
            _byId = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var i = 0; i < all.Count; i++)
            {
                var id = idOf(all[i]);
                if (string.IsNullOrEmpty(id))
                    throw new DataException("table", "пустой id в строке " + (i + 1));
                if (_byId.ContainsKey(id))
                    throw new DataException("table", "дубликат id: " + id);
                _byId.Add(id, all[i]);
            }
        }

        public IReadOnlyList<T> All { get; }

        public T Get(string id)
        {
            if (!_byId.TryGetValue(id, out var value))
                throw new DataException("table", "нет id: " + id);
            return value;
        }

        public bool TryGet(string id, out T value)
        {
            return _byId.TryGetValue(id, out value);
        }
    }
}
