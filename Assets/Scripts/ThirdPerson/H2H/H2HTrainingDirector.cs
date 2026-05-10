using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Single MonoBehaviour that owns the H2H subsystems
    /// (BattleH2HPhaseSystem + BattleH2HOrchestrator) and the unit
    /// registry for the training scene. The full battle game will spin
    /// up the equivalent through `TerrainBattleManager`; this is the
    /// equivalent for the test bench so the H2H system is decoupled
    /// from the legacy battle setup.
    ///
    /// Training UI / position presets / phase-forcing buttons
    /// (`H2HTrainingUI`) read this to drive combat from the inspector.
    /// </summary>
    public class H2HTrainingDirector : MonoBehaviour
    {
        [Header("Registered units (auto-populated if empty)")]
        [SerializeField] private List<H2HUnit> _units = new List<H2HUnit>();

        [Header("Subsystem refs (auto-spawned if blank)")]
        [SerializeField] private BattleH2HPhaseSystem  _phases;
        [SerializeField] private BattleH2HOrchestrator _orchestrator;

        [Header("Behaviour")]
        [Tooltip("Print phase enter/exit events to the console.")]
        [SerializeField] private bool _logPhaseTransitions = false;

        public BattleH2HPhaseSystem  Phases       => _phases;
        public BattleH2HOrchestrator Orchestrator => _orchestrator;
        public IReadOnlyList<H2HUnit> AllUnits    => _units;

        public static H2HTrainingDirector Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            // Spawn subsystems on this GameObject if not pre-wired.
            if (_phases == null) _phases = GetComponent<BattleH2HPhaseSystem>();
            if (_phases == null) _phases = gameObject.AddComponent<BattleH2HPhaseSystem>();

            if (_orchestrator == null) _orchestrator = GetComponent<BattleH2HOrchestrator>();
            if (_orchestrator == null) _orchestrator = gameObject.AddComponent<BattleH2HOrchestrator>();
            _orchestrator.PhaseSystem = _phases;

            // Auto-spawn the logger so any scene with a director gets event
            // tracing for free. Hotkey F10 dumps; the canvas has a button too.
            if (GetComponent<H2HLogger>() == null) gameObject.AddComponent<H2HLogger>();

            // Discover any H2HUnits already in the scene if list is empty.
            if (_units.Count == 0)
                _units.AddRange(FindObjectsByType<H2HUnit>());

            foreach (var u in _units)
                if (u != null) u.Configure(_phases, _orchestrator);

            if (_logPhaseTransitions)
            {
                _phases.OnPhaseEnter += (unit, phase, reason) =>
                    Debug.Log($"[H2H] {unit.name} → {phase} ({reason})");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public registry ─────────────────────────────────────────

        public void Register(H2HUnit unit)
        {
            if (unit == null || _units.Contains(unit)) return;
            _units.Add(unit);
            unit.Configure(_phases, _orchestrator);
        }

        public void Unregister(H2HUnit unit)
        {
            if (unit == null) return;
            _units.Remove(unit);
            _phases?.Unregister(unit);
        }

        // ── Convenience: find by name / team ────────────────────────

        public H2HUnit FindByName(string displayName)
        {
            foreach (var u in _units)
                if (u != null && u.DisplayName == displayName) return u;
            return null;
        }

        public H2HUnit FindByTeam(UnitTeam team)
        {
            foreach (var u in _units)
                if (u != null && u.Team == team) return u;
            return null;
        }
    }
}
