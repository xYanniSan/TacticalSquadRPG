using System;
using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Owns the kinetic-resource Speed pool for every combat unit.
    ///
    /// Speed is built by movement and spent on action — see
    /// Docs/Design/COMBAT_DESIGN.md "Speed — the kinetic resource". Lives in
    /// this subsystem (not on UnitRuntime) so the resource model can evolve
    /// without writes scattering across UnitRuntime.
    ///
    /// Phase 3 deliverable: pool exists, builds from raw movement velocity,
    /// drains while idle, displays a UI bar. Skill costs and gates land in
    /// Phase 4; per-MovementIntent gain rates land in Phase 5.
    /// </summary>
    public class BattleSpeedSystem : MonoBehaviour
    {
        // ── Tunables (per COMBAT_DESIGN spec) ───────────────────────

        [Header("Pool")]
        [SerializeField] private float startingSpeed   = 30f;
        [SerializeField] private float softCap         = 70f;
        [SerializeField] private float hardCap         = 100f;

        [Header("Drain")]
        [SerializeField] private float idleDrainPerSec    = 5f;
        [SerializeField] private float blockingDrainPerSec = 2f;
        [SerializeField] private float staggerDrainPerSec  = 10f;

        [Header("Gain")]
        [Tooltip("Above the soft cap, gain is multiplied by this factor (slows the grind to 100).")]
        [SerializeField] private float aboveSoftCapMultiplier = 0.4f;

        [Header("Speed bands")]
        [SerializeField] private float bandSluggishMax = 20f;
        [SerializeField] private float bandEngagedMax  = 50f;
        [SerializeField] private float bandSharpMax    = 70f;

        // ── State ────────────────────────────────────────────────────

        private readonly Dictionary<TerrainBattleUnit, float> _speed
            = new Dictionary<TerrainBattleUnit, float>();

        // Phase 12 — temporary per-unit soft-cap overrides (Destabilize drops it,
        // Flow State raises it). When the timer expires, falls back to global
        // softCap. Hard-cap overrides use the same shape but a separate dict.
        private struct CapOverride { public float value; public float remaining; }
        private readonly Dictionary<TerrainBattleUnit, CapOverride> _softCapOverrides
            = new Dictionary<TerrainBattleUnit, CapOverride>();

        public event Action<TerrainBattleUnit, float> OnSpeedChanged;

        public float SoftCap => softCap;
        public float HardCap => hardCap;

        public float GetSoftCap(TerrainBattleUnit unit)
        {
            if (unit != null && _softCapOverrides.TryGetValue(unit, out var ov))
                return ov.value;
            return softCap;
        }

        /// <summary>
        /// Apply a temporary soft-cap override (e.g. Destabilize drops to 40,
        /// Flow State raises to 90). Replaces any existing override on this unit.
        /// </summary>
        public void SetSoftCapOverride(TerrainBattleUnit unit, float capValue, float duration)
        {
            if (unit == null || duration <= 0f) return;
            _softCapOverrides[unit] = new CapOverride { value = capValue, remaining = duration };

            // If override drops the cap below current speed, clamp.
            if (_speed.TryGetValue(unit, out float cur) && cur > capValue)
            {
                _speed[unit] = capValue;
                OnSpeedChanged?.Invoke(unit, capValue);
            }
        }

        /// <summary>Drain a flat amount of speed (Phase 12 stagger-strike shatter).</summary>
        public void Shatter(TerrainBattleUnit unit, float amount)
        {
            if (unit == null || amount <= 0f) return;
            if (!_speed.TryGetValue(unit, out float cur)) return;
            float next = Mathf.Max(0f, cur - amount);
            _speed[unit] = next;
            OnSpeedChanged?.Invoke(unit, next);
        }

        // ── Registration ─────────────────────────────────────────────

        public void RegisterUnit(TerrainBattleUnit unit)
        {
            if (unit == null) return;
            if (_speed.ContainsKey(unit)) return;
            _speed[unit] = startingSpeed;
            OnSpeedChanged?.Invoke(unit, startingSpeed);
        }

        public void UnregisterUnit(TerrainBattleUnit unit)
        {
            if (unit == null) return;
            _speed.Remove(unit);
        }

        // ── Public API ───────────────────────────────────────────────

        public float GetSpeed(TerrainBattleUnit unit)
        {
            if (unit == null) return 0f;
            return _speed.TryGetValue(unit, out float s) ? s : 0f;
        }

        public bool CanAfford(TerrainBattleUnit unit, float cost)
        {
            if (cost <= 0f) return true;
            return GetSpeed(unit) >= cost;
        }

        /// <summary>Returns true if the cost was paid; false if the unit lacked speed.</summary>
        public bool SpendSpeed(TerrainBattleUnit unit, float cost)
        {
            if (cost <= 0f) return true;
            if (!_speed.TryGetValue(unit, out float current)) return false;
            if (current < cost) return false;

            float next = current - cost;
            _speed[unit] = next;
            OnSpeedChanged?.Invoke(unit, next);
            return true;
        }

        public void GainSpeed(TerrainBattleUnit unit, float amount)
        {
            if (amount <= 0f) return;
            if (!_speed.TryGetValue(unit, out float current)) return;

            float next = ApplySoftCap(unit, current, amount);
            _speed[unit] = next;
            OnSpeedChanged?.Invoke(unit, next);
        }

        public SpeedBand GetSpeedBand(TerrainBattleUnit unit)
        {
            float s = GetSpeed(unit);
            if (s < bandSluggishMax) return SpeedBand.Sluggish;
            if (s < bandEngagedMax)  return SpeedBand.Engaged;
            if (s < bandSharpMax)    return SpeedBand.Sharp;
            return SpeedBand.Primed;
        }

        // ── Tick ─────────────────────────────────────────────────────

        private void Update()
        {
            if (_speed.Count == 0) return;

            float dt = Time.deltaTime;

            // Tick cap-override timers first so the per-unit cap reflects the
            // current frame's reality.
            if (_softCapOverrides.Count > 0)
            {
                var keys = new List<TerrainBattleUnit>(_softCapOverrides.Keys);
                foreach (var k in keys)
                {
                    var ov = _softCapOverrides[k];
                    ov.remaining -= dt;
                    if (ov.remaining <= 0f) _softCapOverrides.Remove(k);
                    else                    _softCapOverrides[k] = ov;
                }
            }

            // Snapshot keys so we can mutate during iteration safely
            var units = new List<TerrainBattleUnit>(_speed.Keys);
            for (int i = 0; i < units.Count; i++)
            {
                TerrainBattleUnit unit = units[i];
                if (unit == null || unit.IsDead) continue;

                float current = _speed[unit];
                float next = current + ComputeDelta(unit, dt);
                next = Mathf.Clamp(next, 0f, hardCap);

                if (!Mathf.Approximately(next, current))
                {
                    _speed[unit] = next;
                    OnSpeedChanged?.Invoke(unit, next);
                }
            }
        }

        // ── Internal: gain/drain computation ─────────────────────────

        private float ComputeDelta(TerrainBattleUnit unit, float dt)
        {
            UnitMovementController mover = unit.GetComponent<UnitMovementController>();
            float velocity = mover != null ? mover.CurrentMoveSpeed : 0f;

            UnitCombatState state = unit.CombatState;
            CombatRole role = unit.CombatRole;

            // Phase 5 — gain is shaped by MovementIntent (close/circle/disengage/dash),
            // gated by actual movement velocity. Static units don't gain even with
            // an aggressive intent set; moving units gain at the intent's full rate.
            BattleMovementSystem moveSys = TerrainBattleManager.Instance?.Movement;
            float intentRate = moveSys != null ? moveSys.GetGainRate(unit) : 0f;

            // Velocity gate — must actually be moving (>10 % of unit max) for intent
            // to count. Otherwise we drain even with intent set.
            float maxUnitSpeed = unit.Unit != null ? Mathf.Max(1f, unit.Unit.currentStats.moveSpeed) : 1f;
            bool moving = velocity / maxUnitSpeed > 0.1f;

            if (moving && intentRate > 0f)
                return ApplySoftCapDelta(unit, _speed[unit], intentRate * dt);

            // No movement / no positive intent → drain.
            float drain;
            if (state == UnitCombatState.Stagger)
                drain = staggerDrainPerSec;
            else if (state == UnitCombatState.Recover && role == CombatRole.Defender)
                drain = blockingDrainPerSec;
            else
                drain = idleDrainPerSec;

            return -drain * dt;
        }

        private float ApplySoftCap(TerrainBattleUnit unit, float current, float gainAmount)
        {
            if (gainAmount <= 0f) return current;
            float effectiveSoftCap = GetSoftCap(unit);
            float next = current;
            if (next < effectiveSoftCap)
            {
                float toCap = effectiveSoftCap - next;
                float portion = Mathf.Min(gainAmount, toCap);
                next += portion;
                gainAmount -= portion;
            }
            if (gainAmount > 0f)
                next += gainAmount * aboveSoftCapMultiplier;
            return Mathf.Clamp(next, 0f, hardCap);
        }

        private float ApplySoftCapDelta(TerrainBattleUnit unit, float current, float gainAmount)
        {
            // Returns a *delta* (signed), not the new value.
            float next = ApplySoftCap(unit, current, gainAmount);
            return next - current;
        }
    }
}
