using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Tracks per-unit MovementIntent and exposes the per-intent speed-gain
    /// rate consumed by BattleSpeedSystem (Phase 5). Intent is set by the
    /// brain when it picks a state / movement plan; the system itself does
    /// not move units — UnitMovementController still owns translation, this
    /// is just the policy layer.
    ///
    /// See COMBAT_DESIGN.md "How movement builds speed" for the per-intent
    /// rate table this implements.
    /// </summary>
    public class BattleMovementSystem : MonoBehaviour
    {
        // Tunables match COMBAT_DESIGN.md "How movement builds speed".
        [Header("Speed gain per intent (units / second)")]
        [SerializeField] private float gainHold      =  0f;   // walking baseline
        [SerializeField] private float gainCircle    =  6f;
        [SerializeField] private float gainClose     =  8f;
        [SerializeField] private float gainDisengage =  3f;
        [SerializeField] private float gainDash      = 10f;   // sprinting / lunge windows

        private readonly Dictionary<TerrainBattleUnit, MovementIntent> _intent
            = new Dictionary<TerrainBattleUnit, MovementIntent>();

        public void SetIntent(TerrainBattleUnit unit, MovementIntent intent)
        {
            if (unit == null) return;
            _intent[unit] = intent;
        }

        public MovementIntent GetIntent(TerrainBattleUnit unit)
        {
            if (unit == null) return MovementIntent.Hold;
            return _intent.TryGetValue(unit, out var i) ? i : MovementIntent.Hold;
        }

        public void Clear(TerrainBattleUnit unit)
        {
            if (unit != null) _intent.Remove(unit);
        }

        /// <summary>
        /// Per-second speed-gain rate for the unit's current intent. Returns 0
        /// for Hold (which lets BattleSpeedSystem apply its idle drain).
        /// </summary>
        public float GetGainRate(TerrainBattleUnit unit)
        {
            switch (GetIntent(unit))
            {
                case MovementIntent.Close:     return gainClose;
                case MovementIntent.Circle:    return gainCircle;
                case MovementIntent.Disengage: return gainDisengage;
                case MovementIntent.Dash:      return gainDash;
                case MovementIntent.Hold:
                default:                       return gainHold;
            }
        }
    }
}
