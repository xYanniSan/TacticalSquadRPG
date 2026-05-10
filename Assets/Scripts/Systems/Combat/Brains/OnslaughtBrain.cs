using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat.Brains
{
    /// <summary>
    /// Onslaught — constant pressure. Closes fast, jab/hook the moment in
    /// range, always cancels into next combo on hit. Never disengages.
    /// </summary>
    public class OnslaughtBrain : StanceBrainBase
    {
        public override StanceId Id => StanceId.Onslaught;

        public override MoveDefinition PickNeutral(BrainContext ctx)
        {
            // If we've been losing the trade (eating multiple hits in a
            // row), break the loop with a back-dodge before swinging
            // again. Resets after we land our own hit. Without this,
            // even Onslaught vs Onslaught can pin one side indefinitely.
            if (ctx.selfState != null && ctx.selfState.consecutiveHitsTaken >= 2
             && ctx.rangeBand != RangeBand.Far)
            {
                var dodge = GetAffordable(MoveIds.DodgeBack, ctx);
                if (dodge != null) return dodge;
            }

            // Out of range → close.
            if (ctx.rangeBand == RangeBand.Far || ctx.rangeBand == RangeBand.Mid)
                return PickDefaultLocomotion(ctx);

            // In range — high speed → power strike, otherwise jab/hook.
            if (ctx.currentSpeed >= 50f)
            {
                var heavy = TryCommitHeavy(MoveIds.PowerStrike, ctx, speedFloor: 50f);
                if (heavy != null) return heavy;
            }

            // Mix jab/hook so combat doesn't read as a metronome.
            var jab  = GetAffordable(MoveIds.PunchJab,  ctx);
            var hook = GetAffordable(MoveIds.PunchHook, ctx);
            // Seeded RNG: deterministic per-battle, but varied between
            // calls. ~40% hook, 60% jab so jab stays the bread-and-butter.
            if (hook != null && ctx.rng != null && ctx.rng.NextBool(0.4f))
                return hook;
            return jab ?? hook ?? Get(MoveIds.WalkForward, ctx);
        }

        public override MoveDefinition PickReaction(MoveDefinition incoming, BrainContext ctx)
        {
            if (incoming == null) return null;

            // Combo-break: if we're currently locked in a hit reaction,
            // the cancel chain is about to swing through us again —
            // back-dodge to escape rather than eat another stack.
            // Without this, two Onslaught units pin each other in
            // react_hit_light forever.
            bool currentlyStaggered = ctx.selfState != null
                && ctx.selfState.currentMove != null
                && ctx.selfState.currentMove.category == MoveCategory.HitReact;
            if (currentlyStaggered)
            {
                return GetAffordable(MoveIds.DodgeBack, ctx)
                    ?? GetAffordable(MoveIds.BlockReact, ctx);
            }

            // Onslaught otherwise: never disengage. Block the heavy stuff,
            // eat the rest (so they keep pressuring forward).
            switch (incoming.reactionTag)
            {
                case ReactionTag.Heavy:
                case ReactionTag.Launch:
                case ReactionTag.Knockdown:
                case ReactionTag.BigSign:
                    return GetAffordable(MoveIds.BlockReact, ctx);
                default:
                    return null;
            }
        }
    }
}
