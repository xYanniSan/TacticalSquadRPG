using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Short-lived floating damage number. Spawned per hit from
    /// `TerrainBattleUnit.ApplyDamage` so impacts read visually even when
    /// animations don't sell them yet.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private const float Lifetime    = 0.9f;
        private const float RiseSpeed   = 1.3f;
        private const float HSpread     = 0.4f;
        private static readonly Color DefaultColor = new Color(1f, 0.95f, 0.5f, 1f);
        private static readonly Color BlockedColor = new Color(0.7f, 0.85f, 1f, 1f);
        private static readonly Color CritColor    = new Color(1f, 0.4f, 0.3f, 1f);

        private string _text;
        private Color  _color;
        private float  _elapsed;
        private Vector3 _drift;
        private float _fontSize;

        public static void Spawn(Vector3 worldPos, int damage, bool blocked, bool heavy = false)
        {
            var go = new GameObject("DamagePopup");
            go.transform.position = worldPos + Vector3.up * 1.6f;
            var popup = go.AddComponent<DamagePopup>();
            popup._text   = damage.ToString();
            popup._color  = blocked ? BlockedColor
                          : heavy   ? CritColor
                                    : DefaultColor;
            popup._fontSize = heavy ? 28f : (blocked ? 16f : 22f);
            // Slight horizontal scatter so stacked hits don't overlap.
            float xJitter = Random.Range(-HSpread, HSpread);
            popup._drift = new Vector3(xJitter, RiseSpeed, 0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= Lifetime) { Destroy(gameObject); return; }

            // Rise + slight horizontal drift, decelerating.
            transform.position += _drift * Time.deltaTime;
            _drift *= 0.95f;
        }

        private void OnGUI()
        {
            if (Camera.main == null) return;
            Vector3 sp = Camera.main.WorldToScreenPoint(transform.position);
            if (sp.z < 0f) return;

            float alpha = 1f - (_elapsed / Lifetime);
            float pop   = _elapsed < 0.1f ? Mathf.Lerp(1.4f, 1f, _elapsed / 0.1f) : 1f;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(_fontSize * pop),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = new Color(_color.r, _color.g, _color.b, alpha);

            // Drop shadow for legibility on bright backgrounds.
            var shadow = new GUIStyle(style);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.55f * alpha);

            const float w = 80f;
            const float h = 32f;
            float x = sp.x - w / 2f;
            float y = Screen.height - sp.y - h / 2f;
            GUI.Label(new Rect(x + 1, y + 1, w, h), _text, shadow);
            GUI.Label(new Rect(x, y, w, h), _text, style);
        }
    }
}
