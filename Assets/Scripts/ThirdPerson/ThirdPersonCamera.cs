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
        [SerializeField] private float smoothSpeed = 10f;

        private float _yaw;
        private float _pitch = 15f;

        private void Start()
        {
            if (target == null)
            {
                // Try to find any TerrainBattleUnit or PlayerController to follow
                var battleUnit = FindFirstObjectByType<TerrainBattleUnit>();
                if (battleUnit != null)
                    target = battleUnit.transform;
                else
                {
                    var player = FindFirstObjectByType<PlayerController>();
                    if (player != null)
                        target = player.transform;
                }
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            _yaw += mouseDelta.x * mouseSensitivity * 0.1f;
            _pitch -= mouseDelta.y * mouseSensitivity * 0.1f;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            Vector3 targetPos = target.position + Vector3.up * height;

            transform.position = Vector3.Lerp(
                transform.position, targetPos + offset, smoothSpeed * Time.deltaTime);
            transform.LookAt(targetPos);
        }
    }
}
