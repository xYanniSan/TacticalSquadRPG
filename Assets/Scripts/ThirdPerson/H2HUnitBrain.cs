using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// AI brain that drives a single `H2HUnit` through the H2H phase cycle.
    /// Per-phase logic from `Docs/Design/HAND_TO_HAND_COMBAT.md` §8:
    ///
    ///  - NotEngaged → if a hostile is within spotting range, transition to Spotting
    ///  - Spotting   → wait out spotting timer, then Approaching
    ///  - Approaching→ pursue target until in engagement range, then Engaged
    ///  - Engaged    → circle / commit / disengage based on decision lag
    ///  - Exchange   → orchestrator owns the unit; brain idles
    ///  - Separating → move backward; on timer expiry, decide re-engage / NotEngaged
    ///
    /// All motion goes through `H2HMovementController.SetMoveIntent` /
    /// `Stop` / `FaceTowards` — the brain declares intent and the
    /// controller integrates it into a smoothed continuous velocity.
    /// The brain never touches `CharacterController` directly.
    /// </summary>
    public class H2HUnitBrain : MonoBehaviour
    {
        [Header("Combat-context circling (Engaged phase)")]
        [Tooltip("Movement speed during Engaged-phase circling (m/s). " +
                 "Should be close to the natural rate of the combat-stance " +
                 "loop clips: KB_Sidestep ~0.94, KB_WalkLeft45 ~0.65, " +
                 "KB_WalkFwd2 ~0.88. Set to 1.0 by default — readable " +
                 "footwork without foot-slide. 0 disables decoration movement.")]
        [SerializeField] private float _engagedLateralDrift = 1.0f;
        [Tooltip("Minimum dwell time before re-rolling circle direction (seconds).")]
        [SerializeField] private float _circleDwellMin = 1.5f;
        [Tooltip("Maximum dwell time before re-rolling circle direction (seconds).")]
        [SerializeField] private float _circleDwellMax = 2.5f;

        [Header("Approach weave")]
        [Tooltip("Sinusoidal lateral oscillation frequency during approach (Hz).")]
        [SerializeField] private float _approachWeaveFrequency = 1.4f;
        [Tooltip("Approach weave amplitude — Aggressive bias (small zigzag).")]
        [SerializeField] private float _approachWeaveAggressive = 0.3f;
        [Tooltip("Approach weave amplitude — Balanced bias (moderate zigzag).")]
        [SerializeField] private float _approachWeaveBalanced = 0.5f;

        [Header("Skip-in commit burst")]
        [Tooltip("If commit distance > this many meters, fire a forward skip clip as visual prelude before the strike.")]
        [SerializeField] private float _skipInDistanceThreshold = 0.7f;

        [Header("Approach")]
        [Tooltip("Re-evaluate target every N seconds in Approach (so a moving target gets refreshed).")]
        [SerializeField] private float _approachTargetRefresh = 0.25f;

        [Header("Commit chances per Engaged decision tick")]
        [Range(0f, 1f)] [SerializeField] private float _commitChanceAggressive = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float _commitChanceBalanced  = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float _commitChanceDefensive = 0.2f;

        [Header("Disengage chances per Engaged decision tick (random)")]
        [Range(0f, 1f)] [SerializeField] private float _disengageChanceAggressive = 0.01f;
        [Range(0f, 1f)] [SerializeField] private float _disengageChanceBalanced  = 0.02f;
        [Range(0f, 1f)] [SerializeField] private float _disengageChanceDefensive = 0.04f;

        [Header("Disengage triggers (conditional)")]
        [Tooltip("No disengage rolls during the first N seconds after entering Engaged. Even boost-driven disengage is suppressed during this window — the unit holds its ground for a beat after a beating before deciding to bail.")]
        [SerializeField] private float _engagementCooldownSeconds = 1.5f;
        [Tooltip("HP-fraction loss in the most recent exchange that triggers a disengage boost (e.g. 0.20 = lost 20% of max HP).")]
        [Range(0f, 1f)] [SerializeField] private float _heavyDamageFractionThreshold = 0.20f;
        [Tooltip("Number of hits absorbed (Eat or Block) in the most recent exchange that triggers a disengage boost.")]
        [SerializeField] private int   _heavyComboHitsThreshold = 3;
        [Tooltip("Per-tick disengage chance ADDED on top of the base when a heavy exchange just resolved. Decays linearly over `_disengageBoostDuration`.")]
        [Range(0f, 1f)] [SerializeField] private float _disengageBoost = 0.20f;
        [Tooltip("Seconds the disengage boost stays active after a heavy exchange.")]
        [SerializeField] private float _disengageBoostDuration = 2.5f;
        [Tooltip("Cap on how far the brain will retreat in Separating phase (clamps `UnitDefinition.separationDistanceMeters`).")]
        [SerializeField] private float _separationDistanceCap = 1.8f;
        // Set when the most recent post-Exchange transition saw heavy
        // damage / a long combo against this unit. Decays at this time.
        private float _disengageBoostUntil;
        // Tracks whether we already evaluated stats for the current
        // Engaged session — prevents the boost from re-arming on every
        // frame inside the cooldown window. Cleared when phase != Engaged
        // (one-shot per Engaged entry).
        private bool     _exchangeStatsEvaluatedThisEngaged;
        private H2HPhase _lastSeenPhase = H2HPhase.NotEngaged;

        private H2HUnit _unit;
        private H2HUnit _currentTarget;

        // Engaged-phase 8-direction circling state. `_engagedRelativeDir`
        // is the unit's intent direction expressed RELATIVE to its facing
        // (x = lateral [+1 right, -1 left], y = forward [+1 fwd, -1 back]).
        // The world-space intent is computed by composing this with
        // `transform.forward` and `transform.right` each frame.
        private Vector2 _engagedRelativeDir = new Vector2(1f, 0f);
        private float   _engagedDirExpiresAt;

        private float _approachRefreshAt;

        public H2HUnit CurrentTarget => _currentTarget;

        private void Awake()
        {
            _unit = GetComponent<H2HUnit>();
        }

        private void Update()
        {
            if (_unit == null || !_unit.AIEnabled) return;
            if (_unit.Phases == null) return;

            var phase = _unit.Phases.GetPhase(_unit);
            // Reset per-Engaged-session gates exactly once on phase enter.
            // Time-based reset (`SecondsInPhase < 0.05f`) re-fired every
            // frame inside the 50ms window and produced ~40 lines of
            // "disengage boost armed" log spam per Engaged entry.
            if (phase != _lastSeenPhase)
            {
                if (phase == H2HPhase.Engaged)
                    _exchangeStatsEvaluatedThisEngaged = false;
                _lastSeenPhase = phase;
            }
            switch (phase)
            {
                case H2HPhase.NotEngaged:  TickNotEngaged();  break;
                case H2HPhase.Spotting:    TickSpotting();    break;
                case H2HPhase.Approaching: TickApproaching(); break;
                case H2HPhase.Engaged:     TickEngaged();     break;
                case H2HPhase.Exchange:    /* orchestrator owns us */ break;
                case H2HPhase.Separating:  TickSeparating();  break;
            }
        }

        // ── Phase tickers ───────────────────────────────────────────

        private void TickNotEngaged()
        {
            // No movement intent in this phase.
            StopMoving();

            // Continually scan for hostiles.
            _currentTarget = FindClosestHostile();
            if (_currentTarget == null) return;

            float spotRange = ResolveSpottingRange();
            float dist = Vector3.Distance(_unit.transform.position, _currentTarget.transform.position);
            if (dist <= spotRange)
                _unit.Phases.TransitionPhase(_unit, H2HPhase.Spotting, "hostile-detected");
        }

        private void TickSpotting()
        {
            // Stand still, just look at the target (smoothly via the controller).
            StopMoving();
            if (_currentTarget == null) _currentTarget = FindClosestHostile();
            if (_currentTarget != null)
                _unit.Movement?.FaceTowards(_currentTarget.transform.position);

            // Spotting timer is managed by BattleH2HPhaseSystem.
            float expiresAt = _unit.Phases.SpottingExpiresAt(_unit);
            if (Time.time >= expiresAt)
            {
                if (_currentTarget == null)
                    _unit.Phases.TransitionPhase(_unit, H2HPhase.NotEngaged, "spotting-no-target");
                else
                    _unit.Phases.TransitionPhase(_unit, H2HPhase.Approaching, "spotting-expired");
            }
        }

        private void TickApproaching()
        {
            if (_currentTarget == null || Time.time >= _approachRefreshAt)
            {
                _currentTarget = FindClosestHostile();
                _approachRefreshAt = Time.time + _approachTargetRefresh;
            }
            if (_currentTarget == null)
            {
                StopMoving();
                _unit.Phases.TransitionPhase(_unit, H2HPhase.NotEngaged, "approach-target-lost");
                return;
            }

            _unit.Movement?.FaceTowards(_currentTarget.transform.position);
            float dist = Vector3.Distance(_unit.transform.position, _currentTarget.transform.position);
            float engagementRange = ResolveEngagementRange();

            if (dist <= engagementRange)
            {
                _unit.Phases.TransitionPhase(_unit, H2HPhase.Engaged, "in-engagement-range");
                return;
            }

            // Stance-aware approach. Aggressive closes hard at traversal
            // speed; Defensive drifts in slowly + laterally — visibly
            // "not committing"; Balanced closes at traversal but slows
            // when within ~Mid-range.
            //
            // Each branch composes a single intent vector and calls
            // SetMoveIntent once — the controller smoothly accelerates
            // toward the composed direction.
            BehaviorType bias = _unit.Stance != null ? _unit.Stance.behaviorBias : BehaviorType.Balanced;
            float traversal = ResolveTraversalSpeed();
            float combat    = ResolveCombatMovementSpeed();

            Vector3 fwd = _unit.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.001f) fwd.Normalize(); else fwd = Vector3.zero;

            Vector3 intent;
            float intentSpeed;

            // Compute the camera-relative right vector for weave / lateral
            // composition. Used by all three branches.
            Vector3 right = _unit.transform.right; right.y = 0f;
            if (right.sqrMagnitude > 0.001f) right.Normalize();

            switch (bias)
            {
                case BehaviorType.Defensive:
                {
                    // Forward creep + sinusoidal lateral weave. Stronger
                    // amplitude than Aggressive/Balanced so the unit visibly
                    // refuses to commit head-on.
                    float weave = Mathf.Sin(Time.time * _approachWeaveFrequency * Mathf.PI * 2f) * 0.6f;
                    Vector3 fwdComponent = fwd * (combat * 0.6f);
                    Vector3 latComponent = right * weave * 0.4f;
                    intent = fwdComponent + latComponent;
                    intentSpeed = intent.magnitude;
                    break;
                }
                case BehaviorType.Aggressive:
                {
                    // Full traversal forward + small zigzag for boxer-feel.
                    // Amplitude is ~30% of forward — visible weave but the
                    // unit still hammers toward the target.
                    float weave = Mathf.Sin(Time.time * _approachWeaveFrequency * Mathf.PI * 2f) * _approachWeaveAggressive;
                    intent = fwd + right * weave;
                    intentSpeed = traversal;
                    break;
                }
                default: // Balanced
                {
                    // Eased speed (full traversal far, slowing in mid-range)
                    // + moderate sinusoidal weave.
                    float speed = dist > 5f
                        ? traversal
                        : Mathf.Lerp(combat, traversal, Mathf.InverseLerp(engagementRange, 5f, dist));
                    float weave = Mathf.Sin(Time.time * _approachWeaveFrequency * Mathf.PI * 2f) * _approachWeaveBalanced;
                    intent = fwd + right * weave;
                    intentSpeed = speed;
                    break;
                }
            }

            _unit.Movement?.SetMoveIntent(intent, intentSpeed);
        }

        private void TickEngaged()
        {
            if (_currentTarget == null) _currentTarget = FindClosestHostile();
            if (_currentTarget == null)
            {
                _unit.Phases.TransitionPhase(_unit, H2HPhase.NotEngaged, "engage-target-lost");
                return;
            }

            _unit.Movement?.FaceTowards(_currentTarget.transform.position);
            float dist = Vector3.Distance(_unit.transform.position, _currentTarget.transform.position);
            float engagementRange = ResolveEngagementRange();
            float strikeRange = ResolveStrikeRange();

            // Drift detection: only bump back to Approaching after a
            // settle window AND if we're meaningfully outside engagement.
            // Without the settle window, every post-Separating Engaged
            // transition flipped straight back to Approaching because the
            // unit is still backed away by ~5m at the moment Separating
            // resolved. Inside the band we walk forward at combat speed
            // so commits can land instead of looping aborted.
            float settleWindow = 0.4f;
            float driftBuffer = engagementRange * 2f; // ~4m default — well past separation
            bool settled = _unit.Phases.SecondsInPhase(_unit) >= settleWindow;
            if (settled && dist > driftBuffer)
            {
                StopMoving();
                _unit.Phases.TransitionPhase(_unit, H2HPhase.Approaching, "drift-out-of-engagement");
                return;
            }

            if (dist > strikeRange + 0.5f)
            {
                // Outside strike range — close in at combat-walk speed.
                DriveForward(ResolveCombatMovementSpeed());
            }
            else
            {
                // In strike range — sidestep / circle to keep the camera
                // and animation reading as combat-stance footwork.
                DriveCircling();
            }

            // ── Heavy-exchange disengage trigger ────────────────────
            // First tick after entering Engaged from Exchange: check
            // whether the just-finished exchange beat the unit up. If so,
            // arm the disengage boost so the next disengage roll is far
            // more likely to fire. Gate is reset on phase enter (above)
            // so this fires exactly once per Engaged session.
            //
            // After arming, we ConsumeLastExchangeStats() on the unit so
            // the values won't trigger again on a subsequent Engaged entry
            // (e.g., after Separating bounces back). Without this, the
            // brain would re-arm the boost on every Engaged re-entry from
            // the same old stats and disengage forever in a loop.
            if (!_exchangeStatsEvaluatedThisEngaged
                && _unit.HitsAbsorbedLastExchange > 0)
            {
                bool heavyHits   = _unit.HitsAbsorbedLastExchange   >= _heavyComboHitsThreshold;
                bool heavyDamage = _unit.HpFractionLostLastExchange >= _heavyDamageFractionThreshold;
                if (heavyHits || heavyDamage)
                {
                    _disengageBoostUntil = Time.time + _disengageBoostDuration;
                    LogBrain($"disengage boost armed (hits={_unit.HitsAbsorbedLastExchange}, hpLost={_unit.HpFractionLostLastExchange:P0})");
                }
                _unit.ConsumeLastExchangeStats();
                _exchangeStatsEvaluatedThisEngaged = true;
            }

            // Decision lag gate.
            if (!_unit.Phases.CanDecide(_unit)) return;

            // Resource gates: only Sentinel/Stalwart-style stances hoard
            // speed below speedReserveFloor and refuse to commit. Energy
            // and per-combo speed gating is handled in `PickCombo` —
            // letting that be the single source of truth means BasicJab
            // (zero-cost) can still fire when bigger combos can't, which
            // matches the "always-something-to-throw" design.
            float reserveFloor = _unit.Stance != null ? _unit.Stance.speedReserveFloor : 0f;
            bool speedHoarded = _unit.CurrentSpeed < reserveFloor;

            float commitChance = speedHoarded ? 0f : ResolveCommitChance();

            // Honor the orchestrator's SeparationEnabled toggle. Without
            // this gate the brain still rolls DISENGAGE every decision
            // tick — the orchestrator's flag only blocked the
            // post-exchange separation roll, not the in-Engagement
            // disengage pick. Both paths are now controlled by the same
            // switch.
            bool separationAllowed = _unit.Orchestrator == null || _unit.Orchestrator.SeparationEnabled;
            float disengageChance = separationAllowed ? ResolveDisengageChance() : 0f;
            // Low HP biases toward disengage to rebuild — but only if
            // separation is allowed at all.
            if (separationAllowed && _unit.HpFraction < 0.4f) disengageChance += 0.2f;

            // Conditional disengage boost from a recent heavy exchange.
            // Decays linearly over `_disengageBoostDuration`.
            if (separationAllowed && Time.time < _disengageBoostUntil)
            {
                float remaining = _disengageBoostUntil - Time.time;
                float scale = Mathf.Clamp01(remaining / Mathf.Max(0.001f, _disengageBoostDuration));
                disengageChance += _disengageBoost * scale;
            }

            // Engagement cooldown: suppress ALL disengage rolls (including
            // boost-driven) during the first `_engagementCooldownSeconds`
            // of Engaged. Originally this only gated the random base
            // chance, but boost-fueled disengage firing within 0.5s of
            // entering Engaged produced the "constantly disengaging"
            // pattern — heavy combo → bail → re-engage → bail again.
            // The cooldown forces the unit to hold its ground for a beat
            // even after a beating, giving combat space to resolve.
            if (separationAllowed
                && _unit.Phases.SecondsInPhase(_unit) < _engagementCooldownSeconds)
            {
                disengageChance = 0f;
            }
            float roll = Random.value;
            if (roll < commitChance)
            {
                LogBrain($"COMMIT (roll {roll:F2} < commit {commitChance:F2}, sp={_unit.CurrentSpeed:F0} en={_unit.CurrentEnergy:F0})");
                TryInitiateExchange();
            }
            else if (roll < commitChance + disengageChance)
            {
                LogBrain($"DISENGAGE (roll {roll:F2} in [{commitChance:F2}, {commitChance + disengageChance:F2}])");
                _unit.Phases.TransitionPhase(_unit, H2HPhase.Separating, "engaged-disengage");
            }
            else
            {
                if (speedHoarded)
                    LogBrain($"HOLD (speed-hoarded: sp={_unit.CurrentSpeed:F0}<{reserveFloor:F0} stance reserve)");
                else
                    LogBrain($"HOLD (roll {roll:F2}, sp={_unit.CurrentSpeed:F0} en={_unit.CurrentEnergy:F0})");
                _unit.Phases.NoteDecision(_unit);
            }
        }

        private void TickSeparating()
        {
            // Drive backward. If the locomotion driver decides to overlay a
            // backstep clip, the SuppressFor wrapper holds it through.
            float speed = ResolveDisengageSpeed();
            DriveBackward(speed);

            // Hand control back at expiry, but also if we've moved far enough.
            float now = Time.time;
            float expiresAt = _unit.Phases.SeparationExpiresAt(_unit);
            float separationDist = ResolveSeparationDistance();
            float distToTarget = _currentTarget != null
                ? Vector3.Distance(_unit.transform.position, _currentTarget.transform.position)
                : separationDist;

            if (now >= expiresAt && distToTarget >= separationDist)
            {
                _unit.Phases.TransitionPhase(_unit, H2HPhase.Engaged, "separation-resolved");
            }
            else if (now >= expiresAt && _currentTarget != null && distToTarget < separationDist)
            {
                // Even if not enough distance, expire timer eventually
                // returns us to Engaged so combat doesn't stall.
                if (now - expiresAt > 1.5f)
                    _unit.Phases.TransitionPhase(_unit, H2HPhase.Engaged, "separation-timeout");
            }
        }

        // ── Drivers ─────────────────────────────────────────────────
        // Each driver declares INTENT to the movement controller; the
        // controller smoothly accelerates / decelerates toward that intent
        // every frame. The brain itself never calls `cc.Move` — that's
        // what produced the binary 0/0.6 m/s velocity blips and the
        // locomotion-driver thrashing on every engagement frame.

        private void DriveForward(float speed)
        {
            var m = _unit.Movement;
            if (m == null) return;
            Vector3 fwd = _unit.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) { m.Stop(); return; }
            m.SetMoveIntent(fwd, speed);
        }

        private void DriveBackward(float speed)
        {
            var m = _unit.Movement;
            if (m == null) return;
            Vector3 fwd = _unit.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) { m.Stop(); return; }
            m.SetMoveIntent(-fwd, speed);
        }

        private void DriveCircling()
        {
            var m = _unit.Movement;
            if (m == null) return;
            if (_engagedLateralDrift <= 0f) { m.Stop(); return; }

            // Re-roll relative direction periodically. Each "tick" of the
            // dwell timer the unit picks a new 8-direction vector based
            // on its stance bias — aggressive prefers forward/forward-
            // diagonal, defensive prefers back-diagonal/backward, balanced
            // weighted toward lateral with some diagonals mixed in.
            if (Time.time >= _engagedDirExpiresAt)
            {
                _engagedRelativeDir   = PickEngagedRelativeDir();
                _engagedDirExpiresAt = Time.time + Random.Range(_circleDwellMin, _circleDwellMax);
            }

            Vector3 fwd   = _unit.transform.forward; fwd.y = 0f;
            Vector3 right = _unit.transform.right;   right.y = 0f;
            if (fwd.sqrMagnitude < 0.001f || right.sqrMagnitude < 0.001f) { m.Stop(); return; }
            fwd.Normalize();
            right.Normalize();

            // Compose world-space direction from relative (lateral, forward).
            Vector3 worldDir = right * _engagedRelativeDir.x + fwd * _engagedRelativeDir.y;
            if (worldDir.sqrMagnitude < 0.001f) { m.Stop(); return; }
            m.SetMoveIntent(worldDir, _engagedLateralDrift);
        }

        /// <summary>
        /// Picks one of 8 directions (cardinals + 45° diagonals) relative
        /// to the unit's current facing, weighted by stance bias. Result
        /// is a unit-length-ish vector where x = lateral, y = forward.
        ///
        /// The driver's `ResolveDirection` will see the resulting velocity
        /// and pick the matching combat-stance walk clip:
        ///   (0, 1)        → combat_walk_fwd_loop
        ///   (+0.7, +0.7)  → combat_walk_r45_loop
        ///   (1, 0)        → combat_sidestep_r
        ///   (+0.7, -0.7)  → combat_walk_r135_loop
        ///   (0, -1)       → combat_walk_bwd_loop
        ///   (-0.7, -0.7)  → combat_walk_l135_loop
        ///   (-1, 0)       → combat_sidestep_l
        ///   (-0.7, +0.7)  → combat_walk_l45_loop
        /// </summary>
        private Vector2 PickEngagedRelativeDir()
        {
            const float D = 0.7071f;
            BehaviorType bias = _unit.Stance != null ? _unit.Stance.behaviorBias : BehaviorType.Balanced;
            float roll = Random.value;
            switch (bias)
            {
                case BehaviorType.Aggressive:
                    // 60% forward-ish (10% F, 25% FR, 25% FL), 40% lateral
                    if (roll < 0.10f) return new Vector2(0f, 1f);     // F
                    if (roll < 0.35f) return new Vector2( D,  D);     // FR
                    if (roll < 0.60f) return new Vector2(-D,  D);     // FL
                    if (roll < 0.80f) return new Vector2( 1f, 0f);    // R
                    return                  new Vector2(-1f, 0f);    // L
                case BehaviorType.Defensive:
                    // 50% back-diagonal (BR/BL), 20% pure backward,
                    // 30% lateral — keeps spacing while looking for opening
                    if (roll < 0.20f) return new Vector2(0f, -1f);    // B
                    if (roll < 0.45f) return new Vector2( D, -D);     // BR
                    if (roll < 0.70f) return new Vector2(-D, -D);     // BL
                    if (roll < 0.85f) return new Vector2( 1f, 0f);    // R
                    return                  new Vector2(-1f, 0f);    // L
                default: // Balanced
                    // 50% lateral, 25% forward-diagonal, 25% back-diagonal
                    if (roll < 0.25f)  return new Vector2( 1f, 0f);   // R
                    if (roll < 0.50f)  return new Vector2(-1f, 0f);   // L
                    if (roll < 0.625f) return new Vector2( D,  D);    // FR
                    if (roll < 0.75f)  return new Vector2(-D,  D);    // FL
                    if (roll < 0.875f) return new Vector2( D, -D);    // BR
                    return                   new Vector2(-D, -D);   // BL
            }
        }

        private void StopMoving()
        {
            _unit.Movement?.Stop();
        }

        // ── Exchange initiation ─────────────────────────────────────

        private void TryInitiateExchange()
        {
            if (_unit.Orchestrator == null) return;
            if (_currentTarget == null) return;
            if (_unit.Orchestrator.IsBusy(_unit) || _unit.Orchestrator.IsBusy(_currentTarget)) return;

            // Target must be in Engaged for an exchange to make sense; if
            // they're separating / approaching / spotted we'd otherwise
            // promote a non-ready unit to attacker through initiative.
            if (_unit.Phases != null && _currentTarget.Phases != null
                && _unit.Phases.GetPhase(_currentTarget) != H2HPhase.Engaged)
            {
                LogBrain($"commit aborted — target phase is {_unit.Phases.GetPhase(_currentTarget)} (need Engaged)");
                _unit.Phases.NoteDecision(_unit);
                return;
            }

            float strikeRange = ResolveStrikeRange();
            float dist = Vector3.Distance(_unit.transform.position, _currentTarget.transform.position);
            if (dist > strikeRange + 0.5f)
            {
                // Too far — drop the decision but stamp lag so we don't loop hot.
                LogBrain($"commit aborted — dist {dist:F2}m > strike+0.5 ({strikeRange + 0.5f:F2}m)");
                _unit.Phases.NoteDecision(_unit);
                return;
            }

            // Skip-in pre-strike: when committing from outer strike range,
            // play `combat_skip_fwd` (KB_SkipFwd_1) as a visual prelude.
            // Animancer crossfades into the strike clip after ~0.25s, so
            // only the skip's leading boxer-shuffle frames are visible
            // before the strike windup takes over. Pure decoration —
            // orchestrator's pre-position smoothstep continues to drive
            // physical position underneath.
            if (dist > _skipInDistanceThreshold)
            {
                _unit.PlayLibraryClipOneShot("combat_skip_fwd",
                    fallback: "combat_walk_fwd_fast",
                    lockMovement: false);
            }

            var handle = _unit.Orchestrator.RegisterPair(_unit, _currentTarget);
            if (handle == null)
                LogBrain("commit refused by orchestrator (phase eligibility / busy)");
        }

        // ── Resolution helpers (unit/stance/training-overrides) ─────

        private H2HUnit FindClosestHostile()
        {
            var dir = _unit.Orchestrator?.GetComponent<H2HTrainingDirector>();
            if (dir != null && dir.AllUnits != null)
                return _unit.FindClosestHostile(dir.AllUnits);
            // Fallback: scan scene.
            var all = FindObjectsByType<H2HUnit>();
            return _unit.FindClosestHostile(all);
        }

        private float ResolveSpottingRange()
        {
            return _unit.Definition != null ? _unit.Definition.spottingRangeMeters : 8f;
        }

        private float ResolveEngagementRange()
        {
            return _unit.Definition != null ? _unit.Definition.engagementRangeMeters : 2f;
        }

        private float ResolveStrikeRange()
        {
            return _unit.Definition != null ? _unit.Definition.strikeRangeMeters : 1.5f;
        }

        private float ResolveSeparationDistance()
        {
            float fromDef = _unit.Definition != null ? _unit.Definition.separationDistanceMeters : 1.8f;
            // Clamp downward — the user's feedback was the default 3m felt
            // too far for the visual back-skip animation. The cap keeps
            // separation tight unless explicitly relaxed in inspector.
            return Mathf.Min(fromDef, _separationDistanceCap);
        }

        private float ResolveTraversalSpeed()
        {
            return _unit.Definition != null ? _unit.Definition.traversalSpeed : 6f;
        }

        private float ResolveDisengageSpeed()
        {
            return _unit.Definition != null ? _unit.Definition.disengageSpeed : 3.5f;
        }

        private float ResolveCombatMovementSpeed()
        {
            return _unit.Definition != null ? _unit.Definition.combatMovementSpeed : 1.5f;
        }

        private float ResolveCommitChance()
        {
            BehaviorType bias = _unit.Stance != null ? _unit.Stance.behaviorBias : BehaviorType.Balanced;
            switch (bias)
            {
                case BehaviorType.Aggressive: return _commitChanceAggressive;
                case BehaviorType.Defensive:  return _commitChanceDefensive;
                default:                      return _commitChanceBalanced;
            }
        }

        private float ResolveDisengageChance()
        {
            BehaviorType bias = _unit.Stance != null ? _unit.Stance.behaviorBias : BehaviorType.Balanced;
            switch (bias)
            {
                case BehaviorType.Aggressive: return _disengageChanceAggressive;
                case BehaviorType.Defensive:  return _disengageChanceDefensive;
                default:                      return _disengageChanceBalanced;
            }
        }

        private void LogBrain(string msg)
        {
            if (H2HLogger.Instance != null)
                H2HLogger.Instance.Log(H2HLogger.CAT_BRAIN, _unit.DisplayName, msg);
        }
    }
}
