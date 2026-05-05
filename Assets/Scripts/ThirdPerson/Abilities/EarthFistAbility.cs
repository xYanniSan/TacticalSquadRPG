using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Animancer-driven proof-of-concept skill ability.
    ///
    /// Same shape as MeleeStrikeAbility — fires damage on the "HitFrame" event
    /// (clip AnimationEvent or AttackProfile.impactEventName), ends on
    /// "AttackEnd" — but plays the clip via UnitAnimancerDriver instead of
    /// triggering the Animator Controller's Attack state.
    ///
    /// Combat still owns damage, hit-stop, and knockback through the resolver
    /// pipeline. This ability only changes the playback path.
    /// </summary>
    public class EarthFistAbility : ActiveAbility
    {
        private const float ExecuteHoldDuration = 2.0f;

        private readonly AttackProfile _profile;
        private readonly UnitAnimancerDriver _animancer;

        private float _timer;
        private bool  _damageFired;
        private bool  _animFinished;

        public EarthFistAbility(AttackProfile profile, UnitAnimancerDriver animancer)
        {
            _profile   = profile;
            _animancer = animancer;
        }

        public override void OnStart()
        {
            _timer        = ExecuteHoldDuration;
            _damageFired  = false;
            _animFinished = false;

            bool started = _animancer != null && _animancer.PlayAttack(_profile);

            // Fall back to the legacy Animator Controller path if Animancer
            // isn't wired or the profile has no transition. Keeps the unit
            // visually responsive instead of standing idle on misconfiguration.
            if (!started)
            {
                Ctx.Anim.PlayAttack();
                Debug.LogWarning("[EarthFistAbility] Animancer playback unavailable — fell back to legacy PlayAttack().");
            }
        }

        public override bool OnTick(float dt)
        {
            _timer -= dt;

            // Fallback damage at 50% hold if no event arrived (matches MeleeStrikeAbility).
            if (!_damageFired && _timer <= ExecuteHoldDuration * 0.5f)
            {
                _damageFired = true;
                FireDamage();
            }

            return _animFinished || _timer <= 0f;
        }

        public override void OnAnimationEvent(string eventName)
        {
            if (eventName == "HitFrame" && !_damageFired)
            {
                _damageFired = true;
                FireDamage();
            }
            else if (eventName == "AttackEnd")
            {
                _animFinished = true;
            }
        }

        private void FireDamage()
        {
            if (Ctx.Target == null || Ctx.Target.IsDead) return;
            AbilityDamageHelper.Fire(Ctx);
        }
    }
}
