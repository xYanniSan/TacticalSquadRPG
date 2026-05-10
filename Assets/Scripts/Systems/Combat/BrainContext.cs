using System.Collections.Generic;
using UnityEngine;
using TacticalRPG.DataModels;
using TacticalRPG.ThirdPerson;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Snapshot of the world handed to an IStanceBrain decision call. The
    /// engine builds this once per tick, per unit. Brains read; they do
    /// not mutate. See COMBAT_DESIGN "The brain layer" and "BrainContext
    /// extensions" (perception / threats — reserved for later wiring).
    /// </summary>
    public struct BrainContext
    {
        // ── Self ────────────────────────────────────────────────────

        public TerrainBattleUnit  self;
        public UnitRuntime        selfRuntime;
        public UnitMoveExecution  selfState;

        public float  currentSpeed;
        public SpeedBand speedBand;
        public float  currentEnergy;
        public float  currentHPFraction;        // 0..1
        public StanceDefinition stance;
        public BehaviorType behavior;

        // ── Target ──────────────────────────────────────────────────

        public TerrainBattleUnit  target;            // may be null
        public UnitRuntime        targetRuntime;     // may be null
        public UnitMoveExecution  targetState;       // may be null
        public float              distanceToTarget;
        public RangeBand          rangeBand;

        // ── World awareness (reserved — see COMBAT_DESIGN) ──────────

        /// <summary>
        /// Walls / hazards / summons in vicinity. Empty until the entity
        /// registry lands. The slot is here so brain code can be written
        /// against it without a refactor later.
        /// </summary>
        public List<Object> nearbyEntities;

        /// <summary>
        /// Allies' move states. Used by Sentinel/Stalwart to interpose
        /// when an ally is threatened. Empty if not built this tick.
        /// </summary>
        public List<UnitMoveExecution> alliedStates;

        /// <summary>
        /// Enemies' move states. Empty unless explicitly populated.
        /// </summary>
        public List<UnitMoveExecution> enemyStates;

        // ── Catalog (so brains can look up moves by id without a static) ─

        public MoveCatalog catalog;

        /// <summary>
        /// Engine tick rate in seconds (= 0.05 in the canonical config).
        /// Brains can scale frame counts to seconds via this.
        /// </summary>
        public float tickSeconds;

        /// <summary>
        /// Per-battle seeded RNG owned by `BattleCombatEngine`. Brains
        /// MUST use this for any random pick — never `UnityEngine.Random` —
        /// so a battle is replayable from (seed, decisions per tick).
        /// May be null if engine isn't wired (e.g. tests).
        /// </summary>
        public EngineRandom rng;
    }
}
