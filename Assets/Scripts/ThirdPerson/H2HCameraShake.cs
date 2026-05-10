using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Lightweight transform-shake added to the main camera on demand.
    /// `H2HUnit.FireImpactFX` calls `Kick(amplitude, duration)` at each
    /// landed impact frame; the component perturbs the camera's local
    /// position with damped random noise for `duration` seconds, then
    /// snaps back to the saved baseline.
    ///
    /// Cinemachine isn't in this scene; if it lands later, swap this
    /// for `CinemachineImpulseSource.GenerateImpulse` and remove the
    /// component. Read order: ThirdPersonCamera writes the camera's
    /// transform every LateUpdate, so we run AFTER it via a high
    /// `[DefaultExecutionOrder]`.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class H2HCameraShake : MonoBehaviour
    {
        private Vector3 _baselineLocalPos;
        private bool    _baselineCaptured;
        private float   _amplitude;
        private float   _duration;
        private float   _elapsed;
        private bool    _active;

        public void Kick(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f) return;
            // If already shaking, prefer the larger amplitude / fresh timer.
            _amplitude = Mathf.Max(_amplitude, amplitude);
            _duration  = duration;
            _elapsed   = 0f;
            _active    = true;
        }

        private void LateUpdate()
        {
            if (!_active) return;
            if (!_baselineCaptured)
            {
                _baselineLocalPos = transform.localPosition;
                _baselineCaptured = true;
            }

            _elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            // Quartic falloff — sharp pop, fast settle.
            float falloff = (1f - t) * (1f - t);
            Vector3 jitter = new Vector3(
                (Mathf.PerlinNoise(_elapsed * 47f, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, _elapsed * 53f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(_elapsed * 31f, _elapsed * 37f) - 0.5f) * 2f
            ) * (_amplitude * falloff);

            transform.localPosition = _baselineLocalPos + jitter;

            if (_elapsed >= _duration)
            {
                transform.localPosition = _baselineLocalPos;
                _active = false;
                _baselineCaptured = false;
                _amplitude = 0f;
            }
        }

        private void OnDisable()
        {
            if (_baselineCaptured)
            {
                transform.localPosition = _baselineLocalPos;
                _baselineCaptured = false;
                _active = false;
            }
        }
    }
}
