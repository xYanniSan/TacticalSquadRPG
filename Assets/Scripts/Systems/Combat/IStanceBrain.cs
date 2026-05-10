using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// One IStanceBrain implementation per stance (Onslaught, Tempest,
    /// Stalwart, Tactician, Wraith, Sentinel, Conduit). The brain runs at
    /// engine tick rate (20Hz). Each call returns a MoveDefinition or null.
    ///
    /// The four hooks together describe a fighter's personality.
    /// PickNeutral picks fresh moves from a stationary base; PickReaction
    /// is a single-frame reflex when a hit is imminent; PickPreparation is
    /// proactive — "I see this coming, I should get ready" — running every
    /// tick a unit is in neutral; PickCancel chains during a recovery's
    /// cancel window.
    ///
    /// See COMBAT_DESIGN "The brain layer".
    /// </summary>
    public interface IStanceBrain
    {
        /// <summary>
        /// Stance this brain implements. Used by the registry.
        /// </summary>
        StanceId Id { get; }

        /// <summary>
        /// Called when the unit's current move ends and a fresh move must
        /// be picked. Returning null means "fall back to idle".
        /// </summary>
        MoveDefinition PickNeutral(BrainContext ctx);

        /// <summary>
        /// Called when an opponent's attack has just entered active phase
        /// (or is about to within one frame) AND the attack would land on
        /// this unit. Return a defensive move (block / dodge / parry /
        /// fade) or null to "eat the hit".
        /// </summary>
        MoveDefinition PickReaction(MoveDefinition incoming, BrainContext ctx);

        /// <summary>
        /// Called every tick the unit is in a neutral or locomotion move.
        /// Reads perception-gated threats and ally states; lets the brain
        /// preemptively cast walls, interpose, or interrupt long casts.
        /// Distinct from PickReaction (which fires only when a hit is
        /// imminent). Return null to stay in PickNeutral selection.
        /// </summary>
        MoveDefinition PickPreparation(BrainContext ctx);

        /// <summary>
        /// Called when the current move enters its cancel window. Optional
        /// cancel-into pick. Return null to let the move recover normally.
        /// </summary>
        MoveDefinition PickCancel(MoveDefinition current, BrainContext ctx);
    }
}
