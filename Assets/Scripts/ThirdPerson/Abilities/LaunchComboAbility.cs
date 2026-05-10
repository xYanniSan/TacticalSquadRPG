using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Dispatches to `BattleCombatResolver.ResolveSkillAttack` which routes
    /// `TechniqueType.LaunchCombo` techniques into the cinematic 3-segment
    /// `RunLaunchCombo` coroutine (launch → aerial flurry → far knockback).
    ///
    /// Without this dedicated dispatch, the brain falls through to
    /// MeleeStrikeAbility which orchestrates a plain multi-strike combo
    /// without the launch/knockback choreography. This class is just the
    /// "wait while the resolver coroutine plays out" wrapper.
    /// </summary>
    public class LaunchComboAbility : ActiveAbility
    {
        // Total duration matches RunLaunchCombo's beats:
        //   Launch (0.20s) + 3 aerial @ 0.18s spacing + KnockbackFar (0.55s)
        //   ≈ 0.20 + 0.54 + 0.55 = 1.29s, plus a small tail.
        private const float TotalDuration = 1.5f;

        private float _timer;

        public override void OnStart()
        {
            _timer = TotalDuration;

            if (Ctx.Target == null || Ctx.Target.IsDead) return;

            // Play the legacy kick clip on the attacker just for visible
            // commitment — the choreography (launch, knockback) is what the
            // viewer reads, not the punch animation.
            Ctx.Anim.PlayKick(out _);

            var mgr = TerrainBattleManager.Instance;
            mgr?.ResolveSkillAttack(Ctx.Unit, Ctx.Target, Ctx.Technique);

            TerrainBattleManager.Instance?.ExchangeCoordinator?
                .AdvancePhase(Ctx.Unit, ExchangePhase.WindUp);
        }

        public override bool OnTick(float dt)
        {
            _timer -= dt;
            return _timer <= 0f;
        }

        public override void OnAnimationEvent(string eventName) { }
    }
}
