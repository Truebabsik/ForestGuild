using System.Collections.Generic;

namespace ForestGuild.Core.Sim
{
    /// <summary>Итог одного похода: что нашли, кого ранили, чем кончилось.</summary>
    public sealed class ExpeditionResult
    {
        public string ZoneId;
        public readonly List<KeyValuePair<string, int>> Found = new List<KeyValuePair<string, int>>();
        public readonly List<string> Log = new List<string>();
        public int MinutesUsed;
        public bool Encountered;
        public bool Wounded;
        public string WoundedPersonId;
    }
}
