using Animancer;
using UnityEngine;
using TacticalRPG.DataModels;
using TacticalRPG.ThirdPerson.Abilities;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Thin container and public API surface for a combat unit on 3D terrain.
    ///
    /// Behaviour lives in:
    ///   UnitMovementController  ??CharacterController movement
    ///   UnitAnimationDriver     ??Animator calls
    ///   UnitBrainAI             ??state machine, energy, initiative, skill selection
    ///   AbilityExecutor         ??runs active ability each frame
    ///   Abilities/*             ??per-ability logic (melee, flying kick, dash, dodge)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(UnitMovementController))]
    [RequireComponent(typeof(UnitAnimationDriver))]
    [RequireComponent(typeof(UnitBrainAI))]
    [RequireComponent(typeof(AbilityExecutor))]
    public class TerrainBattleUnit : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private Animator _animator;
        [SerializeField] [Range(0.5f, 3f)] private float animationSpeed = 1.5f;

        [Header("Foot IK")]
        [SerializeField] private bool      enableFootIK        = true;
        [SerializeField] private float     footRaycastDistance = 1.5f;
        [SerializeField] private LayerMask footIKLayerMask = ~0;

        // ?? Subsystems ???????????????????????????????????????????????

        private UnitMovementController _mover;
        private UnitAnimationDriver    _animDriver;
        private BattleAnimancerDriver  _animancerDriver;   // central subsystem ref; null until manager has spawned it
        private UnitBrainAI            _brain;
        private AbilityExecutor        _executor;
        private HealthSystem           _health;
        private Renderer               _renderer;

        // ?? Public state (delegates to brain) ????????????????????????

        public UnitRuntime       Unit        { get; private set; }
        public bool              IsDead      => Unit != null && Unit.isDead;
        public UnitCombatState   CombatState => _brain != null ? _brain.CombatState : UnitCombatState.Backline;
        public CombatRole        CombatRole  => _brain != null ? _brain.CombatRole  : CombatRole.Free;
        public float             Initiative  => _brain != null ? _brain.Initiative  : 0f;
        public bool              IsAnimating => _brain != null && _brain.IsAnimating;
        public bool              IsUsingKick => _brain != null && _brain.IsUsingKick;
        public TerrainBattleUnit CurrentTarget => _brain?.CurrentTarget;

        // ?? Initialization ???????????????????????????????????????????

        public void Initialize(UnitRuntime unit)
        {
            Unit = unit;

            _mover           = GetComponent<UnitMovementController>();
            _animDriver      = GetComponent<UnitAnimationDriver>();
            _animancerDriver = TerrainBattleManager.Instance != null
                ? TerrainBattleManager.Instance.AnimancerDriver
                : null;
            _brain           = GetComponent<UnitBrainAI>();
            _executor        = GetComponent<AbilityExecutor>();

            var cc = GetComponent<CharacterController>();
            cc.center = new Vector3(0f, 1f, 0f);
            cc.height = 2f;

            var capsuleCol = GetComponent<CapsuleCollider>();
            if (capsuleCol != null) Destroy(capsuleCol);

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            _animDriver.Initialize(_animator, animationSpeed);
            // Register this unit's AnimancerComponent (if present) with the central driver.
            // Driver may legitimately be null in scenes that don't host TerrainBattleManager.
            if (_animancerDriver != null)
            {
                var animancer = GetComponentInChildren<AnimancerComponent>();
                if (animancer != null)
                    _animancerDriver.RegisterUnit(this, animancer);
            }
            // Register with the speed system and attach a speed bar.
            BattleSpeedSystem speedSys = TerrainBattleManager.Instance?.Speed;
            if (speedSys != null)
            {
                speedSys.RegisterUnit(this);
                var speedBar = GetComponent<SpeedBarUI>();
                if (speedBar == null) speedBar = gameObject.AddComponent<SpeedBarUI>();
                speedBar.Initialize(this, speedSys);
            }

            // Live combat overlay (archetype / intent / phase / CC).
            var overlay = GetComponent<CombatOverlayUI>();
            if (overlay == null) overlay = gameObject.AddComponent<CombatOverlayUI>();
            overlay.Initialize(this);

            _mover.Initialize();

            _health = GetComponent<HealthSystem>();
            if (_health != null)
                _health.Setup(unit.maxHP, unit.DisplayName);

            _renderer = GetComponentInChildren<Renderer>();

            unit.maxEnergy     = 100f;
            unit.currentEnergy = 50f;

            _brain.Initialize(this, _mover, _animDriver, _animancerDriver, _executor);

            // Register with the move-based combat engine. The engine will
            // skip backline units; the legacy brain still handles
            // engagement promotion. Once promoted, the engine takes over
            // moves/hits and the legacy state machine no-ops.
            var engine = TerrainBattleManager.Instance?.CombatEngine;
            if (engine != null && TerrainBattleManager.Instance.UseMoveEngine)
                engine.RegisterUnit(this);
        }

        // ?? Unity Update ?????????????????????????????????????????????

        private void Update()
        {
            if (Unit == null || IsDead) return;

            bool selfManaged = CombatState == UnitCombatState.Dodging
                            || CombatState == UnitCombatState.Melee
                            || CombatState == UnitCombatState.Engage
                            || CombatState == UnitCombatState.CastMobile
                            || CombatState == UnitCombatState.Recover
                            || CombatState == UnitCombatState.Stagger;
            _mover.TickGravity(selfManaged);

            _brain.Tick(Time.deltaTime);

            _animDriver.SetSpeed(_brain.GetNormalizedSpeed(_mover.CurrentMoveSpeed));
        }

        // ?? Foot IK (Unity lifecycle ??must live here) ???????????????

        private void OnAnimatorIK(int layerIndex)
        {
            if (!enableFootIK || _animDriver == null || !_animDriver.HasAnimator) return;

            bool isKicking = CombatState == UnitCombatState.Execute
                          && _executor != null && _executor.IsExecuting;

            if (isKicking)
            {
                ApplyFootIK(AvatarIKGoal.LeftFoot,  1f);
                ApplyFootIK(AvatarIKGoal.RightFoot, 0f);
            }
            else
            {
                ApplyFootIK(AvatarIKGoal.LeftFoot,  0f);
                ApplyFootIK(AvatarIKGoal.RightFoot, 0f);
            }
        }

        private void ApplyFootIK(AvatarIKGoal foot, float weight)
        {
            _animDriver.SetIKPositionWeight(foot, weight);
            _animDriver.SetIKRotationWeight(foot, weight);
            if (weight <= 0f) return;

            Vector3 footPos = _animDriver.GetIKPosition(foot);
            Vector3 origin  = footPos + Vector3.up * 0.5f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, footRaycastDistance, footIKLayerMask))
            {
                _animDriver.SetIKPosition(foot, hit.point);
                _animDriver.SetIKRotation(foot, Quaternion.LookRotation(transform.forward, hit.normal));
            }
        }

        // ?? Animation Events (forwarded from UnitAnimationEventRelay) ?

        public void OnHitFrame()
        {
            // When the move-based engine controls this unit, animation
            // events are decoupled from hit timing — engine uses frame
            // counters. Drop silently to keep logs clean.
            if (_brain != null && _brain.EngineControlled) return;

            // Animation events keep firing while the underlying clip plays.
            // Drop them outside the windows where damage timing is meaningful
            // — fix for "OnHitFrame state=Recover" leakage observed in the
            // combat log.
            bool live = CombatState == UnitCombatState.Execute
                     || CombatState == UnitCombatState.AttackDash;
            CombatLogger.Instance?.Log(CombatLogger.CAT_ANIM,
                Unit?.DisplayName ?? gameObject.name,
                $"OnHitFrame  state={CombatState}  forwarded={live}");
            if (!live) return;
            _executor?.NotifyAnimationEvent("HitFrame");
        }

        public void OnAttackEnd()
        {
            if (_brain != null && _brain.EngineControlled) return;

            bool live = CombatState == UnitCombatState.Execute
                     || CombatState == UnitCombatState.AttackDash;
            CombatLogger.Instance?.Log(CombatLogger.CAT_ANIM,
                Unit?.DisplayName ?? gameObject.name,
                $"OnAttackEnd  state={CombatState}  forwarded={live}");
            if (!live) return;
            _executor?.NotifyAnimationEvent("AttackEnd");
        }

        public void OnBlockEnd()  => _brain?.HandleBlockEnd();
        public void OnHitEnd()    => _brain?.HandleHitEnd();

        // ?? Public API used by external combat systems ???????????????

        public void SetCombatRole(CombatRole role)   => _brain?.SetCombatRole(role);
        public void SpendInitiative(float amount)    => _brain?.SpendInitiative(amount);
        public void GainInitiative(float amount)     => _brain?.GainInitiative(amount);

        public void EnterDefendWindow(float duration) => _brain?.EnterDefendWindow(duration);
        public void EnterStagger(float duration)      => _brain?.EnterStagger(duration);
        public bool WillDodge()                       => _brain != null && _brain.WillDodge();
        public bool TryDodge()                        => _brain != null && _brain.TryDodge();

        public void ApplyDamage(int finalDamage)
        {
            if (Unit == null || Unit.isDead) return;

            Unit.TakeDamage(finalDamage);

            bool isBlocking = CombatState == UnitCombatState.Recover
                           && CombatRole  == CombatRole.Defender;

            // Floating damage popup so impacts read visually even with placeholder anims.
            DamagePopup.Spawn(transform.position, finalDamage, isBlocking, finalDamage >= 25);

            Vector3 hitPos = transform.position;
            CombatLogger.Instance?.Log(CombatLogger.CAT_DMG, Unit?.DisplayName ?? gameObject.name,
                $"took {finalDamage}  blocking={isBlocking}  state={CombatState}  role={CombatRole}  hp={Unit.currentHP}  " +
                $"pos=({hitPos.x:F1},{hitPos.z:F1})");

            if (isBlocking)
                _brain?.GainInitiative(1f);
            else
            {
                _brain?.GainInitiative(2f);
                _animDriver?.PlayHitReact();
            }

            if (_health != null)
                _health.SyncHP(Unit.currentHP);

            if (Unit.isDead)
                Die();
        }

        public void ApplyDamage(int finalDamage, TerrainBattleUnit attacker)
        {
            bool isBlocking = CombatState == UnitCombatState.Recover
                           && CombatRole  == CombatRole.Defender;
            ApplyDamage(finalDamage);

            if (attacker != null && !Unit.isDead && !isBlocking && TerrainBattleManager.Instance != null)
                TerrainBattleManager.Instance.ApplyKnockback(attacker, this, finalDamage);
        }

        public void ApplyKnockbackMove(Vector3 delta)
        {
            if (Unit == null || Unit.isDead) return;
            _mover?.ApplyExternalDelta(delta);
        }

        public void RefreshAnimationSpeed()
        {
            _animDriver?.SetAnimationSpeed(animationSpeed);
        }

        private void OnDestroy()
        {
            _animancerDriver?.UnregisterUnit(this);
            TerrainBattleManager.Instance?.Speed?.UnregisterUnit(this);
            TerrainBattleManager.Instance?.StatusEffects?.RemoveAll(this);
            TerrainBattleManager.Instance?.Movement?.Clear(this);
            TerrainBattleManager.Instance?.CombatEngine?.UnregisterUnit(this);
        }

        // ── Status / CC hooks (called by BattleStatusEffectSystem) ───

        public void OnStunApplied()
        {
            if (_brain == null || IsDead) return;
            _brain.EnterStun();
        }

        public void OnStunExpired()
        {
            if (_brain == null || IsDead) return;
            _brain.ExitStun();
        }

        public void OnAirborneStart()
        {
            if (_brain == null || IsDead) return;
            _brain.EnterAirborne();
        }

        public void OnAirborneEnd()
        {
            if (_brain == null || IsDead) return;
            _brain.ExitAirborne();
        }

        // Death
        private void Die()
        {
            _brain?.TransitionTo(UnitCombatState.Dead, forceOverride: true);

            if (_animator == null)
                transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            if (_renderer != null && _animator == null)
                _renderer.material.color = Color.grey;

            if (TerrainBattleManager.Instance != null)
                TerrainBattleManager.Instance.OnUnitDied(this);
        }
    }
}
