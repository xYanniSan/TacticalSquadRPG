using UnityEngine;
using TacticalRPG.DataModels;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Static helper that resolves and dispatches damage for any ability.
    /// Extracted from TerrainBattleUnit.FireDamage().
    /// </summary>
    public static class AbilityDamageHelper
    {
        private const float RecoverDuration = 1.5f;

        public static void Fire(AbilityContext ctx)
        {
            var unit   = ctx.Unit;
            var target = ctx.Target;
            var mgr    = TerrainBattleManager.Instance;

            if (target == null || target.IsDead) return;
            if (mgr == null) return;

            if (ctx.Skill != null)
            {
                float totalCost = 0f;
                foreach (var slot in ctx.Skill.actionSequence)
                    if (slot.action != null) totalCost += slot.action.energyCost;
                unit.Unit.SpendEnergy(totalCost);

                Debug.Log($"[AbilityDamageHelper] {unit.Unit.DisplayName} firing skill — " +
                          $"isCombo={ctx.Technique?.isCombo} " +
                          $"type={ctx.Technique?.type} " +
                          $"name={ctx.Technique?.techniqueName} " +
                          $"actions={ctx.Skill.actionSequence.Count}");

                bool willDodgeSkill = target.WillDodge();
                if (!willDodgeSkill && target.CombatRole == CombatRole.Defender)
                    target.EnterDefendWindow(RecoverDuration);

                if (ctx.Technique != null && ctx.Technique.isCombo)
                    mgr.ResolveSkillAttack(unit, target, ctx.Technique);
                else
                    mgr.ExecuteIndividualActions(unit, target, ctx.Skill);
            }
            else
            {
                bool hitLanded = !target.WillDodge();
                if (hitLanded && target.CombatRole == CombatRole.Defender)
                    target.EnterDefendWindow(RecoverDuration);

                mgr.ResolveBasicAttack(unit, target);
            }
        }
    }
}
