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
        private BattleSpeedSystem   _speed;
        private BattleStatusEffectSystem _statusEffects;

        [Header("Orb Skill")]
        [Tooltip("Drag the Orb prefab (must have OrbProjectile component) here.")]
        [SerializeField] private GameObject orbPrefab;

        /// <summary>Exposes the orb prefab to subsystems that share it (e.g. BattleOrbRaySystem).</summary>
        public GameObject OrbPrefab => orbPrefab;

        public void Initialize(CombatResolutionSystem combat, SkillSystem skill)
        {
            _combat        = combat;
            _skill         = skill;
            _summons       = GetComponent<BattleSummonManager>();
            _hitStop       = GetComponent<BattleHitStopSystem>();
            _orbRay        = GetComponent<BattleOrbRaySystem>();
            _speed         = GetComponent<BattleSpeedSystem>();
            _statusEffects = GetComponent<BattleStatusEffectSystem>();
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

        /// <summary>
        /// Cast-time bookkeeping for a skill: speed cost, speed gain, caster
        /// soft-cap modifier. Returns false if the caster can't afford the
        /// cost — caller aborts. Called once per cast even when the combo
        /// will play out as multiple strikes.
        /// </summary>
        public bool BeginCast(TerrainBattleUnit attacker, ResolvedTechnique tech)
        {
            if (_speed == null || tech == null) return true;

            if (tech.speedCost > 0f && !_speed.SpendSpeed(attacker, tech.speedCost))
            {
                Debug.Log($"  {attacker.Unit.DisplayName} cannot cast [{tech.techniqueName}] — insufficient speed.");
                return false;
            }
            if (tech.speedGain > 0f)
                _speed.GainSpeed(attacker, tech.speedGain);

            if (tech.casterSoftCapOverride > 0f && tech.speedCapModifierDuration > 0f)
            {
                _speed.SetSoftCapOverride(attacker, tech.casterSoftCapOverride, tech.speedCapModifierDuration);
                Debug.Log($"  {attacker.Unit.DisplayName} [{tech.techniqueName}] → soft-cap raised to {tech.casterSoftCapOverride:F0} for {tech.speedCapModifierDuration:F1}s");
            }
            return true;
        }

        /// <summary>
        /// Resolve one strike of a (possibly multi-strike) combo. Damage is
        /// partitioned by `totalStrikes`; CC, shatter, and cap-modifiers
        /// apply only on the LAST strike so multi-hit combos don't multi-CC.
        /// Returns true if the defender dodged this strike — caller aborts
        /// remaining strikes (per spec line 406).
        /// </summary>
        public bool ResolveStrike(TerrainBattleUnit attacker, TerrainBattleUnit defender,
            ResolvedTechnique tech, int strikeIndex, int totalStrikes)
        {
            if (attacker.IsDead || defender == null || defender.IsDead) return false;
            if (tech == null) return false;
            if (totalStrikes < 1) totalStrikes = 1;

            // Paired-reaction lookup. The defender's reaction depends on the
            // attack archetype, defender stance, and current speed band. The
            // result modifies damage, may move the defender, may block damage
            // entirely (FadeOut/Parry) or apply special handling (Airborne).
            var defStanceForReact = defender.GetComponent<UnitBrainAI>()?.Stance;
            var speedSysReact     = TerrainBattleManager.Instance?.Speed;
            SpeedBand defBand     = speedSysReact != null ? speedSysReact.GetSpeedBand(defender) : SpeedBand.Engaged;
            bool canDodge         = defender.WillDodge();
            bool canBlock         = TerrainBattleManager.Instance == null || TerrainBattleManager.Instance.IsBlockEnabled;
            var reaction = TacticalRPG.Systems.DefenderReactionTable.Lookup(
                tech.attackArchetype, defStanceForReact, defBand, canDodge, canBlock);

            // Reactions that produce no damage and abort the strike pipeline.
            if (reaction.type == ReactionType.FadeOut)
            {
                var choreoFade = TerrainBattleManager.Instance?.Choreography;
                if (choreoFade != null)
                {
                    choreoFade.TeleportFlank(defender, attacker, flankAngleDegrees: 180f, orbitDistance: 5.5f);
                }
                Debug.Log($"  {defender.Unit.DisplayName} FADEOUT vs {tech.attackArchetype} ({reaction.reason})");
                var coordF = TerrainBattleManager.Instance?.ExchangeCoordinator;
                coordF?.OnStrikeResolved(attacker, defender, DefenderResponse.Dodge);
                return true;
            }
            if (reaction.type == ReactionType.Dodge)
            {
                if (defender.TryDodge())
                {
                    Debug.Log($"  {defender.Unit.DisplayName} DODGED via reaction ({reaction.reason})");
                    var coordD = TerrainBattleManager.Instance?.ExchangeCoordinator;
                    coordD?.OnStrikeResolved(attacker, defender, DefenderResponse.Dodge);
                    return true;
                }
            }
            if (reaction.type == ReactionType.BobWeave)
            {
                Debug.Log($"  {defender.Unit.DisplayName} BOB-WEAVE evades ({reaction.reason})");
                var coordBW = TerrainBattleManager.Instance?.ExchangeCoordinator;
                coordBW?.OnStrikeResolved(attacker, defender, DefenderResponse.Dodge);
                return true;
            }
            if (reaction.type == ReactionType.Parry)
            {
                // Parry produces a brief mutual freeze + no damage. Counter
                // window is reserved (defender → attacker flip is Phase 8.5+).
                var animDriverP = TerrainBattleManager.Instance?.AnimancerDriver;
                animDriverP?.ApplyPoseHold(attacker, defender, 0.30f);
                Debug.Log($"  {defender.Unit.DisplayName} PARRIES ({reaction.reason})");
                var coordP = TerrainBattleManager.Instance?.ExchangeCoordinator;
                coordP?.OnStrikeResolved(attacker, defender, DefenderResponse.Counter);
                return true;
            }
            if (reaction.type == ReactionType.Airborne)
            {
                // Should generally route through RunLaunchCombo, not ResolveStrike.
                // Defensive log if we got here anyway.
                Debug.LogWarning($"  Airborne reaction reached ResolveStrike — should be RunLaunchCombo path. {reaction.reason}");
            }

            // Damage-applying reactions (Eat / Block / BraceBlock / Recoil)
            // continue through the normal pipeline with a modifier.
            CombatContext ctx = _combat.ResolveTechnique(attacker.Unit, defender.Unit, tech);
            int strikeDamage = Mathf.Max(1, ctx.finalDamage / totalStrikes);

            // Phase 9 — speed-scaling damage modifier per strike.
            if (_speed != null && tech.speedScaling > 0f)
            {
                float curSpeed = _speed.GetSpeed(attacker);
                float modifier = 1f + (curSpeed / 200f) * tech.speedScaling;
                strikeDamage = Mathf.Max(1, (int)(strikeDamage * modifier));
            }

            // Buff and power-boost both apply on the FIRST strike only — they're
            // one-shot bonuses on the cast, not per-hit.
            if (strikeIndex == 0)
            {
                strikeDamage += ConsumeBuff(attacker.Unit);
                strikeDamage  = ApplyPowerBoost(attacker.Unit, strikeDamage);
            }

            // Apply paired-reaction damage modifier. Block reactions go
            // through TryBlock (preserves +5 energy and existing block log);
            // BraceBlock / Recoil apply their own multipliers without the
            // energy bonus.
            if (reaction.type == ReactionType.Block)
            {
                strikeDamage = TryBlock(defender, strikeDamage);
            }
            else if (reaction.type == ReactionType.BraceBlock)
            {
                int braced = Mathf.Max(1, (int)(strikeDamage * reaction.damageMultiplier));
                Debug.Log($"  {defender.Unit.DisplayName} BRACE BLOCK — {strikeDamage} → {braced} ({reaction.reason})");
                strikeDamage = braced;
            }
            else if (reaction.type == ReactionType.Recoil)
            {
                int chip = Mathf.Max(1, (int)(strikeDamage * reaction.damageMultiplier));
                Debug.Log($"  {defender.Unit.DisplayName} RECOIL — {strikeDamage} → {chip} ({reaction.reason})");
                strikeDamage = chip;
            }
            // Eat = full damage, no modifier.

            HitStopStrength strength = strikeDamage > 20 ? HitStopStrength.Medium : HitStopStrength.Light;
            defender.ApplyDamage(strikeDamage, attacker);
            _hitStop?.TriggerHitStop(strength);

            // Reference 5 — PoseAttack hold on heavy hits (last strike of a
            // multi-strike or any single hit ≥ 25 dmg). Both fighters freeze
            // mid-pose so the moment reads.
            bool isLast2 = strikeIndex >= totalStrikes - 1;
            if (isLast2 && (strikeDamage >= 25 || totalStrikes >= 3))
            {
                var animDriver = TerrainBattleManager.Instance?.AnimancerDriver;
                animDriver?.ApplyPoseHold(attacker, defender, 0.30f);
            }

            bool isLast = strikeIndex >= totalStrikes - 1;
            var coord = TerrainBattleManager.Instance?.ExchangeCoordinator;
            if (coord != null)
            {
                DefenderResponse resp = TerrainBattleManager.Instance.IsBlockEnabled
                    && defender.CombatRole == CombatRole.Defender
                    ? DefenderResponse.Block : DefenderResponse.Eat;
                coord.OnStrikeResolved(attacker, defender, resp);
                coord.AdvancePhase(attacker,
                    isLast ? ExchangePhase.Resolution : ExchangePhase.StrikeSequence);
            }

            // CC, shatter, cap modifiers — only on the last strike (so a 4-hit
            // combo doesn't apply 4 stuns).
            if (isLast && _statusEffects != null && tech.ccType != CCEffectType.None
                && tech.ccDuration > 0f && !defender.IsDead)
            {
                if (tech.ccChance >= 1f || Random.value <= tech.ccChance)
                {
                    _statusEffects.Apply(defender, tech.ccType, tech.ccDuration, tech.ccMagnitude);
                    Debug.Log($"  {defender.Unit.DisplayName} afflicted with {tech.ccType} for {tech.ccDuration:F1}s");
                }
            }
            if (isLast && _speed != null && !defender.IsDead)
            {
                if (tech.targetSpeedShatter > 0f)
                {
                    _speed.Shatter(defender, tech.targetSpeedShatter);
                    Debug.Log($"  {defender.Unit.DisplayName} speed shattered by {tech.targetSpeedShatter:F0}");
                }
                if (tech.targetSoftCapOverride > 0f && tech.speedCapModifierDuration > 0f)
                {
                    _speed.SetSoftCapOverride(defender, tech.targetSoftCapOverride, tech.speedCapModifierDuration);
                    Debug.Log($"  {defender.Unit.DisplayName} soft-cap dropped to {tech.targetSoftCapOverride:F0} for {tech.speedCapModifierDuration:F1}s");
                }
            }

            Debug.Log($"  {attacker.Unit.DisplayName} [{tech.techniqueName} {strikeIndex + 1}/{totalStrikes}] → {defender.Unit.DisplayName}  {strikeDamage} dmg  (HP {defender.Unit.currentHP}/{defender.Unit.maxHP})");

            if (defender.Unit.isDead)
                Debug.Log($"  ** {defender.Unit.DisplayName} DEFEATED! **");

            return false;  // not dodged
        }

        public void ResolveSkillAttack(TerrainBattleUnit attacker, TerrainBattleUnit defender,
            ResolvedTechnique tech)
        {
            if (attacker.IsDead) return;
            if (tech == null) return;

            // Cast-time costs / cap modifiers.
            if (!BeginCast(attacker, tech)) return;

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

            if (tech.type == TechniqueType.LaunchCombo)
            {
                StartCoroutine(RunLaunchCombo(attacker, defender, tech));
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

            // Single-strike convenience path — multi-strike combos call
            // BeginCast + ResolveStrike directly from MultiStrikeAbility.
            ResolveStrike(attacker, defender, tech, 0, 1);
        }

        // ── LaunchCombo (Reference 3 — Naruto: launch → aerial flurry → far knockback) ─

        /// <summary>
        /// 3-segment cinematic combo:
        ///   1. LaunchUp the defender (~1.5m for 0.35s)
        ///   2. Multi-hit aerial follow-up (3 strikes spaced 0.18s; PoseHold on last)
        ///   3. KnockbackFar the defender (~10u over 0.55s)
        /// Plays out automatically in coroutine form. Damage is partitioned
        /// across the strikes via ResolveStrike. Speed cost is consumed via
        /// BeginCast already (called from ResolveSkillAttack before this).
        /// </summary>
        private System.Collections.IEnumerator RunLaunchCombo(
            TerrainBattleUnit attacker, TerrainBattleUnit defender, ResolvedTechnique tech)
        {
            if (defender == null || defender.IsDead) yield break;
            var choreo = TerrainBattleManager.Instance?.Choreography;
            if (choreo == null) yield break;

            const int aerialStrikes = 3;
            int totalStrikes = aerialStrikes + 1;

            // Paired reaction: defender enters Airborne state for the duration.
            // Brain stops ticking; damage during Airborne gets a 1.5x multiplier
            // applied below per strike. Auto-exits via OnAirborneEnd after KnockbackFar.
            defender.OnAirborneStart();

            // 1. Launch hit lifts the target.
            choreo.LaunchUp(defender, launchHeight: 2.0f, riseSec: 0.35f);
            ResolveStrikeAirborne(attacker, defender, tech, 0, totalStrikes);
            yield return new WaitForSeconds(0.20f);

            // 2. Aerial follow-up strikes — attacker grounded, hits airborne defender.
            for (int i = 0; i < aerialStrikes; i++)
            {
                if (defender == null || defender.IsDead) break;
                ResolveStrikeAirborne(attacker, defender, tech, i + 1, totalStrikes);
                yield return new WaitForSeconds(0.18f);
            }

            // 3. Final knockback — slide the target far away (cinematic).
            if (defender != null && !defender.IsDead)
            {
                choreo.KnockbackFar(attacker, defender, distance: 10f, durationSec: 0.55f);
                yield return new WaitForSeconds(0.55f);
            }

            // Exit Airborne back into Stagger then Decide.
            if (defender != null && !defender.IsDead)
                defender.OnAirborneEnd();
        }

        /// <summary>
        /// Strike against an airborne defender — same as ResolveStrike but
        /// applies an additional 1.5x damage multiplier and skips dodge/block
        /// (airborne targets can't react). Used by RunLaunchCombo.
        /// </summary>
        private void ResolveStrikeAirborne(TerrainBattleUnit attacker, TerrainBattleUnit defender,
            ResolvedTechnique tech, int strikeIndex, int totalStrikes)
        {
            if (attacker.IsDead || defender == null || defender.IsDead) return;
            if (tech == null) return;

            CombatContext ctx = _combat.ResolveTechnique(attacker.Unit, defender.Unit, tech);
            int strikeDamage = Mathf.Max(1, ctx.finalDamage / Mathf.Max(1, totalStrikes));

            if (_speed != null && tech.speedScaling > 0f)
            {
                float curSpeed = _speed.GetSpeed(attacker);
                strikeDamage = Mathf.Max(1, (int)(strikeDamage *
                    (1f + (curSpeed / 200f) * tech.speedScaling)));
            }
            if (strikeIndex == 0)
            {
                strikeDamage += ConsumeBuff(attacker.Unit);
                strikeDamage  = ApplyPowerBoost(attacker.Unit, strikeDamage);
            }

            // Airborne 1.5x damage multiplier.
            strikeDamage = Mathf.Max(1, (int)(strikeDamage * 1.5f));

            HitStopStrength strength = strikeDamage > 20 ? HitStopStrength.Medium : HitStopStrength.Light;
            defender.ApplyDamage(strikeDamage, attacker);
            _hitStop?.TriggerHitStop(strength);

            Debug.Log($"  {attacker.Unit.DisplayName} [{tech.techniqueName} airborne {strikeIndex + 1}/{totalStrikes}] " +
                      $"→ {defender.Unit.DisplayName}  {strikeDamage} dmg (×1.5 airborne)  HP {defender.Unit.currentHP}");
        }

        public void ExecuteIndividualActions(TerrainBattleUnit attacker, TerrainBattleUnit defender,
            SkillSlot skill)
        {
            // Coroutine-paced runner — actions fire over time instead of all
            // landing on the same frame. Without spacing, a 4-action chain
            // produces 4 simultaneous damage events that read as one big hit.
            if (attacker.IsDead) return;
            StartCoroutine(RunIndividualActions(attacker, defender, skill));
        }

        private System.Collections.IEnumerator RunIndividualActions(
            TerrainBattleUnit attacker, TerrainBattleUnit defender, SkillSlot skill)
        {
            const float ActionInterval = 0.20f;  // beat between actions in a non-combo chain

            // Dodge gate at the chain level. Roll once for the whole exchange
            // so a successful dodge wipes the entire chain rather than just
            // the first Physical action.
            bool defenderDodged = defender != null && !defender.IsDead && defender.TryDodge();
            if (defenderDodged)
            {
                Debug.Log($"  {defender.Unit.DisplayName} DODGED {attacker.Unit.DisplayName}'s chain!");
            }

            int actionIndex = 0;
            foreach (var actionSlot in skill.actionSequence)
            {
                if (attacker == null || attacker.IsDead) yield break;

                // Pace: the first action fires immediately; each subsequent
                // action waits a beat. Caller controls overall duration via
                // the brain's Execute → Recover transition timing.
                if (actionIndex > 0)
                    yield return new WaitForSeconds(ActionInterval);
                actionIndex++;

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

                        // Single chain-level dodge already rolled above; skip
                        // every Physical hit if the defender dodged.
                        if (defenderDodged) break;

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
                            yield break;
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
