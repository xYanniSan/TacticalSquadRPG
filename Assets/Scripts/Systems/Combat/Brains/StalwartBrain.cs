using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat.Brains
{
    /// <summary>
    /// Stalwart — protector role. Holds position, blocks. Rarely commits.
    /// </summary>
    public class StalwartBrain : StanceBrainBase
    {
        public override StanceId Id => StanceId.Stalwart;

        public override MoveDefinition PickNeutral(BrainContext ctx)
        {
            if (ctx.target != null && !ctx.target.IsDead
                && ctx.rangeBand == RangeBand.Close
                && ctx.currentSpeed >= 50f)
            {
                var heavy = GetAffordable(MoveIds.PowerStrike, ctx);
                if (heavy != null) return heavy;
            }

            // Stalwart will swing if the target is right there, but
            // doesn't chase aggressively.
            if (ctx.target != null && !ctx.target.IsDead
                && (ctx.rangeBand == RangeBand.Close || ctx.rangeBand == RangeBand.Locked))
            {
                var jab = GetAffordable(MoveIds.PunchJab, ctx);
                if (jab != null) return jab;
            }

            if (ctx.target != null && !ctx.target.IsDead
                && (ctx.rangeBand == RangeBand.Far || ctx.rangeBand == RangeBand.Mid))
            {
                return Get(MoveIds.WalkForward, ctx) ?? Get(MoveIds.Idle, ctx);
            }

            return Get(MoveIds.BlockIdle, ctx) ?? Get(MoveIds.Idle, ctx);
        }

        public override MoveDefinition PickReaction(MoveDefinition incoming, BrainContext ctx)
        {
            // Stalwart never moves out of the way — anchored block.
            return GetAffordable(MoveIds.BlockReact, ctx)
                ?? GetAffordable(MoveIds.StaticAnchor, ctx);
        }

        public override MoveDefinition PickCancel(MoveDefinition current, BrainContext ctx)
        {
            // Stalwart never cancels — committed moves play through.
            return null;
        }
    }
}
