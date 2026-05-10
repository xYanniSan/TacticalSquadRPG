using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat.Brains
{
    /// <summary>
    /// Tempest — burst skirmisher. Builds in mid range, dashes in for big
    /// committed combos, disengages after.
    /// </summary>
    public class TempestBrain : StanceBrainBase
    {
        public override StanceId Id => StanceId.Tempest;

        public override MoveDefinition PickNeutral(BrainContext ctx)
        {
            // Far → close via dash if Sharp+, else run.
            if (ctx.rangeBand == RangeBand.Far)
                return PickDefaultLocomotion(ctx);

            // Mid → dash to close if Primed (saved-up burst), else run.
            if (ctx.rangeBand == RangeBand.Mid)
            {
                if (ctx.speedBand == SpeedBand.Primed)
                {
                    var dash = GetAffordable(MoveIds.DashForward, ctx);
                    if (dash != null) return dash;
                }
                return Get(MoveIds.Run, ctx) ?? Get(MoveIds.WalkForward, ctx);
            }

            // Close → big commit if Primed, else light.
            if (ctx.speedBand == SpeedBand.Primed)
            {
                var crescent = TryCommitHeavy(MoveIds.KickCrescent, ctx, speedFloor: 30f);
                if (crescent != null) return crescent;
                var power = TryCommitHeavy(MoveIds.PowerStrike, ctx, speedFloor: 50f);
                if (power != null) return power;
            }
            return GetAffordable(MoveIds.PunchHook, ctx)
                ?? GetAffordable(MoveIds.PunchJab, ctx)
                ?? Get(MoveIds.WalkForward, ctx);
        }

        public override MoveDefinition PickReaction(MoveDefinition incoming, BrainContext ctx)
        {
            if (incoming == null) return null;

            // Side-dodge to maintain pressure rather than retreat.
            switch (incoming.reactionTag)
            {
                case ReactionTag.Heavy:
                case ReactionTag.Launch:
                case ReactionTag.Knockdown:
                    return GetAffordable(MoveIds.DodgeBack, ctx)
                        ?? GetAffordable(MoveIds.BlockReact, ctx);
                case ReactionTag.LightHit:
                    return GetAffordable(MoveIds.DodgeSideLeft, ctx);
                case ReactionTag.BigSign:
                    return GetAffordable(MoveIds.DodgeSideLeft, ctx);
                default:
                    return null;
            }
        }
    }
}
