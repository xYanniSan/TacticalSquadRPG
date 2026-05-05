using System;
using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Runs one ability at a time on behalf of the unit's brain.
    /// Receives animation events from UnitAnimationEventRelay and forwards them
    /// to the active ability.
    ///
    /// Attach to the same GameObject as TerrainBattleUnit.
    /// </summary>
    public class AbilityExecutor : MonoBehaviour
    {
        private ActiveAbility _active;

        /// <summary>Raised when the current ability reports it is done.</summary>
        public event Action OnAbilityComplete;

        public bool IsExecuting => _active != null;

        // ── Public API ────────────────────────────────────────────────

        /// <summary>
        /// Start running an ability. Cancels and ends any currently active ability.
        /// </summary>
        public void Run(ActiveAbility ability)
        {
            if (_active != null)
            {
                _active.OnEnd();
                _active = null;
            }

            _active = ability;
            _active.OnStart();
        }

        /// <summary>
        /// Stop the active ability without completing it (interrupt / death).
        /// </summary>
        public void Cancel()
        {
            if (_active == null) return;
            _active.OnEnd();
            _active = null;
        }

        /// <summary>
        /// Forward an animation event name to the active ability.
        /// Called by UnitAnimationEventRelay.
        /// </summary>
        public void NotifyAnimationEvent(string eventName)
        {
            _active?.OnAnimationEvent(eventName);
        }

        // ── Unity Update ──────────────────────────────────────────────

        private void Update()
        {
            if (_active == null) return;

            bool done = _active.OnTick(Time.deltaTime);
            if (done)
            {
                var finished = _active;
                _active = null;
                finished.OnEnd();
                OnAbilityComplete?.Invoke();
            }
        }
    }
}
