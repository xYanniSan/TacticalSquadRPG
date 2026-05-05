using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// In-place punch or non-flying kick.
    /// Fires damage on OnHitFrame animation event (fallback at 50 % of hold timer).
    /// Ends on OnAttackEnd animation event (fallback at ExecuteHoldDuration).
    /// </summary>
    public class MeleeStrikeAbility : ActiveAbility
    {
        private const float ExecuteHoldDuration = 2.0f;

        private float _timer;
        private bool  _damageFired;
        private bool  _animFinished;
        private bool  _useKick;

        public MeleeStrikeAbility(bool useKick) { _useKick = useKick; }

        public override void OnStart()
        {
            _timer        = ExecuteHoldDuration;
            _damageFired  = false;
            _animFinished = false;

            if (_useKick)
                Ctx.Anim.PlayKick(out _);   // non-flying variant chosen internally
            else
                Ctx.Anim.PlayAttack();
        }

        public override bool OnTick(float dt)
        {
            _timer -= dt;

            // Fallback damage at 50 %
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
