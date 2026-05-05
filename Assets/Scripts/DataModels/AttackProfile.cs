using Animancer;
using UnityEngine;

namespace TacticalRPG.DataModels
{
    /// <summary>
    /// Profile-driven configuration for a single skill animation.
    /// Source of truth for ranges, timing, root-motion policy, and the Animancer
    /// transition that plays the clip. Combat still owns damage, hit-stop tier,
    /// and knockback — the profile only describes presentation and positioning.
    ///
    /// See Docs/07_PRESENTATION.md "Animation runtime (Animancer Pro)".
    /// </summary>
    [CreateAssetMenu(menuName = "TacticalRPG/Attack Profile")]
    public class AttackProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Resolved technique name this profile services (e.g. 'Earth Fist').")]
        public string techniqueName;

        [Header("Animation")]
        [Tooltip("Animancer transition that plays the skill clip. Required.")]
        public TransitionAsset transition;

        [Tooltip("Animancer named event fired at the impact frame. " +
                 "If left null, the ability falls back to legacy clip AnimationEvents " +
                 "forwarded via UnitAnimationEventRelay.")]
        public StringAsset impactEventName;

        [Header("Range and positioning")]
        public float minStartRange      = 0f;
        public float idealStartRange    = 2f;
        public float maxStartRange      = 3f;
        public float desiredImpactDistance = 1.5f;
        public float allowedAngleDegrees  = 30f;

        [Header("Setup behavior")]
        public bool requiresPreAlign       = true;
        public bool requiresEngagementSlot = true;
        public bool canUseIfTooClose       = true;

        [Header("Movement during the action")]
        public ActionMovementMode movementMode = ActionMovementMode.InPlace;
        public bool  useRootMotion           = false;
        public bool  lockMovementDuringCommit = true;
        public bool  lockRotationDuringImpact = true;
        public float scriptedTravelDistance  = 0f;

        [Header("Outcome")]
        public bool  causesKnockback   = false;
        public float knockbackDistance = 0f;
    }
}
