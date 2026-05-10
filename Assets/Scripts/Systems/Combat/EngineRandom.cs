namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Per-battle deterministic PRNG. Owned by `BattleCombatEngine`,
    /// passed to brains via `BrainContext.rng`.
    ///
    /// All randomness in combat decisions MUST go through this — never
    /// `UnityEngine.Random` — so a battle is replayable from
    /// `(seed, list of brain decisions per tick)`. See COMBAT_DESIGN.md
    /// "Determinism for replay".
    ///
    /// Implementation: 32-bit xorshift. Cheap, deterministic, decent
    /// distribution. Not cryptographic.
    /// </summary>
    public sealed class EngineRandom
    {
        private uint _state;

        public EngineRandom(int seed)
        {
            // Avoid the zero-state lockup of xorshift.
            _state = unchecked((uint)seed);
            if (_state == 0u) _state = 0x9E3779B9u; // golden-ratio fallback
        }

        public uint NextUint()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Float in [0, 1).</summary>
        public float Next01() => (NextUint() & 0x00FFFFFFu) / 16777216f;

        /// <summary>Inclusive int in [min, max].</summary>
        public int NextRangeInclusive(int min, int max)
        {
            if (max <= min) return min;
            uint span = (uint)(max - min + 1);
            return min + (int)(NextUint() % span);
        }

        /// <summary>True with probability p (clamped to [0,1]).</summary>
        public bool NextBool(float p)
        {
            if (p <= 0f) return false;
            if (p >= 1f) return true;
            return Next01() < p;
        }

        /// <summary>Returns a random element from a list (deterministic).</summary>
        public T Pick<T>(System.Collections.Generic.IList<T> items)
        {
            if (items == null || items.Count == 0) return default;
            return items[NextRangeInclusive(0, items.Count - 1)];
        }
    }
}
