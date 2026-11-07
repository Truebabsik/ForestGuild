namespace ForestGuild.Core.Sim
{
    /// <summary>
    /// Детерминированный генератор: один и тот же сид — одна и та же игра.
    /// Никакого UnityEngine.Random и DateTime.Now в ядре.
    /// </summary>
    public sealed class Rng
    {
        private ulong _state;

        public Rng(int seed)
        {
            _state = (ulong)seed * 6364136223846793005UL + 1442695040888963407UL;
            if (_state == 0) _state = 0x9E3779B97F4A7C15UL;
        }

        public ulong NextULong()
        {
            _state ^= _state << 13;
            _state ^= _state >> 7;
            _state ^= _state << 17;
            return _state;
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        public int Range(int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive) return minInclusive;
            return minInclusive + Next(maxInclusive - minInclusive + 1);
        }

        public double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / 9007199254740992.0);
        }

        public bool Chance(double probability)
        {
            return NextDouble() < probability;
        }
    }
}
