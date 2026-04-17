using UnityEngine;
using TacticalRPG.DataModels;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// AI-controlled battle unit on 3D terrain. Works for BOTH player and enemy teams.
    /// Uses UnitRuntime stats and CombatResolutionSystem for damage.
    /// Attach to a capsule with CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TerrainBattleUnit : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float gravity = -15f;

        [Header("Combat")]
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float moveCooldown = 0.0f;

        public UnitRuntime Unit { get; private set; }
        public bool IsDead => Unit != null && Unit.isDead;

        private CharacterController _controller;
        private HealthSystem _health;
        private TerrainBattleUnit _currentTarget;
        private float _verticalVelocity;
        private float _attackTimer;
        private Renderer _renderer;

        // Set by TerrainBattleManager after spawning
        public void Initialize(UnitRuntime unit)
        {
            Unit = unit;

            _controller = GetComponent<CharacterController>();
            _controller.center = Vector3.zero;
            _controller.height = 2f;

            _health = GetComponent<HealthSystem>();
            if (_health != null)
                _health.Setup(unit.maxHP, unit.DisplayName);

            _renderer = GetComponentInChildren<Renderer>();

            // Destroy the default CapsuleCollider (CharacterController IS the collider)
            var capsuleCol = GetComponent<CapsuleCollider>();
            if (capsuleCol != null) Destroy(capsuleCol);

            // Speed-based cooldown: faster units attack more often
            float spd = unit.currentStats.moveSpeed;
            attackCooldown = Mathf.Max(0.4f, 1.5f - spd * 0.1f);
        }

        private void Update()
        {
            if (IsDead) return;
            if (Unit == null) return;

            ApplyGravity();

            // Find target
            if (_currentTarget == null || _currentTarget.IsDead)
                _currentTarget = FindNearestEnemy();

            if (_currentTarget == null) return;

            float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);

            if (dist <= attackRange)
            {
                FaceTarget(_currentTarget.transform);
                TryAttack();
            }
            else
            {
                ChaseTarget();
            }
        }

        private void ChaseTarget()
        {
            Vector3 direction = _currentTarget.transform.position - transform.position;
            direction.y = 0f;
            direction.Normalize();

            FaceTarget(_currentTarget.transform);

            float speed = Unit.currentStats.moveSpeed;
            Vector3 move = direction * speed + Vector3.up * _verticalVelocity;
            _controller.Move(move * Time.deltaTime);
        }

        private void TryAttack()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer > 0f) return;
            _attackTimer = attackCooldown;

            // Use CombatResolutionSystem via the manager
            TerrainBattleManager.Instance.ResolveAttack(this, _currentTarget);
        }

        public void ApplyDamage(int finalDamage)
        {
            if (Unit == null || Unit.isDead) return;

            Unit.TakeDamage(finalDamage);

            if (_health != null)
                _health.SyncHP(Unit.currentHP);

            if (Unit.isDead)
                Die();
        }

        private void Die()
        {
            // Fall over
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

            if (_controller != null) _controller.enabled = false;

            if (_renderer != null)
                _renderer.material.color = Color.grey;
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

            // Always apply gravity even when not chasing
            if (_currentTarget == null || IsDead)
                _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }
    }
}
