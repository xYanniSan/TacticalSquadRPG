using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Visual
{
    public class UnitVisualizer : MonoBehaviour
    {
        private static readonly Color PlayerColor = new Color(0.2f, 0.45f, 0.95f);
        private static readonly Color EnemyColor  = new Color(0.95f, 0.25f, 0.2f);

        private const float MoveSmoothing = 8f;

        // Set by HexBattleManager for hex grids, null = use square GridVisualizer
        public static System.Func<GridPosition, Vector3> WorldPositionProvider;

        private UnitRuntime _unit;
        private GameObject  _hpBarBg;
        private GameObject  _hpBarFill;
        private TextMesh    _nameText;
        private Vector3     _targetWorldPos;

        public UnitRuntime Unit => _unit;

        public void Initialize(UnitRuntime unit)
        {
            _unit = unit;
            gameObject.name = $"Unit_{unit.DisplayName}";

            // ── Capsule Body ──────────────────────────────────────────
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.parent        = transform;
            capsule.transform.localPosition = Vector3.zero;
            capsule.transform.localScale    = new Vector3(0.4f, 0.4f, 0.4f);
            capsule.GetComponent<Renderer>().material.color =
                unit.team == UnitTeam.Player ? PlayerColor : EnemyColor;
            Destroy(capsule.GetComponent<Collider>());

            // ── HP Bar Background ─────────────────────────────────────
            _hpBarBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _hpBarBg.transform.parent     = transform;
            _hpBarBg.transform.localScale = new Vector3(0.7f, 0.06f, 0.12f);
            _hpBarBg.GetComponent<Renderer>().material.color = new Color(0.15f, 0.15f, 0.15f);
            Destroy(_hpBarBg.GetComponent<Collider>());

            // ── HP Bar Fill ───────────────────────────────────────────
            _hpBarFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _hpBarFill.transform.parent     = transform;
            _hpBarFill.transform.localScale = new Vector3(0.68f, 0.05f, 0.10f);
            _hpBarFill.GetComponent<Renderer>().material.color = Color.green;
            Destroy(_hpBarFill.GetComponent<Collider>());

            // ── Name Label ────────────────────────────────────────────
            GameObject textObj = new GameObject("NameLabel");
            textObj.transform.parent = transform;
            _nameText               = textObj.AddComponent<TextMesh>();
            _nameText.text           = unit.DisplayName;
            _nameText.fontSize       = 32;
            _nameText.characterSize  = 0.06f;
            _nameText.anchor         = TextAnchor.MiddleCenter;
            _nameText.alignment      = TextAlignment.Center;
            _nameText.color          = Color.white;
            _nameText.transform.localPosition = new Vector3(0f, 0.72f, 0f);

            // Snap to initial position
            Vector3 worldPos = GetWorldPosition();
            _targetWorldPos = new Vector3(worldPos.x, 0.5f, worldPos.z);
            transform.position = _targetWorldPos;

            UpdateVisual();
        }

        private void Update()
        {
            if (_unit == null || _unit.isDead) return;

            // Smoothly lerp toward grid position
            transform.position = Vector3.Lerp(
                transform.position, _targetWorldPos,
                Time.deltaTime * MoveSmoothing);

            // Face camera for name label
            if (_nameText != null && Camera.main != null)
                _nameText.transform.rotation = Camera.main.transform.rotation;
        }

        public void UpdateVisual()
        {
            if (_unit == null) return;

            // ── Target position (visual will lerp there) ─────────────
            Vector3 worldPos = GetWorldPosition();
            _targetWorldPos = new Vector3(worldPos.x, 0.5f, worldPos.z);

            // ── HP Bar ────────────────────────────────────────────────
            float ratio     = (float)_unit.currentHP / _unit.maxHP;
            float fullWidth = 0.68f;
            float barWidth  = fullWidth * Mathf.Max(ratio, 0f);
            float barOffset = (fullWidth - barWidth) * 0.5f;

            _hpBarBg.transform.localPosition   = new Vector3(0f, 0.55f, 0f);
            _hpBarFill.transform.localScale    = new Vector3(barWidth, 0.05f, 0.10f);
            _hpBarFill.transform.localPosition = new Vector3(-barOffset, 0.55f, 0f);

            // HP color: green > yellow > red
            Color barColor = ratio > 0.5f  ? Color.green
                           : ratio > 0.25f ? Color.yellow
                           :                  Color.red;
            _hpBarFill.GetComponent<Renderer>().material.color = barColor;

            // ── Dead: hide ────────────────────────────────────────────
            if (_unit.isDead)
                gameObject.SetActive(false);
        }

        private Vector3 GetWorldPosition()
        {
            if (WorldPositionProvider != null)
                return WorldPositionProvider(_unit.position);
            return GridVisualizer.GridToWorld(_unit.position);
        }
    }
}
