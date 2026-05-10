using System;
using System.Collections.Generic;
using UnityEngine;
using TacticalRPG.DataModels;
using TacticalRPG.ThirdPerson.Abilities;
using Random = UnityEngine.Random;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// AI state machine for a TerrainBattleUnit.
    /// Owns: UnitCombatState, CombatRole, Initiative, energy regen,
    ///       all UpdateXxx() state handlers, TransitionTo(), PickBestSkill(),
    ///       WillDodge(), and TryDodge().
    ///
    /// Communicates with the unit via the thin public API on TerrainBattleUnit;
    /// all external callers (BattleExchangeCoordinator, BattleCombatResolver, etc.)
    /// continue to reference TerrainBattleUnit, not this class.
    /// </summary>
    public class UnitBrainAI : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────

        [Header("Combat Timing")]
        [SerializeField] private float attackCooldownBase = 1.2f;

        [Header("Energy")]
        [SerializeField] private float energyRegenRate   = 15f;
        [SerializeField] private float backlineEnergyBonus = 10f;

        [Header("Melee Engagement")]
        [SerializeField] private float attackRange  = 2.5f;
        [SerializeField] private float latchRange   = 3.0f;

        [Header("Dodge")]
        [SerializeField] private float dodgeDistance    = 3.5f;
        [SerializeField] private float dodgeArcHeight   = 1.5f;
        [SerializeField] private float dodgeMoveDuration  = 0.3f;
        [SerializeField] private float dodgePauseDuration = 0.5f;
        [SerializeField] private float dodgeEnergyCost  = 10f;
        [SerializeField] private float dodgeCooldown    = 2f;

        [Header("Attack Dash")]
        [SerializeField] private float attackDashDuration = 0.22f;

        [Header("Flying Kick")]
        [SerializeField] private float flyingKickLungeDistance = 2.5f;
        [SerializeField] private float flyingKickLungeDuration = 0.35f;

        [Header("Initiative")]
        [SerializeField] private float startingInitiative = 10f;

        [Header("Animancer Skill Profiles")]
        [Tooltip("Map technique names to AttackProfiles. When the brain resolves a technique " +
                 "with a matching name, AnimancerMeleeAbility plays the profile through " +
                 "BattleAnimancerDriver. Techniques without a binding fall through to the " +
                 "legacy random punch / kick / dash mix.")]
        [SerializeField] private List<TechniqueProfileBinding> techniqueProfiles = new List<TechniqueProfileBinding>();

        [Header("Stance (Phase 7)")]
        [Tooltip("Combat preset assigned to this unit for the current mission. Modulates " +
                 "BattleAIBrain decision thresholds. If null, falls back to UnitDefinition.defaultStance.")]
        [SerializeField] private StanceDefinition stance;

        /// <summary>
        /// Resolved stance: serialized override → unit definition's default → null.
        /// Computed once in Initialize so the AI doesn't pay the lookup per-tick.
        /// </summary>
        private StanceDefinition _resolvedStance;
        public StanceDefinition Stance => _resolvedStance;

        [Serializable]
        public struct TechniqueProfileBinding
        {
            [Tooltip("Must exactly match ResolvedTechnique.techniqueName (e.g. \"Earth Fist\", \"Punch\").")]
            public string techniqueName;
            public AttackProfile profile;
        }

        // ── Wired on Initialize() ────────────────────────────────────

        private TerrainBattleUnit _unit;
        private UnitMovementController _mover;
        private UnitAnimationDriver    _anim;
        private BattleAnimancerDriver  _animancer;   // central subsystem; null if Animancer isn't wired in this scene
        private AbilityExecutor        _executor;

        // ── State ────────────────────────────────────────────────────

        public UnitCombatState CombatState { get; private set; } = UnitCombatState.Backline;
        public CombatRole      CombatRole  { get; private set; } = CombatRole.Free;
        public float           Initiative  { get; private set; }

        public bool IsUsingKick { get; private set; }

        public bool IsAnimating => CombatState == UnitCombatState.Execute
                                || CombatState == UnitCombatState.Recover
                                || CombatState == UnitCombatState.Stagger
                                || CombatState == UnitCombatState.Dodging;

        private TerrainBattleUnit _currentTarget;
        public  TerrainBattleUnit CurrentTarget => _currentTarget;

        // Move-based engine handoff. Set true once the unit is engaged
        // and BattleCombatEngine has taken over its move execution. While
        // true, this brain only updates target / energy regen and skips
        // the state machine; the engine drives moves, animation, hits.
        public bool EngineControlled { get; private set; }
        public void SetEngineControlled(bool v) { EngineControlled = v; }

        // Phase 6 — implicit loadout archetype inferred once at battle start.
        // Drives `BattleAIBrain.Decide` weighting; not a hard role lock.
        private TacticalRPG.Systems.BattleAIBrain.LoadoutArchetype _loadoutArchetype;
        public TacticalRPG.Systems.BattleAIBrain.LoadoutArchetype LoadoutArchetype => _loadoutArchetype;

        private float _attackTimer;
        private float _attackCooldown;
        private float _castTimer;
        private float _recoverTimer;
        private float _dodgeCooldownTimer;
        private float _staggerTimer;
        private UnitCombatState _stateBeforeStun;

        private bool  _engagedInMelee;
        private bool  _blockAnimFinished;
        private bool  _hitAnimFinished;

        private ResolvedTechnique _pendingTechnique;
        private SkillSlot         _pendingSkill;

        // Dodge pre-roll cache
        private bool _dodgePreRolled;
        private bool _dodgePreRollResult;

        private const float RecoverDuration = 1.5f;

        // ── Initialization ───────────────────────────────────────────

        public void Initialize(TerrainBattleUnit unit,
                               UnitMovementController mover,
                               UnitAnimationDriver anim,
                               BattleAnimancerDriver animancer,
                               AbilityExecutor executor)
        {
            _unit      = unit;
            _mover     = mover;
            _anim      = anim;
            _animancer = animancer;
            _executor  = executor;

            float spd = unit.Unit.currentStats.moveSpeed;
            _attackCooldown = Mathf.Max(0.6f, attackCooldownBase - spd * 0.1f);
            _attackTimer    = _attackCooldown;
            Initiative      = startingInitiative;

            _executor.OnAbilityComplete += OnAbilityComplete;

            // Phase 6 — infer loadout archetype once. Drives Decide weighting.
            _loadoutArchetype = TacticalRPG.Systems.BattleAIBrain.InferArchetype(unit.Unit);

            // Phase 7 — resolve stance once. Inspector override wins; otherwise
            // pull the unit definition's default stance.
            _resolvedStance = stance != null
                ? stance
                : (unit.Unit?.definition != null ? unit.Unit.definition.defaultStance : null);

            CombatState = UnitCombatState.Backline;
        }

        // ── Main Tick ────────────────────────────────────────────────

        public void Tick(float dt)
        {
            if (_unit.IsDead) return;

            _dodgeCooldownTimer -= dt;

            float regen = energyRegenRate;
            if (CombatState == UnitCombatState.Backline)
                regen += backlineEnergyBonus;
            _unit.Unit.RegenEnergy(regen * dt);

            // Move-based engine has taken over this unit — skip the
            // legacy state-machine path. Energy regen still ticks above
            // (engine doesn't own that yet). Target tracking is also
            // delegated; engine resolves target each tick.
            if (EngineControlled) return;

            switch (CombatState)
            {
                case UnitCombatState.Backline:    UpdateBackline();    break;
                case UnitCombatState.Engage:      UpdateEngage();      break;
                case UnitCombatState.Decide:      HandoffOrDecide();   break;
                case UnitCombatState.Melee:       UpdateMelee();       break;
                case UnitCombatState.CastMobile:  UpdateCastMobile();  break;
                case UnitCombatState.CastRooted:  UpdateCastRooted();  break;
                case UnitCombatState.Recover:     UpdateRecover();     break;
                case UnitCombatState.Stagger:     UpdateStagger();     break;
                case UnitCombatState.Stunned:     /* held by status system */ break;
                case UnitCombatState.Airborne:    /* held by reaction system */ break;
                case UnitCombatState.Repositioning: UpdateRepositioning(); break;
                // Execute / AttackDash / Dodging are driven by AbilityExecutor
            }
        }

        // When the move-based engine is active, the moment a unit reaches
        // Decide for the first time after engagement we hand control off
        // to the engine. From that point the legacy state machine no-ops
        // for this unit and the engine owns all combat behaviour.
        private void HandoffOrDecide()
        {
            var mgr = TerrainBattleManager.Instance;
            if (mgr != null && mgr.UseMoveEngine && mgr.CombatEngine != null)
            {
                EngineControlled = true;
                mgr.CombatEngine.RegisterUnit(_unit);
                CombatLogger.Instance?.Log(CombatLogger.CAT_STATE,
                    _unit.Unit?.DisplayName ?? _unit.gameObject.name,
                    "[engine] handoff — legacy state machine off, move engine takes over");
                _currentTarget = _currentTarget ?? FindNearestEnemy();
                return;
            }
            UpdateDecide();
        }

        // ── Airborne (paired-reaction state, set by resolver) ────────

        public void EnterAirborne()
        {
            if (_unit.IsDead || CombatState == UnitCombatState.Dead) return;
            _executor?.Cancel();
            TransitionTo(UnitCombatState.Airborne, forceOverride: true);
        }

        public void ExitAirborne()
        {
            if (CombatState != UnitCombatState.Airborne) return;
            // Brief stagger before the brain re-enters Decide (per spec line 521).
            _staggerTimer = 0.4f;
            TransitionTo(UnitCombatState.Stagger, forceOverride: true);
        }

        // Phase 14 — Repositioning runs a choreography primitive (orbit, fade, etc.).
        // When the primitive completes, fall back to Decide for the next plan.
        private void UpdateRepositioning()
        {
            var choreo = TerrainBattleManager.Instance?.Choreography;
            if (choreo == null || !choreo.IsRunningAny(_unit))
            {
                TransitionTo(UnitCombatState.Decide);
            }
        }

        // ── Stun (driven by BattleStatusEffectSystem via TerrainBattleUnit) ──

        public void EnterStun()
        {
            if (CombatState == UnitCombatState.Stunned || CombatState == UnitCombatState.Dead) return;
            _stateBeforeStun = CombatState;
            _executor?.Cancel();
            TransitionTo(UnitCombatState.Stunned, forceOverride: true);
        }

        public void ExitStun()
        {
            if (CombatState != UnitCombatState.Stunned) return;
            // Always come back through Decide so the brain re-evaluates after CC.
            TransitionTo(UnitCombatState.Decide, forceOverride: true);
        }

        // ── Speed for animator (consumed by TerrainBattleUnit.Update) ──

        public float GetNormalizedSpeed(float rawSpeed)
        {
            bool suppressSpeed = CombatState == UnitCombatState.Execute
                              || CombatState == UnitCombatState.Recover
                              || CombatState == UnitCombatState.Stagger
                              || (CombatState == UnitCombatState.Melee && _engagedInMelee);

            if (suppressSpeed) return 0f;

            float maxSpeed = Mathf.Max(1f, _unit.Unit.currentStats.moveSpeed);
            float n = Mathf.Clamp01(rawSpeed / maxSpeed);
            if (n < 0.1f) return 0f;
            if (n > 0.9f) return 1f;
            return n;
        }

        // ── State: Backline ──────────────────────────────────────────

        private void UpdateBackline()
        {
            if (TerrainBattleManager.Instance != null
                && TerrainBattleManager.Instance.RequestFrontlineSlot(_unit))
            {
                TransitionTo(UnitCombatState.Engage);
            }
        }

        // ── State: Engage ────────────────────────────────────────────

        private void UpdateEngage()
        {
            if (_currentTarget == null || _currentTarget.IsDead)
                _currentTarget = FindNearestEnemy();

            if (_currentTarget == null) return;

            TransitionTo(UnitCombatState.Decide);
        }

        // ── State: Decide ────────────────────────────────────────────

        private void UpdateDecide()
        {
            if (_currentTarget == null || _currentTarget.IsDead)
            {
                _currentTarget = FindNearestEnemy();
                if (_currentTarget == null) return;
            }

            // Phase 6 — run the centralized decision tree to set movement intent
            // and produce an archetype hint. Skill selection still uses the
            // existing PickBestSkill (gated by speed cost / gate); the archetype
            // hint informs movement and could later bias selection.
            var speedSysDecide = TerrainBattleManager.Instance?.Speed;
            var moveSysDecide  = TerrainBattleManager.Instance?.Movement;
            var choreoDecide   = TerrainBattleManager.Instance?.Choreography;
            BehaviorType beh   = _unit.Unit.behavior?.behaviorType ?? BehaviorType.Balanced;
            float curSpeed = speedSysDecide != null ? speedSysDecide.GetSpeed(_unit) : 30f;
            RangeBand band = choreoDecide != null
                ? choreoDecide.GetRangeBand(_unit, _currentTarget) : RangeBand.Mid;
            var decision = TacticalRPG.Systems.BattleAIBrain.Decide(
                _unit.Unit, curSpeed, _unit.Unit.currentEnergy, beh, _loadoutArchetype, _resolvedStance, band);
            moveSysDecide?.SetIntent(_unit, decision.movementIntent);

            string stanceTag = _resolvedStance != null ? _resolvedStance.id.ToString() : "—";
            Vector3 unitPos  = _unit.transform.position;
            float distToTarget = _currentTarget != null
                ? Vector3.Distance(unitPos, _currentTarget.transform.position) : 0f;
            CombatLogger.Instance?.Log(CombatLogger.CAT_STATE,
                _unit.Unit?.DisplayName ?? _unit.gameObject.name,
                $"Decide → archetype={decision.archetype} move={decision.movementIntent} " +
                $"(speed={curSpeed:F0} energy={_unit.Unit.currentEnergy:F0} " +
                $"pos=({unitPos.x:F1},{unitPos.z:F1}) dist={distToTarget:F1} " +
                $"stance={stanceTag} {decision.reason})");

            // BuildSpeed → just close on the target. The act of running with
            // Close intent gains +8/sec speed, which is enough to climb out
            // of Sluggish band over the next few exchanges. The dedicated
            // orbit primitive is parked for future use as a combo flourish
            // — running circles around a target in the middle of an active
            // fight didn't read well.
            if (decision.archetype == TacticalRPG.Systems.BattleAIBrain.ActionArchetype.BuildSpeed)
            {
                _pendingSkill     = null;
                _pendingTechnique = null;
                _castTimer        = 0f;
                TransitionTo(UnitCombatState.Melee);
                return;
            }

            // Disengage → physically back-step away from target via the
            // choreography primitive. Defensive units pull back to rebuild
            // speed / re-evaluate. Visible animation: backstep over 0.45s.
            if (decision.archetype == TacticalRPG.Systems.BattleAIBrain.ActionArchetype.Disengage)
            {
                _pendingSkill     = null;
                _pendingTechnique = null;
                _castTimer        = 0f;

                var choreo = TerrainBattleManager.Instance?.Choreography;
                if (choreo != null && _currentTarget != null)
                {
                    choreo.BackstepAway(_unit, _currentTarget, distance: 4f, durationSec: 0.45f);
                    TransitionTo(UnitCombatState.Repositioning);
                    return;
                }
                TransitionTo(UnitCombatState.Melee);
                return;
            }

            // Wait → standstill, re-enter Decide on next tick.
            if (decision.archetype == TacticalRPG.Systems.BattleAIBrain.ActionArchetype.Wait)
            {
                _pendingSkill     = null;
                _pendingTechnique = null;
                _castTimer        = 0f;
                TransitionTo(UnitCombatState.Melee);
                return;
            }

            SkillSlot chosenSkill = PickBestSkill();

            if (chosenSkill != null)
            {
                _pendingSkill      = chosenSkill;
                _pendingTechnique  = TerrainBattleManager.Instance.ResolveForDecide(chosenSkill, _unit.Unit);

                // Surface what was picked so combat is debuggable from the log.
                CombatLogger.Instance?.Log(CombatLogger.CAT_STATE,
                    _unit.Unit?.DisplayName ?? _unit.gameObject.name,
                    $"  picked [{_pendingTechnique.techniqueName}] strikes={_pendingTechnique.strikeCount} " +
                    $"speedCost={_pendingTechnique.speedCost:F0} cc={_pendingTechnique.ccType} " +
                    $"isCombo={_pendingTechnique.isCombo}");

                if (_pendingTechnique.type == TechniqueType.Summon
                    && TerrainBattleManager.Instance.HasActiveSummon(_unit.Unit.runtimeId))
                {
                    _pendingSkill     = null;
                    _pendingTechnique = null;
                    _castTimer        = 0f;
                    TransitionTo(UnitCombatState.Melee);
                    return;
                }

                // Don't re-cast orbs if they're already orbiting — punch to consume them instead
                if (_pendingTechnique.type == TechniqueType.OrbSummon
                    && _unit.GetComponent<OrbBuffHandler>()?.HasOrbs == true)
                {
                    _pendingSkill     = null;
                    _pendingTechnique = null;
                    _castTimer        = 0f;
                    TransitionTo(UnitCombatState.Melee);
                    return;
                }

                _castTimer = chosenSkill.actionSequence.Count * 0.3f;

                switch (_pendingTechnique.castType)
                {
                    case CastType.Melee:   TransitionTo(UnitCombatState.Melee);      break;
                    case CastType.Mobile:  TransitionTo(UnitCombatState.CastMobile); break;
                    case CastType.Rooted:  TransitionTo(UnitCombatState.CastRooted); break;
                }
            }
            else
            {
                _pendingSkill     = null;
                _pendingTechnique = null;
                _castTimer        = 0f;
                TransitionTo(UnitCombatState.Melee);
            }
        }

        // ── State: Melee ─────────────────────────────────────────────

        private void EnterMelee()
        {
            _attackTimer    = 0.15f;
            _engagedInMelee = false;
            TerrainBattleManager.Instance?.MeleeTokens?.ReleaseToken(_unit);
        }

        private void UpdateMelee()
        {
            if (_currentTarget == null || _currentTarget.IsDead)
            {
                _currentTarget = FindNearestEnemy();
                if (_currentTarget == null)
                {
                    TerrainBattleManager.Instance?.MeleeTokens?.ReleaseToken(_unit);
                    TransitionTo(UnitCombatState.Engage);
                    return;
                }
            }

            // If the target is mid-orbit (Repositioning), don't chase. Stand,
            // face them, wait. This produces the Gaara-style "static defender
            // while opponent circles" visual instead of an awkward catch-up
            // where the chaser runs after the orbiter.
            if (_currentTarget.CombatState == UnitCombatState.Repositioning)
            {
                _mover.FaceTarget(_currentTarget.transform);
                _engagedInMelee = false;
                return;
            }

            // Yield movement to active choreography primitives. When the
            // exchange coordinator fires BackstepAway after Recovery, the
            // brain would otherwise immediately call ChaseTarget — the two
            // movement systems fight each other and the visible result is a
            // brief backstep instantly cancelled by a forward walk. Pause
            // chase while the primitive owns the body.
            var activeChoreo = TerrainBattleManager.Instance?.Choreography;
            if (activeChoreo != null && activeChoreo.IsRunningAny(_unit))
            {
                _mover.FaceTarget(_currentTarget.transform);
                return;
            }

            var tokens   = TerrainBattleManager.Instance?.MeleeTokens;
            bool hasToken = tokens == null || tokens.RequestToken(_unit, _currentTarget);

            float dist = Vector3.Distance(_unit.transform.position, _currentTarget.transform.position);

            if (!hasToken)
            {
                _engagedInMelee = false;
                // Orbiting around the target → Circle intent (mid speed gain).
                TerrainBattleManager.Instance?.Movement?.SetIntent(_unit, MovementIntent.Circle);
                Vector3 orbitPos = tokens.GetOrbitPosition(_unit, _currentTarget);
                Vector3 toOrbit  = orbitPos - _unit.transform.position;
                toOrbit.y = 0f;
                if (toOrbit.magnitude > 0.3f)
                {
                    float orbitSpeed = _unit.Unit.currentStats.moveSpeed * 0.7f;
                    _mover.MoveDirection(toOrbit.normalized, orbitSpeed);
                    _mover.FaceTarget(_currentTarget.transform);
                }
                return;
            }

            // Separation: push back only before latch
            float minDist = attackRange * 0.55f;
            if (!_engagedInMelee && dist < minDist)
            {
                Vector3 awayDir = (_unit.transform.position - _currentTarget.transform.position);
                awayDir.y = 0f;
                if (awayDir.sqrMagnitude < 0.001f) awayDir = _unit.transform.right;
                awayDir.Normalize();
                _mover.MoveDirection(awayDir, _unit.Unit.currentStats.moveSpeed * 0.5f);
                _mover.FaceTarget(_currentTarget.transform);
                return;
            }

            // Latch hysteresis
            float chaseThreshold = _engagedInMelee ? attackRange * 1.5f : latchRange;
            if (dist > chaseThreshold)
            {
                _engagedInMelee = false;
                ChaseTarget();
                return;
            }

            _engagedInMelee = true;
            _mover.FaceTarget(_currentTarget.transform);

            var coordinator = TerrainBattleManager.Instance?.ExchangeCoordinator;
            if (coordinator != null)
            {
                CombatRole role = coordinator.RequestRole(_unit);
                if (role == CombatRole.Defender) return;
                if (role == CombatRole.Free)     return;
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = _attackCooldown;

            _mover.FaceTargetSnap(_currentTarget.transform);
            TransitionTo(UnitCombatState.Execute);
        }

        // ── State: CastMobile ────────────────────────────────────────

        private void UpdateCastMobile()
        {
            if (_currentTarget != null && !_currentTarget.IsDead)
            {
                float dist = Vector3.Distance(_unit.transform.position, _currentTarget.transform.position);
                if (dist > attackRange)
                    ChaseTarget();
                else
                    _mover.FaceTarget(_currentTarget.transform);
            }

            _castTimer -= Time.deltaTime;
            if (_castTimer <= 0f)
                TransitionTo(UnitCombatState.Execute);
        }

        // ── State: CastRooted ────────────────────────────────────────

        private void UpdateCastRooted()
        {
            if (_currentTarget != null && !_currentTarget.IsDead)
                _mover.FaceTarget(_currentTarget.transform);

            _castTimer -= Time.deltaTime;
            if (_castTimer <= 0f)
                TransitionTo(UnitCombatState.Execute);
        }

        // ── State: Recover ───────────────────────────────────────────

        public void EnterDefendWindow(float duration)
        {
            if (_unit.IsDead) return;
            _recoverTimer      = duration;
            _blockAnimFinished = false;
            TransitionTo(UnitCombatState.Recover);
        }

        private void UpdateRecover()
        {
            _recoverTimer -= Time.deltaTime;

            bool isDefender = CombatRole == CombatRole.Defender;
            bool animDone   = isDefender
                ? (_blockAnimFinished || _recoverTimer <= 0f)
                : _recoverTimer <= 0f;

            if (animDone)
            {
                _anim.ClearRecovering();

                if (CombatRole == CombatRole.Attacker)
                {
                    SetCombatRole(CombatRole.Free);
                    TerrainBattleManager.Instance?.ExchangeCoordinator?.OnAttackerRecoveryComplete(_unit);
                }
                else
                {
                    SetCombatRole(CombatRole.Free);
                }

                TransitionTo(UnitCombatState.Decide);
            }
        }

        // ── State: Stagger ───────────────────────────────────────────

        public void EnterStagger(float duration)
        {
            if (_unit.IsDead) return;
            if (CombatState == UnitCombatState.Stagger && _staggerTimer > duration) return;

            _staggerTimer = duration;
            TransitionTo(UnitCombatState.Stagger, forceOverride: true);
        }

        private void UpdateStagger()
        {
            _staggerTimer -= Time.deltaTime;
            if (_hitAnimFinished || _staggerTimer <= 0f)
            {
                _hitAnimFinished = false;
                _mover.StartReengage();
                TransitionTo(UnitCombatState.Engage);
            }
        }

        // ── Animation event receivers (called by TerrainBattleUnit, NOT by Unity directly) ──

        public void HandleBlockEnd()
        {
            CombatLogger.Instance?.Log(CombatLogger.CAT_ANIM,
                _unit.Unit?.DisplayName ?? _unit.gameObject.name,
                $"OnBlockEnd  state={CombatState}  role={CombatRole}");
            if (CombatState != UnitCombatState.Recover) return;
            _blockAnimFinished = true;
        }

        public void HandleHitEnd()
        {
            CombatLogger.Instance?.Log(CombatLogger.CAT_ANIM,
                _unit.Unit?.DisplayName ?? _unit.gameObject.name,
                $"OnHitEnd  state={CombatState}");
            if (CombatState != UnitCombatState.Stagger) return;
            _hitAnimFinished = true;
        }

        // ── AbilityExecutor callback ─────────────────────────────────

        private void OnAbilityComplete()
        {
            // Execute finished → move to Recover
            if (CombatState == UnitCombatState.Execute
             || CombatState == UnitCombatState.AttackDash)
            {
                _recoverTimer = RecoverDuration;
                TransitionTo(UnitCombatState.Recover);
            }
            else if (CombatState == UnitCombatState.Dodging)
            {
                TransitionTo(UnitCombatState.Decide);
            }
        }

        // ── TransitionTo ─────────────────────────────────────────────

        public void TransitionTo(UnitCombatState newState, bool forceOverride = false)
        {
            if (!forceOverride && CombatState == newState) return;

            var logger = CombatLogger.Instance;
            string uName = _unit.Unit?.DisplayName ?? _unit.gameObject.name;

            if (newState == UnitCombatState.Execute && CombatState == UnitCombatState.Execute)
                logger?.Warn(uName, $"Execute→Execute without Recover (interrupt!) role={CombatRole}");

            logger?.Log(CombatLogger.CAT_STATE, uName,
                $"{CombatState} → {newState}  role={CombatRole}  ini={Initiative:F1}  animating={IsAnimating}");

            CombatState = newState;

            // Phase 5 — push a movement intent for the entered state. The
            // brain may override this within a state (e.g. UpdateMelee sets
            // Circle when orbiting), but the entry intent gives every state a
            // sensible default for BattleSpeedSystem's gain calculation.
            BattleMovementSystem moveSys = TerrainBattleManager.Instance?.Movement;
            if (moveSys != null)
            {
                switch (newState)
                {
                    case UnitCombatState.Engage:
                    case UnitCombatState.Melee:
                    case UnitCombatState.CastMobile:
                        moveSys.SetIntent(_unit, MovementIntent.Close);
                        break;
                    case UnitCombatState.AttackDash:
                        moveSys.SetIntent(_unit, MovementIntent.Dash);
                        break;
                    case UnitCombatState.Dodging:
                        moveSys.SetIntent(_unit, MovementIntent.Disengage);
                        break;
                    case UnitCombatState.Repositioning:
                        moveSys.SetIntent(_unit, MovementIntent.Circle);
                        break;
                    case UnitCombatState.Backline:
                    case UnitCombatState.Decide:
                    case UnitCombatState.CastRooted:
                    case UnitCombatState.Execute:
                    case UnitCombatState.Recover:
                    case UnitCombatState.Stagger:
                    case UnitCombatState.Stunned:
                    case UnitCombatState.Dead:
                        moveSys.SetIntent(_unit, MovementIntent.Hold);
                        break;
                }
            }

            switch (newState)
            {
                case UnitCombatState.Melee:
                    EnterMelee();
                    break;

                case UnitCombatState.Execute:
                    LaunchExecuteAbility();
                    break;

                case UnitCombatState.Stagger:
                    _hitAnimFinished = false;
                    _anim.PlayHitReact();
                    break;

                case UnitCombatState.Dodging:
                    var dodgeCtx = new AbilityContext
                    {
                        Unit  = _unit,
                        Mover = _mover,
                        Anim  = _anim,
                    };
                    var dodgeAbility = new DodgeAbility(
                        dodgeDistance, dodgeArcHeight,
                        dodgeMoveDuration, dodgePauseDuration);
                    dodgeAbility.Bind(dodgeCtx);
                    _executor.Run(dodgeAbility);
                    break;

                case UnitCombatState.Recover:
                    if (CombatRole == CombatRole.Defender)
                        _anim.PlayBlock();
                    else
                        _anim.PlayAttackerRecover();
                    break;

                case UnitCombatState.CastMobile:
                case UnitCombatState.CastRooted:
                    _anim.PlayCast();
                    break;

                case UnitCombatState.Dead:
                    _anim.PlayDeath();
                    TerrainBattleManager.Instance?.MeleeTokens?.ReleaseToken(_unit);
                    _executor.Cancel();
                    break;
            }
        }

        private void LaunchExecuteAbility()
        {
            // Phase 14 — Initiation dash. Only fires when there's enough
            // ground to cover that the dash reads as a real approach (>4.5u);
            // otherwise the dash becomes a sub-second snap that compounds
            // with the BackstepAway and looks jittery.
            if (_currentTarget != null && !_currentTarget.IsDead)
            {
                float dist = Vector3.Distance(_unit.transform.position, _currentTarget.transform.position);
                var choreoLE = TerrainBattleManager.Instance?.Choreography;
                if (choreoLE != null && dist > 4.5f)
                {
                    choreoLE.DashToTarget(_unit, _currentTarget, closeDistance: 1.8f, durationSec: 0.20f);
                }
            }

            // Phase 11 — populate executionTime on the resolved technique so
            // the ability can scale its hold duration. Computed here (Execute
            // entry) so the speed band reflects the unit's real-time state,
            // not the slightly-stale state at Decide time.
            // Reference base of 1.0s — combat should be snappy at default SPD.
            const float ReferenceBaseHold = 1.0f;
            BattleSpeedSystem speedSys = TerrainBattleManager.Instance?.Speed;
            BattleStatusEffectSystem statusSys = TerrainBattleManager.Instance?.StatusEffects;
            SpeedBand band = speedSys != null ? speedSys.GetSpeedBand(_unit) : SpeedBand.Engaged;
            if (_pendingTechnique != null && _pendingTechnique.executionTime <= 0f)
            {
                float t = TacticalRPG.Systems.CombatTimingFormula
                    .ComputeExecutionTime(ReferenceBaseHold, _unit.Unit, _pendingTechnique, band);
                // Phase 10 — Slow CC stretches execution time (1 / slowFactor).
                if (statusSys != null)
                {
                    float slowFactor = statusSys.GetSlowFactor(_unit);
                    if (slowFactor < 1f) t /= slowFactor;
                }
                _pendingTechnique.executionTime = t;
            }

            // Build context shared by all abilities
            var ctx = new AbilityContext
            {
                Unit      = _unit,
                Mover     = _mover,
                Anim      = _anim,
                Animancer = _animancer,
                Target    = _currentTarget,
                Skill     = _pendingSkill,
                Technique = _pendingTechnique,
            };

            ActiveAbility ability = SelectExecuteAbility();

            _pendingSkill     = null;
            _pendingTechnique = null;

            ability.Bind(ctx);
            _executor.Run(ability);
        }

        // Skill-specific Animancer dispatch goes first; falls through to the
        // legacy random punch / kick / dash mix when no profile is configured
        // for the resolved technique.
        private ActiveAbility SelectExecuteAbility()
        {
            // LaunchCombo techniques (e.g. Crescent Kick) dispatch to the
            // dedicated cinematic ability, which routes through the
            // resolver's RunLaunchCombo coroutine (launch + aerial + far
            // knockback). Without this, the brain falls through to plain
            // multi-strike and the launch choreography never fires.
            if (_pendingTechnique != null
                && _pendingTechnique.type == TechniqueType.LaunchCombo)
            {
                IsUsingKick = true;
                return new LaunchComboAbility();
            }

            // Phase 2 — data-driven Animancer dispatch. Look up a profile bound
            // to this technique name; if found, play it through the central driver.
            AttackProfile profile = FindProfileFor(_pendingTechnique);
            if (profile != null
                && _animancer != null
                && _animancer.IsAvailable(_unit))
            {
                IsUsingKick = false;
                return new AnimancerMeleeAbility(profile, _animancer);
            }

            float roll = Random.value;
            if (roll < 0.15f)
            {
                IsUsingKick = false;
                return new AttackDashAbility(attackDashDuration, attackRange);
            }
            if (roll < 0.55f)
            {
                IsUsingKick = true;
                return new FlyingKickAbility(flyingKickLungeDistance, flyingKickLungeDuration);
            }
            IsUsingKick = false;
            return new MeleeStrikeAbility(useKick: false);
        }

        // ── Dodge ────────────────────────────────────────────────────

        public bool WillDodge()
        {
            if (CombatState == UnitCombatState.Dead) return false;
            if (CombatState == UnitCombatState.Stunned) return false;
            if (TerrainBattleManager.Instance != null && !TerrainBattleManager.Instance.IsDodgeEnabled) return false;
            if (_dodgeCooldownTimer > 0f) return false;
            if (_unit.Unit.currentEnergy < dodgeEnergyCost) return false;
            float chance = ComputeDodgeChance();
            _dodgePreRollResult = Random.value <= chance;
            _dodgePreRolled     = true;
            return _dodgePreRollResult;
        }

        public bool TryDodge()
        {
            if (CombatState == UnitCombatState.Dead) return false;
            if (CombatState == UnitCombatState.Stunned) return false;
            if (TerrainBattleManager.Instance != null && !TerrainBattleManager.Instance.IsDodgeEnabled) return false;
            if (_dodgeCooldownTimer > 0f) return false;
            if (_unit.Unit.currentEnergy < dodgeEnergyCost) return false;

            bool dodges;
            if (_dodgePreRolled)
            {
                dodges          = _dodgePreRollResult;
                _dodgePreRolled = false;
            }
            else
            {
                dodges = Random.value <= ComputeDodgeChance();
            }

            if (!dodges) return false;

            _unit.Unit.SpendEnergy(dodgeEnergyCost);
            _dodgeCooldownTimer = dodgeCooldown;
            TransitionTo(UnitCombatState.Dodging);
            return true;
        }

        // Phase 9 — speed-modulated dodge formula.
        //   stat-base = SPD_stat × 0.05   (existing)
        //   modifier  = 0.5 + currentSpeed / 100   (0.5x at 0 speed, 1.5x at 100)
        // Clamped to a sane upper bound so 100-SPD heroes don't reach near-100 % dodge.
        private float ComputeDodgeChance()
        {
            float statChance = _unit.Unit.currentStats.moveSpeed * 0.05f;
            BattleSpeedSystem speedSys = TerrainBattleManager.Instance?.Speed;
            float curSpeed = speedSys != null ? speedSys.GetSpeed(_unit) : 30f;
            float modifier = 0.5f + curSpeed / 100f;
            return Mathf.Clamp(statChance * modifier, 0f, 0.75f);
        }

        // ── Initiative ───────────────────────────────────────────────

        public void SpendInitiative(float amount)
        {
            float before = Initiative;
            Initiative = Mathf.Max(0f, Initiative - amount);
            CombatLogger.Instance?.Log(CombatLogger.CAT_INIT,
                _unit.Unit?.DisplayName ?? _unit.gameObject.name,
                $"SPEND {amount:F1}  {before:F1} → {Initiative:F1}");
        }

        public void GainInitiative(float amount)
        {
            float before = Initiative;
            Initiative += amount;
            CombatLogger.Instance?.Log(CombatLogger.CAT_INIT,
                _unit.Unit?.DisplayName ?? _unit.gameObject.name,
                $"GAIN  {amount:F1}  {before:F1} → {Initiative:F1}");
        }

        public void SetCombatRole(CombatRole role) { CombatRole = role; }

        // ── Helpers ──────────────────────────────────────────────────

        private void ChaseTarget()
        {
            float fullSpeed = _unit.Unit.currentStats.moveSpeed;
            float speed     = _mover.GetChaseSpeed(fullSpeed);

            // Phase 10 — Slow CC reduces movement speed.
            BattleStatusEffectSystem statusSys = TerrainBattleManager.Instance?.StatusEffects;
            if (statusSys != null) speed *= statusSys.GetSlowFactor(_unit);

            float slowZone = attackRange + 1f;
            float dist     = Vector3.Distance(_unit.transform.position, _currentTarget.transform.position);
            if (dist < slowZone)
                speed *= Mathf.Clamp01((dist - attackRange) / 1f);

            _mover.FaceTarget(_currentTarget.transform);
            _mover.MoveToward(_currentTarget.transform.position, speed, attackRange);
        }

        private SkillSlot PickBestSkill()
        {
            if (_unit.Unit.equippedSkills == null || _unit.Unit.equippedSkills.Count == 0)
                return null;

            SkillSlot best     = null;
            float     bestCost = -1f;

            BattleSpeedSystem speedSys = TerrainBattleManager.Instance?.Speed;
            float currentSpeed = speedSys != null ? speedSys.GetSpeed(_unit) : 0f;

            foreach (var skill in _unit.Unit.equippedSkills)
            {
                if (skill.actionSequence.Count == 0) continue;

                float totalEnergyCost = 0f;
                foreach (var slot in skill.actionSequence)
                    if (slot.action != null) totalEnergyCost += slot.action.energyCost;

                if (totalEnergyCost > _unit.Unit.currentEnergy)
                {
                    Debug.Log($"[PickBestSkill] {_unit.Unit.DisplayName} CANNOT afford skill " +
                              $"(energy cost={totalEnergyCost} have={_unit.Unit.currentEnergy:F1})");
                    continue;
                }

                // Phase 4 — speed gate / cost check. Resolve the technique to
                // read its speed properties; gate-fail or unaffordable skills
                // are skipped here so the brain falls back to a basic action.
                ResolvedTechnique preview = TerrainBattleManager.Instance?.ResolveForDecide(skill, _unit.Unit);
                if (preview != null)
                {
                    if (preview.speedGate > 0f && currentSpeed < preview.speedGate)
                    {
                        Debug.Log($"[PickBestSkill] {_unit.Unit.DisplayName} skill [{preview.techniqueName}] " +
                                  $"gated by speed (need {preview.speedGate:F0}, have {currentSpeed:F0}).");
                        continue;
                    }
                    if (preview.speedCost > 0f && currentSpeed < preview.speedCost)
                    {
                        Debug.Log($"[PickBestSkill] {_unit.Unit.DisplayName} skill [{preview.techniqueName}] " +
                                  $"unaffordable speed (cost {preview.speedCost:F0}, have {currentSpeed:F0}).");
                        continue;
                    }
                }

                if (totalEnergyCost > bestCost)
                {
                    bestCost = totalEnergyCost;
                    best     = skill;
                }
            }

            return best;
        }

        private TerrainBattleUnit FindNearestEnemy()
        {
            // Phase 7 — stance-driven target priority. Falls back to nearest
            // if no stance is assigned, or if the priority finds no candidate.
            var mgr = TerrainBattleManager.Instance;
            if (mgr == null) return null;
            if (_resolvedStance != null)
            {
                var t = mgr.TargetFinder.GetTarget(_unit, _resolvedStance.targetPriority);
                if (t != null && !t.IsDead) return t;
            }
            return mgr.GetNearestEnemy(_unit);
        }

        private AttackProfile FindProfileFor(ResolvedTechnique tech)
        {
            if (tech == null || techniqueProfiles == null) return null;
            for (int i = 0; i < techniqueProfiles.Count; i++)
            {
                var binding = techniqueProfiles[i];
                if (binding.profile == null) continue;
                if (string.Equals(binding.techniqueName, tech.techniqueName, StringComparison.Ordinal))
                    return binding.profile;
            }
            return null;
        }
    }
}
