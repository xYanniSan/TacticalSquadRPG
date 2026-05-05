using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Backward arc dodge jump.
    /// Movement is driven by UnitMovementController.TickDodge().
    /// After landing, the brain transitions back to Decide.
    /// </summary>
    public class DodgeAbility : ActiveAbility
    {
        private readonly float _dodgeDistance;
        private readonly float _dodgeArcHeight;
        private readonly float _dodgeMoveDuration;
        private readonly float _dodgePauseDuration;

        private bool  _moving;
        private float _pauseTimer;

        public DodgeAbility(float distance, float arcHeight, float moveDuration, float pauseDuration)
        {
            _dodgeDistance    = distance;
            _dodgeArcHeight   = arcHeight;
            _dodgeMoveDuration  = moveDuration;
            _dodgePauseDuration = pauseDuration;
        }

        public override void OnStart()
        {
            _moving      = true;
            _pauseTimer  = 0f;

            Ctx.Anim.PlayDodge();

            Vector3 backward = -Ctx.Unit.transform.forward;
            backward.y = 0f;
            backward.Normalize();

            Vector3 dodgeEnd = Ctx.Unit.transform.position + backward * _dodgeDistance;
            Ctx.Mover.StartDodge(dodgeEnd, _dodgeArcHeight, _dodgeMoveDuration);
        }

        public override bool OnTick(float dt)
        {
            if (_moving)
            {
                bool still = Ctx.Mover.TickDodge();
                if (!still)
                {
                    _moving = false;
                    _pauseTimer = _dodgePauseDuration;
                }
                return false;
            }

            // Pause after landing
            _pauseTimer -= dt;
            return _pauseTimer <= 0f;
        }

        public override void OnAnimationEvent(string eventName) { }

        public override void OnEnd()
        {
            Ctx.Mover.StopDodge();
        }
    }
}
