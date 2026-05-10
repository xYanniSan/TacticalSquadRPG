using System.Collections.Generic;
using UnityEngine;

namespace TacticalRPG.DataModels
{
    /// <summary>
    /// The atomic unit of the move-based combat engine. Every unit, every
    /// frame, is executing one of these. See COMBAT_DESIGN "Combat engine
    /// — move-based, frame-data driven" and MOVES_CATALOG for the full
    /// catalog of expected move ids.
    ///
    /// One frame == 50ms (20Hz tick). totalFrames computed at validate.
    /// </summary>
    [CreateAssetMenu(menuName = "TacticalRPG/Move Definition")]
    public class MoveDefinition : ScriptableObject
    {
        // ── Identity ────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Stable id — also used as the animationName by default. " +
                 "Must match an entry in Docs/Design/MOVES_CATALOG.md.")]
        public string id;

        [Tooltip("Animation handle the engine passes to UnitAnimationDriver.PlayMove. " +
                 "Defaults to id when blank.")]
        public string animationName;

        [TextArea] public string description;

        public MoveCategory category = MoveCategory.LightAttack;

        // ── Frame timeline (50ms ticks) ─────────────────────────────

        [Header("Frame timeline (50ms each)")]
        [Min(0)] public int startupFrames = 2;
        [Min(0)] public int activeFrames = 1;
        [Min(0)] public int recoveryFrames = 4;

        [Tooltip("Last N frames of recovery accept a cancel into a chained move.")]
        [Min(0)] public int cancelWindowFrames = 0;

        public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
        public float TotalSeconds => TotalFrames * 0.05f;

        // ── Hit properties ──────────────────────────────────────────

        [Header("Hit properties")]
        [Tooltip("Damage applied on full hit. Heavy/Sign moves can scale up.")]
        public int damage = 0;

        [Tooltip("Reach in meters. Cone tip is at the unit's chest.")]
        [Min(0f)] public float range = 1.8f;

        [Tooltip("Half-angle of the hit cone in degrees (full cone = 2× this).")]
        [Range(5f, 180f)] public float angleDegrees = 45f;

        public AttackArchetype archetype = AttackArchetype.Light;
        public ReactionTag     reactionTag = ReactionTag.None;

        [Tooltip("If true, this move is an attack — engine performs hit checks during active frames.")]
        public bool isAttack = false;

        [Tooltip("Flat damage scalar applied per current speed (0=no scaling, 0.5=+50% at speed=100).")]
        [Range(0f, 1f)] public float speedDamageScaling = 0f;

        // ── Defensive properties ────────────────────────────────────

        [Header("Defensive properties")]
        [Tooltip("First active frame at which i-frames begin (relative to active phase).")]
        [Min(0)] public int iFrameStart = 0;

        [Tooltip("Last active frame (inclusive) at which i-frames are valid.")]
        [Min(0)] public int iFrameEnd = 0;

        [Tooltip("If >0, the unit ignores incoming hits during these many startup+active frames.")]
        [Min(0)] public int superArmorFrames = 0;

        [Tooltip("If true, this move is itself a block — defender resolution treats it as active block.")]
        public bool isBlock = false;

        [Tooltip("If true, this move is a parry — defender resolution treats it as active parry " +
                 "for the parry window (= active frames).")]
        public bool isParry = false;

        [Tooltip("Damage multiplier applied to incoming hits while this move is in active phase.")]
        [Range(0f, 1f)] public float incomingDamageMultiplier = 1f;

        // ── Cost & gating ───────────────────────────────────────────

        [Header("Cost & gating")]
        [Min(0f)] public float speedCost   = 0f;
        [Min(0f)] public float energyCost  = 0f;
        [Tooltip("Below this current-speed value, brain may not pick this move.")]
        [Min(0f)] public float speedGate   = 0f;

        // ── Cancel & chain windows ──────────────────────────────────

        [Header("Cancel & chain")]
        [Tooltip("On confirmed hit, these moves can replace this move during its cancel window.")]
        public List<MoveDefinition> cancelIntoOnHit = new List<MoveDefinition>();

        [Tooltip("On whiff (no contact) these moves may still cancel — usually a smaller list.")]
        public List<MoveDefinition> cancelIntoOnWhiff = new List<MoveDefinition>();

        // ── Movement ────────────────────────────────────────────────

        [Header("Movement during the move")]
        [Tooltip("Per-frame forward translation in meters. Multiplied by 1.0 each frame to integrate. " +
                 "Use 0 for stationary moves; positive = forward, negative = backward.")]
        public float forwardSpeedMetersPerSecond = 0f;

        [Tooltip("Lateral translation in meters/sec — positive = right, negative = left.")]
        public float lateralSpeedMetersPerSecond = 0f;

        [Tooltip("Optional displacement curve evaluated over the full move (0..1 → meters forward). " +
                 "When non-empty, OVERRIDES forwardSpeed for the move duration. Used by dashes/dodges.")]
        public AnimationCurve forwardDisplacementCurve;

        [Tooltip("Optional vertical arc curve (for parabolic dodges/launches).")]
        public AnimationCurve verticalDisplacementCurve;

        public FacingPolicy facing = FacingPolicy.FaceTarget;

        // ── Reserved: world-entity spawn (see COMBAT_DESIGN) ────────

        [Header("Reserved — entity spawn (not yet wired)")]
        [Tooltip("Reserved slot for cast_earth_wall / cast_fire_zone etc. Leave null until " +
                 "the entity registry lands.")]
        public GameObject spawnsEntityPrefab;
        public Vector3    spawnOffsetLocal = new Vector3(0f, 0f, 1.5f);

        // ── Convenience ────────────────────────────────────────────

        public string ResolvedAnimationName =>
            string.IsNullOrEmpty(animationName) ? id : animationName;

        public bool IsAttack => isAttack || damage > 0;

        public bool IsLocomotion =>
            category == MoveCategory.Locomotion || category == MoveCategory.Idle;

        /// <summary>
        /// Phase of a move at the given frame index (0-based, [0..TotalFrames-1]).
        /// At >= TotalFrames, returns Done.
        /// </summary>
        public MovePhase PhaseAtFrame(int frame)
        {
            if (frame < 0)                                     return MovePhase.Startup;
            if (frame < startupFrames)                         return MovePhase.Startup;
            if (frame < startupFrames + activeFrames)          return MovePhase.Active;
            int recoveryEnd = startupFrames + activeFrames + recoveryFrames;
            if (frame < recoveryEnd)
            {
                int recoveryStart = startupFrames + activeFrames;
                int recoveryFrameIndex = frame - recoveryStart;
                int cancelStart = recoveryFrames - cancelWindowFrames;
                if (cancelWindowFrames > 0 && recoveryFrameIndex >= cancelStart)
                    return MovePhase.CancelWindow;
                return MovePhase.Recovery;
            }
            return MovePhase.Done;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
            if (iFrameEnd < iFrameStart) iFrameEnd = iFrameStart;
            if (cancelWindowFrames > recoveryFrames) cancelWindowFrames = recoveryFrames;
        }
    }
}
