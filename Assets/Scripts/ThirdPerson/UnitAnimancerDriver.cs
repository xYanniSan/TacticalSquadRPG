using Animancer;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Animancer-backed sibling to UnitAnimationDriver.
    ///
    /// Plays an AttackProfile's TransitionAsset on the unit's AnimancerComponent
    /// and forwards timing events back into the same vocabulary the legacy
    /// Animation Event relay uses ("HitFrame", "AttackEnd"), so abilities don't
    /// know or care which playback path produced the event.
    ///
    /// If the profile defines an Animancer named impact event, this driver
    /// subscribes to it. Otherwise it relies on the clip's legacy
    /// AnimationEvents being forwarded by UnitAnimationEventRelay.
    /// The state's OnEnd callback always fires "AttackEnd" so abilities have a
    /// guaranteed completion notification regardless of clip authoring.
    ///
    /// See Docs/07_PRESENTATION.md "Animation runtime (Animancer Pro)".
    /// </summary>
    public class UnitAnimancerDriver : MonoBehaviour
    {
        [SerializeField] private AnimancerComponent _animancer;

        public bool IsAvailable => _animancer != null;

        private TerrainBattleUnit _unit;
        private bool _impactDispatched;

        public void Initialize(TerrainBattleUnit unit)
        {
            _unit = unit;

            if (_animancer == null)
                _animancer = GetComponentInChildren<AnimancerComponent>();
        }

        /// <summary>
        /// Play the profile's transition. Returns true if playback started,
        /// false if Animancer is unavailable or the profile has no transition
        /// (caller should fall back to the legacy Animator Controller path).
        /// </summary>
        public bool PlayAttack(AttackProfile profile)
        {
            if (_animancer == null || profile == null || profile.transition == null)
                return false;

            _impactDispatched = false;

            AnimancerState state = _animancer.Play(profile.transition);

            if (profile.impactEventName != null)
            {
                state.Events(this).SetCallback(profile.impactEventName, OnImpactNamedEvent);
            }

            // OnEnd fires once when the state reaches its end time. Used to give
            // the ability a presentation-side "AttackEnd" cue even if the clip
            // has no legacy AnimationEvent at the end.
            state.Events(this).OnEnd = OnTransitionEnd;

            return true;
        }

        private void OnImpactNamedEvent()
        {
            if (_impactDispatched) return;
            _impactDispatched = true;

            // Route into the same path the legacy clip events take. The unit
            // logs and forwards to the active ability via AbilityExecutor.
            _unit?.OnHitFrame();
        }

        private void OnTransitionEnd()
        {
            _unit?.OnAttackEnd();
        }
    }
}
