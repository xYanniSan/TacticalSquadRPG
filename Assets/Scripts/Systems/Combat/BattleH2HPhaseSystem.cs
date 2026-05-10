using System;
using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Owns the H2H combat phase per registered unit and dispatches enter /
    /// exit events. The state machine itself is described in
    /// `Docs/Design/HAND_TO_HAND_COMBAT.md` §6.
    ///
    /// All phase transitions go through this subsystem. Other code (the
    /// H2H brain, the locomotion driver, the orchestrator, the training UI)
    /// queries `GetPhase`, calls `TransitionPhase`, or subscribes to
    /// `OnPhaseEnter` / `OnPhaseExit`. Per-unit timers / decision lag are
    /// also kept here so the brain stays small.
    ///
    /// The unit type is `MonoBehaviour` rather than `TerrainBattleUnit` so
    /// the H2H layer can run in the TrainingDummy scene (no
    /// TerrainBattleManager / UnitRuntime present) and a future battle
    /// integration without coupling either side to the other.
    /// </summary>
    public class BattleH2HPhaseSystem : MonoBehaviour
    {
        public struct PhaseRecord
        {
            public H2HPhase phase;
            public float    enteredAt;       // Time.time
            public float    nextDecisionAt;  // earliest Time.time the brain may make a fresh decision
            public float    spottingExpiresAt; // valid while phase == Spotting
            public float    separationExpiresAt; // valid while phase == Separating
        }

        private readonly Dictionary<MonoBehaviour, PhaseRecord> _phases
            = new Dictionary<MonoBehaviour, PhaseRecord>();

        public event Action<MonoBehaviour, H2HPhase, string> OnPhaseEnter;
        public event Action<MonoBehaviour, H2HPhase, string> OnPhaseExit;

        // ── Registration ────────────────────────────────────────────

        public void Register(MonoBehaviour unit, H2HPhase initial = H2HPhase.NotEngaged)
        {
            if (unit == null) return;
            _phases[unit] = new PhaseRecord
            {
                phase = initial,
                enteredAt = Time.time,
                nextDecisionAt = Time.time
            };
            OnPhaseEnter?.Invoke(unit, initial, "register");
        }

        public void Unregister(MonoBehaviour unit)
        {
            if (unit == null) return;
            if (_phases.TryGetValue(unit, out var rec))
                OnPhaseExit?.Invoke(unit, rec.phase, "unregister");
            _phases.Remove(unit);
        }

        public bool IsRegistered(MonoBehaviour unit) => unit != null && _phases.ContainsKey(unit);

        public IEnumerable<MonoBehaviour> RegisteredUnits => _phases.Keys;

        // ── Phase queries ───────────────────────────────────────────

        public H2HPhase GetPhase(MonoBehaviour unit)
        {
            return unit != null && _phases.TryGetValue(unit, out var rec)
                ? rec.phase
                : H2HPhase.NotEngaged;
        }

        public PhaseRecord GetRecord(MonoBehaviour unit)
        {
            if (unit != null && _phases.TryGetValue(unit, out var rec)) return rec;
            return default;
        }

        public float SecondsInPhase(MonoBehaviour unit)
        {
            return unit != null && _phases.TryGetValue(unit, out var rec)
                ? Mathf.Max(0f, Time.time - rec.enteredAt)
                : 0f;
        }

        // ── Transition ──────────────────────────────────────────────

        /// <summary>
        /// Forces a transition to <paramref name="newPhase"/>. Returns true if
        /// the unit changed phase, false if it was already in that phase or
        /// not registered. <paramref name="reason"/> appears in event payloads
        /// and `Debug.Log` traces — useful for verification scenarios.
        /// </summary>
        public bool TransitionPhase(MonoBehaviour unit, H2HPhase newPhase, string reason)
        {
            if (unit == null) return false;
            if (!_phases.TryGetValue(unit, out var rec)) return false;
            if (rec.phase == newPhase) return false;

            H2HPhase old = rec.phase;
            OnPhaseExit?.Invoke(unit, old, reason);

            rec.phase = newPhase;
            rec.enteredAt = Time.time;

            // Per-phase entry book-keeping. Timers are filled in here so the
            // brain doesn't have to touch them.
            switch (newPhase)
            {
                case H2HPhase.Spotting:
                    rec.spottingExpiresAt = Time.time + RollSpottingDelay(unit);
                    break;
                case H2HPhase.Separating:
                    rec.separationExpiresAt = Time.time + RollSeparationDuration(unit);
                    break;
                case H2HPhase.Engaged:
                    rec.nextDecisionAt = Time.time + RollDecisionLag(unit);
                    break;
            }

            _phases[unit] = rec;
            OnPhaseEnter?.Invoke(unit, newPhase, reason);
            return true;
        }

        // ── Decision lag ────────────────────────────────────────────

        /// <summary>True iff the unit is allowed to make a fresh AI decision
        /// this frame. Decision lag prevents jittery rapid-fire behavior in
        /// Engagement phase.</summary>
        public bool CanDecide(MonoBehaviour unit)
        {
            return unit != null
                && _phases.TryGetValue(unit, out var rec)
                && Time.time >= rec.nextDecisionAt;
        }

        /// <summary>Stamps a fresh decision-lag interval. Call after the
        /// brain decides not to commit to anything this tick.</summary>
        public void NoteDecision(MonoBehaviour unit)
        {
            if (unit == null) return;
            if (!_phases.TryGetValue(unit, out var rec)) return;
            rec.nextDecisionAt = Time.time + RollDecisionLag(unit);
            _phases[unit] = rec;
        }

        public float SpottingExpiresAt(MonoBehaviour unit)
            => _phases.TryGetValue(unit, out var rec) ? rec.spottingExpiresAt : 0f;
        public float SeparationExpiresAt(MonoBehaviour unit)
            => _phases.TryGetValue(unit, out var rec) ? rec.separationExpiresAt : 0f;

        // ── Override hooks (used by training UI sliders) ─────────────

        [Header("Reaction-time overrides (optional)")]
        [Tooltip("If >= 0, used in place of the unit's spottingMin/Max.")]
        public float OverrideSpottingTime = -1f;
        [Tooltip("If >= 0, used in place of the unit's decisionLagMin/Max.")]
        public float OverrideDecisionLag = -1f;

        private float RollSpottingDelay(MonoBehaviour unit)
        {
            if (OverrideSpottingTime >= 0f) return OverrideSpottingTime;
            var def = ResolveDefinition(unit);
            if (def == null) return UnityEngine.Random.Range(0.3f, 0.7f);
            return UnityEngine.Random.Range(def.spottingMinTime, def.spottingMaxTime);
        }

        private float RollDecisionLag(MonoBehaviour unit)
        {
            if (OverrideDecisionLag >= 0f) return OverrideDecisionLag;
            var def = ResolveDefinition(unit);
            if (def == null) return UnityEngine.Random.Range(0.2f, 0.5f);
            return UnityEngine.Random.Range(def.decisionLagMin, def.decisionLagMax);
        }

        private float RollSeparationDuration(MonoBehaviour unit)
        {
            var def = ResolveDefinition(unit);
            if (def == null) return UnityEngine.Random.Range(1f, 1.5f);
            return UnityEngine.Random.Range(def.separationMinDuration, def.separationMaxDuration);
        }

        private static UnitDefinition ResolveDefinition(MonoBehaviour unit)
        {
            // The H2H layer doesn't directly know about UnitRuntime, so each
            // unit MonoBehaviour exposes a `Definition` property via the
            // `IH2HConfigured` contract. Cast and read.
            if (unit is IH2HConfigured cfg) return cfg.Definition;
            return null;
        }
    }

    /// <summary>
    /// Implemented by unit MonoBehaviours that participate in H2H phase
    /// management. Lets the phase system pull config (spotting time,
    /// movement speeds, etc.) without depending on `TerrainBattleUnit`
    /// or `UnitRuntime`.
    /// </summary>
    public interface IH2HConfigured
    {
        UnitDefinition Definition { get; }
    }
}
