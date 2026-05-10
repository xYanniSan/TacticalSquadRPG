using System.Collections.Generic;
using UnityEngine;
using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// The 20Hz move-based combat engine. One tick == 50ms. Each registered
    /// unit always has exactly one MoveDefinition currently executing; the
    /// engine advances frame counters, runs hit resolution during active
    /// frames, integrates movement, and dispatches brain hooks at the
    /// right phase boundaries.
    ///
    /// See COMBAT_DESIGN "Combat engine — move-based, frame-data driven".
    ///
    /// Architectural rules (per CLAUDE.md):
    /// - Subsystem MonoBehaviour on TerrainBattleManager, no logic in
    ///   TerrainBattleManager itself.
    /// - All combat state lives in this engine; UnitMoveExecution is owned
    ///   here, never copied to other subsystems.
    /// - Animation is downstream of truth: PlayMove fires only when the
    ///   currentMove changes, and never gates engine logic.
    /// </summary>
    public class BattleCombatEngine : MonoBehaviour
    {
        public const float TickSeconds = 0.05f;   // 20Hz canonical tick

        [Header("Engine")]
        [Tooltip("Master toggle. When false, the engine doesn't tick — units fall back to the legacy state machine.")]
        [SerializeField] private bool enabled_ = true;

        [Tooltip("If true, log every move start/phase transition / hit to CombatLogger. Voluminous; turn off for perf.")]
        [SerializeField] private bool verboseLogging = true;

        [Header("Move catalog (optional Inspector additions)")]
        [Tooltip("Moves assigned here are added to the catalog at startup, in addition to anything " +
                 "discovered under Resources/Moves/.")]
        [SerializeField] private List<MoveDefinition> additionalMoves = new List<MoveDefinition>();

        [Header("Range bands (must align with COMBAT_DESIGN)")]
        [SerializeField] private float farThreshold    = 8f;
        [SerializeField] private float midThreshold    = 3f;
        [SerializeField] private float closeThreshold  = 1f;

        [Header("Reaction window")]
        [Tooltip("Frames before active phase begins during which the defender's brain is given a " +
                 "PickReaction call. 1 frame = 50ms. Higher = more responsive defenders.")]
        [Range(0, 4)] [SerializeField] private int reactionLookahead = 1;

        // ── Wired via TerrainBattleManager ─────────────────────────

        private MoveCatalog _catalog;
        public  MoveCatalog Catalog => _catalog;

        [Header("Determinism")]
        [Tooltip("Seed for the per-battle RNG. Brains use this PRNG instead of UnityEngine.Random " +
                 "so a fight is replayable from (seed + decisions per tick). 0 means \"derive from " +
                 "Time.frameCount at engine startup\" (non-deterministic, useful for variety in " +
                 "playtesting).")]
        [SerializeField] private int randomSeed = 0;

        private EngineRandom _rng;
        public  EngineRandom Random => _rng;

        // ── Per-unit state ─────────────────────────────────────────

        private readonly Dictionary<TerrainBattleUnit, UnitMoveExecution> _state =
            new Dictionary<TerrainBattleUnit, UnitMoveExecution>();

        private readonly Dictionary<TerrainBattleUnit, TerrainBattleUnit> _engineTarget =
            new Dictionary<TerrainBattleUnit, TerrainBattleUnit>();

        private readonly List<TerrainBattleUnit> _registered = new List<TerrainBattleUnit>();

        // ── Tick accumulator ───────────────────────────────────────

        private float _accumulator;

        public bool Enabled => enabled_;

        // ── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            _catalog = new MoveCatalog(additionalMoves, loadFromResources: true);
            int seed = randomSeed != 0 ? randomSeed : Time.frameCount;
            _rng = new EngineRandom(seed);
            if (verboseLogging)
                Debug.Log($"[BattleCombatEngine] Catalog loaded with {_catalog.Count} moves. RNG seed={seed}.");
        }

        public void RegisterUnit(TerrainBattleUnit unit)
        {
            if (unit == null || _state.ContainsKey(unit)) return;
            var exec = new UnitMoveExecution();
            // Start in idle.
            var idle = _catalog.Get(MoveIds.Idle);
            exec.StartMove(idle, unit.transform.forward);
            _state[unit] = exec;
            _registered.Add(unit);
            // Drive idle animation immediately so units don't T-pose
            // while waiting for first tick.
            unit.GetComponent<UnitAnimationDriver>()?.PlayMove(MoveIds.Idle);
        }

        public void UnregisterUnit(TerrainBattleUnit unit)
        {
            if (unit == null) return;
            _state.Remove(unit);
            _engineTarget.Remove(unit);
            _registered.Remove(unit);
        }

        public UnitMoveExecution GetState(TerrainBattleUnit unit)
            => unit != null && _state.TryGetValue(unit, out var s) ? s : null;

        // ── Update loop ────────────────────────────────────────────

        private void Update()
        {
            if (!enabled_) return;
            // Don't tick after battle is over — units freeze in their last
            // pose, log stops growing.
            var mgr = TerrainBattleManager.Instance;
            if (mgr != null && mgr.IsBattleOver) return;
            _accumulator += Time.deltaTime;
            // Cap accumulator to avoid huge catch-up bursts after a freeze.
            if (_accumulator > TickSeconds * 4f) _accumulator = TickSeconds * 4f;
            while (_accumulator >= TickSeconds)
            {
                _accumulator -= TickSeconds;
                Tick();
            }
        }

        private int _tickCount;

        [Header("Diagnostics")]
        [Tooltip("Periodic snapshot to combat log (positions, current moves). Set to 0 to disable. " +
                 "Default 100 = every 5s of game time.")]
        [SerializeField] private int heartbeatTicks = 100;

        private void Tick()
        {
            _tickCount++;
            if (heartbeatTicks > 0 && (_tickCount % heartbeatTicks) == 0)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append($"tick={_tickCount} ");
                    var speedSys = TerrainBattleManager.Instance?.Speed;
                    for (int i = 0; i < _registered.Count; i++)
                    {
                        var u = _registered[i];
                        if (u == null) continue;
                        if (u.IsDead) continue;
                        if (!_state.TryGetValue(u, out var st)) continue;
                        Vector3 p = u.transform.position;
                        string name = (u.Unit != null) ? u.Unit.DisplayName : u.gameObject.name;
                        string moveId = (st.currentMove != null) ? st.currentMove.id : "null";
                        float spd = speedSys != null ? speedSys.GetSpeed(u) : 0f;
                        int hp = u.Unit != null ? u.Unit.currentHP : 0;
                        sb.Append($"[{name} hp={hp} spd={spd:F0} pos=({p.x:F1},{p.z:F1}) move={moveId} f={st.framesElapsed} hits={st.consecutiveHitsTaken}] ");
                    }
                    CombatLogger.Instance?.Log(CombatLogger.CAT_STATE, "engine", sb.ToString());
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[engine] heartbeat threw: {ex.Message}");
                }
            }
            // Snapshot — units may be unregistered mid-tick by death etc.
            for (int i = 0; i < _registered.Count; i++)
            {
                var unit = _registered[i];
                if (unit == null || unit.IsDead) continue;
                if (!_state.TryGetValue(unit, out var exec)) continue;
                // Skip until the legacy brain hands off (post-engagement).
                // While the unit is still Backline / Engage, leave it to
                // the existing engagement system.
                var brain = unit.GetComponent<UnitBrainAI>();
                if (brain == null || !brain.EngineControlled) continue;
                TickUnit(unit, exec);
            }
        }

        // ── Per-unit tick ──────────────────────────────────────────

        private void TickUnit(TerrainBattleUnit unit, UnitMoveExecution exec)
        {
            // Don't drive units that haven't been promoted to frontline yet.
            // The legacy backline/engagement system still gates that. When
            // they request engagement, TerrainBattleUnit will register them.
            if (exec.currentMove == null)
            {
                var idle = _catalog.Get(MoveIds.Idle);
                exec.StartMove(idle, unit.transform.forward);
            }

            // Resolve target each tick (cheap; existing finder caches).
            var target = ResolveTarget(unit);
            float dist = target != null
                ? Vector3.Distance(unit.transform.position, target.transform.position)
                : 999f;

            // Build per-tick brain context up front; some hooks consume it.
            BrainContext ctx = BuildContext(unit, exec, target, dist);

            // ── Step 1: integrate movement & facing for current move ──
            ApplyMovement(unit, exec, ctx);

            // ── Step 2: hit resolution if we are entering active frame ─
            // (Done before incrementing frame counter so the just-entered
            // active phase is what's evaluated.)
            UpdatePhase(exec);
            if (exec.IsActive && exec.currentMove.IsAttack && !exec.activeHitResolved && target != null)
            {
                ResolveHit(unit, exec, target, ctx);
            }

            // ── Step 3: defender reaction lookahead ────────────────────
            // Look at every other registered unit; if one is about to
            // strike us (within reactionLookahead frames of entering
            // active), give our brain a PickReaction call.
            TryPickReaction(unit, exec, ctx);

            // ── Step 4: cancel-window decision ─────────────────────────
            if (exec.phase == MovePhase.CancelWindow && !exec.cancelDecisionMade)
            {
                exec.cancelDecisionMade = true;
                var cancelPick = StanceBrainRegistry.Get(ctx.stance).PickCancel(exec.currentMove, ctx);
                if (cancelPick != null)
                    StartMove(unit, exec, cancelPick, ctx, "cancel");
            }

            // ── Step 5: advance frame ──────────────────────────────────
            exec.framesElapsed++;
            UpdatePhase(exec);

            // ── Step 6: if move is now Done, pick next ────────────────
            if (exec.phase == MovePhase.Done)
            {
                MoveDefinition next = exec.queuedNext;
                if (next == null)
                {
                    var brain = StanceBrainRegistry.Get(ctx.stance);
                    next = brain.PickPreparation(ctx);
                    if (next == null) next = brain.PickNeutral(ctx);
                    if (next == null) next = _catalog.Get(MoveIds.Idle);
                }
                StartMove(unit, exec, next, ctx, "next");
            }
        }

        private void UpdatePhase(UnitMoveExecution exec)
        {
            if (exec.currentMove == null) { exec.phase = MovePhase.Done; return; }
            exec.phase = exec.currentMove.PhaseAtFrame(exec.framesElapsed);
            // Cache i-frame state for the active phase.
            if (exec.IsActive)
            {
                int activeFrame = exec.framesElapsed
                                - exec.currentMove.startupFrames;  // 0-indexed within active
                if (activeFrame >= exec.currentMove.iFrameStart
                 && activeFrame <= exec.currentMove.iFrameEnd)
                    exec.remainingIFrames = 1;
                else
                    exec.remainingIFrames = 0;
            }
            else
            {
                exec.remainingIFrames = 0;
            }
            // Super armor active during startup + active.
            exec.superArmorActive = exec.currentMove.superArmorFrames > 0
                                  && exec.framesElapsed < exec.currentMove.superArmorFrames;
        }

        // ── Move start ─────────────────────────────────────────────

        private void StartMove(TerrainBattleUnit unit, UnitMoveExecution exec,
                               MoveDefinition move, BrainContext ctx, string reason)
        {
            if (move == null) return;

            // Detect "same locomotion repeated" before mutating exec.
            bool isRepeatLocomotion = exec.currentMove != null
                                   && move != null
                                   && move.id == exec.currentMove.id
                                   && move.IsLocomotion;

            // Pay costs (engine deducts so brains can be stateless about it).
            BattleSpeedSystem speedSys = TerrainBattleManager.Instance?.Speed;
            if (move.speedCost > 0f && speedSys != null)
                speedSys.SpendSpeed(unit, move.speedCost);
            if (move.energyCost > 0f && unit.Unit != null)
                unit.Unit.SpendEnergy(move.energyCost);

            // Lock facing if requested.
            Vector3 facing = unit.transform.forward;
            if (ctx.target != null && move.facing == FacingPolicy.FaceTarget)
            {
                Vector3 toTarget = ctx.target.transform.position - unit.transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f)
                    facing = toTarget.normalized;
            }
            // If we're starting a dodge / parry / block move, that's an
            // intentional defensive break — clear the consecutive-hits
            // counter so the brain doesn't re-trigger the same dodge
            // every tick after.
            if (move.category == MoveCategory.Dodge
             || move.category == MoveCategory.Parry
             || move.category == MoveCategory.Block)
            {
                exec.consecutiveHitsTaken = 0;
            }

            // Cancel-chain depth tracking. Cancel into another attack
            // increments; any non-attack move resets to 0 so brains can
            // cap combos and produce visible "beats" between exchanges.
            if (reason == "cancel" && move.IsAttack)
            {
                exec.cancelChainDepth++;
            }
            else if (!move.IsAttack)
            {
                exec.cancelChainDepth = 0;
            }
            else
            {
                // Fresh attack from neutral — reset and start at 1.
                exec.cancelChainDepth = 1;
            }

            exec.StartMove(move, facing);

            if (verboseLogging && !isRepeatLocomotion)
            {
                CombatLogger.Instance?.Log(CombatLogger.CAT_STATE,
                    unit.Unit?.DisplayName ?? unit.gameObject.name,
                    $"[engine] {reason} → {move.id} (start={move.startupFrames} act={move.activeFrames} rec={move.recoveryFrames})");
            }

            // Drive animation. PlayMove no-ops on missing names.
            // Skip for repeat-locomotion to avoid resetting the blend tree.
            if (!isRepeatLocomotion)
                unit.GetComponent<UnitAnimationDriver>()?.PlayMove(move.ResolvedAnimationName);
        }

        // ── Movement integration ───────────────────────────────────

        private void ApplyMovement(TerrainBattleUnit unit, UnitMoveExecution exec, BrainContext ctx)
        {
            var mover = unit.GetComponent<UnitMovementController>();
            if (mover == null || exec.currentMove == null) return;

            // Facing: snap target-locked if the move asks; otherwise keep
            // current rotation. (FaceTarget eases it via mover.FaceTarget.)
            if (exec.currentMove.facing == FacingPolicy.FaceTarget && ctx.target != null && !ctx.target.IsDead)
                mover.FaceTarget(ctx.target.transform);
            else if (exec.currentMove.facing == FacingPolicy.Lock && exec.lockedFacing.sqrMagnitude > 0.01f)
                mover.FaceDirection(exec.lockedFacing);

            float fwdMps = exec.currentMove.forwardSpeedMetersPerSecond;
            float latMps = exec.currentMove.lateralSpeedMetersPerSecond;

            // Stop short on forward locomotion: don't run through the
            // target. Stop just inside the typical strike reach (light
            // attacks have range 2.0u) so the brain picks a strike at
            // exactly the right distance — not so close that they can't
            // swing freely.
            if (exec.currentMove.IsLocomotion && fwdMps > 0.1f
             && ctx.target != null && !ctx.target.IsDead
             && ctx.distanceToTarget <= 1.9f)
            {
                exec.framesElapsed = exec.currentMove.TotalFrames;
                exec.phase = MovePhase.Done;
                mover.EngineMove(Vector3.zero, 0f);
                return;
            }

            // Resolve movement basis. FaceTarget locomotion uses the
            // direction-toward-target each tick (so chasing a moving
            // target doesn't curve into orbits as the unit's transform
            // rotation lags via Slerp). Lock uses lockedFacing. Free uses
            // current transform.forward.
            Vector3 fwdDir;
            if (exec.currentMove.facing == FacingPolicy.FaceTarget
             && ctx.target != null && !ctx.target.IsDead)
            {
                Vector3 toTarget = ctx.target.transform.position - unit.transform.position;
                toTarget.y = 0f;
                fwdDir = (toTarget.sqrMagnitude > 0.001f) ? toTarget.normalized : unit.transform.forward;
            }
            else if (exec.currentMove.facing == FacingPolicy.Lock && exec.lockedFacing.sqrMagnitude > 0.001f)
            {
                fwdDir = exec.lockedFacing;
                fwdDir.y = 0f;
                if (fwdDir.sqrMagnitude > 0.001f) fwdDir.Normalize();
                else fwdDir = unit.transform.forward;
            }
            else
            {
                fwdDir = unit.transform.forward;
            }
            // Lateral basis: world-up cross fwdDir → right-perpendicular.
            Vector3 latDir = Vector3.Cross(Vector3.up, fwdDir);

            // Curve override for forward displacement (dashes/dodges).
            if (exec.currentMove.forwardDisplacementCurve != null
             && exec.currentMove.forwardDisplacementCurve.length > 0)
            {
                int total = Mathf.Max(1, exec.currentMove.TotalFrames);
                float t0 = (float)exec.framesElapsed / total;
                float t1 = (float)(exec.framesElapsed + 1) / total;
                float meters = exec.currentMove.forwardDisplacementCurve.Evaluate(t1)
                             - exec.currentMove.forwardDisplacementCurve.Evaluate(t0);
                // meters over TickSeconds → m/s for that frame.
                fwdMps = meters / TickSeconds;
            }

            // Drive movement intent so BattleSpeedSystem ticks speed gain
            // appropriately (Close/Disengage/Circle/Hold/Dash). The system's
            // gain table reads this each tick.
            BattleMovementSystem moveSys = TerrainBattleManager.Instance?.Movement;
            if (moveSys != null)
            {
                MovementIntent intent;
                if      (fwdMps >  4.5f)        intent = MovementIntent.Dash;
                else if (fwdMps >  0.1f)        intent = MovementIntent.Close;
                else if (fwdMps < -0.1f)        intent = MovementIntent.Disengage;
                else if (Mathf.Abs(latMps) > 0.1f) intent = MovementIntent.Circle;
                else                            intent = MovementIntent.Hold;
                moveSys.SetIntent(unit, intent);
            }

            // Stationary moves still need a zero-delta call so the
            // animator's Speed parameter falls to zero. Otherwise
            // CurrentMoveSpeed lingers at the last running value and the
            // unit "runs in place" while idle.
            if (Mathf.Abs(fwdMps) < 0.001f && Mathf.Abs(latMps) < 0.001f)
            {
                mover.EngineMove(Vector3.zero, 0f);
                return;
            }

            // Move along the (target-aware) basis we computed. Scale to
            // engine tick (TickSeconds = 50ms) — NOT Time.deltaTime.
            Vector3 fwd = fwdDir * fwdMps;
            Vector3 lat = latDir * latMps;
            Vector3 dirMps = fwd + lat;
            Vector3 delta = dirMps * TickSeconds;
            mover.EngineMove(delta, dirMps.magnitude);
        }

        // ── Hit resolution ─────────────────────────────────────────

        private void ResolveHit(TerrainBattleUnit attacker, UnitMoveExecution attackerExec,
                                TerrainBattleUnit target, BrainContext ctx)
        {
            // Mark resolved so we don't multi-hit per active frame.
            attackerExec.activeHitResolved = true;

            if (target == null || target.IsDead) return;

            float dist = Vector3.Distance(attacker.transform.position, target.transform.position);
            if (dist > attackerExec.currentMove.range + 0.5f)
                return; // OutOfRange — silently miss.

            // Cone check.
            Vector3 toDef = target.transform.position - attacker.transform.position;
            toDef.y = 0f;
            if (toDef.sqrMagnitude > 0.001f)
            {
                Vector3 fwd = attacker.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.001f)
                {
                    float ang = Vector3.Angle(fwd.normalized, toDef.normalized);
                    if (ang > attackerExec.currentMove.angleDegrees) return;
                }
            }

            var defenderExec = GetState(target);

            // Defender state pair table.
            HitResolution outcome = HitResolution.FullHit;
            float defenderIncomingMult = 1f;
            bool wasBlocked = false;

            // Defender's i-frame state may be from its previous tick (the
            // engine ticks units sequentially); recompute against current
            // framesElapsed so the check sees the same frame the
            // defender's render is on.
            if (defenderExec != null) UpdatePhase(defenderExec);

            if (defenderExec != null)
            {
                if (defenderExec.remainingIFrames > 0)
                {
                    outcome = HitResolution.Whiff;
                }
                else if (defenderExec.IsBlocking)
                {
                    outcome = HitResolution.Blocked;
                    wasBlocked = true;
                    defenderIncomingMult = defenderExec.currentMove.incomingDamageMultiplier;
                    if (defenderIncomingMult <= 0f) defenderIncomingMult = 0.5f;
                }
                else if (defenderExec.IsParrying)
                {
                    // Parry: attacker's move is interrupted; defender opens
                    // counter window. We force attacker into Stagger via
                    // the engine and freeze defender into a brief recoil.
                    outcome = HitResolution.Whiff;
                    ForceMove(attacker, attackerExec, _catalog.Get(MoveIds.Dazed), ctx, "parried");
                }
                else if (defenderExec.IsActive && defenderExec.currentMove.IsAttack
                      && attackerExec.IsActive)
                {
                    // Both active → trade.
                    outcome = HitResolution.Trade;
                }
                else if (defenderExec.superArmorActive)
                {
                    // Super armor: defender takes damage but is not
                    // forced into a hit-react move.
                    outcome = HitResolution.FullHit;
                }
            }

            // Whiff → log and bail (no damage).
            if (outcome == HitResolution.Whiff)
            {
                if (verboseLogging)
                    CombatLogger.Instance?.Log(CombatLogger.CAT_DMG,
                        attacker.Unit?.DisplayName ?? attacker.gameObject.name,
                        $"[engine] WHIFF on {target.Unit?.DisplayName ?? target.gameObject.name} ({attackerExec.currentMove.id})");
                return;
            }

            // Compute damage.
            int baseDmg = attackerExec.currentMove.damage;
            // Speed scaling.
            if (attackerExec.currentMove.speedDamageScaling > 0f)
            {
                float scale = 1f + (ctx.currentSpeed / 100f) * attackerExec.currentMove.speedDamageScaling;
                baseDmg = Mathf.RoundToInt(baseDmg * scale);
            }
            // Apply attacker.attack vs defender.defense linearly (existing pattern).
            int atk = attacker.Unit != null ? attacker.Unit.currentStats.attack : 10;
            int def = target.Unit   != null ? target.Unit.currentStats.defense  : 0;
            int dmg = Mathf.Max(1, Mathf.RoundToInt((baseDmg + atk * 0.5f) - def * 0.4f));
            dmg = Mathf.RoundToInt(dmg * defenderIncomingMult);
            if (dmg < 1) dmg = 1;

            // Apply damage.
            target.ApplyDamage(dmg);
            attackerExec.lastActiveHitConfirmed = true;
            // Reset attacker's consecutive-hits-taken (they're winning
            // the trade) and increment defender's so brains can react.
            attackerExec.consecutiveHitsTaken = 0;
            if (defenderExec != null) defenderExec.consecutiveHitsTaken++;

            if (verboseLogging)
                CombatLogger.Instance?.Log(CombatLogger.CAT_DMG,
                    attacker.Unit?.DisplayName ?? attacker.gameObject.name,
                    $"[engine] {outcome} on {target.Unit?.DisplayName ?? target.gameObject.name} " +
                    $"({attackerExec.currentMove.id} dmg={dmg} blocked={wasBlocked})");

            // Force defender into the paired forced-reaction move,
            // unless they just blocked (RecoilBlocked is the paired move
            // for a successful block, not a hit-react).
            if (target.IsDead) return;
            if (outcome == HitResolution.Trade) return; // no force on trade — both keep swinging
            if (defenderExec != null && defenderExec.superArmorActive) return;

            string reactId = MoveReactionTable.PickForcedReaction(
                attackerExec.currentMove.reactionTag, wasBlocked, dmg);
            var reactMove = _catalog.Get(reactId);
            if (reactMove != null && defenderExec != null)
            {
                BrainContext defCtx = BuildContext(target, defenderExec, attacker, dist);
                ForceMove(target, defenderExec, reactMove, defCtx, "forced-reaction");
            }
        }

        private void ForceMove(TerrainBattleUnit unit, UnitMoveExecution exec,
                               MoveDefinition move, BrainContext ctx, string reason)
        {
            if (move == null) return;

            // If the same forced reaction is already playing, don't reset
            // it to frame 0 — damage was already applied separately, and
            // re-triggering produces the "stuck in hit anim" visual where
            // the defender pops back to the same pose every 100ms. Let
            // the existing move keep progressing toward exit; the
            // follow-up hits land but the unit's animation/state is
            // continuous.
            if (exec.currentMove != null
             && exec.currentMove == move
             && move.category == MoveCategory.HitReact)
            {
                if (verboseLogging)
                    CombatLogger.Instance?.Log(CombatLogger.CAT_STATE,
                        unit.Unit?.DisplayName ?? unit.gameObject.name,
                        $"[engine] {reason} → continuing {move.id} (frame {exec.framesElapsed})");
                return;
            }

            // Forced moves bypass cost (CC etc.).
            Vector3 facing = unit.transform.forward;
            if (ctx.target != null && move.facing == FacingPolicy.FaceTarget)
            {
                Vector3 toTarget = ctx.target.transform.position - unit.transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f)
                    facing = toTarget.normalized;
            }
            exec.StartMove(move, facing);
            unit.GetComponent<UnitAnimationDriver>()?.PlayMove(move.ResolvedAnimationName);
            if (verboseLogging)
                CombatLogger.Instance?.Log(CombatLogger.CAT_STATE,
                    unit.Unit?.DisplayName ?? unit.gameObject.name,
                    $"[engine] {reason} → {move.id}");
        }

        // ── Reaction lookahead ────────────────────────────────────

        private void TryPickReaction(TerrainBattleUnit defender, UnitMoveExecution defenderExec, BrainContext ctx)
        {
            // Allow reactions during locomotion / idle / hit-react.
            // (HitReact must remain reactable so defender can dodge out
            // of a combo per Onslaught.PickReaction's stagger-escape.)
            // Block already in progress: don't re-trigger. Dodge already
            // active: don't re-pick (would reset to frame 0 and lose the
            // i-frame window). Death / knockdown / stunned: cannot act.
            if (defenderExec.currentMove == null) return;
            if (defenderExec.currentMove.IsAttack) return;
            if (defenderExec.currentMove.isBlock || defenderExec.currentMove.isParry) return;
            if (defenderExec.airborne) return;
            var cat = defenderExec.currentMove.category;
            if (cat == MoveCategory.Dodge
             || cat == MoveCategory.Block
             || cat == MoveCategory.Parry
             || cat == MoveCategory.Knockdown
             || cat == MoveCategory.Stun
             || cat == MoveCategory.Death) return;

            // Search for incoming attack.
            for (int i = 0; i < _registered.Count; i++)
            {
                var attacker = _registered[i];
                if (attacker == null || attacker == defender || attacker.IsDead) continue;
                if (!_state.TryGetValue(attacker, out var aexec)) continue;
                if (aexec.currentMove == null || !aexec.currentMove.IsAttack) continue;

                // How far is attacker from us? Skip if outside reach + buffer.
                float dist = Vector3.Distance(attacker.transform.position, defender.transform.position);
                if (dist > aexec.currentMove.range + 1.0f) continue;

                // Frames until first active frame.
                int framesUntilActive = aexec.currentMove.startupFrames - aexec.framesElapsed;
                if (framesUntilActive < 0) continue; // already past startup, too late
                if (framesUntilActive > reactionLookahead) continue;

                // Aim cone — only react if we're plausibly the target.
                Vector3 toMe = defender.transform.position - attacker.transform.position;
                toMe.y = 0f;
                Vector3 fwd = attacker.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.001f && toMe.sqrMagnitude > 0.001f)
                {
                    float ang = Vector3.Angle(fwd.normalized, toMe.normalized);
                    if (ang > aexec.currentMove.angleDegrees + 10f) continue;
                }

                // Ask brain — pass a context that holds the attacker as our target
                // for the duration of the call (so PickReaction sees "the threat").
                BrainContext defCtx = ctx;
                defCtx.target = attacker;
                defCtx.targetRuntime = attacker.Unit;
                defCtx.targetState = aexec;
                defCtx.distanceToTarget = dist;
                defCtx.rangeBand = ResolveRangeBand(dist);
                var reaction = StanceBrainRegistry.Get(defCtx.stance).PickReaction(aexec.currentMove, defCtx);
                if (reaction != null)
                {
                    StartMove(defender, defenderExec, reaction, defCtx, "reaction");
                    return;
                }
                // Brain chose to eat the hit (returned null) — leave defender state alone.
                return;
            }
        }

        // ── Context build ──────────────────────────────────────────

        private BrainContext BuildContext(TerrainBattleUnit unit, UnitMoveExecution exec,
                                          TerrainBattleUnit target, float dist)
        {
            BattleSpeedSystem speedSys = TerrainBattleManager.Instance?.Speed;
            float curSpeed = speedSys != null ? speedSys.GetSpeed(unit) : 30f;
            SpeedBand band = speedSys != null ? speedSys.GetSpeedBand(unit) : SpeedBand.Engaged;

            StanceDefinition stance = ResolveStance(unit);
            BehaviorType behavior = unit.Unit?.behavior?.behaviorType ?? BehaviorType.Balanced;

            float hpFrac = 1f;
            if (unit.Unit != null && unit.Unit.maxHP > 0)
                hpFrac = (float)unit.Unit.currentHP / unit.Unit.maxHP;

            return new BrainContext
            {
                self = unit,
                selfRuntime = unit.Unit,
                selfState = exec,
                currentSpeed = curSpeed,
                speedBand = band,
                currentEnergy = unit.Unit != null ? unit.Unit.currentEnergy : 0f,
                currentHPFraction = hpFrac,
                stance = stance,
                behavior = behavior,
                target = target,
                targetRuntime = target != null ? target.Unit : null,
                targetState = target != null ? GetState(target) : null,
                distanceToTarget = dist,
                rangeBand = ResolveRangeBand(dist),
                catalog = _catalog,
                tickSeconds = TickSeconds,
                rng = _rng,
            };
        }

        public RangeBand ResolveRangeBand(float distance)
        {
            if (distance > farThreshold)   return RangeBand.Far;
            if (distance > midThreshold)   return RangeBand.Mid;
            if (distance > closeThreshold) return RangeBand.Close;
            return RangeBand.Locked;
        }

        private static StanceDefinition ResolveStance(TerrainBattleUnit unit)
        {
            if (unit == null) return null;
            // Pull cached stance from the legacy brain (it already does the resolution).
            var brain = unit.GetComponent<UnitBrainAI>();
            if (brain != null && brain.Stance != null) return brain.Stance;
            return unit.Unit?.definition != null ? unit.Unit.definition.defaultStance : null;
        }

        private TerrainBattleUnit ResolveTarget(TerrainBattleUnit unit)
        {
            if (unit == null) return null;
            // Engine-cached target wins as long as it's alive.
            if (_engineTarget.TryGetValue(unit, out var cached) && cached != null && !cached.IsDead)
                return cached;
            // Try legacy brain's target — only valid before handoff.
            var fromBrain = unit.CurrentTarget;
            if (fromBrain != null && !fromBrain.IsDead)
            {
                _engineTarget[unit] = fromBrain;
                return fromBrain;
            }
            // Fallback: nearest enemy. Cache so we don't query each tick.
            var nearest = TerrainBattleManager.Instance?.GetNearestEnemy(unit);
            if (nearest != null) _engineTarget[unit] = nearest;
            return nearest;
        }
    }
}
