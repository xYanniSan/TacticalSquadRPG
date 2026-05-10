using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalRPG.ThirdPerson
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Orbit Settings")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float height = 2f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 60f;

        [Header("Smoothing (rubber-band follow)")]
        [Tooltip("Time camera takes to catch up to target — bigger = more rubber-band lag.")]
        [SerializeField] private float positionSmoothTime = 0.18f;
        [Tooltip("Maximum catch-up speed when the target dashes/teleports (units/sec).")]
        [SerializeField] private float maxCatchUpSpeed = 35f;

        private float _yaw;
        private float _pitch = 15f;
        // Smooth-damped target anchor (unit position + height). The orbital
        // offset and look-at are applied AFTER smoothing, so mouse rotation
        // feels tight and unit movement rubber-bands.
        private Vector3 _smoothedAnchor;
        private Vector3 _anchorVelocity;
        private bool    _anchorInitialized;

        private void Start()
        {
            if (target == null)
            {
                // Try to find any TerrainBattleUnit or PlayerController to follow
                var battleUnit = FindAnyObjectByType<TerrainBattleUnit>();
                if (battleUnit != null)
                    target = battleUnit.transform;
                else
                {
                    var player = FindAnyObjectByType<PlayerController>();
                    if (player != null)
                        target = player.transform;
                }
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            // Reset smoothing so the new target anchor doesn't carry over
            // the previous target's velocity — that's how the camera
            // "ran off in one direction and didn't stop" on a fresh switch.
            _anchorInitialized = false;
            _anchorVelocity    = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            _yaw += mouseDelta.x * mouseSensitivity * 0.1f;
            _pitch -= mouseDelta.y * mouseSensitivity * 0.1f;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            // 1. Smooth ONLY the target anchor (unit world position).
            //    Applying smoothing to anchor+offset caused velocity to
            //    accumulate every frame the mouse was moving (offset shifts
            //    each frame on rotation), which is what made the camera
            //    "ride off in one direction." Smoothing the anchor isolates
            //    the rubber-band behaviour to physical unit motion.
            Vector3 rawAnchor = target.position + Vector3.up * height;
            if (!_anchorInitialized)
            {
                _smoothedAnchor    = rawAnchor;
                _anchorInitialized = true;
            }
            else
            {
                _smoothedAnchor = Vector3.SmoothDamp(
                    _smoothedAnchor, rawAnchor, ref _anchorVelocity,
                    positionSmoothTime, maxCatchUpSpeed, Time.deltaTime);
            }

            // Sanity bound — if the smoothed anchor ever drifts far from the
            // raw anchor (e.g. unit teleported off-map), snap closer rather
            // than letting the camera coast.
            if (Vector3.Distance(_smoothedAnchor, rawAnchor) > 25f)
            {
                _smoothedAnchor = rawAnchor;
                _anchorVelocity = Vector3.zero;
            }

            // 2. Mouse rotation builds the orbital offset INSTANTLY (no
            //    smoothing) so look feel is tight.
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

            transform.position = _smoothedAnchor + offset;
            transform.LookAt(_smoothedAnchor);
        }
    }
}
