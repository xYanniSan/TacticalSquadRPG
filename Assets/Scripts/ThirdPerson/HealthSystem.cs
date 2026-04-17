using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    public class HealthSystem : MonoBehaviour
    {
        [SerializeField] private int maxHP = 100;
        [SerializeField] private string displayName = "Unit";

        public int CurrentHP => _currentHP;
        public int MaxHP => maxHP;
        public bool IsDead => _currentHP <= 0;

        private int _currentHP;
        private Renderer _renderer;
        private Color _originalColor;
        private float _flashTimer;
        private bool _initialized;
        private static readonly Color FlashColor = Color.red;
        private const float FlashDuration = 0.15f;

        /// <summary>
        /// Called by TerrainBattleManager to set HP from UnitDefinition stats.
        /// </summary>
        public void Setup(int hp, string name)
        {
            maxHP = hp;
            _currentHP = hp;
            displayName = name;
            _initialized = true;

            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null)
                _originalColor = _renderer.material.color;
        }

        /// <summary>
        /// Sync displayed HP with UnitRuntime (damage is applied through CombatResolutionSystem).
        /// </summary>
        public void SyncHP(int currentHP)
        {
            _currentHP = currentHP;

            // Flash red
            if (_renderer != null)
            {
                _renderer.material.color = FlashColor;
                _flashTimer = FlashDuration;
            }

            if (IsDead)
                Die();
        }

        private void Start()
        {
            if (!_initialized)
            {
                _currentHP = maxHP;
                _renderer = GetComponentInChildren<Renderer>();
                if (_renderer != null)
                    _originalColor = _renderer.material.color;
            }
        }

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && _renderer != null && !IsDead)
                    _renderer.material.color = _originalColor;
            }
        }

        public void TakeDamage(int damage)
        {
            if (IsDead) return;

            _currentHP = Mathf.Max(0, _currentHP - damage);

            // Flash red
            if (_renderer != null)
            {
                _renderer.material.color = FlashColor;
                _flashTimer = FlashDuration;
            }

            if (IsDead)
                Die();
        }

        private void Die()
        {
            // Fall over
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

            // Disable movement
            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            var enemy = GetComponent<EnemyAI>();
            if (enemy != null) enemy.enabled = false;

            var player = GetComponent<PlayerController>();
            if (player != null) player.enabled = false;

            // Grey out
            if (_renderer != null)
                _renderer.material.color = Color.grey;
        }

        private void OnGUI()
        {
            if (Camera.main == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 2.2f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Behind camera check
            if (screenPos.z < 0f) return;

            // Too far away check
            float dist = Vector3.Distance(Camera.main.transform.position, transform.position);
            if (dist > 25f) return;

            float barWidth = 60f;
            float barHeight = 8f;
            float x = screenPos.x - barWidth / 2f;
            float y = Screen.height - screenPos.y - barHeight - 15f;

            float ratio = (float)_currentHP / maxHP;

            // Background
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture,
                ScaleMode.StretchToFill, false, 0f, Color.black, 0f, 0f);

            // Fill
            Color barColor = ratio > 0.5f ? Color.green :
                             ratio > 0.25f ? Color.yellow : Color.red;
            GUI.DrawTexture(new Rect(x + 1, y + 1, (barWidth - 2) * ratio, barHeight - 2),
                Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, barColor, 0f, 0f);

            // Name
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.LowerCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y - 16f, barWidth, 16f), displayName, style);
        }
    }
}
