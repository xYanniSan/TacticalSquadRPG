using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat.Brains
{
    /// <summary>
    /// Wraith — assassin. Orbits at far, fades in for a strike, fades back
    /// out. Defensive teleport when pressured at Primed.
    /// </summary>
    public class WraithBrain : StanceBrainBase
    {
        public override StanceId Id => StanceId.Wraith;

        public override MoveDefinition PickNeutral(BrainContext ctx)
        {
            // Far → orbit and build speed.
            if (ctx.rangeBand == RangeBand.Far)
            {
                bool clockwise = ctx.selfRuntime != null && (ctx.selfRuntime.runtimeId & 1) == 0;
                var orbitId = clockwise ? MoveIds.OrbitClockwise : MoveIds.OrbitCounter;
                return Get(orbitId, ctx) ?? Get(MoveIds.Run, ctx);
            }

            // Mid → orbit. Wraith doesn't approach by walking.
            if (ctx.rangeBand == RangeBand.Mid)
            {
                if (ctx.speedBand == SpeedBand.Primed)
                {
                    var dash = GetAffordable(MoveIds.DashForward, ctx);
                    if (dash != null) return dash;
                }
                bool clockwise = ctx.selfRuntime != null && (ctx.selfRuntime.runtimeId & 1) == 0;
                var orbitId = clockwise ? MoveIds.OrbitClockwise : MoveIds.OrbitCounter;
                return Get(orbitId, ctx) ?? Get(MoveIds.Run, ctx);
            }

            // Close → fast strike.
            if (ctx.speedBand >= SpeedBand.Sharp)
            {
                var crescent = TryCommitHeavy(MoveIds.KickCrescent, ctx, speedFloor: 30f);
                if (crescent != null) return crescent;
            }
            return GetAffordable(MoveIds.PunchJab, ctx)
                ?? Get(MoveIds.WalkForward, ctx);
        }

        public override MoveDefinition PickReaction(MoveDefinition incoming, BrainContext ctx)
        {
            // Primed → fade-out anything dangerous.
            if (ctx.speedBand == SpeedBand.Primed)
            {
                var fade = GetAffordable(MoveIds.FadeOut, ctx);
                if (fade != null) return fade;
            }
            // Sharp → bob/weave / side-dodge.
            return GetAffordable(MoveIds.DodgeSideRight, ctx)
                ?? GetAffordable(MoveIds.DodgeSideLeft, ctx)
                ?? GetAffordable(MoveIds.DodgeBack, ctx);
        }

        public override MoveDefinition PickCancel(MoveDefinition current, BrainContext ctx)
        {
            // Wraith rarely cancels — prefers to disengage and re-engage.
            return null;
        }
    }
}
