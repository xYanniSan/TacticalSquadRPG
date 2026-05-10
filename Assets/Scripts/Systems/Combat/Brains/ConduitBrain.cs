using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat.Brains
{
    /// <summary>
    /// Conduit — caster archetype. Stays at far range, casts hand signs
    /// and ranged element moves. Disengages on close pressure.
    /// </summary>
    public class ConduitBrain : StanceBrainBase
    {
        public override StanceId Id => StanceId.Conduit;

        public override MoveDefinition PickNeutral(BrainContext ctx)
        {
            // Too close → walk back / dodge.
            if (ctx.rangeBand == RangeBand.Close || ctx.rangeBand == RangeBand.Locked)
            {
                return GetAffordable(MoveIds.DodgeBack, ctx)
                    ?? Get(MoveIds.WalkBackward, ctx)
                    ?? Get(MoveIds.WalkForward, ctx);
            }

            // Far / mid: cast hand signs to build energy / power.
            if (ctx.currentEnergy >= 30f)
            {
                var bigCast = GetAffordable(MoveIds.TripleSign, ctx);
                if (bigCast != null) return bigCast;
            }
            if (ctx.currentEnergy >= 10f)
            {
                // Cycle through A/B/C deterministically.
                int slot = ctx.selfRuntime != null ? (ctx.selfRuntime.runtimeId % 3) : 0;
                string id = slot == 0 ? MoveIds.HandsignA : (slot == 1 ? MoveIds.HandsignB : MoveIds.HandsignC);
                var sign = GetAffordable(id, ctx);
                if (sign != null) return sign;
            }
            return Get(MoveIds.Idle, ctx);
        }

        public override MoveDefinition PickReaction(MoveDefinition incoming, BrainContext ctx)
        {
            // Caster's main defense is distance.
            if (ctx.speedBand == SpeedBand.Primed)
            {
                var fade = GetAffordable(MoveIds.FadeOut, ctx);
                if (fade != null) return fade;
            }
            return GetAffordable(MoveIds.DodgeBack, ctx)
                ?? GetAffordable(MoveIds.DodgeSideLeft, ctx);
        }

        public override MoveDefinition PickCancel(MoveDefinition current, BrainContext ctx)
        {
            // Conduit chains sign casts.
            if (current != null && current.id != null && current.id.StartsWith("cast_handsign"))
            {
                int next = ctx.selfState != null ? (ctx.selfState.framesElapsed % 3) : 0;
                string id = next == 0 ? MoveIds.HandsignA : (next == 1 ? MoveIds.HandsignB : MoveIds.HandsignC);
                return GetAffordable(id, ctx);
            }
            return base.PickCancel(current, ctx);
        }
    }
}
