using UnityEngine;
using TacticalRPG.DataModels;

namespace TacticalRPG.Systems.Combat
{
    /// <summary>
    /// Per-unit, per-tick state of the move-based combat engine. Lives in
    /// BattleCombatEngine; one instance per registered unit. Mutable.
    ///
    /// See COMBAT_DESIGN "Combat engine — Move state per unit".
    /// </summary>
    public class UnitMoveExecution
    {
        // ── The currently-playing move ──────────────────────────────

        public MoveDefinition currentMove;
        public int            framesElapsed;       // tick count since currentMove started
        public MovePhase      phase;               // recomputed each tick from currentMove + framesElapsed

        // ── Pre-queued next move ────────────────────────────────────

        /// <summary>
        /// Brain queues the next move during current move's CancelWindow
        /// (combo chain) or after Done (neutral pick). Engine consumes on
        /// next tick.
        /// </summary>
        public MoveDefinition queuedNext;

        // ── Combat-relevant flags ───────────────────────────────────

        public bool airborne;
        public bool superArmorActive;
        public int  remainingIFrames;              // counted down each tick within active phase

        /// <summary>
        /// Hits taken in immediate succession without landing one of our
        /// own. Reset when we land a hit ourselves or successfully evade
        /// (dodge / block). Brains read this to escalate from "eat the
        /// light hit" to "back-dodge" once the defender is clearly losing
        /// the trade.
        /// </summary>
        public int consecutiveHitsTaken;

        /// <summary>
        /// Number of consecutive cancel-into-attack moves chained.
        /// Increments each time a cancel-into produces an attack; resets
        /// on starting a non-attack (locomotion / dodge / idle / cast).
        /// Brains read this to cap combo length — beyond ~3 chains the
        /// attacker is forced to take a beat before the next exchange.
        /// </summary>
        public int cancelChainDepth;

        /// <summary>
        /// Set when this unit's most-recent active hit landed full damage.
        /// Used by brain to pick onHit-cancel chains rather than onWhiff.
        /// Reset when current move's active phase ends.
        /// </summary>
        public bool lastActiveHitConfirmed;

        /// <summary>
        /// Set once the brain has seen the cancel window for the current
        /// move and made its pick. Prevents the engine from asking the
        /// brain every frame inside the window.
        /// </summary>
        public bool cancelDecisionMade;

        /// <summary>
        /// Set once an active-frame hit check has been resolved this move,
        /// so multi-frame active windows don't re-hit the same target each
        /// tick. (A multi-hit move is composed of multiple sequential
        /// MoveDefinitions chained via cancelIntoOnHit.)
        /// </summary>
        public bool activeHitResolved;

        // ── Facing override ─────────────────────────────────────────

        /// <summary>
        /// World-space facing the move locked at start (used when
        /// FacingPolicy == Lock).
        /// </summary>
        public Vector3 lockedFacing;

        // ── Helpers ─────────────────────────────────────────────────

        public bool IsAttack => currentMove != null && currentMove.IsAttack;
        public bool IsActive => phase == MovePhase.Active;
        public bool IsBlocking => currentMove != null && currentMove.isBlock && phase == MovePhase.Active;
        public bool IsParrying => currentMove != null && currentMove.isParry && phase == MovePhase.Active;

        public void StartMove(MoveDefinition move, Vector3 facing)
        {
            currentMove   = move;
            framesElapsed = 0;
            phase         = move != null ? move.PhaseAtFrame(0) : MovePhase.Done;
            queuedNext    = null;
            lastActiveHitConfirmed = false;
            cancelDecisionMade     = false;
            activeHitResolved      = false;
            superArmorActive       = move != null && move.superArmorFrames > 0;
            lockedFacing           = facing;
            remainingIFrames       = 0;
        }

        public void Reset()
        {
            currentMove = null;
            framesElapsed = 0;
            phase = MovePhase.Done;
            queuedNext = null;
            airborne = false;
            superArmorActive = false;
            remainingIFrames = 0;
            lastActiveHitConfirmed = false;
            cancelDecisionMade = false;
            activeHitResolved = false;
        }
    }
}
