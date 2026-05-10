using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Defender evasion ability with stance/band-varied flavor:
    /// - Wraith + Primed → DefensiveTeleport (vanish, ghost-trail, reappear at Mid range)
    /// - Sharp+         → BobWeave (in-place narrow window — barely moves)
    /// - Engaged+       → LateralSidestep (sideways shift; stays Close)
    /// - Default        → original parabolic backflip
    ///
    /// Same combat outcome (no damage) but visibly distinct movement signatures.
    /// </summary>
    public class DodgeAbility : ActiveAbility
    {
        private readonly float _dodgeDistance;
        private readonly float _dodgeArcHeight;
        private readonly float _dodgeMoveDuration;
        private readonly float _dodgePauseDuration;

        private bool  _moving;
        private float _pauseTimer;
        private bool  _instant;     // teleport variant: no movement phase

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
            _instant     = false;

            // Read stance + band to pick the visible variant.
            var stance = Ctx.Unit.GetComponent<UnitBrainAI>()?.Stance;
            var speedSys = TerrainBattleManager.Instance?.Speed;
            SpeedBand band = speedSys != null ? speedSys.GetSpeedBand(Ctx.Unit) : SpeedBand.Engaged;

            // Variant 1: Wraith + Primed → defensive teleport. Snap to Mid
            // range with a ghost trail. No movement phase needed.
            if (stance != null && stance.id == StanceId.Wraith && band == SpeedBand.Primed)
            {
                ExecuteDefensiveTeleport();
                return;
            }

            Ctx.Anim.PlayDodge();

            // Variant 2: Sharp+ → BobWeave. Tiny in-place dodge.
            // Variant 3: Engaged+ → LateralSidestep. Sideways instead of back.
            // Default: parabolic backflip.
            Vector3 dodgeDir;
            float distance = _dodgeDistance;
            float arc = _dodgeArcHeight;

            if (band == SpeedBand.Sharp || band == SpeedBand.Primed)
            {
                // BobWeave — narrow lateral shimmy, stays in range
                Vector3 lateral = Ctx.Unit.transform.right
                    * (Random.value > 0.5f ? 1f : -1f);
                dodgeDir = lateral;
                distance = 0.6f;
                arc = 0.3f;
            }
            else if (band == SpeedBand.Engaged)
            {
                // LateralSidestep — sideways at full distance, attacker overcommits
                Vector3 lateral = Ctx.Unit.transform.right
                    * (Random.value > 0.5f ? 1f : -1f);
                dodgeDir = lateral;
                distance = 1.8f;
                arc = 0.5f;
            }
            else
            {
                // Default backflip — Sluggish band keeps the legacy behavior
                Vector3 backward = -Ctx.Unit.transform.forward;
                dodgeDir = backward;
            }

            dodgeDir.y = 0f;
            dodgeDir.Normalize();

            Vector3 dodgeEnd = Ctx.Unit.transform.position + dodgeDir * distance;
            Ctx.Mover.StartDodge(dodgeEnd, arc, _dodgeMoveDuration);
        }

        private void ExecuteDefensiveTeleport()
        {
            _instant = true;
            _pauseTimer = _dodgePauseDuration;

            var target = Ctx.Unit.CurrentTarget;
            var choreo = TerrainBattleManager.Instance?.Choreography;
            if (choreo != null && target != null)
            {
                // Reappear at a random flank at Mid range.
                float angle = Random.Range(60f, 300f);
                choreo.TeleportFlank(Ctx.Unit, target,
                    flankAngleDegrees: angle, orbitDistance: 5.5f, ghostCount: 5);
            }
            Ctx.Anim.PlayDodge();  // brief pose just for the wind-up read
        }

        public override bool OnTick(float dt)
        {
            if (_instant)
            {
                _pauseTimer -= dt;
                return _pauseTimer <= 0f;
            }

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

            _pauseTimer -= dt;
            return _pauseTimer <= 0f;
        }

        public override void OnAnimationEvent(string eventName) { }

        public override void OnEnd()
        {
            if (!_instant) Ctx.Mover.StopDodge();
        }
    }
}
