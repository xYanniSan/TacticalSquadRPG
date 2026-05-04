using System.Collections;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Severity levels that map to different freeze durations.
    /// </summary>
    public enum HitStopStrength
    {
        Light  = 0,   //  2 frames — glancing hit
        Medium = 1,   //  4 frames — solid hit
        Heavy  = 2,   //  8 frames — heavy / combo finisher
        Crush  = 3    // 14 frames — boss-level slam or cinematic
    }

    /// <summary>
    /// Freezes Time.timeScale briefly on impact to sell the hit weight.
    /// Sits on the same GameObject as TerrainBattleManager.
    /// </summary>
    public class BattleHitStopSystem : MonoBehaviour
    {
        // Durations in real seconds (unscaled), tuned for 60 fps feel.
        private static readonly float[] Durations = { 0.033f, 0.067f, 0.133f, 0.233f };

        // Snap timeScale back when we are within this distance of 1
        private const float SnapThreshold = 0.05f;
        private const float RecoverSpeed  = 8f;   // lerp speed back to 1

        private bool   _active;
        private float  _endTime;
        private float  _savedScale = 1f;

        public bool IsActive => _active;

        public void TriggerHitStop(HitStopStrength strength)
        {
            float dur = Durations[(int)strength];

            // Don't interrupt a heavier ongoing stop with a lighter one
            if (_active && Time.unscaledTime + dur <= _endTime) return;

            _savedScale      = 1f;
            Time.timeScale   = 0f;
            _endTime         = Time.unscaledTime + dur;
            _active          = true;
        }

        private void Update()
        {
            if (!_active) return;

            if (Time.unscaledTime >= _endTime)
            {
                // Smoothly restore timeScale so motion feels natural again
                Time.timeScale = Mathf.MoveTowards(
                    Time.timeScale, _savedScale,
                    RecoverSpeed * Time.unscaledDeltaTime);

                if (Mathf.Abs(Time.timeScale - _savedScale) < SnapThreshold)
                {
                    Time.timeScale = _savedScale;
                    _active        = false;
                }
            }
        }

        private void OnDestroy()
        {
            // Safety: always restore time if this object is torn down
            if (_active) Time.timeScale = 1f;
        }
    }
}
