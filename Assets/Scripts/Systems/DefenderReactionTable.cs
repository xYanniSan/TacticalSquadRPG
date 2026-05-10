using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Systems
{
    /// <summary>
    /// Paired-reaction lookup. Given an `AttackArchetype` plus the defender's
    /// stance and current speed band, returns a `ReactionType`. The resolver
    /// then orchestrates the reaction (movement / animation / damage modifier).
    ///
    /// Spec: COMBAT_DESIGN.md "Spatial combat choreography → Defender response
    /// variants". Each row of this table is a paired reaction. Adding a new
    /// reaction is a data-only change here — the resolver dispatches by enum.
    /// </summary>
    public static class DefenderReactionTable
    {
        /// <summary>
        /// Returned bundle. The resolver uses these to orchestrate the reaction.
        /// </summary>
        public struct Reaction
        {
            public ReactionType type;
            public float        damageMultiplier;  // 1.0 = full damage
            public string       reason;            // for logs
        }

        public static Reaction Lookup(
            AttackArchetype attackArchetype,
            StanceDefinition defenderStance,
            SpeedBand defenderBand,
            bool defenderCanDodge,
            bool defenderCanBlock)
        {
            StanceId stance = defenderStance != null ? defenderStance.id : StanceId.Tactician;

            // ── Launch attacks ALWAYS Airborne the target ──────────────
            // Stance/band don't matter — Naruto-uppercut beats everyone.
            if (attackArchetype == AttackArchetype.Launch)
                return Make(ReactionType.Airborne, 1.5f, "Launch always Airborne");

            // ── Sentinel / Stalwart anchor against Heavy ────────────────
            if (attackArchetype == AttackArchetype.Heavy
                && (stance == StanceId.Sentinel || stance == StanceId.Stalwart))
                return Make(ReactionType.BraceBlock, 0.25f, $"{stance} braces vs Heavy");

            // ── Wraith Primed band fades out of Heavy / Flurry ──────────
            if ((attackArchetype == AttackArchetype.Heavy || attackArchetype == AttackArchetype.Flurry)
                && stance == StanceId.Wraith && defenderBand == SpeedBand.Primed)
                return Make(ReactionType.FadeOut, 0f, "Wraith Primed FadeOut");

            // ── Tactician parries Flurry at Sharp+ ─────────────────────
            if (attackArchetype == AttackArchetype.Flurry
                && stance == StanceId.Tactician
                && (defenderBand == SpeedBand.Sharp || defenderBand == SpeedBand.Primed))
                return Make(ReactionType.Parry, 0f, "Tactician Sharp+ Parry");

            // ── Light attacks at Sharp+ band → BobWeave (in-place) ─────
            if (attackArchetype == AttackArchetype.Light
                && (defenderBand == SpeedBand.Sharp || defenderBand == SpeedBand.Primed))
                return Make(ReactionType.BobWeave, 0f, "Sharp+ Light → BobWeave");

            // ── Sweep at Engaged+ → Dodge (jump over) ──────────────────
            if (attackArchetype == AttackArchetype.Sweep
                && defenderBand != SpeedBand.Sluggish
                && defenderCanDodge)
                return Make(ReactionType.Dodge, 0f, "Sweep + mobile defender → jump-dodge");

            // ── Sign-cast at range → Recoil (chip damage only) ─────────
            // Conduit/Sentinel especially good at this.
            if (attackArchetype == AttackArchetype.Sign
                && (stance == StanceId.Conduit || stance == StanceId.Sentinel
                    || stance == StanceId.Stalwart))
                return Make(ReactionType.Recoil, 0.4f, $"{stance} Sign Recoil");

            // ── Default: Block if available, else Eat ──────────────────
            if (defenderCanBlock)
                return Make(ReactionType.Block, 0.5f, "default Block");
            return Make(ReactionType.Eat, 1.0f, "default Eat");
        }

        private static Reaction Make(ReactionType type, float dmgMult, string reason)
        {
            return new Reaction { type = type, damageMultiplier = dmgMult, reason = reason };
        }
    }
}
