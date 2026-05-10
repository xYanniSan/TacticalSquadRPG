using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Per-unit foot IK that snaps the humanoid feet to the ground via
    /// raycasts, weighted by the Animator's IK pass. Designed for the
    /// H2H training scene where combat movement is slow and feet
    /// floating above the ground reads as wrong.
    ///
    /// Sit alongside the unit's Animator (humanoid). The Animator must
    /// have IK Pass enabled on the relevant layer (layer 0 by default).
    /// We auto-enable that on Awake.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class H2HFootIK : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private float _raycastDownDistance = 1.5f;
        [SerializeField] private float _raycastUpOffset = 0.6f;
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Foot offset")]
        [Tooltip("Vertical offset from the ray hit to the IK foot target. Adjust to match shoe / foot bone height.")]
        [SerializeField] private float _footHeightOffset = 0.05f;

        [Header("Weight")]
        [Range(0f, 1f)] [SerializeField] private float _ikWeight = 1f;
        [Tooltip("Disable IK while the unit's H2H phase is Exchange (so attack / hit react animations aren't pulled to the ground).")]
        [SerializeField] private bool _suppressDuringExchange = true;

        private Animator _animator;
        private H2HUnit _unit;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _unit = GetComponentInParent<H2HUnit>();
            if (_unit == null) _unit = GetComponent<H2HUnit>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || layerIndex != 0) return;
            float weight = _ikWeight;
            if (_suppressDuringExchange && _unit != null && _unit.Phases != null
                && _unit.Phases.GetPhase(_unit) == DataModels.H2HPhase.Exchange)
            {
                weight = 0f;
            }

            ApplyFootIK(AvatarIKGoal.LeftFoot, weight);
            ApplyFootIK(AvatarIKGoal.RightFoot, weight);
        }

        private void ApplyFootIK(AvatarIKGoal goal, float weight)
        {
            Vector3 footPos = _animator.GetIKPosition(goal);
            Vector3 origin = footPos + Vector3.up * _raycastUpOffset;
            if (Physics.Raycast(origin, Vector3.down, out var hit, _raycastUpOffset + _raycastDownDistance, _groundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 target = hit.point + Vector3.up * _footHeightOffset;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal) * _animator.GetIKRotation(goal);
                _animator.SetIKPositionWeight(goal, weight);
                _animator.SetIKRotationWeight(goal, weight);
                _animator.SetIKPosition(goal, target);
                _animator.SetIKRotation(goal, rot);
            }
            else
            {
                _animator.SetIKPositionWeight(goal, 0f);
                _animator.SetIKRotationWeight(goal, 0f);
            }
        }
    }
}
