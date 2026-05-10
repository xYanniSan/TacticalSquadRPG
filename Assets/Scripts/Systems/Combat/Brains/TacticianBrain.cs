using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat.Brains
{
    /// <summary>
    /// Tactician — orbits, baits, parries. Commits on read.
    /// </summary>
    public class TacticianBrain : StanceBrainBase
    {
        public override StanceId Id => StanceId.Tactician;

        public override MoveDefinition PickNeutral(BrainContext ctx)
        {
            // Orbit at mid; jab in close to bait reactions.
            if (ctx.rangeBand == RangeBand.Mid)
            {
                // Alternate orbit direction to read as evasive footwork.
                bool clockwise = ctx.selfRuntime != null && (ctx.selfRuntime.runtimeId & 1) == 0;
                var orbitId = clockwise ? MoveIds.OrbitClockwise : MoveIds.OrbitCounter;
                return Get(orbitId, ctx) ?? Get(MoveIds.WalkForward, ctx);
            }

            if (ctx.rangeBand == RangeBand.Close || ctx.rangeBand == RangeBand.Locked)
            {
                // Light pressure unless lowest-HP target is killable.
                if (ctx.target != null && !ctx.target.IsDead
                    && ctx.targetRuntime != null
                    && ctx.targetRuntime.currentHP * 4 <= ctx.targetRuntime.maxHP)
                {
                    var heavy = TryCommitHeavy(MoveIds.PowerStrike, ctx, speedFloor: 40f);
                    if (heavy != null) return heavy;
                }
                return GetAffordable(MoveIds.PunchJab, ctx)
                    ?? GetAffordable(MoveIds.PunchHook, ctx)
                    ?? Get(MoveIds.WalkForward, ctx);
            }

            return PickDefaultLocomotion(ctx);
        }

        public override MoveDefinition PickReaction(MoveDefinition incoming, BrainContext ctx)
        {
            if (incoming == null) return null;
            // Parry first if Sharp+; otherwise side-dodge.
            if (ctx.speedBand == SpeedBand.Sharp || ctx.speedBand == SpeedBand.Primed)
            {
                var parry = GetAffordable(MoveIds.Parry, ctx);
                if (parry != null) return parry;
            }
            return GetAffordable(MoveIds.DodgeSideLeft, ctx)
                ?? GetAffordable(MoveIds.BlockReact, ctx);
        }

        public override MoveDefinition PickCancel(MoveDefinition current, BrainContext ctx)
        {
            // Tactician cancels into uppercut when current was a successful jab.
            if (current != null && ctx.selfState != null && ctx.selfState.lastActiveHitConfirmed)
            {
                if (current.id == MoveIds.PunchJab)
                    return GetAffordable(MoveIds.PunchUppercut, ctx);
                if (current.id == MoveIds.PunchHook)
                    return GetAffordable(MoveIds.PunchUppercut, ctx);
            }
            return base.PickCancel(current, ctx);
        }
    }
}
