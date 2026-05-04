using UnityEngine;
using TacticalRPG.DataModels;
using System.Collections.Generic;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// AI-controlled battle unit on 3D terrain. Works for BOTH player and enemy teams.
    /// Uses a UnitCombatState state machine, energy-based skill selection,
    /// casting phases, dodge/block, and engagement limits.
    /// Attach to a capsule with CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TerrainBattleUnit : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float gravity = -15f;

        [Header("Combat")]
        [SerializeField] private float attackRange    = 2.5f;
        [SerializeField] private float attackCooldown = 1.2f;

        [Header("Energy")]
        [SerializeField] private float energyRegenRate = 15f;
        [SerializeField] private float backlineEnergyBonus = 10f;

        [Header("Animation")]
        [SerializeField] private Animator _animator;
        [SerializeField] [Range(0.5f, 3f)] private float animationSpeed = 1.5f;

        [Header("Flying Kick")]
        [Tooltip("Which KickVariant float value selects the flying kick clip in the blend tree.")]
        [SerializeField] private float flyingKickVariantThreshold = 0.75f;
        [Tooltip("How far forward the unit lunges during the flying kick.")]
        [SerializeField] private float flyingKickLungeDistance = 2.5f;
        [Tooltip("How long the lunge movement takes (seconds). Match to the clip's airborne phase).")]
        [SerializeField] private float flyingKickLungeDuration = 0.35f;
        [Tooltip("Enable foot IK during kick animations to keep the planted foot on the ground.")]
        [SerializeField] private bool enableFootIK = true;
        [Tooltip("How far down to raycast from each foot bone to find the floor.")]
        [SerializeField] private float footRaycastDistance = 1.5f;
        [Tooltip("Layer mask for the floor raycast — set to your ground layer.")]
        [SerializeField] private LayerMask footIKLayerMask = ~0;



        [Header("Dodge")]
        [SerializeField] private float dodgeDistance = 3.5f;
        [SerializeField] private float dodgeMoveDuration = 0.3f;
        [SerializeField] private float dodgePauseDuration = 0.5f;
        [SerializeField] private float dodgeEnergyCost = 10f;
        [SerializeField] private float dodgeCooldown = 2f;
        [SerializeField] private float dodgeArcHeight = 1.5f;

        [Header("Attack Dash")]
        [SerializeField] private float attackDashDuration = 0.22f;

        [Header("Melee Engagement")]
        [Tooltip("Unit latches into melee and starts attacking once within this distance. Should be >= attackRange.")]
        [SerializeField] private float latchRange = 3.0f;

        [Header("Initiative")]
        [SerializeField] private float startingInitiative = 10f;

        public UnitRuntime Unit { get; private set; }
        public bool IsDead => Unit != null && Unit.isDead;
        public UnitCombatState CombatState { get; private set; } = UnitCombatState.Backline;
        public CombatRole CombatRole { get; private set; } = CombatRole.Free;
        public TerrainBattleUnit CurrentTarget => _currentTarget;

        /// <summary>
        /// Initiative pool — gates who may attack next.
        /// Higher initiative = more right to attack.
        /// Spent on each attack, gained on block or being hit.
        /// </summary>
        public float Initiative { get; private set; }

        public void SpendInitiative(float amount)
        {
            float before = Initiative;
            Initiative = Mathf.Max(0f, Initiative - amount);
            CombatLogger.Instance?.Log(CombatLogger.CAT_INIT, Unit?.DisplayName ?? gameObject.name,
                $"SPEND {amount:F1}  {before:F1} → {Initiative:F1}");
        }

        public void GainInitiative(float amount)
        {
            float before = Initiative;
            Initiative += amount;
            CombatLogger.Instance?.Log(CombatLogger.CAT_INIT, Unit?.DisplayName ?? gameObject.name,
                $"GAIN  {amount:F1}  {before:F1} → {Initiative:F1}");
        }

        public void SetCombatRole(CombatRole role)
        {
            CombatRole = role;
        }

        private CharacterController _controller;
        private HealthSystem _health;
        private TerrainBattleUnit _currentTarget;
        private Renderer _renderer;

        // Gravity
        private float _verticalVelocity;

        // Set to true once unit reaches attack range — prevents re-chase oscillation
        private bool  _engagedInMelee;

        // After knockback/stagger the unit walks back in slowly then ramps to full speed
        private float _reengageSpeed;        // current chase speed, ramped up over time
        private bool  _reengaging;           // true while ramping back up after stagger

        // State timers
        private float _attackTimer;
        private float _castTimer;
        private float _recoverTimer;
        private float _dodgeCooldownTimer;

        // Dodge movement
        private Vector3 _dodgeStart;
        private Vector3 _dodgeEnd;
        private float _dodgeTimer;
        private float _dodgePauseTimer;
        private bool _dodgeJumping;

        // Casting — resolved in Decide, consumed in Execute
        private ResolvedTechnique _pendingTechnique;
        private SkillSlot _pendingSkill;

        // Attack dash
        private Vector3 _dashStart;
        private Vector3 _dashEnd;
        private float   _dashTimer;

        // Step-back after hit
        private Vector3 _stepBackDir;
        private float   _stepBackTimer;

        // Stagger after heavy knockback
        private float _staggerTimer;

        // Recover duration — must be >= longest block clip length so OnBlockEnd fires first
        private const float RecoverDuration = 1.5f;

        // Safety fallback — must be longer than the longest attack/kick clip
        // so OnAttackEnd always fires first. Increase if clips are longer than this.
        private const float ExecuteHoldDuration = 2.0f;
        private float _executeTimer;
        private bool  _damageFired;
        private bool  _useKick;
        private bool  _attackAnimFinished;
        private bool  _blockAnimFinished;
        private bool  _hitAnimFinished;
        private bool  _isFlyingKick;
        private Vector3 _flyingKickStart;
        private Vector3 _flyingKickEnd;
        private float   _flyingKickTimer;

        /// <summary>True while this unit is playing a combat animation that must not be interrupted.</summary>
        public bool IsAnimating => CombatState == UnitCombatState.Execute
                                || CombatState == UnitCombatState.Recover
                                || CombatState == UnitCombatState.Stagger
                                || CombatState == UnitCombatState.Dodging;

        /// <summary>True when the current melee attack is a kick (not a punch).</summary>
        public bool IsUsingKick => _useKick;

        // Movement speed fed to animator each frame
        private float _currentMoveSpeed;

        // ── Initialization ──────────────────────────────────────────

        public void Initialize(UnitRuntime unit)
        {
            Unit = unit;

            _controller = GetComponent<CharacterController>();
            _controller.center = new Vector3(0f, 1f, 0f);
            _controller.height = 2f;

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            // If the unit has a relevant skill stat, scale animation speed from it now.
            // Call RefreshAnimationSpeed() again any time skills change.
            RefreshAnimationSpeed();

            _health = GetComponent<HealthSystem>();
            if (_health != null)
                _health.Setup(unit.maxHP, unit.DisplayName);

            _renderer = GetComponentInChildren<Renderer>();

            var capsuleCol = GetComponent<CapsuleCollider>();
            if (capsuleCol != null) Destroy(capsuleCol);

            float spd = unit.currentStats.moveSpeed;
            attackCooldown = Mathf.Max(0.6f, 1.8f - spd * 0.1f);

            // Pre-fill the attack timer so units don't fire the moment they enter melee
            _attackTimer = attackCooldown;

            Initiative = startingInitiative;

            unit.maxEnergy = 100f;
            unit.currentEnergy = 50f;

            CombatState = UnitCombatState.Backline;
        }

        // ── Main Update ─────────────────────────────────────────────

        private void Update()
        {
            if (Unit == null) return;
            if (IsDead) return;

            ApplyGravity();
            _dodgeCooldownTimer -= Time.deltaTime;

            // Energy regen (bonus when backline)
            float regen = energyRegenRate;
            if (CombatState == UnitCombatState.Backline)
                regen += backlineEnergyBonus;
            Unit.RegenEnergy(regen * Time.deltaTime);

            _currentMoveSpeed = 0f;

            switch (CombatState)
            {
                case UnitCombatState.Backline: UpdateBackline(); break;
                case UnitCombatState.Engage:   UpdateEngage();   break;
                case UnitCombatState.Decide:   UpdateDecide();   break;
                case UnitCombatState.Melee:    UpdateMelee();    break;
                case UnitCombatState.CastMobile:  UpdateCastMobile();  break;
                case UnitCombatState.CastRooted:  UpdateCastRooted();  break;
                case UnitCombatState.AttackDash:  UpdateAttackDash();  break;
                case UnitCombatState.Execute:     UpdateExecute();     break;
                case UnitCombatState.Recover:     UpdateRecover();     break;
                case UnitCombatState.Stagger:     UpdateStagger();     break;
                case UnitCombatState.Dodging:     UpdateDodging();     break;
            }

            // Normalize to 0–1 so the Animator blend tree threshold is always consistent.
            // Suppress Speed entirely during combat actions so they don't flicker run/idle.
            bool suppressSpeed = CombatState == UnitCombatState.Execute
                              || CombatState == UnitCombatState.Recover
                              || CombatState == UnitCombatState.Stagger
                              || (CombatState == UnitCombatState.Melee && _engagedInMelee);

            float normalizedSpeed = suppressSpeed ? 0f : (Unit != null
                ? Mathf.Clamp01(_currentMoveSpeed / Mathf.Max(1f, Unit.currentStats.moveSpeed))
                : 0f);

            // Hard snap — no damping, instant transition between idle and run
            if (normalizedSpeed < 0.1f) normalizedSpeed = 0f;
            else if (normalizedSpeed > 0.9f) normalizedSpeed = 1f;

            _animator?.SetFloat("Speed", normalizedSpeed);
            }

        // ── State: Backline ─────────────────────────────────────────

        private void UpdateBackline()
        {
            // Wait for a frontline slot from the manager
            if (TerrainBattleManager.Instance != null
                && TerrainBattleManager.Instance.RequestFrontlineSlot(this))
            {
                TransitionTo(UnitCombatState.Engage);
            }
        }

        // ── State: Engage ───────────────────────────────────────────

        private void UpdateEngage()
        {
            // Acquire a target, then immediately decide what to cast.
            // Positioning (chasing to melee range) is handled per cast type in
            // Melee / CastMobile / CastRooted — not here.
            if (_currentTarget == null || _currentTarget.IsDead)
                _currentTarget = FindNearestEnemy();

            if (_currentTarget == null) return;

            TransitionTo(UnitCombatState.Decide);
        }

        // ── State: Decide ───────────────────────────────────────────

        private void UpdateDecide()
        {
            // Re-acquire target if needed
            if (_currentTarget == null || _currentTarget.IsDead)
            {
                _currentTarget = FindNearestEnemy();
                if (_currentTarget == null) return;
            }

            // Pick a skill: highest energy cost we can afford
            SkillSlot chosenSkill = PickBestSkill();

            if (chosenSkill != null)
            {
                _pendingSkill = chosenSkill;

                // Resolve now to determine cast type — result stored for Execute
                _pendingTechnique = TerrainBattleManager.Instance.ResolveForDecide(chosenSkill, Unit);

                // If this skill would summon but a summon is already alive, skip to melee
                if (_pendingTechnique.type == TechniqueType.Summon
                    && TerrainBattleManager.Instance.HasActiveSummon(Unit.runtimeId))
                {
                    _pendingSkill = null;
                    _pendingTechnique = null;
                    _castTimer = 0f;
                    TransitionTo(UnitCombatState.Melee);
                    return;
                }

                CastType castType = _pendingTechnique.castType;
                float castTime = chosenSkill.actionSequence.Count * 0.3f;
                _castTimer = castTime;

                switch (castType)
                {
                    case CastType.Melee:   TransitionTo(UnitCombatState.Melee);      break;
                    case CastType.Mobile:  TransitionTo(UnitCombatState.CastMobile); break;
                    case CastType.Rooted:  TransitionTo(UnitCombatState.CastRooted); break;
                }
            }
            else
            {
                // No skill affordable — basic melee punch
                _pendingSkill = null;
                _pendingTechnique = null;
                _castTimer = 0f;
                TransitionTo(UnitCombatState.Melee);
            }
        }

        // ── State: Melee ────────────────────────────────────────────

        private void EnterMelee()
        {
            // Brief pause before first swing — long enough to face target cleanly,
            // short enough not to feel sluggish. NOT an artificial settle delay.
            _attackTimer    = 0.15f;
            _engagedInMelee = false;
            TerrainBattleManager.Instance?.MeleeTokens?.ReleaseToken(this);
        }

        /// <summary>
        /// Called by the attacker on the defender when a hit lands.
        /// Puts the defender into a timed block/guard state.
        /// </summary>
        public void EnterDefendWindow(float duration)
        {
            if (IsDead) return;
            _recoverTimer      = duration;
            _blockAnimFinished = false;
            TransitionTo(UnitCombatState.Recover);
        }

        private void UpdateMelee()
        {
            if (_currentTarget == null || _currentTarget.IsDead)
            {
                _currentTarget = FindNearestEnemy();
                if (_currentTarget == null)
                {
                    TerrainBattleManager.Instance?.MeleeTokens?.ReleaseToken(this);
                    TransitionTo(UnitCombatState.Engage);
                    return;
                }
            }

            var tokens = TerrainBattleManager.Instance?.MeleeTokens;
            bool hasToken = tokens == null || tokens.RequestToken(this, _currentTarget);

            float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);

            // ── No token: orbit at standoff distance ─────────────────────
            if (!hasToken)
            {
                _engagedInMelee = false;
                Vector3 orbitPos = tokens.GetOrbitPosition(this, _currentTarget);
                Vector3 toOrbit  = orbitPos - transform.position;
                toOrbit.y = 0f;
                if (toOrbit.magnitude > 0.3f)
                {
                    float orbitSpeed  = Unit.currentStats.moveSpeed * 0.7f;
                    Vector3 move = toOrbit.normalized * orbitSpeed + Vector3.up * _verticalVelocity;
                    _controller.Move(move * Time.deltaTime);
                    _currentMoveSpeed = orbitSpeed;
                    FaceTarget(_currentTarget.transform);
                }
                return;
            }

            // Separation: only push back if not yet engaged — once latched, don't micro-adjust
            float minDist = attackRange * 0.55f;
            if (!_engagedInMelee && dist < minDist)
            {
                Vector3 awayDir = (transform.position - _currentTarget.transform.position);
                awayDir.y = 0f;
                if (awayDir.sqrMagnitude < 0.001f) awayDir = transform.right;
                awayDir.Normalize();
                float separateSpeed = Unit.currentStats.moveSpeed * 0.5f;
                _controller.Move((awayDir * separateSpeed + Vector3.up * _verticalVelocity) * Time.deltaTime);
                _currentMoveSpeed = 0f;
                FaceTarget(_currentTarget.transform);
                return;
            }

            // Latch once within latchRange; hysteresis keeps us latched until 1.5x attackRange
            float chaseThreshold = _engagedInMelee ? attackRange * 1.5f : latchRange;
            if (dist > chaseThreshold)
            {
                _engagedInMelee = false;
                ChaseTarget();
                return;
            }

            // Latch: in ideal range — stand and attack
            _engagedInMelee = true;
            _currentMoveSpeed = 0f;
            FaceTarget(_currentTarget.transform);

            // Exchange coordinator: gate the attack timer behind role assignment.
            // This ensures only one unit swings at a time — the other waits as Defender.
            var coordinator = TerrainBattleManager.Instance?.ExchangeCoordinator;
            if (coordinator != null)
            {
                CombatRole role = coordinator.RequestRole(this);
                if (role == CombatRole.Defender) return;  // wait for attacker to finish
                if (role == CombatRole.Free)     return;  // target mid-action, hold
                // role == Attacker — fall through to fire
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = attackCooldown;

            Vector3 toTarget = _currentTarget.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(toTarget);

            TransitionTo(UnitCombatState.Execute);
        }

        // ── State: AttackDash ───────────────────────────────────────

        private void BeginAttackDash()
        {
            if (_currentTarget == null || _currentTarget.IsDead)
            {
                TransitionTo(UnitCombatState.Execute);
                return;
            }

            Vector3 toTarget = _currentTarget.transform.position - transform.position;
            toTarget.y = 0f;
            float currentDist = toTarget.magnitude;

            // If already well inside attack range, skip the dash entirely — just punch in place
            if (currentDist <= attackRange * 0.9f)
            {
                if (toTarget.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(toTarget);
                TransitionTo(UnitCombatState.Execute);
                return;
            }

            // Snap face to target instantly so the dash looks intentional
            if (toTarget.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(toTarget);

            _dashStart = transform.position;

            // Only step forward a short amount — not all the way to the target
            // This makes it look like a punch step rather than a teleport
            float stepDist = Mathf.Min(currentDist - attackRange * 0.7f, 1.5f);
            Vector3 dir = toTarget.normalized;
            _dashEnd   = transform.position + dir * stepDist;
            _dashEnd.y = _dashStart.y;

            _dashTimer = 0f;
            TransitionTo(UnitCombatState.AttackDash);
        }

        private void UpdateAttackDash()
        {
            if (_currentTarget == null || _currentTarget.IsDead)
            {
                TransitionTo(UnitCombatState.Execute);
                return;
            }

            _dashTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_dashTimer / attackDashDuration);
            float tSmooth = t * t * (3f - 2f * t);

            Vector3 desired = Vector3.Lerp(_dashStart, _dashEnd, tSmooth);
            Vector3 delta   = desired - transform.position;
            delta.y = _verticalVelocity * Time.deltaTime;
            _controller.Move(delta);

            FaceTarget(_currentTarget.transform);

            // Keep Speed at 0 during the dash — it's a short combat step, not a run
            // This stops the run animation firing for a single frame
            _currentMoveSpeed = 0f;

            if (t >= 1f)
            {
                Debug.Log($"[{Unit?.DisplayName}] AttackDash complete → Execute");
                TransitionTo(UnitCombatState.Execute);
            }
        }

        // ── State: CastMobile ───────────────────────────────────────

        private void UpdateCastMobile()
        {
            // Can still move while casting (Elemental)
            if (_currentTarget != null && !_currentTarget.IsDead)
            {
                float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);
                if (dist > attackRange)
                    ChaseTarget();
                else
                    FaceTarget(_currentTarget.transform);
            }

            _castTimer -= Time.deltaTime;
            if (_castTimer <= 0f)
                TransitionTo(UnitCombatState.Execute);
        }

        // ── State: CastRooted ───────────────────────────────────────

        private void UpdateCastRooted()
        {
            // Rooted — cannot move (Support)
            if (_currentTarget != null && !_currentTarget.IsDead)
                FaceTarget(_currentTarget.transform);

            _castTimer -= Time.deltaTime;
            if (_castTimer <= 0f)
                TransitionTo(UnitCombatState.Execute);
        }

        // ── State: Execute ──────────────────────────────────────────

        // Waits for OnAttackEnd Animation Event before moving to Recover.
        // _executeTimer is a safety fallback in case the event is missed.
        private void UpdateExecute()
        {
            _executeTimer -= Time.deltaTime;

            // Flying kick lunge — drive forward movement for the first flyingKickLungeDuration seconds
            if (_isFlyingKick && _flyingKickTimer < flyingKickLungeDuration)
            {
                _flyingKickTimer += Time.deltaTime;
                float t       = Mathf.Clamp01(_flyingKickTimer / flyingKickLungeDuration);
                float tSmooth = t * t * (3f - 2f * t);
                Vector3 desired = Vector3.Lerp(_flyingKickStart, _flyingKickEnd, tSmooth);
                Vector3 delta   = desired - transform.position;
                delta.y = _verticalVelocity * Time.deltaTime;
                _controller.Move(delta);
            }

            bool animDone = _attackAnimFinished || _executeTimer <= 0f;

            // Fire damage at hit frame (via OnHitFrame event) — fallback at 50% of timer
            if (!_damageFired && _executeTimer <= ExecuteHoldDuration * 0.5f)
            {
                _damageFired = true;
                FireDamage();
            }

            if (animDone)
            {
                _recoverTimer = RecoverDuration;
                TransitionTo(UnitCombatState.Recover);
            }
        }

        // ── Animation speed ─────────────────────────────────────────

        /// <summary>
        /// Recalculates and applies <see cref="Animator.speed"/> from the unit's stats.
        /// Call this once on Initialize and again whenever skills/stats change.
        /// </summary>
        public void RefreshAnimationSpeed()
        {
            if (_animator == null) return;

            float speed = animationSpeed;

            // Future: scale speed by a combat skill stat.
            // Example when skill data is ready:
            //   float skillFactor = Mathf.Lerp(0.8f, 1.5f, unit.CombatSkill / 100f);
            //   speed *= skillFactor;

            _animator.speed = speed;
        }

        // ── Foot IK ──────────────────────────────────────────────────

        /// <summary>
        /// Called by Unity after animation is evaluated each frame (requires IK Pass
        /// enabled on the Animator layer). Pins the planted foot to the floor during kicks.
        /// </summary>
        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || !enableFootIK) return;

            // Only apply during kick Execute — punches and other states don't need it
            bool isKicking = CombatState == UnitCombatState.Execute && _useKick;

            if (isKicking)
            {
                // During a kick the LEFT foot is typically planted — apply full IK weight
                ApplyFootIK(AvatarIKGoal.LeftFoot, 1f);
                // Right foot is the kicking foot — no IK so the animation drives it freely
                ApplyFootIK(AvatarIKGoal.RightFoot, 0f);
            }
            else
            {
                // Outside kicks, release both feet back to animation-driven
                ApplyFootIK(AvatarIKGoal.LeftFoot, 0f);
                ApplyFootIK(AvatarIKGoal.RightFoot, 0f);
            }
        }

        private void ApplyFootIK(AvatarIKGoal foot, float weight)
        {
            _animator.SetIKPositionWeight(foot, weight);
            _animator.SetIKRotationWeight(foot, weight);

            if (weight <= 0f) return;

            // Start raycast from slightly above the foot bone position
            Vector3 footPos = _animator.GetIKPosition(foot);
            Vector3 origin  = footPos + Vector3.up * 0.5f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, footRaycastDistance, footIKLayerMask))
            {
                _animator.SetIKPosition(foot, hit.point);
                _animator.SetIKRotation(foot,
                    Quaternion.LookRotation(transform.forward, hit.normal));
            }
        }

        // ── Animation Events ─────────────────────────────────────────

        /// <summary>
        /// Called by Animation Event at the impact frame of every attack/kick clip.
        /// </summary>
        public void OnHitFrame()
        {
            CombatLogger.Instance?.Log(CombatLogger.CAT_ANIM, Unit?.DisplayName ?? gameObject.name,
                $"OnHitFrame  state={CombatState}  damageFired={_damageFired}");
            if (CombatState != UnitCombatState.Execute || _damageFired) return;
            _damageFired = true;
            FireDamage();
        }

        /// <summary>
        /// Called by Animation Event at the last frame of every attack/kick clip.
        /// Signals that the full animation has finished — safe to transition to Recover.
        /// </summary>
        public void OnAttackEnd()
        {
            CombatLogger.Instance?.Log(CombatLogger.CAT_ANIM, Unit?.DisplayName ?? gameObject.name,
                $"OnAttackEnd  state={CombatState}");
            if (CombatState != UnitCombatState.Execute) return;
            _attackAnimFinished = true;
        }

        private void FireDamage()
        {
            if (_currentTarget == null || _currentTarget.IsDead)
                _currentTarget = FindNearestEnemy();

            if (_currentTarget == null || _currentTarget.IsDead) return;

            if (_pendingSkill != null)
            {
                float totalCost = 0f;
                foreach (var slot in _pendingSkill.actionSequence)
                    if (slot.action != null) totalCost += slot.action.energyCost;
                Unit.SpendEnergy(totalCost);

                Debug.Log($"[FireDamage] {Unit.DisplayName} firing skill — " +
                          $"isCombo={_pendingTechnique?.isCombo} " +
                          $"type={_pendingTechnique?.type} " +
                          $"name={_pendingTechnique?.techniqueName} " +
                          $"actions={_pendingSkill.actionSequence.Count}");

                // Open defend window for skill attacks too — defender should block, not stand still
                bool willDodgeSkill = _currentTarget.WillDodge();
                if (!willDodgeSkill && _currentTarget.CombatRole == CombatRole.Defender)
                    _currentTarget.EnterDefendWindow(RecoverDuration);

                if (_pendingTechnique != null && _pendingTechnique.isCombo)
                    TerrainBattleManager.Instance.ResolveSkillAttack(this, _currentTarget, _pendingTechnique);
                else
                    TerrainBattleManager.Instance.ExecuteIndividualActions(this, _currentTarget, _pendingSkill);
            }
            else
            {
                // Check if target will dodge before committing the block window
                bool hitLanded = !_currentTarget.WillDodge();
                if (hitLanded)
                {
                    if (_currentTarget.CombatRole == CombatRole.Defender)
                        _currentTarget.EnterDefendWindow(RecoverDuration);
                    TerrainBattleManager.Instance.ResolveBasicAttack(this, _currentTarget);
                }
                else
                {
                    TerrainBattleManager.Instance.ResolveBasicAttack(this, _currentTarget);
                }
            }

            _pendingSkill     = null;
            _pendingTechnique = null;
        }

        // ── State: Recover ──────────────────────────────────────────

        // Attacker: waits for _recoverTimer (no anim event needed — just blends back).
        // Defender: waits for OnBlockEnd Animation Event, timer is fallback.
        private void UpdateRecover()
        {
            _recoverTimer -= Time.deltaTime;

            bool isDefender = CombatRole == CombatRole.Defender;
            bool animDone   = isDefender ? (_blockAnimFinished || _recoverTimer <= 0f)
                                         : _recoverTimer <= 0f;

            if (animDone)
            {
                SetAnimBool("IsRecovering", false);

                if (CombatRole == CombatRole.Attacker)
                {
                    SetCombatRole(CombatRole.Free);
                    TerrainBattleManager.Instance?.ExchangeCoordinator?.OnAttackerRecoveryComplete(this);
                }
                else
                    SetCombatRole(CombatRole.Free);

                TransitionTo(UnitCombatState.Decide);
            }
        }

        /// <summary>
        /// Called by Animation Event at the last frame of the block clip.
        /// Signals the defender's guard animation has fully finished.
        /// </summary>
        public void OnBlockEnd()
        {
            CombatLogger.Instance?.Log(CombatLogger.CAT_ANIM, Unit?.DisplayName ?? gameObject.name,
                $"OnBlockEnd  state={CombatState}  role={CombatRole}");
            if (CombatState != UnitCombatState.Recover) return;
            _blockAnimFinished = true;
        }

        /// <summary>
        /// Called by Animation Event at the last frame of the hit-reaction clip.
        /// Signals that the stagger/hit animation has fully finished.
        /// </summary>
        public void OnHitEnd()
        {
            CombatLogger.Instance?.Log(CombatLogger.CAT_ANIM, Unit?.DisplayName ?? gameObject.name,
                $"OnHitEnd  state={CombatState}");
            if (CombatState != UnitCombatState.Stagger) return;
            _hitAnimFinished = true;
        }

        // ── Animator helpers ────────────────────────────────────────

        /// <summary>
        /// Sets a bool parameter only if it exists on this Animator Controller.
        /// Prevents errors when enemy prefabs use a simpler controller without the parameter.
        /// </summary>
        private void SetAnimBool(string name, bool value)
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
                if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
                { _animator.SetBool(name, value); return; }
        }

        private void SetAnimTrigger(string name)
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
                if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
                { _animator.SetTrigger(name); return; }
        }

        private void ResetAnimTrigger(string name)
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
                if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
                { _animator.ResetTrigger(name); return; }
        }

        // ── State: Stagger ──────────────────────────────────────────

        /// <summary>
        /// Called by BattleKnockbackSystem when knockback strength is Stumble or higher.
        /// Interrupts whatever the unit was doing and locks it out of attacking briefly.
        /// </summary>
        public void EnterStagger(float duration)
        {
            if (IsDead) return;

            // Don't re-stagger if already staggering with more time left
            if (CombatState == UnitCombatState.Stagger && _staggerTimer > duration) return;

            _staggerTimer = duration;
            TransitionTo(UnitCombatState.Stagger, forceOverride: true);
        }

        private void UpdateStagger()
        {
            // Wait for the hit-reaction animation to finish, or fall back to the timer
            _staggerTimer -= Time.deltaTime;
            bool done = _hitAnimFinished || _staggerTimer <= 0f;
            if (done)
            {
                _hitAnimFinished = false;
                // Flag that we're re-engaging so ChaseTarget starts at walk speed
                _reengaging     = true;
                _reengageSpeed  = 0f;
                TransitionTo(UnitCombatState.Engage);
            }
        }

        // ── State: Dodging ──────────────────────────────────────────

        private void UpdateDodging()
        {
            if (_dodgeJumping)
            {
                _dodgeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_dodgeTimer / dodgeMoveDuration);

                // Lerp with arc
                Vector3 pos = Vector3.Lerp(_dodgeStart, _dodgeEnd, t);
                float arc = dodgeArcHeight * Mathf.Sin(t * Mathf.PI);
                pos.y += arc;

                // Move via controller
                Vector3 delta = pos - transform.position;
                _controller.Move(delta);

                if (t >= 1f)
                {
                    _dodgeJumping = false;
                    _dodgePauseTimer = dodgePauseDuration;
                }
            }
            else
            {
                // Pause after landing
                _dodgePauseTimer -= Time.deltaTime;
                if (_dodgePauseTimer <= 0f)
                {
                    TransitionTo(UnitCombatState.Decide);
                }
            }
        }

        // ── Dodge Trigger (called by TerrainBattleManager) ──────────

        private bool _dodgePreRolled  = false;
        private bool _dodgePreRollResult = false;

        /// <summary>
        /// Peeks whether this unit would dodge right now, without consuming energy or triggering the dodge.
        /// Caches the result so TryDodge() uses the same roll.
        /// </summary>
        public bool WillDodge()
        {
            if (CombatState == UnitCombatState.Dead) return false;
            if (TerrainBattleManager.Instance != null && !TerrainBattleManager.Instance.IsDodgeEnabled) return false;
            if (_dodgeCooldownTimer > 0f) return false;
            if (Unit.currentEnergy < dodgeEnergyCost) return false;
            float chance = Mathf.Clamp(Unit.currentStats.moveSpeed * 0.01f, 0f, 0.25f);
            _dodgePreRollResult = Random.value <= chance;
            _dodgePreRolled     = true;
            return _dodgePreRollResult;
        }

        /// <summary>
        /// Attempts a dodge. Returns true if the dodge was performed.
        /// Called during opponent's Execute/Recover window.
        /// </summary>
        public bool TryDodge()
        {
            if (CombatState == UnitCombatState.Dead) return false;
            if (TerrainBattleManager.Instance != null && !TerrainBattleManager.Instance.IsDodgeEnabled) return false;
            if (_dodgeCooldownTimer > 0f) return false;
            if (Unit.currentEnergy < dodgeEnergyCost) return false;

            // Use pre-rolled result if available, otherwise roll fresh
            bool dodges;
            if (_dodgePreRolled)
            {
                dodges = _dodgePreRollResult;
                _dodgePreRolled = false;
            }
            else
            {
                float chance = Mathf.Clamp(Unit.currentStats.moveSpeed * 0.01f, 0f, 0.25f);
                dodges = Random.value <= chance;
            }

            if (!dodges) return false;

            Unit.SpendEnergy(dodgeEnergyCost);
            _dodgeCooldownTimer = dodgeCooldown;

            // Jump backward
            Vector3 backward = -transform.forward;
            backward.y = 0f;
            backward.Normalize();

            _dodgeStart = transform.position;
            _dodgeEnd = transform.position + backward * dodgeDistance;
            _dodgeTimer = 0f;
            _dodgeJumping = true;

            TransitionTo(UnitCombatState.Dodging);
            return true;
        }

        // ── Damage ──────────────────────────────────────────────────

        public void ApplyDamage(int finalDamage)
        {
            if (Unit == null || Unit.isDead) return;

            Unit.TakeDamage(finalDamage);

            bool isBlocking = CombatState == UnitCombatState.Recover && CombatRole == CombatRole.Defender;

            CombatLogger.Instance?.Log(CombatLogger.CAT_DMG, Unit?.DisplayName ?? gameObject.name,
                $"took {finalDamage}  blocking={isBlocking}  state={CombatState}  role={CombatRole}  hp={Unit.currentHP}");

            if (isBlocking)
            {
                GainInitiative(1f);
            }
            else
            {
                GainInitiative(2f);
                SetAnimTrigger("Hit");
            }

            if (_health != null)
                _health.SyncHP(Unit.currentHP);

            if (Unit.isDead)
                Die();
        }

        /// <summary>
        /// Damage with attacker reference so knockback can be triggered.
        /// </summary>
        public void ApplyDamage(int finalDamage, TerrainBattleUnit attacker)
        {
            bool isBlocking = CombatState == UnitCombatState.Recover && CombatRole == CombatRole.Defender;
            ApplyDamage(finalDamage);

            // Skip knockback when blocking — the block absorbs the impact
            if (attacker != null && !Unit.isDead && !isBlocking && TerrainBattleManager.Instance != null)
                TerrainBattleManager.Instance.ApplyKnockback(attacker, this, finalDamage);
        }

        // ── Knockback ────────────────────────────────────────────────

        /// <summary>
        /// Called each frame by BattleKnockbackSystem to push the unit
        /// in the calculated direction. Ignores the unit's own movement logic.
        /// </summary>
        public void ApplyKnockbackMove(Vector3 delta)
        {
            if (Unit == null || Unit.isDead || _controller == null) return;
            _controller.Move(delta);
        }

        // ── AI Skill Selection ──────────────────────────────────────

        /// <summary>
        /// Picks the skill with the highest total energy cost that the unit can afford.
        /// Returns null if no skill is affordable (fallback to basic attack).
        /// </summary>
        private SkillSlot PickBestSkill()
        {
            if (Unit.equippedSkills == null || Unit.equippedSkills.Count == 0)
                return null;

            SkillSlot best = null;
            float bestCost = -1f;

            foreach (var skill in Unit.equippedSkills)
            {
                if (skill.actionSequence.Count == 0) continue;

                float totalCost = 0f;
                foreach (var slot in skill.actionSequence)
                {
                    if (slot.action != null)
                        totalCost += slot.action.energyCost;
                }

                if (totalCost <= Unit.currentEnergy && totalCost > bestCost)
                {
                    bestCost = totalCost;
                    best = skill;
                }
                else
                {
                    Debug.Log($"[PickBestSkill] {Unit.DisplayName} CANNOT afford skill " +
                              $"(cost={totalCost} energy={Unit.currentEnergy:F1})");
                }
            }

            return best;
        }

        // ── Helpers ─────────────────────────────────────────────────

        private void TransitionTo(UnitCombatState newState, bool forceOverride = false)
        {
            if (!forceOverride && CombatState == newState) return;

            // ── Log suspicious back-to-back Execute (interrupt) ──────
            var logger = CombatLogger.Instance;
            string uName = Unit?.DisplayName ?? gameObject.name;

            if (newState == UnitCombatState.Execute && CombatState == UnitCombatState.Execute)
                logger?.Warn(uName, $"Execute→Execute without Recover (interrupt!) role={CombatRole}");

            if (newState == UnitCombatState.Execute && !_attackAnimFinished && CombatState == UnitCombatState.Recover)
                logger?.Warn(uName, "Entering Execute while still in Recover (no OnAttackEnd received yet)");

            logger?.Log(CombatLogger.CAT_STATE, uName,
                $"{CombatState} → {newState}  role={CombatRole}  ini={Initiative:F1}  animating={IsAnimating}");

            CombatState = newState;

            switch (newState)
            {
                case UnitCombatState.Melee:
                    EnterMelee();
                    break;
                case UnitCombatState.Execute:
                    _executeTimer        = ExecuteHoldDuration;
                    _damageFired         = false;
                    _attackAnimFinished  = false;
                    _useKick             = Random.value > 0.5f;
                    break;
            }

            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            ResetAnimTrigger("Attack");
            ResetAnimTrigger("Kick");
            ResetAnimTrigger("Hit");
            ResetAnimTrigger("Cast");
            ResetAnimTrigger("Death");
            ResetAnimTrigger("Dodge");
            ResetAnimTrigger("Block");

            switch (newState)
            {
                case UnitCombatState.Execute:
                    if (_useKick)
                    {
                        float variant = Random.value;
                        _isFlyingKick = variant >= flyingKickVariantThreshold;
                        _animator.SetFloat("KickVariant", variant);
                        SetAnimTrigger("Kick");

                        if (_isFlyingKick && _currentTarget != null)
                        {
                            // Snap to face target immediately — direction must be locked before lunge starts
                            Vector3 toTarget = _currentTarget.transform.position - transform.position;
                            toTarget.y = 0f;
                            if (toTarget.sqrMagnitude > 0.001f)
                                transform.rotation = Quaternion.LookRotation(toTarget);

                            _flyingKickStart = transform.position;
                            _flyingKickEnd   = transform.position + transform.forward * flyingKickLungeDistance;
                            _flyingKickEnd.y = _flyingKickStart.y;
                            _flyingKickTimer = 0f;
                        }
                    }
                    else
                    {
                        _isFlyingKick = false;
                        _animator.SetFloat("AttackVariant", Random.value);
                        SetAnimTrigger("Attack");
                    }
                    SetAnimBool("IsRecovering", false);
                    break;
                case UnitCombatState.Stagger:
                    _hitAnimFinished = false;
                    SetAnimTrigger("Hit");
                    SetAnimBool("IsRecovering", false);
                    break;
                case UnitCombatState.Dodging:
                    SetAnimTrigger("Dodge");
                    break;
                case UnitCombatState.Recover:
                    // Only play the block/guard animation if this unit is the Defender.
                    // Attacker recovery just blends back to locomotion silently.
                    if (CombatRole == CombatRole.Defender)
                        SetAnimTrigger("Block");
                    SetAnimBool("IsRecovering", true);
                    break;
                case UnitCombatState.CastMobile:
                case UnitCombatState.CastRooted:
                    SetAnimTrigger("Cast");
                    break;
                case UnitCombatState.Dead:
                    SetAnimTrigger("Death");
                    TerrainBattleManager.Instance?.MeleeTokens?.ReleaseToken(this);
                    break;
            }
        }

        private void ChaseTarget()
        {
            Vector3 direction = _currentTarget.transform.position - transform.position;
            direction.y = 0f;
            float dist = direction.magnitude;
            direction.Normalize();

            FaceTarget(_currentTarget.transform);

            // Never move closer than the minimum separation — stops units walking through each other
            float minDist = attackRange * 0.55f;
            if (dist <= minDist) return;
            if (dist <= attackRange) return;

            float fullSpeed = Unit.currentStats.moveSpeed;

            // After a stagger/knockback, ramp from walk speed up to full over 1.5 seconds
            // so the unit strides back in deliberately rather than snapping to a sprint
            float speed;
            if (_reengaging)
            {
                _reengageSpeed  = Mathf.MoveTowards(_reengageSpeed, fullSpeed, fullSpeed / 1.5f * Time.deltaTime);
                speed           = _reengageSpeed;
                if (_reengageSpeed >= fullSpeed) _reengaging = false;
            }
            else
            {
                speed = fullSpeed;
            }

            // Slow down over the final metre so we glide to a stop
            float slowZone = attackRange + 1f;
            if (dist < slowZone)
                speed *= Mathf.Clamp01((dist - attackRange) / 1f);

            Vector3 move = direction * speed + Vector3.up * _verticalVelocity;
            _controller.Move(move * Time.deltaTime);
            _currentMoveSpeed = speed;
        }

        private void Die()
        {
            TransitionTo(UnitCombatState.Dead, forceOverride: true);

            // Only tilt the capsule if no animator is driving a death pose
            if (_animator == null)
                transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

            if (_controller != null) _controller.enabled = false;

            if (_renderer != null && _animator == null)
                _renderer.material.color = Color.grey;

            if (TerrainBattleManager.Instance != null)
                TerrainBattleManager.Instance.OnUnitDied(this);
        }

        private TerrainBattleUnit FindNearestEnemy()
        {
            return TerrainBattleManager.Instance != null
                ? TerrainBattleManager.Instance.GetNearestEnemy(this)
                : null;
        }

        private void FaceTarget(Transform target)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            // States that manage their own vertical movement (fold _verticalVelocity inline)
            bool selfManaged = CombatState == UnitCombatState.Dodging
                            || CombatState == UnitCombatState.Melee
                            || CombatState == UnitCombatState.Engage
                            || CombatState == UnitCombatState.CastMobile
                            || CombatState == UnitCombatState.Recover
                            || CombatState == UnitCombatState.Stagger;
            if (!selfManaged)
                _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }
    }
}
