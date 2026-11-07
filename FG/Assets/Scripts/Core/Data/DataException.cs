using System;

namespace ForestGuild.Core.Data
{
    /// <summary>
    /// Ошибка данных: таблица не применилась. Падаем громко, а не подставляем дефолт —
    /// молча не применившаяся цифра хуже отсутствующей.
    /// </summary>
    public sealed class DataException : Exception
    {
        public DataException(string where, string message) : base(where + ": " + message)
        {
        }
    }
}
