using System.Collections.Generic;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Ragdoll driver baked by <see cref="TacticalRPG.EditorTools.RagdollBaker"/>.
    /// Operates in two modes:
    ///
    ///  1. <b>Animation-driven</b> (default): all ragdoll rigidbodies are
    ///     kinematic; the Animator writes bone transforms unfought. Physics
    ///     overhead is near-zero. This is the resting state during normal
    ///     combat.
    ///
    ///  2. <b>Ragdoll</b>: <see cref="ActivateRagdoll"/> sets every bone
    ///     non-kinematic, drops joint drives to zero, and (optionally)
    ///     disables the Animator. Bones fall under gravity, react to
    ///     impulses, and tangle realistically. <see cref="DeactivateRagdoll"/>
    ///     flips back: kinematic bones, springs restored, Animator re-enabled.
    ///     Bone transforms are snapped to whatever the animator writes next
    ///     frame.
    ///
    /// Skills with a "knockdown chance" call <see cref="ActivateRagdoll"/>
    /// then <see cref="ApplyImpulseAll"/> to add a knockback. After a
    /// timer, code can play a get-up clip and call <see cref="DeactivateRagdoll"/>.
    ///
    /// True Tier-3 (every-hit physical deflection while still standing)
    /// requires a dual-skeleton setup — separate animated source, physics
    /// target, mesh skinned to physics. The bake here lays the groundwork
    /// (bones / joints / colliders / mass) and the driver leaves room for
    /// that upgrade: when the dual skeleton lands, <see cref="FixedUpdate"/>
    /// will copy source-bone rotations to joint targets without the animator
    /// fighting the physics. For now it's a no-op when <see cref="_active"/>
    /// is false.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ActiveRagdollDriver : MonoBehaviour
    {
        [System.Serializable]
        public struct BoneEntry
        {
            public string boneName;
            public Transform bone;
            public Rigidbody body;
            public ConfigurableJoint joint;          // null on root (hips)
            public Quaternion initialLocalRotation;  // bone.localRotation at bake time
            [Tooltip("Per-bone position spring (slerpDrive Kp). Set by the baker; tunable in Inspector.")]
            public float baselineSpring;
            [Tooltip("Per-bone position damper (slerpDrive Kd). Set by the baker; tunable in Inspector.")]
            public float baselineDamper;
        }

        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private List<BoneEntry> _bones = new List<BoneEntry>();
        [SerializeField] private bool _active;
        [SerializeField] [Range(0f, 2f)] private float _springMultiplier = 1f;

        public IReadOnlyList<BoneEntry> Bones => _bones;
        public Animator Animator => _animator;
        public bool IsRagdolled => _active;
        public float SpringMultiplier => _springMultiplier;

        // ── Setup callable from the baker ───────────────────────────

        public void ConfigureFromBake(Animator animator, List<BoneEntry> bones)
        {
            _animator = animator;
            _bones = bones;
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
        }

        private void Awake()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_characterController == null) _characterController = GetComponent<CharacterController>();
            ApplyMode();
        }

        // ── Mode switch ─────────────────────────────────────────────

        /// <summary>
        /// Become a ragdoll: all rigidbodies non-kinematic, gravity on,
        /// joint drives off, animator suspended. The body falls and reacts
        /// to forces. The CharacterController stops driving the root so it
        /// doesn't fight gravity.
        /// </summary>
        public void ActivateRagdoll()
        {
            if (_active) return;
            _active = true;
            ApplyMode();
        }

        /// <summary>
        /// Return to animation-driven state. Bones become kinematic and
        /// follow the Animator's writes again. Pose snaps to whatever the
        /// animator writes next frame — call <see cref="SnapAnimatorToPose"/>
        /// first if you want the animator to start from the current ragdoll
        /// pose (typical for "play get-up clip" recovery).
        /// </summary>
        public void DeactivateRagdoll()
        {
            if (!_active) return;
            _active = false;
            ApplyMode();
        }

        private void ApplyMode()
        {
            // Animator: off while ragdolling so it doesn't fight physics.
            if (_animator != null) _animator.enabled = !_active;
            // CharacterController: off while ragdolling so the root doesn't
            // anchor the body in mid-air. Re-enabled on recover, with the
            // root snapped to current hips position.
            if (_characterController != null) _characterController.enabled = !_active;

            for (int i = 0; i < _bones.Count; i++)
            {
                var b = _bones[i];
                if (b.body == null) continue;

                b.body.isKinematic   = !_active;
                b.body.useGravity    = _active;
                // Velocity writes only valid on non-kinematic bodies — Unity
                // warns otherwise. After ActivateRagdoll the bones are
                // dynamic, so it's safe to zero them out before letting
                // gravity take over.
                if (!b.body.isKinematic)
                {
                    b.body.linearVelocity  = Vector3.zero;
                    b.body.angularVelocity = Vector3.zero;
                }

                if (b.joint != null)
                {
                    b.joint.slerpDrive = _active
                        ? new JointDrive { positionSpring = 0f, positionDamper = 0f, maximumForce = 0f }
                        : new JointDrive { positionSpring = b.baselineSpring * _springMultiplier,
                                           positionDamper = b.baselineDamper * _springMultiplier,
                                           maximumForce   = Mathf.Infinity };
                }
            }

            // When deactivating, bring the root back to the hips' world
            // position so the CharacterController isn't fighting an offset.
            if (!_active && _bones.Count > 0)
            {
                var hips = _bones[0].bone;
                if (hips != null && _characterController != null)
                {
                    var rootPos = hips.position; rootPos.y = transform.position.y;
                    transform.position = rootPos;
                }
            }
        }

        // ── External impulse APIs (skill effects, hit reactions) ────

        public void ApplyImpulse(string boneName, Vector3 impulse)
        {
            for (int i = 0; i < _bones.Count; i++)
                if (_bones[i].boneName == boneName && _bones[i].body != null)
                { _bones[i].body.AddForce(impulse, ForceMode.Impulse); return; }
        }

        /// <summary>
        /// Adds an impulse to ALL ragdoll bones at once — typical "knockback"
        /// effect. Force is the same vector applied to every bone, so heavier
        /// bones (torso) move less than lighter ones (arms), giving a
        /// natural distribution.
        /// </summary>
        public void ApplyImpulseAll(Vector3 impulse)
        {
            for (int i = 0; i < _bones.Count; i++)
                if (_bones[i].body != null) _bones[i].body.AddForce(impulse, ForceMode.Impulse);
        }

        // ── Drive tuning ────────────────────────────────────────────

        public void SetGlobalSpringMultiplier(float mult)
        {
            _springMultiplier = Mathf.Max(0f, mult);
            if (!_active) ApplyMode();   // re-apply baseline scaled by new mult
        }

        // ── Aliases kept for the existing UI buttons ────────────────

        public void GoLimp() => ActivateRagdoll();
        public void Recover() => DeactivateRagdoll();
    }
}
