using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalRPG.ThirdPerson
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = -15f;

        [Header("Combat")]
        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private int attackDamage = 15;
        [SerializeField] private float attackCooldown = 0.5f;

        private CharacterController _controller;
        private Transform _cameraTransform;
        private float _verticalVelocity;
        private float _attackTimer;
        private HealthSystem _health;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _controller.center = Vector3.zero;
            _controller.height = 2f;
            _health = GetComponent<HealthSystem>();
            _cameraTransform = Camera.main != null ? Camera.main.transform : null;
            Cursor.lockState = CursorLockMode.Locked;

            // Remove primitive collider if present (CharacterController is the collider)
            var capsuleCol = GetComponent<CapsuleCollider>();
            if (capsuleCol != null) Destroy(capsuleCol);
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) return;

            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main != null ? Camera.main.transform : null;
                if (_cameraTransform == null) return;
            }

            HandleMovement();
            HandleAttack();

            // Toggle cursor lock with Escape
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
        }

        private void HandleMovement()
        {
            // Read WASD
            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            // Movement direction relative to camera facing
            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * input.y + camRight * input.x;

            // Rotate toward movement direction
            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Gravity
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 finalMove = moveDir * moveSpeed + Vector3.up * _verticalVelocity;
            _controller.Move(finalMove * Time.deltaTime);
        }

        private void HandleAttack()
        {
            _attackTimer -= Time.deltaTime;

            if (Mouse.current.leftButton.wasPressedThisFrame && _attackTimer <= 0f)
            {
                _attackTimer = attackCooldown;

                // Find nearest enemy in range
                var enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
                float closestDist = float.MaxValue;
                EnemyAI closestEnemy = null;

                foreach (var enemy in enemies)
                {
                    var enemyHealth = enemy.GetComponent<HealthSystem>();
                    if (enemyHealth != null && enemyHealth.IsDead) continue;

                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist <= attackRange && dist < closestDist)
                    {
                        closestDist = dist;
                        closestEnemy = enemy;
                    }
                }

                if (closestEnemy != null)
                {
                    var health = closestEnemy.GetComponent<HealthSystem>();
                    if (health != null)
                        health.TakeDamage(attackDamage);
                }
            }
        }

        private void OnGUI()
        {
            if (_health == null || !_health.IsDead) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100),
                "DEFEATED!", style);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(
                new Rect(Screen.width / 2 - 100, Screen.height / 2 + 60, 200, 45),
                "RESTART", btnStyle))
            {
                Cursor.lockState = CursorLockMode.None;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }
    }
}
