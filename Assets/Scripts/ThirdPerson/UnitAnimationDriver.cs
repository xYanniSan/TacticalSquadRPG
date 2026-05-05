using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Owns all Animator access for a battle unit.
    /// All SetBool / SetTrigger / SetFloat calls go through here.
    /// UnitBrainAI and AbilityExecutor call the public API.
    /// </summary>
    public class UnitAnimationDriver : MonoBehaviour
    {
        private Animator _animator;

        [Header("Flying Kick")]
        [Tooltip("KickVariant float value at which the flying kick clip is selected.")]
        [SerializeField] private float flyingKickVariantThreshold = 0.75f;

        // ── Lifecycle ────────────────────────────────────────────────

        public void Initialize(Animator animator, float animSpeed)
        {
            _animator = animator;
            SetAnimationSpeed(animSpeed);
        }

        // ── Speed blend tree ─────────────────────────────────────────

        public void SetSpeed(float normalized)
        {
            if (_animator == null) return;

            // Hard snap — no damping
            if (normalized < 0.1f) normalized = 0f;
            else if (normalized > 0.9f) normalized = 1f;

            _animator.SetFloat("Speed", normalized);
        }

        // ── Animation playback speed ─────────────────────────────────

        public void SetAnimationSpeed(float speed)
        {
            if (_animator != null)
                _animator.speed = speed;
        }

        // ── Combat triggers ──────────────────────────────────────────

        /// <summary>
        /// Play a punch/non-flying attack. Returns the chosen AttackVariant float.
        /// </summary>
        public float PlayAttack()
        {
            ResetAllCombatTriggers();
            // Snap to 0 or 1 — never a mid-blend value.
            // A raw Random.value that lands between two clip thresholds causes Unity
            // to fire animation events from both blending clips simultaneously.
            float variant = Random.value < 0.5f ? 0f : 1f;
            SafeSetFloat("AttackVariant", variant);
            SafeSetTrigger("Attack");
            SafeSetBool("IsRecovering", false);
            return variant;
        }

        /// <summary>
        /// Play a kick. Returns whether the selected variant is a flying kick.
        /// Outputs the raw variant value so the caller can use it for lunge setup.
        /// </summary>
        public bool PlayKick(out float variant)
        {
            ResetAllCombatTriggers();
            // Snap to a threshold boundary — 0 = grounded kick, 1 = flying kick.
            // Avoids blend-tree event doubling from partial blends.
            variant = Random.value >= flyingKickVariantThreshold ? 1f : 0f;
            bool isFlyingKick = variant >= flyingKickVariantThreshold;
            SafeSetFloat("KickVariant", variant);
            SafeSetTrigger("Kick");
            SafeSetBool("IsRecovering", false);
            return isFlyingKick;
        }

        public void PlayCast()
        {
            ResetAllCombatTriggers();
            SafeSetTrigger("Cast");
        }

        public void PlayHitReact()
        {
            ResetAllCombatTriggers();
            SafeSetTrigger("Hit");
            SafeSetBool("IsRecovering", false);
        }

        /// <summary>Play block animation (defenders only).</summary>
        public void PlayBlock()
        {
            ResetAllCombatTriggers();
            SafeSetTrigger("Block");
            SafeSetBool("IsRecovering", true);
        }

        /// <summary>Play attacker recovery (no anim trigger — just clears IsRecovering).</summary>
        public void PlayAttackerRecover()
        {
            SafeSetBool("IsRecovering", true);
        }

        public void PlayDodge()
        {
            ResetAllCombatTriggers();
            SafeSetTrigger("Dodge");
        }

        public void PlayDeath()
        {
            ResetAllCombatTriggers();
            SafeSetTrigger("Death");
        }

        public void ClearRecovering()
        {
            SafeSetBool("IsRecovering", false);
        }

        // ── Foot IK helpers ──────────────────────────────────────────

        public void SetIKPositionWeight(AvatarIKGoal goal, float weight)
            => _animator?.SetIKPositionWeight(goal, weight);

        public void SetIKRotationWeight(AvatarIKGoal goal, float weight)
            => _animator?.SetIKRotationWeight(goal, weight);

        public void SetIKPosition(AvatarIKGoal goal, Vector3 pos)
            => _animator?.SetIKPosition(goal, pos);

        public void SetIKRotation(AvatarIKGoal goal, Quaternion rot)
            => _animator?.SetIKRotation(goal, rot);

        public Vector3 GetIKPosition(AvatarIKGoal goal)
            => _animator != null ? _animator.GetIKPosition(goal) : Vector3.zero;

        public bool HasAnimator => _animator != null && _animator.runtimeAnimatorController != null;

        // ── Internals ────────────────────────────────────────────────

        private void ResetAllCombatTriggers()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            SafeResetTrigger("Attack");
            SafeResetTrigger("Kick");
            SafeResetTrigger("Hit");
            SafeResetTrigger("Cast");
            SafeResetTrigger("Death");
            SafeResetTrigger("Dodge");
            SafeResetTrigger("Block");
        }

        private void SafeSetTrigger(string name)
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
                if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
                { _animator.SetTrigger(name); return; }
        }

        private void SafeResetTrigger(string name)
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
                if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
                { _animator.ResetTrigger(name); return; }
        }

        private void SafeSetBool(string name, bool value)
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
                if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
                { _animator.SetBool(name, value); return; }
        }

        private void SafeSetFloat(string name, float value)
        {
            if (_animator == null) return;
            foreach (var p in _animator.parameters)
                if (p.name == name && p.type == AnimatorControllerParameterType.Float)
                { _animator.SetFloat(name, value); return; }
        }
    }
}
