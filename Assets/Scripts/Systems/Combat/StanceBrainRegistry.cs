using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat.Brains;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Singleton-ish lookup: StanceId → IStanceBrain. Brains are stateless
    /// — one instance is shared across every unit using that stance. The
    /// brain reads the per-tick BrainContext for state.
    /// </summary>
    public static class StanceBrainRegistry
    {
        private static readonly Dictionary<StanceId, IStanceBrain> _brains =
            BuildDefault();

        private static IStanceBrain _fallback = new OnslaughtBrain();

        private static Dictionary<StanceId, IStanceBrain> BuildDefault()
        {
            var d = new Dictionary<StanceId, IStanceBrain>
            {
                { StanceId.Onslaught, new OnslaughtBrain() },
                { StanceId.Tempest,   new TempestBrain()   },
                { StanceId.Stalwart,  new StalwartBrain()  },
                { StanceId.Tactician, new TacticianBrain() },
                { StanceId.Wraith,    new WraithBrain()    },
                { StanceId.Sentinel,  new SentinelBrain()  },
                { StanceId.Conduit,   new ConduitBrain()   },
            };
            return d;
        }

        /// <summary>
        /// Resolve a stance to its brain. Falls back to Onslaught if the
        /// stance has no implementation (shouldn't happen in shipped code,
        /// but safe).
        /// </summary>
        public static IStanceBrain Get(StanceDefinition stance)
        {
            if (stance == null) return _fallback;
            return _brains.TryGetValue(stance.id, out var b) ? b : _fallback;
        }

        public static IStanceBrain Get(StanceId id)
            => _brains.TryGetValue(id, out var b) ? b : _fallback;

        /// <summary>
        /// Replace the implementation for a stance — useful for tests or
        /// for plugging in alternative brains at runtime.
        /// </summary>
        public static void Override(StanceId id, IStanceBrain brain)
        {
            if (brain == null) return;
            _brains[id] = brain;
        }
    }
}
