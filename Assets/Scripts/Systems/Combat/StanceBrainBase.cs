using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Shared helpers for stance brains. Concrete brains override only
    /// the hooks where they want different behaviour.
    /// </summary>
    public abstract class StanceBrainBase : IStanceBrain
    {
        public abstract StanceId Id { get; }

        public abstract MoveDefinition PickNeutral(BrainContext ctx);

        public virtual MoveDefinition PickReaction(MoveDefinition incoming, BrainContext ctx)
        {
            if (incoming == null) return null;
            string id = MoveReactionTable.PickPreHitReaction(
                incoming.reactionTag, ctx.speedBand, Id);
            // Affordability check — never pick a reaction the unit can't afford.
            return GetAffordable(id, ctx);
        }

        public virtual MoveDefinition PickPreparation(BrainContext ctx) => null;

        public virtual MoveDefinition PickCancel(MoveDefinition current, BrainContext ctx)
        {
            if (current == null) return null;

            // Cap cancel chain depth so combos resolve and combat has a
            // beat between exchanges. Beyond MaxChainDepth attacks in a
            // row, the brain skips the cancel — current move plays
            // through full recovery before the next pick.
            if (ctx.selfState != null && ctx.selfState.cancelChainDepth >= MaxChainDepth)
                return null;

            var pool = (ctx.selfState != null && ctx.selfState.lastActiveHitConfirmed)
                ? current.cancelIntoOnHit
                : current.cancelIntoOnWhiff;
            if (pool == null) return null;
            for (int i = 0; i < pool.Count; i++)
            {
                var m = pool[i];
                if (m == null) continue;
                if (CanAfford(m, ctx)) return m;
            }
            return null;
        }

        /// <summary>
        /// Maximum cancel-into-attack chains in a row before the engine
        /// forces a beat (no cancel). 3 = jab → hook → uppercut → done.
        /// Stances may override (Tempest could allow 4 for the launch
        /// sequence, Wraith caps at 2 for hit-and-run).
        /// </summary>
        protected virtual int MaxChainDepth => 3;

        // ── Helpers ────────────────────────────────────────────────

        protected static bool CanAfford(MoveDefinition m, BrainContext ctx)
        {
            if (m == null) return false;
            if (m.speedGate  > 0f && ctx.currentSpeed  < m.speedGate)  return false;
            if (m.speedCost  > 0f && ctx.currentSpeed  < m.speedCost)  return false;
            if (m.energyCost > 0f && ctx.currentEnergy < m.energyCost) return false;
            return true;
        }

        protected static MoveDefinition GetAffordable(string id, BrainContext ctx)
        {
            if (ctx.catalog == null) return null;
            var m = ctx.catalog.Get(id);
            if (m == null) return null;
            return CanAfford(m, ctx) ? m : null;
        }

        protected static MoveDefinition Get(string id, BrainContext ctx)
            => ctx.catalog != null ? ctx.catalog.Get(id) : null;

        /// <summary>
        /// Pick a locomotion move appropriate to the current range band.
        /// Most stances share this default; only Stalwart/Sentinel
        /// override to "always idle".
        /// </summary>
        protected static MoveDefinition PickDefaultLocomotion(BrainContext ctx)
        {
            if (ctx.target == null || ctx.target.IsDead)
                return Get(MoveIds.Idle, ctx);

            switch (ctx.rangeBand)
            {
                case RangeBand.Far:
                    // Sharp+ can dash; otherwise run.
                    if (ctx.speedBand >= SpeedBand.Sharp)
                    {
                        var dash = GetAffordable(MoveIds.DashForward, ctx);
                        if (dash != null) return dash;
                    }
                    return Get(MoveIds.Run, ctx) ?? Get(MoveIds.WalkForward, ctx);
                case RangeBand.Mid:
                    return Get(MoveIds.Run, ctx) ?? Get(MoveIds.WalkForward, ctx);
                case RangeBand.Close:
                case RangeBand.Locked:
                    return Get(MoveIds.WalkForward, ctx);
                default:
                    return Get(MoveIds.Idle, ctx);
            }
        }

        /// <summary>
        /// "Should I commit a heavy this tick?" — shared logic for the
        /// aggressive stances. Returns the heavy move if affordable AND
        /// in range, else null.
        /// </summary>
        protected static MoveDefinition TryCommitHeavy(string heavyId, BrainContext ctx, float speedFloor = 30f)
        {
            if (ctx.currentSpeed < speedFloor) return null;
            if (ctx.rangeBand != RangeBand.Close && ctx.rangeBand != RangeBand.Locked) return null;
            return GetAffordable(heavyId, ctx);
        }
    }
}
