using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Short dash-in toward the target before executing a melee strike.
    /// Skipped automatically if already inside attack range.
    /// On completion transitions to MeleeStrikeAbility.
    /// </summary>
    public class AttackDashAbility : ActiveAbility
    {
        private readonly float _dashDuration;
        private readonly float _attackRange;

        private bool _dashComplete;

        public AttackDashAbility(float dashDuration, float attackRange)
        {
            _dashDuration = dashDuration;
            _attackRange  = attackRange;
        }

        public override void OnStart()
        {
            _dashComplete = false;

            if (Ctx.Target == null || Ctx.Target.IsDead)
            {
                _dashComplete = true;
                return;
            }

            Vector3 toTarget = Ctx.Target.transform.position - Ctx.Unit.transform.position;
            toTarget.y = 0f;
            float currentDist = toTarget.magnitude;

            // Already inside range — skip dash
            if (currentDist <= _attackRange * 0.9f)
            {
                if (toTarget.sqrMagnitude > 0.001f)
                    Ctx.Mover.FaceDirection(toTarget);
                _dashComplete = true;
                return;
            }

            Ctx.Mover.FaceDirection(toTarget);

            float stepDist = Mathf.Min(currentDist - _attackRange * 0.7f, 1.5f);
            Vector3 dashEnd = Ctx.Unit.transform.position + toTarget.normalized * stepDist;
            Ctx.Mover.StartLunge(dashEnd, _dashDuration);
        }

        public override bool OnTick(float dt)
        {
            if (_dashComplete) return true;

            if (Ctx.Target != null && !Ctx.Target.IsDead)
                Ctx.Mover.FaceTarget(Ctx.Target.transform);

            bool still = Ctx.Mover.TickLunge();
            return !still; // done when lunge finishes
        }

        public override void OnAnimationEvent(string eventName) { }

        public override void OnEnd()
        {
            Ctx.Mover.StopLunge();
            // Chain into MeleeStrikeAbility — must Bind before Run so Ctx is available in OnStart
            var strike = new MeleeStrikeAbility(useKick: false);
            strike.Bind(Ctx);
            Ctx.Unit.GetComponent<AbilityExecutor>()?.Run(strike);
        }
    }
}
