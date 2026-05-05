using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Handles all combat resolution: basic attacks, skill attacks,
    /// individual actions, block, buff, and power boost.
    /// Sits on the same GameObject as TerrainBattleManager.
    /// </summary>
    public class BattleCombatResolver : MonoBehaviour
    {
        private CombatResolutionSystem _combat;
        private SkillSystem _skill;
        private BattleSummonManager _summons;
        private BattleHitStopSystem _hitStop;
        private BattleOrbRaySystem  _orbRay;

        [Header("Orb Skill")]
        [Tooltip("Drag the Orb prefab (must have OrbProjectile component) here.")]
        [SerializeField] private GameObject orbPrefab;

        /// <summary>Exposes the orb prefab to subsystems that share it (e.g. BattleOrbRaySystem).</summary>
        public GameObject OrbPrefab => orbPrefab;

        public void Initialize(CombatResolutionSystem combat, SkillSystem skill)
        {
            _combat  = combat;
            _skill   = skill;
            _summons = GetComponent<BattleSummonManager>();
            _hitStop = GetComponent<BattleHitStopSystem>();
            _orbRay  = GetComponent<BattleOrbRaySystem>();
        }

        public ResolvedTechnique ResolveForDecide(SkillSlot skill, UnitRuntime caster)
        {
            return _skill.ResolveSkill(skill, caster);
        }

        public void ResolveBasicAttack(TerrainBattleUnit attacker, TerrainBattleUnit defender)
        {
            if (attacker.IsDead || defender == null || defender.IsDead) return;

            if (defender.TryDodge())
            {
                Debug.Log($"  {defender.Unit.DisplayName} DODGED {attacker.Unit.DisplayName}'s attack!");
                return;
            }

            CombatContext ctx = _combat.ResolveBasicAttack(attacker.Unit, defender.Unit);
            int finalDamage   = ctx.finalDamage;

            finalDamage += ConsumeBuff(attacker.Unit);
            finalDamage  = ApplyPowerBoost(attacker.Unit, finalDamage);
            finalDamage  = TryBlock(defender, finalDamage);

            defender.ApplyDamage(finalDamage, attacker);
            _hitStop?.TriggerHitStop(HitStopStrength.Light);

            // If the attacker has orbs and this is a punch (not a kick), fire one orb
            if (!attacker.IsUsingKick)
            {
                var orbHandler = attacker.GetComponent<OrbBuffHandler>();
                orbHandler?.TryConsumeOrb(defender);
            }

            Debug.Log($"  {attacker.Unit.DisplayName} [Basic Attack] → {defender.Unit.DisplayName} " +
                      $"{finalDamage} dmg  (HP {defender.Unit.currentHP}/{defender.Unit.maxHP})");

            if (defender.Unit.isDead)
                Debug.Log($"  ** {defender.Unit.DisplayName} DEFEATED! **");
        }

        public void ResolveSkillAttack(TerrainBattleUnit attacker, TerrainBattleUnit defender,
            ResolvedTechnique tech)
        {
            if (attacker.IsDead) return;

            if (tech.type == TechniqueType.Summon)
            {
                _summons?.TrySummon(attacker, tech);
                return;
            }

            if (tech.type == TechniqueType.Buff)
            {
                ApplyBuff(attacker, tech);
                return;
            }

            if (tech.type == TechniqueType.OrbSummon)
            {
                ApplyOrbSummon(attacker, tech);
                return;
            }

            if (tech.type == TechniqueType.OrbRay)
            {
                _orbRay?.FireOrbRay(attacker, tech, orbPrefab);
                return;
            }

            if (tech.type == TechniqueType.Heal)
            {
                int healAmount = tech.power;
                attacker.Unit.Heal(healAmount);
                var health = attacker.GetComponent<HealthSystem>();
                if (health != null) health.SyncHP(attacker.Unit.currentHP);
                Debug.Log($"  {attacker.Unit.DisplayName} [{tech.techniqueName}] HEALS {healAmount} " +
                          $"(HP {attacker.Unit.currentHP}/{attacker.Unit.maxHP})");
                return;
            }

            if (defender == null || defender.IsDead)
            {
                defender = TerrainBattleManager.Instance.GetNearestEnemy(attacker);
                if (defender == null) return;
            }

            if (defender.TryDodge())
            {
                Debug.Log($"  {defender.Unit.DisplayName} DODGED [{tech.techniqueName}]!");
                return;
            }

            CombatContext ctx = _combat.ResolveTechnique(attacker.Unit, defender.Unit, tech);
            int finalDamage   = ctx.finalDamage;

            finalDamage += ConsumeBuff(attacker.Unit);
            finalDamage  = ApplyPowerBoost(attacker.Unit, finalDamage);
            finalDamage  = TryBlock(defender, finalDamage);

            // Heavy skills get stronger hit stop
            HitStopStrength strength = finalDamage > 30 ? HitStopStrength.Heavy : HitStopStrength.Medium;
            defender.ApplyDamage(finalDamage, attacker);
            _hitStop?.TriggerHitStop(strength);

            Debug.Log($"  {attacker.Unit.DisplayName} [{tech.techniqueName}] ({tech.element}) " +
                      $"→ {defender.Unit.DisplayName}  {finalDamage} dmg  " +
                      $"(HP {defender.Unit.currentHP}/{defender.Unit.maxHP})");

            if (defender.Unit.isDead)
                Debug.Log($"  ** {defender.Unit.DisplayName} DEFEATED! **");
        }

        public void ExecuteIndividualActions(TerrainBattleUnit attacker, TerrainBattleUnit defender,
            SkillSlot skill)
        {
            if (attacker.IsDead) return;

            foreach (var actionSlot in skill.actionSequence)
            {
                var action = actionSlot.action;
                if (action == null) continue;

                switch (action.actionType)
                {
                    case ActionType.Elemental:
                        var buff = new ActiveBuff(
                            action.displayName,
                            action.element,
                            action.selfBuffDamage,
                            action.selfBuffCharges);
                        attacker.Unit.activeBuffs.Add(buff);
                        Debug.Log($"  {attacker.Unit.DisplayName} [{action.displayName}] → " +
                                  $"SELF BUFF +{action.selfBuffDamage} {action.element} " +
                                  $"for {action.selfBuffCharges} hits");
                        break;

                    case ActionType.Support:
                        attacker.Unit.pendingPowerBoost += action.powerBoostPercent;
                        Debug.Log($"  {attacker.Unit.DisplayName} [{action.displayName}] → " +
                                  $"POWER BOOST +{action.powerBoostPercent * 100f:0}% next skill");
                        break;

                    case ActionType.OrbSummon:
                        if (orbPrefab == null)
                        {
                            Debug.LogWarning("[BattleCombatResolver] orbPrefab not assigned — cannot spawn orbs.");
                            break;
                        }
                        OrbBuffHandler.Spawn(attacker, orbPrefab, action.orbCount, action.orbDamage);
                        Debug.Log($"  {attacker.Unit.DisplayName} [{action.displayName}] → " +
                                  $"ORB SUMMON ×{action.orbCount} ({action.orbDamage} dmg each)");
                        break;

                    case ActionType.Physical:
                        if (defender == null || defender.IsDead)
                            defender = TerrainBattleManager.Instance.GetNearestEnemy(attacker);
                        if (defender == null) break;

                        if (defender.TryDodge())
                        {
                            Debug.Log($"  {defender.Unit.DisplayName} DODGED [{action.displayName}]!");
                            break;
                        }

                        CombatContext ctx = _combat.ResolveBasicAttack(attacker.Unit, defender.Unit);
                        int dmg = ctx.finalDamage;
                        dmg += ConsumeBuff(attacker.Unit);
                        dmg  = ApplyPowerBoost(attacker.Unit, dmg);
                        dmg  = TryBlock(defender, dmg);

                        defender.ApplyDamage(dmg, attacker);
                        _hitStop?.TriggerHitStop(HitStopStrength.Light);

                        Debug.Log($"  {attacker.Unit.DisplayName} [{action.displayName}] → " +
                                  $"{defender.Unit.DisplayName}  {dmg} dmg  " +
                                  $"(HP {defender.Unit.currentHP}/{defender.Unit.maxHP})");

                        if (defender.Unit.isDead)
                        {
                            Debug.Log($"  ** {defender.Unit.DisplayName} DEFEATED! **");
                            return;
                        }
                        break;
                }
            }
        }

        // ── Block ────────────────────────────────────────────────────

        private int TryBlock(TerrainBattleUnit defender, int incomingDamage)
        {
            if (TerrainBattleManager.Instance != null && !TerrainBattleManager.Instance.IsBlockEnabled)
                return incomingDamage;

            float blockChance = defender.Unit.currentStats.defense * 0.02f;
            if (Random.value < blockChance)
            {
                int blocked = Mathf.Max(1, incomingDamage / 2);
                defender.Unit.RegenEnergy(5f);
                Debug.Log($"  {defender.Unit.DisplayName} BLOCKED! ({blocked} dmg reduced, +5 energy)");
                return incomingDamage - blocked;
            }
            return incomingDamage;
        }

        // ── Power Boost ──────────────────────────────────────────────

        private int ApplyPowerBoost(UnitRuntime attacker, int damage)
        {
            if (attacker.pendingPowerBoost <= 0f) return damage;
            int boosted = (int)(damage * (1f + attacker.pendingPowerBoost));
            Debug.Log($"  Power Boost ×{1f + attacker.pendingPowerBoost:0.0} applied! ({damage}→{boosted})");
            attacker.pendingPowerBoost = 0f;
            return boosted;
        }

        // ── Buff ─────────────────────────────────────────────────────

        private void ApplyBuff(TerrainBattleUnit caster, ResolvedTechnique tech)
        {
            int charges  = 3;
            int bonusDmg = Mathf.Max(1, tech.power / 2);
            var buff     = new ActiveBuff(tech.techniqueName, tech.element, bonusDmg, charges);
            caster.Unit.activeBuffs.Add(buff);

            Debug.Log($"  {caster.Unit.DisplayName} uses [{tech.techniqueName}] → BUFF! " +
                      $"+{bonusDmg} dmg for {charges} hits ({tech.element})");
        }

        private void ApplyOrbSummon(TerrainBattleUnit caster, ResolvedTechnique tech)
        {
            if (orbPrefab == null)
            {
                Debug.LogWarning("[BattleCombatResolver] orbPrefab not assigned — cannot spawn orbs.");
                return;
            }

            // Read orb settings from the OrbSummon action in the source action list.
            // If the combo is triggered by hand signs (no OrbSummon action in chain),
            // fall back to the orbPrefab defaults set on this component.
            int orbCount  = 3;
            int orbDamage = 15;
            if (tech.sourceActions != null)
            {
                foreach (var a in tech.sourceActions)
                {
                    if (a != null && a.actionType == ActionType.OrbSummon)
                    {
                        orbCount  = a.orbCount;
                        orbDamage = a.orbDamage;
                        break;
                    }
                }
            }

            OrbBuffHandler.Spawn(caster, orbPrefab, orbCount, orbDamage);
            Debug.Log($"  {caster.Unit.DisplayName} [{tech.techniqueName}] → ORB SUMMON ×{orbCount} ({orbDamage} dmg each)");
        }

        private int ConsumeBuff(UnitRuntime unit)
        {
            if (unit.activeBuffs == null || unit.activeBuffs.Count == 0) return 0;

            int bonus = 0;
            for (int i = unit.activeBuffs.Count - 1; i >= 0; i--)
            {
                bonus += unit.activeBuffs[i].Consume();
                if (unit.activeBuffs[i].IsExpired)
                    unit.activeBuffs.RemoveAt(i);
            }
            return bonus;
        }
    }
}
