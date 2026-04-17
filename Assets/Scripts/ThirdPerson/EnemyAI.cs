using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    [RequireComponent(typeof(CharacterController))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float attackRange = 2f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float gravity = -15f;

        [Header("Combat")]
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private float attackCooldown = 1f;

        private CharacterController _controller;
        private Transform _player;
        private float _verticalVelocity;
        private float _attackTimer;
        private HealthSystem _health;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<HealthSystem>();

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                _player = player.transform;

            _controller.center = Vector3.zero;
            _controller.height = 2f;

            var capsuleCol = GetComponent<CapsuleCollider>();
            if (capsuleCol != null) Destroy(capsuleCol);
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) return;
            if (_player == null) return;

            var playerHealth = _player.GetComponent<HealthSystem>();
            if (playerHealth != null && playerHealth.IsDead) return;

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist <= attackRange)
            {
                FaceTarget();
                Attack();
            }
            else if (dist <= detectionRange)
            {
                ChasePlayer();
            }

            ApplyGravity();
        }

        private void ChasePlayer()
        {
            Vector3 direction = _player.position - transform.position;
            direction.y = 0f;
            direction.Normalize();

            FaceTarget();
            _controller.Move(direction * moveSpeed * Time.deltaTime +
                             Vector3.up * _verticalVelocity * Time.deltaTime);
        }

        private void Attack()
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                _attackTimer = attackCooldown;
                var playerHealth = _player.GetComponent<HealthSystem>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(attackDamage);
            }

            // Still apply gravity while attacking
            _controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }

        private void FaceTarget()
        {
            Vector3 dir = _player.position - transform.position;
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
        }
    }
}
