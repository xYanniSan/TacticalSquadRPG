using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Draws a small speed bar above each unit (below the HP bar). Reads from
    /// BattleSpeedSystem each frame; cheap OnGUI rendering, mirrors HealthSystem's
    /// pattern. Auto-attached by TerrainBattleUnit.Initialize so prefabs don't
    /// need to be modified for Phase 3.
    /// </summary>
    public class SpeedBarUI : MonoBehaviour
    {
        private TerrainBattleUnit _unit;
        private BattleSpeedSystem _speed;

        public void Initialize(TerrainBattleUnit unit, BattleSpeedSystem speed)
        {
            _unit  = unit;
            _speed = speed;
        }

        private void OnGUI()
        {
            if (_unit == null || _unit.IsDead) return;
            if (_speed == null) return;
            if (Camera.main == null) return;

            Vector3 worldPos = transform.position + Vector3.up * 2.2f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f) return;

            float dist = Vector3.Distance(Camera.main.transform.position, transform.position);
            if (dist > 25f) return;

            const float barWidth  = 60f;
            const float barHeight = 4f;
            float x = screenPos.x - barWidth / 2f;
            // Sits just below the HP bar, which lives at HP-bar y. We offset further down.
            float y = Screen.height - screenPos.y + 2f;

            float speed = _speed.GetSpeed(_unit);
            float ratio = Mathf.Clamp01(speed / _speed.HardCap);

            // Background
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture,
                ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.7f), 0f, 0f);

            // Soft-cap tick at 70 %
            float softCapMark = barWidth * (_speed.SoftCap / _speed.HardCap);
            GUI.DrawTexture(new Rect(x + softCapMark - 0.5f, y, 1f, barHeight),
                Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f,
                new Color(1f, 1f, 1f, 0.4f), 0f, 0f);

            // Fill — color shifts by SpeedBand (sluggish dim, primed bright cyan)
            SpeedBand band = _speed.GetSpeedBand(_unit);
            Color barColor = band switch
            {
                SpeedBand.Sluggish => new Color(0.4f, 0.4f, 0.5f, 1f),  // dull blue-grey
                SpeedBand.Engaged  => new Color(0.4f, 0.7f, 1f, 1f),    // soft blue
                SpeedBand.Sharp    => new Color(0.2f, 0.9f, 1f, 1f),    // cyan
                SpeedBand.Primed   => new Color(0.4f, 1f, 1f, 1f),      // glowing cyan
                _                  => Color.white
            };

            GUI.DrawTexture(new Rect(x + 1, y + 1, (barWidth - 2) * ratio, barHeight - 2),
                Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, barColor, 0f, 0f);
        }
    }
}
