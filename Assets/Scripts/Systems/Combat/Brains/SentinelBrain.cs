using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat.Brains
{
    /// <summary>
    /// Sentinel — static defender. Idle in place; never chases, never
    /// disengages. Reaction-only; static anchor when struck.
    /// </summary>
    public class SentinelBrain : StanceBrainBase
    {
        public override StanceId Id => StanceId.Sentinel;

        public override MoveDefinition PickNeutral(BrainContext ctx)
        {
            // Strikes only when contact comes to them at high speed.
            if (ctx.target != null && !ctx.target.IsDead
                && ctx.rangeBand == RangeBand.Locked
                && ctx.currentSpeed >= 60f)
            {
                var heavy = GetAffordable(MoveIds.PowerStrike, ctx);
                if (heavy != null) return heavy;
            }

            // If close, throw the occasional jab so the Sentinel isn't a
            // pure punching bag. Stays defensively positioned (no orbit,
            // no chase), but at Close/Locked actually swings back.
            if (ctx.target != null && !ctx.target.IsDead
                && (ctx.rangeBand == RangeBand.Close || ctx.rangeBand == RangeBand.Locked))
            {
                var jab = GetAffordable(MoveIds.PunchJab, ctx);
                if (jab != null) return jab;
            }

            // Sentinel doesn't chase, but they DO walk slowly toward
            // their target if no one's in range — otherwise they just
            // stand at spawn forever, contributing nothing. The stance's
            // "no advance" identity is preserved by the slow walk speed
            // and the lack of dash/run picks.
            if (ctx.target != null && !ctx.target.IsDead
                && (ctx.rangeBand == RangeBand.Far || ctx.rangeBand == RangeBand.Mid))
            {
                return Get(MoveIds.WalkForward, ctx) ?? Get(MoveIds.Idle, ctx);
            }

            return Get(MoveIds.BlockIdle, ctx) ?? Get(MoveIds.Idle, ctx);
        }

        public override MoveDefinition PickReaction(MoveDefinition incoming, BrainContext ctx)
        {
            return GetAffordable(MoveIds.StaticAnchor, ctx)
                ?? GetAffordable(MoveIds.BlockReact, ctx);
        }

        public override MoveDefinition PickCancel(MoveDefinition current, BrainContext ctx) => null;
    }
}
