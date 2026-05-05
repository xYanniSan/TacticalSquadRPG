using UnityEngine;
using TacticalRPG.DataModels;
using TacticalRPG.ThirdPerson.Abilities;
// AttackProfile lives in TacticalRPG.DataModels; UnitAnimancerDriver in this namespace.

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

        [Header("Animancer Skill Profiles (PoC)")]
        [Tooltip("AttackProfile played for the Earth Fist combo when this unit fires it. " +
                 "Leave null to keep using the legacy Animator Controller path. " +
                 "Future: replace with an AbilityCatalog mapping technique name → profile.")]
        [SerializeField] private AttackProfile earthFistProfile;

        // ── Wired on Initialize() ────────────────────────────────────

        private TerrainBattleUnit _unit;
        private UnitMovementController _mover;
        private UnitAnimationDriver    _anim;
        private UnitAnimancerDriver    _animancer;   // optional
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

        private float _attackTimer;
        private float _attackCooldown;
        private float _castTimer;
        private float _recoverTimer;
        private float _dodgeCooldownTimer;
        private float _staggerTimer;

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
                               UnitAnimancerDriver animancer,
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

            switch (CombatState)
            {
                case UnitCombatState.Backline:    UpdateBackline();    break;
                case UnitCombatState.Engage:      UpdateEngage();      break;
                case UnitCombatState.Decide:      UpdateDecide();      break;
                case UnitCombatState.Melee:       UpdateMelee();       break;
                case UnitCombatState.CastMobile:  UpdateCastMobile();  break;
                case UnitCombatState.CastRooted:  UpdateCastRooted();  break;
                case UnitCombatState.Recover:     UpdateRecover();     break;
                case UnitCombatState.Stagger:     UpdateStagger();     break;
                // Execute / AttackDash / Dodging are driven by AbilityExecutor
            }
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

            SkillSlot chosenSkill = PickBestSkill();

            if (chosenSkill != null)
            {
                _pendingSkill      = chosenSkill;
                _pendingTechnique  = TerrainBattleManager.Instance.ResolveForDecide(chosenSkill, _unit.Unit);

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

            var tokens   = TerrainBattleManager.Instance?.MeleeTokens;
            bool hasToken = tokens == null || tokens.RequestToken(_unit, _currentTarget);

            float dist = Vector3.Distance(_unit.transform.position, _currentTarget.transform.position);

            if (!hasToken)
            {
                _engagedInMelee = false;
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
            if (_pendingTechnique != null
                && earthFistProfile != null
                && _animancer != null
                && _animancer.IsAvailable
                && _pendingTechnique.techniqueName == earthFistProfile.techniqueName)
            {
                IsUsingKick = false;
                return new EarthFistAbility(earthFistProfile, _animancer);
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
            if (TerrainBattleManager.Instance != null && !TerrainBattleManager.Instance.IsDodgeEnabled) return false;
            if (_dodgeCooldownTimer > 0f) return false;
            if (_unit.Unit.currentEnergy < dodgeEnergyCost) return false;
            float chance = Mathf.Clamp(_unit.Unit.currentStats.moveSpeed * 0.01f, 0f, 0.25f);
            _dodgePreRollResult = Random.value <= chance;
            _dodgePreRolled     = true;
            return _dodgePreRollResult;
        }

        public bool TryDodge()
        {
            if (CombatState == UnitCombatState.Dead) return false;
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
                float chance = Mathf.Clamp(_unit.Unit.currentStats.moveSpeed * 0.01f, 0f, 0.25f);
                dodges = Random.value <= chance;
            }

            if (!dodges) return false;

            _unit.Unit.SpendEnergy(dodgeEnergyCost);
            _dodgeCooldownTimer = dodgeCooldown;
            TransitionTo(UnitCombatState.Dodging);
            return true;
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

            foreach (var skill in _unit.Unit.equippedSkills)
            {
                if (skill.actionSequence.Count == 0) continue;

                float totalCost = 0f;
                foreach (var slot in skill.actionSequence)
                    if (slot.action != null) totalCost += slot.action.energyCost;

                if (totalCost <= _unit.Unit.currentEnergy && totalCost > bestCost)
                {
                    bestCost = totalCost;
                    best     = skill;
                }
                else
                {
                    Debug.Log($"[PickBestSkill] {_unit.Unit.DisplayName} CANNOT afford skill " +
                              $"(cost={totalCost} energy={_unit.Unit.currentEnergy:F1})");
                }
            }

            return best;
        }

        private TerrainBattleUnit FindNearestEnemy()
            => TerrainBattleManager.Instance?.GetNearestEnemy(_unit);
    }
}
