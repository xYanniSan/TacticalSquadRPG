using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Generic Animancer-driven melee ability with multi-strike support.
    /// Plays an AttackProfile through the centralized BattleAnimancerDriver
    /// and orchestrates the combo as N strikes spaced by `strikeInterval`,
    /// each going through the resolver's per-strike pipeline (dodge → damage
    /// → block → speed economy → CC on last strike).
    ///
    /// A combo with `strikeCount = 1` behaves identically to a single-swing
    /// melee strike. With `strikeCount > 1` it produces a flurry cadence —
    /// the visible difference between "punch" and "Earth Fist".
    ///
    /// Per spec line 406: a successful dodge mid-combo aborts the remaining
    /// strikes (attacker exits Execute early; speed cost is NOT refunded
    /// here — the spec mentions a 70% refund as future tuning).
    /// </summary>
    public class AnimancerMeleeAbility : ActiveAbility
    {
        private const float DefaultHoldDuration = 1.5f;
        private const float WindUpFraction      = 0.25f;  // first strike fires after this fraction of hold
        private const float TailFraction        = 0.20f;  // post-last-strike tail before ability ends

        private readonly AttackProfile _profile;
        private readonly BattleAnimancerDriver _animancer;

        private int   _strikeCount;
        private float _strikeInterval;
        private int   _strikesFired;
        private float _phaseTimer;       // counts down to next phase event
        private bool  _aborted;
        private bool  _castBegan;

        public AnimancerMeleeAbility(AttackProfile profile, BattleAnimancerDriver animancer)
        {
            _profile   = profile;
            _animancer = animancer;
        }

        public override void OnStart()
        {
            _strikeCount    = Ctx.Technique != null ? Mathf.Max(1, Ctx.Technique.strikeCount) : 1;
            _strikeInterval = Ctx.Technique != null && Ctx.Technique.strikeInterval > 0f
                ? Ctx.Technique.strikeInterval : 0.25f;

            _strikesFired = 0;
            _aborted      = false;
            _castBegan    = false;

            // Wind-up: short pause before the first strike so the defender has
            // a beat to read the commit. Spec phase: Initiation → WindUp.
            float hold = Ctx.Technique != null && Ctx.Technique.executionTime > 0f
                ? Ctx.Technique.executionTime
                : DefaultHoldDuration;
            _phaseTimer = hold * WindUpFraction;

            // Pay cast costs once. If unaffordable, abort immediately.
            var resolver = TerrainBattleManager.Instance?.CombatResolver;
            if (resolver != null && Ctx.Technique != null)
            {
                _castBegan = resolver.BeginCast(Ctx.Unit, Ctx.Technique);
                if (!_castBegan) { _aborted = true; return; }
            }
            else _castBegan = true;

            // Play the animation transition.
            bool started = _animancer != null && _animancer.PlayAttack(Ctx.Unit, _profile);
            if (!started)
            {
                Ctx.Anim.PlayAttack();
                Debug.LogWarning("[AnimancerMeleeAbility] Animancer playback unavailable — fell back to legacy PlayAttack().");
            }

            // Notify exchange phase: WindUp.
            TerrainBattleManager.Instance?.ExchangeCoordinator?
                .AdvancePhase(Ctx.Unit, ExchangePhase.WindUp);
        }

        public override bool OnTick(float dt)
        {
            if (_aborted) return true;

            _phaseTimer -= dt;
            if (_phaseTimer > 0f) return false;

            // Strike phase: fire one strike, schedule next.
            if (_strikesFired < _strikeCount)
            {
                FireOneStrike(_strikesFired);
                _strikesFired++;

                if (_aborted) return true;  // dodge aborted the combo
                if (_strikesFired < _strikeCount)
                {
                    _phaseTimer = _strikeInterval;
                    return false;
                }

                // Last strike fired — short tail before ending so impact reads.
                float hold = Ctx.Technique != null && Ctx.Technique.executionTime > 0f
                    ? Ctx.Technique.executionTime : DefaultHoldDuration;
                _phaseTimer = hold * TailFraction;
                return false;
            }

            // Tail expired or animation finished — done.
            return true;
        }

        public override void OnAnimationEvent(string eventName)
        {
            // Multi-strike combos derive their cadence from strikeInterval
            // rather than animation events. AttackEnd is intentionally ignored.
        }

        private void FireOneStrike(int strikeIndex)
        {
            if (Ctx.Target == null || Ctx.Target.IsDead) { _aborted = true; return; }

            var resolver = TerrainBattleManager.Instance?.CombatResolver;
            if (resolver == null) { _aborted = true; return; }

            bool dodged = resolver.ResolveStrike(Ctx.Unit, Ctx.Target,
                Ctx.Technique, strikeIndex, _strikeCount);
            if (dodged)
            {
                _aborted = true;
            }
        }
    }
}
