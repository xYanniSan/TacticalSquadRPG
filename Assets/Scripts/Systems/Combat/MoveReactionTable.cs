using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Maps (incoming attack's ReactionTag, defender state) → the move id
    /// the defender's body should snap into when the hit lands.
    ///
    /// This is the "forced" reaction — engaged after damage resolution
    /// when the defender did NOT successfully avoid the hit. (Pre-hit
    /// reactions like dodge/block/parry are picked by IStanceBrain.PickReaction
    /// before the engine even checks for contact.)
    ///
    /// See COMBAT_DESIGN "Reaction system — paired moves" and "Status / CC
    /// as moves".
    /// </summary>
    public static class MoveReactionTable
    {
        /// <summary>
        /// Pick a forced-reaction move id given the attack's reaction tag,
        /// whether the hit was blocked (Recoil instead of Hit), and
        /// whether the damage was significant enough to push out of the
        /// "light" tier.
        /// </summary>
        public static string PickForcedReaction(ReactionTag tag, bool wasBlocked, int damageDealt)
        {
            if (wasBlocked) return MoveIds.RecoilBlocked;

            switch (tag)
            {
                case ReactionTag.Launch:
                    return MoveIds.LaunchAirborne;
                case ReactionTag.Knockdown:
                    return MoveIds.KnockdownBack;
                case ReactionTag.Sweep:
                    return MoveIds.HitSweep;
                case ReactionTag.Heavy:
                    return MoveIds.HitHeavy;
                case ReactionTag.BigSign:
                    return damageDealt >= 25 ? MoveIds.HitHeavy : MoveIds.HitLight;
                case ReactionTag.LightHit:
                case ReactionTag.None:
                default:
                    return MoveIds.HitLight;
            }
        }

        /// <summary>
        /// Pick the *pre-hit* reaction the brain should consider. Used by
        /// stance brains' PickReaction implementations to keep their
        /// per-stance branching short. Stance & speed-band gating happens
        /// in the brain — this just produces the candidate.
        /// </summary>
        public static string PickPreHitReaction(
            ReactionTag tag,
            SpeedBand band,
            StanceId stance)
        {
            // Wraith Primed: fade-out everything possible.
            if (stance == StanceId.Wraith && band == SpeedBand.Primed && tag != ReactionTag.None)
                return MoveIds.FadeOut;

            // Sentinel / Stalwart: anchored block, never moves.
            if (stance == StanceId.Sentinel || stance == StanceId.Stalwart)
                return MoveIds.StaticAnchor;

            // Tactician: parry first when in Sharp+ band; else side dodge.
            if (stance == StanceId.Tactician)
            {
                if (band == SpeedBand.Sharp || band == SpeedBand.Primed)
                    return MoveIds.Parry;
                return MoveIds.DodgeSideLeft;
            }

            // Conduit: backstep / fade-out for big signs; side-dodge otherwise.
            if (stance == StanceId.Conduit)
            {
                if (tag == ReactionTag.BigSign || tag == ReactionTag.Heavy)
                    return MoveIds.DodgeBack;
                return MoveIds.DodgeSideLeft;
            }

            // Default by attack tag:
            switch (tag)
            {
                case ReactionTag.Launch:
                case ReactionTag.Knockdown:
                    // Forced launches can sometimes be evaded with a back-dodge.
                    return MoveIds.DodgeBack;
                case ReactionTag.Heavy:
                    return MoveIds.DodgeBack;
                case ReactionTag.Sweep:
                    return MoveIds.BobWeave;
                case ReactionTag.BigSign:
                    return MoveIds.DodgeSideLeft;
                case ReactionTag.LightHit:
                default:
                    return band >= SpeedBand.Sharp ? MoveIds.BobWeave : MoveIds.BlockReact;
            }
        }
    }
}
