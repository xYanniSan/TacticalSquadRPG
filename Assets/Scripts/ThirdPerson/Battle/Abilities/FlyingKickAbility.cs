using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Gap-closing flying kick. Enforces a minimum start distance so the lunge
    /// never fires from point-blank range. Falls through to MeleeStrikeAbility
    /// if the target is too close at activation time.
    ///
    /// Lunge end is computed as targetPosition − forward * desiredImpactOffset
    /// so the unit always stops at the correct striking distance regardless of
    /// how far away the target was when the kick started.
    /// </summary>
    public class FlyingKickAbility : ActiveAbility
    {
        private const float ExecuteHoldDuration   = 2.0f;

        [Tooltip("Below this distance the ability downgrades to MeleeStrikeAbility.")]
        private const float MinStartRange         = 2.0f;

        [Tooltip("How far in front of the target the unit should stop (striking offset).")]
        private const float DesiredImpactOffset   = 0.6f;

        private readonly float _lungeDistance;
        private readonly float _lungeDuration;

        private float _timer;
        private bool  _damageFired;
        private bool  _animFinished;
        private bool  _downgraded;   // true when this instance was replaced by MeleeStrikeAbility

        public FlyingKickAbility(float lungeDistance, float lungeDuration)
        {
            _lungeDistance = lungeDistance;
            _lungeDuration = lungeDuration;
        }

        public override void OnStart()
        {
            _timer        = ExecuteHoldDuration;
            _damageFired  = false;
            _animFinished = false;
            _downgraded   = false;

            // ── Range check — downgrade if too close ─────────────────
            if (Ctx.Target != null)
            {
                float dist = Vector3.Distance(
                    Ctx.Unit.transform.position, Ctx.Target.transform.position);

                if (dist < MinStartRange)
                {
                    // Swap to MeleeStrikeAbility — executor will run it next frame
                    _downgraded = true;
                    var strike = new MeleeStrikeAbility(useKick: false);
                    strike.Bind(Ctx);
                    Ctx.Unit.GetComponent<AbilityExecutor>()?.Run(strike);
                    return;
                }

                // ── Compute target-relative lunge end ─────────────────
                Ctx.Mover.FaceTargetSnap(Ctx.Target.transform);

                Vector3 toTarget = Ctx.Target.transform.position - Ctx.Unit.transform.position;
                toTarget.y = 0f;
                toTarget.Normalize();

                // Land just in front of the target, not on top of them
                Vector3 lungeEnd = Ctx.Target.transform.position - toTarget * DesiredImpactOffset;

                // Clamp lunge distance so we can't overshoot
                float actualDist = Vector3.Distance(Ctx.Unit.transform.position, lungeEnd);
                if (actualDist > _lungeDistance)
                {
                    lungeEnd = Ctx.Unit.transform.position + toTarget * _lungeDistance;
                    lungeEnd.y = Ctx.Unit.transform.position.y;
                }

                Ctx.Mover.StartLunge(lungeEnd, _lungeDuration);
            }

            bool isFlyingKick = Ctx.Anim.PlayKick(out _);
            _ = isFlyingKick; // always a flying kick variant — variant selection in driver
        }

        public override bool OnTick(float dt)
        {
            if (_downgraded) return true; // executor already running replacement

            _timer -= dt;
            Ctx.Mover.TickLunge();

            if (!_damageFired && _timer <= ExecuteHoldDuration * 0.5f)
            {
                _damageFired = true;
                FireDamage();
            }

            return _animFinished || _timer <= 0f;
        }

        public override void OnAnimationEvent(string eventName)
        {
            if (_downgraded) return;

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

        public override void OnEnd()
        {
            Ctx.Mover.StopLunge();
        }

        private void FireDamage()
        {
            if (Ctx.Target == null || Ctx.Target.IsDead) return;
            AbilityDamageHelper.Fire(Ctx);
        }
    }
}
