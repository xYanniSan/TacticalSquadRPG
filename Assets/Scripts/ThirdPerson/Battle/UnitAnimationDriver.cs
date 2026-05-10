using System.Collections.Generic;
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

        // ── Move-based engine animation contract ─────────────────────
        // PlayMove(name) is the new entry point used by BattleCombatEngine.
        // Lookup hierarchy:
        //   1. _moveAnimationOverrides — Inspector-bound animation name → Animator trigger.
        //   2. Built-in name-prefix mapping (attack_punch_* → "Attack", etc.).
        //   3. No-op (combat continues; missing animation logged once).

        [System.Serializable]
        public struct MoveAnimationBinding
        {
            [Tooltip("Move animation name from MOVES_CATALOG.md (e.g. 'attack_punch_jab').")]
            public string moveAnimationName;
            [Tooltip("Animator trigger name to fire (e.g. 'Attack', 'Kick', 'Block', 'Dodge').")]
            public string animatorTrigger;
            [Tooltip("Optional float parameter to set alongside the trigger. Leave blank to skip.")]
            public string floatParam;
            public float floatValue;
        }

        [Header("Move animation bindings (move-based engine)")]
        [Tooltip("Per-unit overrides for the move-based engine. When PlayMove(name) is called, " +
                 "the driver fires the matching animator trigger. If unset, falls back to a built-in " +
                 "category map; if neither matches, combat continues without animation.")]
        [SerializeField] private List<MoveAnimationBinding> _moveAnimationOverrides
            = new List<MoveAnimationBinding>();

        // Cache lookup by name; first-miss logs once.
        private Dictionary<string, MoveAnimationBinding> _moveOverrideMap;
        private static readonly HashSet<string> _missingMoveLoggedOnce = new HashSet<string>();

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

        // ── Move-based engine entry point ────────────────────────────

        /// <summary>
        /// Play an animation by move-catalog name. Lookup order:
        ///   1. Inspector-assigned binding for this exact name
        ///   2. Built-in name-prefix fallback (attack_punch_* → Attack, etc.)
        ///   3. No-op (combat does not fail on missing animation)
        ///
        /// Called by BattleCombatEngine when a unit transitions to a new move.
        /// </summary>
        public void PlayMove(string moveAnimationName)
        {
            if (string.IsNullOrEmpty(moveAnimationName)) return;

            // 1. Inspector overrides
            if (_moveOverrideMap == null) BuildMoveOverrideMap();
            if (_moveOverrideMap.TryGetValue(moveAnimationName, out var binding))
            {
                if (!string.IsNullOrEmpty(binding.floatParam))
                    SafeSetFloat(binding.floatParam, binding.floatValue);
                if (!string.IsNullOrEmpty(binding.animatorTrigger))
                {
                    ResetAllCombatTriggers();
                    SafeSetTrigger(binding.animatorTrigger);
                }
                return;
            }

            // 2. Built-in name-prefix fallback
            string trigger = ResolveBuiltinTrigger(moveAnimationName);
            if (trigger != null)
            {
                ResetAllCombatTriggers();
                SafeSetTrigger(trigger);
                return;
            }

            // 3. No-op (idle / locomotion handled by Speed blend tree)
            //    Locomotion moves don't need a trigger — the blend tree
            //    already animates running based on the SetSpeed value.
            //    Idle / unknown — log once and continue.
            if (!IsLocomotionOrIdle(moveAnimationName) && !_missingMoveLoggedOnce.Contains(moveAnimationName))
            {
                _missingMoveLoggedOnce.Add(moveAnimationName);
                Debug.Log($"[UnitAnimationDriver] No animation binding for move '{moveAnimationName}' — combat continues.");
            }
        }

        /// <summary>
        /// Maps a move animation name to a built-in Animator trigger when
        /// no Inspector binding is configured. Heuristic by category prefix.
        /// </summary>
        private static string ResolveBuiltinTrigger(string moveName)
        {
            if (string.IsNullOrEmpty(moveName)) return null;
            // Defensive
            if (moveName.StartsWith("defend_block"))     return "Block";
            if (moveName.StartsWith("defend_dodge"))     return "Dodge";
            if (moveName.StartsWith("defend_bob_weave")) return "Dodge";
            if (moveName.StartsWith("defend_parry"))     return "Block";
            if (moveName.StartsWith("defend_static"))    return "Block";
            if (moveName.StartsWith("defend_fade"))      return "Dodge";
            // Attack
            if (moveName.StartsWith("attack_kick"))      return "Kick";
            if (moveName.StartsWith("attack_punch"))     return "Attack";
            if (moveName.StartsWith("attack_power"))     return "Attack";
            if (moveName.StartsWith("attack_slam"))      return "Attack";
            if (moveName.StartsWith("attack_lunge"))     return "Attack";
            if (moveName.StartsWith("attack_elbow"))     return "Attack";
            if (moveName.StartsWith("attack_knee"))      return "Kick";
            if (moveName.StartsWith("attack_double"))    return "Attack";
            // Cast
            if (moveName.StartsWith("cast_"))            return "Cast";
            // Reactions
            if (moveName.StartsWith("react_hit"))        return "Hit";
            if (moveName.StartsWith("react_recoil"))     return "Hit";
            if (moveName.StartsWith("react_launch"))     return "Hit";
            if (moveName.StartsWith("react_knockdown"))  return "Hit";
            if (moveName.StartsWith("react_stunned"))    return "Hit";
            if (moveName.StartsWith("react_dazed"))      return "Hit";
            // Death
            if (moveName.StartsWith("death_"))           return "Death";
            // Mobility
            if (moveName.StartsWith("mobility_dash"))    return null;     // movement, no trigger
            if (moveName.StartsWith("mobility_teleport")) return "Dodge";
            return null;
        }

        private static bool IsLocomotionOrIdle(string moveName)
        {
            if (string.IsNullOrEmpty(moveName)) return false;
            if (moveName == "idle") return true;
            if (moveName.StartsWith("locomotion_")) return true;
            if (moveName.StartsWith("mobility_dash")) return true;
            return false;
        }

        private void BuildMoveOverrideMap()
        {
            _moveOverrideMap = new Dictionary<string, MoveAnimationBinding>();
            if (_moveAnimationOverrides == null) return;
            for (int i = 0; i < _moveAnimationOverrides.Count; i++)
            {
                var b = _moveAnimationOverrides[i];
                if (!string.IsNullOrEmpty(b.moveAnimationName))
                    _moveOverrideMap[b.moveAnimationName] = b;
            }
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
