using Animancer;
using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Drives locomotion playback off the unit's `CharacterController.velocity`.
    /// Picks the correct Kubold clip for the current `(engaged-state × speed-band ×
    /// movement-direction)` tuple every frame, using ids registered by
    /// `KuboldClipLibrarySetup`.
    ///
    /// Phase mapping:
    /// - `Engaged` / `Exchange` / `Separating`  → combat-stance loops (`combat_*` ids).
    /// - `NotEngaged` / `Spotting` / `Approaching` → standing loops (`loco_*` ids).
    ///
    /// One-shots (strikes, hit-reacts) are played by other code; they call
    /// `SuppressFor` so this driver yields. See
    /// `Docs/Design/LOCOMOTION_CHEATSHEET.md` for the full clip matrix and
    /// `Docs/07_PRESENTATION.md` §"Locomotion driver" for the contract.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AnimancerComponent))]
    public class KuboldLocomotionDriver : MonoBehaviour
    {
        [SerializeField] private BattleAnimancerClipLibrary _library;

        // ── Speed bands (m/s) ────────────────────────────────────────
        [Header("Speed bands (m/s)")]
        [Tooltip("Below this horizontal speed, play idle.")]
        [SerializeField] private float _idleSpeedThreshold = 0.30f;

        [Tooltip("Sub-band inside combat-walk: above this, swap to the 'fast' combat walk variant (KB_WalkFwd2, ~0.88 m/s).")]
        [SerializeField] private float _combatWalkFastThreshold = 0.75f;

        [Tooltip("Walk → run band boundary.")]
        [SerializeField] private float _walkRunBoundary = 1.80f;

        [Tooltip("Run → sprint band boundary.")]
        [SerializeField] private float _runSprintBoundary = 4.50f;

        // ── Direction thresholds (degrees) ───────────────────────────
        [Header("Direction (degrees from facing)")]
        [Tooltip("Within ±this angle of forward = pure forward.")]
        [SerializeField] private float _forwardCone = 30f;

        [Tooltip("Beyond ±(180-this) of forward = pure backward.")]
        [SerializeField] private float _backwardCone = 30f;

        [Tooltip("Diagonal cones extend this many degrees on each side of the cardinal axes.")]
        [SerializeField] private float _diagonalHalfWidth = 30f;

        // ── Turn-in-place ────────────────────────────────────────────
        [Header("Turn-in-place")]
        [Tooltip("When idle and pending turn angle exceeds this many degrees, play a turn-in-place clip instead of idle.")]
        [SerializeField] private float _turnInPlaceThreshold = 30f;
        [Tooltip("Pending turn angles in [threshold, 90+thisSlop] use the *_turn_*90 clip; anything bigger uses the *_turn_*180 clip.")]
        [SerializeField] private float _turn180Threshold = 120f;

        // ── Start / Stop transitions ─────────────────────────────────
        [Header("Start/Stop foot-plant clips (standing locomotion only)")]
        [Tooltip("Use the *_start clip when transitioning from idle to walk/run, and *_stop_lu/ru when decelerating to idle.")]
        [SerializeField] private bool  _useStartStopClips = true;
        [Tooltip("Hold a transition clip for this many seconds before letting the resolver switch (matches typical Kubold start/stop length 0.7-1.5s).")]
        [SerializeField] private float _transitionHoldSeconds = 0.6f;

        // ── Smoothing ────────────────────────────────────────────────
        [Header("Smoothing")]
        [Tooltip("Seconds to ease between H2H phase max speeds.")]
        [SerializeField] private float _phaseSpeedSmoothTime = 0.20f;

        [Tooltip("Minimum seconds between locomotion clip switches. Prevents direction-cone and band-boundary flicker.")]
        [SerializeField] private float _minDwellTime = 0.12f;

        // ── Debug ─────────────────────────────────────────────────────
        [Header("Debug")]
        [SerializeField] private bool _debugLog;
        [Tooltip("Throttle debug logs to this interval (seconds).")]
        [SerializeField] private float _debugLogInterval = 0.5f;

        // ── Runtime ──────────────────────────────────────────────────
        private CharacterController     _cc;
        private AnimancerComponent      _animancer;
        private H2HUnit                 _h2h;        // optional — null in legacy scenes
        private H2HMovementController   _movement;   // optional — preferred velocity source when present

        private string _currentLocomotionId = "";
        private float  _suppressUntil;
        private float  _lastSwitchAt;                // for dwell-time hysteresis

        // Transition state machine: tracks whether we're currently playing
        // a Start (idle → moving) or Stop (moving → idle) clip and refuses
        // to override it until `_transitionHoldSeconds` elapses. Without
        // this hold, the dwell-time alone (120ms) lets the resolver swap
        // out of the start clip before its foot-plant completes.
        private Band      _prevBand     = Band.Idle;
        private Direction _prevDirection = Direction.Forward;
        private float     _transitionHoldUntil;
        private bool      _stopFootRight; // alternates each stop clip (LU vs RU)

        private float _smoothedPhaseMaxSpeed = -1f;
        private float _phaseMaxSpeedVelocity;
        private float _debugTimer;

        public string CurrentLocomotionId => _currentLocomotionId;
        public bool   IsSuppressed       => Time.time < _suppressUntil;
        public BattleAnimancerClipLibrary Library => _library;

        /// <summary>
        /// Runtime hook for `H2HUnit.Awake` to wire the library when it
        /// added this driver as a safety net (scene authored without it).
        /// No-op if the library is already wired in the inspector.
        /// </summary>
        public void EnsureLibrary(BattleAnimancerClipLibrary library)
        {
            if (_library == null && library != null) _library = library;
        }

        // ── Speed bands & directions ─────────────────────────────────
        private enum Band      { Idle, Walk, Run, Sprint }
        private enum Direction { Forward, Backward, Left, Right, ForwardLeft, ForwardRight, BackLeft, BackRight }

        private void Awake()
        {
            _cc        = GetComponent<CharacterController>();
            _animancer = GetComponent<AnimancerComponent>();
            _h2h       = GetComponent<H2HUnit>();
            _movement  = GetComponent<H2HMovementController>();
        }

        public void SuppressFor(float seconds)
        {
            float until = Time.time + Mathf.Max(0f, seconds);
            if (until > _suppressUntil) _suppressUntil = until;
            _currentLocomotionId = "";
        }

        public void ClearSuppression()
        {
            _suppressUntil = 0f;
            _currentLocomotionId = "";
        }

        /// <summary>Effective max horizontal speed for the current H2H phase,
        /// smoothed to avoid the instant step-change when phases flip.
        /// Returns -1 if no phase clamp applies.</summary>
        public float ResolvePhaseMaxSpeed()
        {
            float target = ResolveRawPhaseMaxSpeed();
            if (target < 0f)
            {
                _smoothedPhaseMaxSpeed = -1f;
                _phaseMaxSpeedVelocity = 0f;
                return -1f;
            }
            if (_smoothedPhaseMaxSpeed < 0f)
            {
                _smoothedPhaseMaxSpeed = target;
                _phaseMaxSpeedVelocity = 0f;
                return target;
            }
            _smoothedPhaseMaxSpeed = Mathf.SmoothDamp(_smoothedPhaseMaxSpeed, target,
                ref _phaseMaxSpeedVelocity, _phaseSpeedSmoothTime);
            return _smoothedPhaseMaxSpeed;
        }

        private float ResolveRawPhaseMaxSpeed()
        {
            if (_h2h == null || _h2h.Phases == null || _h2h.Definition == null) return -1f;
            switch (_h2h.Phases.GetPhase(_h2h))
            {
                case H2HPhase.Engaged:     return _h2h.Definition.combatMovementSpeed;
                case H2HPhase.Approaching: return _h2h.Definition.traversalSpeed;
                case H2HPhase.Separating:  return _h2h.Definition.disengageSpeed;
                default:                   return -1f;
            }
        }

        private void Update()
        {
            if (IsSuppressed) return;
            if (_animancer == null || _library == null || _cc == null) return;

            H2HPhase phase = _h2h?.Phases != null ? _h2h.Phases.GetPhase(_h2h) : H2HPhase.NotEngaged;
            bool engaged   = IsCombatStance(phase);

            // Prefer the MovementController's smoothed velocity over the raw
            // CharacterController velocity. The CC reports velocity from the
            // most recent Move() call only, which produces 0/peak blips when
            // the brain skips frames. The controller integrates motion
            // continuously — much more stable.
            Vector3 vel;
            float   speed;
            if (_movement != null)
            {
                vel = _movement.Velocity; vel.y = 0f;
                speed = _movement.Speed;
            }
            else
            {
                vel = _cc.velocity; vel.y = 0f;
                speed = vel.magnitude;
            }

            Direction direction = ResolveDirection(vel, speed);
            Band      band      = ResolveBand(speed);

            // Start/Stop transitions: detect band edges and possibly play a
            // foot-plant transition clip, holding it for _transitionHoldSeconds.
            // Combat-stance has no _start/_stop clips in the Kubold pack, so
            // this only fires when standing locomotion is active.
            string transitionDesired = MaybePickStartStop(engaged, band, direction);

            string desired = transitionDesired ?? ResolveClipId(phase, engaged, band, direction, speed);

            if (_debugLog)
            {
                _debugTimer += Time.deltaTime;
                if (_debugTimer >= _debugLogInterval)
                {
                    _debugTimer = 0f;
                    Debug.Log($"[KuboldLoco] phase={phase} engaged={engaged} hSpeed={speed:F2} band={band} dir={direction} → '{desired}' (cur='{_currentLocomotionId}')");
                }
            }

            if (desired == _currentLocomotionId || string.IsNullOrEmpty(desired)) return;

            // Dwell-time hysteresis: refuse to switch within `_minDwellTime`
            // of the previous switch. Kills cone-boundary jitter (Forward ↔
            // ForwardRight ↔ Right when velocity wiggles near 22.5°) and
            // band-boundary jitter (Walk ↔ Idle near `_idleSpeedThreshold`).
            // The first switch after a long dwell always goes through.
            if (Time.time - _lastSwitchAt < _minDwellTime) return;

            string playedId = null;

            if (TryPlay(desired)) { playedId = desired; }
            else
            {
                // Walk the fallback chain until a clip resolves or we run out.
                const int MaxFallbackDepth = 6;
                string current = desired;
                for (int i = 0; i < MaxFallbackDepth; i++)
                {
                    string fb = FallbackFor(current);
                    if (string.IsNullOrEmpty(fb)) break;
                    if (TryPlay(fb, recordAs: desired)) { playedId = fb; break; }
                    current = fb;
                }
            }

            if (playedId != null) _lastSwitchAt = Time.time;

            // ── Root-motion toggle for turn-in-place clips ──────────
            // Turn clips have ~180° / ~90° baked into root rotation. Without
            // applyRootMotion=true the bones rotate in pose space but the
            // transform stays put, then the next clip's crossfade un-rotates
            // the bones — net effect: a confused 360° spin. Enable root
            // motion only while a pure turn-in-place clip is playing AND
            // tell the controller to pause its facing Slerp so the two
            // don't double-rotate.
            ApplyTurnInPlaceRootMotion(playedId ?? desired);

            // Pipe the decision into the H2HLogger if it exists, so the
            // locomotion stream lands in the same dump as phase/brain/impact.
            if (H2HLogger.Instance != null && _h2h != null)
            {
                H2HLogger.Instance.LogLocomotion(_h2h, phase, speed, direction.ToString(), desired, playedId);
            }

            if (playedId == null && _debugLog)
            {
                Debug.LogWarning($"[KuboldLoco] '{desired}' and all fallbacks missing from library — staying on '{_currentLocomotionId}'.");
            }

            // Track band/direction for the next frame's start/stop edge detection.
            _prevBand      = band;
            _prevDirection = direction;
        }

        // ── Start / Stop transition logic ────────────────────────────
        /// <summary>
        /// If a Start/Stop foot-plant clip should play right now, returns its id;
        /// otherwise returns null and the normal resolver runs. Combat-stance
        /// returns null (the Kubold pack ships start/stop clips only for the
        /// standing locomotion family).
        /// </summary>
        private string MaybePickStartStop(bool engaged, Band band, Direction dir)
        {
            if (!_useStartStopClips) return null;
            if (engaged)             return null;

            // Hold an active transition clip until its window expires —
            // returning the current id makes the early-return path take
            // over so we don't churn into the loop mid foot-plant.
            if (Time.time < _transitionHoldUntil)
                return _currentLocomotionId;

            bool wasIdle = _prevBand == Band.Idle;
            bool isIdle  = band == Band.Idle;

            if (wasIdle && !isIdle)
            {
                // Pivot-start: when idle and the controller's intent
                // direction differs from current facing by ≥ 60°, the unit
                // should kick off with the matching pivot-start clip
                // (`loco_walk_fwd_start_l90` / `_r90` / `_l135` / `_r135` /
                // `_l180` / `_r180`) before falling into the loop. Only
                // applies to forward intent — strafe/back intents go
                // straight into their respective start clips.
                string pivotId = TryPickPivotStart(band, dir);
                string id = pivotId ?? ResolveStartClip(band, dir);
                if (id != null)
                {
                    _transitionHoldUntil = Time.time + _transitionHoldSeconds;
                    return id;
                }
            }
            else if (!wasIdle && isIdle)
            {
                string id = ResolveStopClip(_prevBand, _prevDirection);
                if (id != null)
                {
                    _transitionHoldUntil = Time.time + _transitionHoldSeconds;
                    return id;
                }
            }
            return null;
        }

        /// <summary>
        /// At the idle→moving edge, when the unit's intent direction is
        /// offset from current facing by a significant angle, swap the
        /// generic `_start` clip for the matching pivot-start variant.
        /// Returns null when no pivot is needed (small angle) or the band
        /// has no pivot start clips (only walk/run-forward have them).
        /// </summary>
        private string TryPickPivotStart(Band band, Direction dir)
        {
            if (_movement == null) return null;
            if (dir != Direction.Forward) return null;
            if (band != Band.Walk && band != Band.Run) return null;

            // Use the angle between current facing and IntentDir (where the
            // unit is actually trying to go), not the facing target — the
            // brain may be pointing at a target while drifting laterally.
            Vector3 intentDir = _movement.IntentDir; intentDir.y = 0f;
            if (intentDir.sqrMagnitude < 0.0001f) return null;
            Vector3 fwd = transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) return null;
            float angle = Vector3.SignedAngle(fwd.normalized, intentDir.normalized, Vector3.up);
            float a = Mathf.Abs(angle);

            // Three pivot bands: ~90° / ~135° / ~180°. Below ~60°, the
            // straight-start clip looks fine because the controller can
            // smoothly rotate during the start.
            if (a < 60f) return null;

            string side = angle > 0f ? "r" : "l";
            string angleSuffix = a >= 160f ? "180"
                                : a >= 110f ? "135"
                                :             "90";

            string prefix = band == Band.Run ? "loco_run_fwd_start_" : "loco_walk_fwd_start_";
            return prefix + side + angleSuffix;
        }

        private static string ResolveStartClip(Band band, Direction dir)
        {
            // Library coverage: forward (walk + run), backward (walk only),
            // strafe L/R (walk only). Diagonals have no start clip — they
            // fall through to the loop. Run-band sideways/backwards also
            // falls through (no run-strafe-start in the pack).
            if (band == Band.Run && dir == Direction.Forward) return "loco_run_fwd_start";
            switch (dir)
            {
                case Direction.Forward:  return "loco_walk_fwd_start";
                case Direction.Backward: return "loco_walk_bwd_start";
                case Direction.Left:     return "loco_strafe_l_start";
                case Direction.Right:    return "loco_strafe_r_start";
            }
            return null;
        }

        private string ResolveStopClip(Band prevBand, Direction prevDir)
        {
            // Alternate the foot-plant variant each stop so two stops in a
            // row don't replay the exact same animation.
            _stopFootRight = !_stopFootRight;
            string footSuffix = _stopFootRight ? "stop_r" : "stop_l";

            string prefix;
            if (prevBand == Band.Run && prevDir == Direction.Forward)
                prefix = "loco_run_fwd_";
            else
            {
                switch (prevDir)
                {
                    case Direction.Forward:  prefix = "loco_walk_fwd_";  break;
                    case Direction.Backward: prefix = "loco_walk_bwd_";  break;
                    case Direction.Left:     prefix = "loco_strafe_l_";  break;
                    case Direction.Right:    prefix = "loco_strafe_r_";  break;
                    default: return null;
                }
            }
            return prefix + footSuffix;
        }

        private static bool IsPureTurnInPlaceClip(string id)
        {
            // Pure turn-in-place clips have ~0 forward / lateral / vertical
            // motion and 90° / 180° baked rotation. Pivot-starts and
            // mid-run-pivots ALSO have rotation but combine it with forward
            // motion — their root motion would translate the unit through
            // colliders during gameplay, so we don't enable it for them
            // here. They render correctly in the showcase via the explicit
            // T() flag instead.
            switch (id)
            {
                case "combat_turn_l90":
                case "combat_turn_r90":
                case "combat_turn_l180":
                case "combat_turn_r180":
                case "loco_turn_l90":
                case "loco_turn_r90":
                case "loco_turn_l180":
                case "loco_turn_r180":
                    return true;
            }
            return false;
        }

        private void ApplyTurnInPlaceRootMotion(string id)
        {
            bool isTurn = !string.IsNullOrEmpty(id) && IsPureTurnInPlaceClip(id);
            var animator = _animancer != null ? _animancer.Animator : null;
            if (animator != null && animator.applyRootMotion != isTurn)
                animator.applyRootMotion = isTurn;
            if (_movement != null && _movement.IsTurnInPlaceActive != isTurn)
                _movement.SetTurnInPlaceActive(isTurn);
        }

        private bool TryPlay(string id, string recordAs = null)
        {
            if (_library.TryGet(id, out var transition) && transition != null)
            {
                _animancer.Play(transition);
                _currentLocomotionId = recordAs ?? id;
                if (_debugLog) Debug.Log($"[KuboldLoco] play '{id}' (recorded='{_currentLocomotionId}')");
                return true;
            }
            return false;
        }

        // ── Resolution ───────────────────────────────────────────────
        private static bool IsCombatStance(H2HPhase phase)
        {
            // Spotting flips the unit into combat stance immediately so the
            // alert-idle the unit plays on phase enter (`combat_idle`) keeps
            // playing rather than the driver swapping back to relaxed idle
            // mid-spotting. Approaching stays standing — that's running
            // toward the enemy, not engaging yet.
            return phase == H2HPhase.Spotting
                || phase == H2HPhase.Engaged
                || phase == H2HPhase.Exchange
                || phase == H2HPhase.Separating;
        }

        private Band ResolveBand(float speed)
        {
            if (speed < _idleSpeedThreshold)  return Band.Idle;
            if (speed < _walkRunBoundary)     return Band.Walk;
            if (speed < _runSprintBoundary)   return Band.Run;
            return Band.Sprint;
        }

        private Direction ResolveDirection(Vector3 worldVel, float speed)
        {
            if (speed < _idleSpeedThreshold) return Direction.Forward;

            Vector3 fwd = transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            else fwd.Normalize();

            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x); // 90° clockwise

            float fwdDot = Vector3.Dot(worldVel, fwd)   / speed;
            float latDot = Vector3.Dot(worldVel, right) / speed;

            // angle in [-180, 180]: 0 = forward, 90 = right, 180 = back, -90 = left
            float angle = Mathf.Atan2(latDot, fwdDot) * Mathf.Rad2Deg;
            float a = Mathf.Abs(angle);

            if (a <= _forwardCone)               return Direction.Forward;
            if (a >= 180f - _backwardCone)       return Direction.Backward;

            // Diagonals straddle the 45°/-45°/135°/-135° axes
            float diag = _diagonalHalfWidth;

            if (Mathf.Abs(angle - 45f)  <= diag) return Direction.ForwardRight;
            if (Mathf.Abs(angle + 45f)  <= diag) return Direction.ForwardLeft;
            if (Mathf.Abs(angle - 135f) <= diag) return Direction.BackRight;
            if (Mathf.Abs(angle + 135f) <= diag) return Direction.BackLeft;

            // Falls into the pure left/right cardinals between the cones
            return angle > 0f ? Direction.Right : Direction.Left;
        }

        /// <summary>
        /// Maps the resolved tuple to a library id. Combat-stance gets the
        /// full 8-direction matrix at walk speed; standing locomotion gets
        /// the more restricted MovementAnimsetPro coverage with diagonals
        /// at run band.
        /// </summary>
        private string ResolveClipId(H2HPhase phase, bool engaged, Band band, Direction dir, float speed)
        {
            // Exchange phase: orchestrator owns playback. Yield to combat-idle
            // so the driver doesn't fight the strike clip when SuppressFor lifts.
            if (phase == H2HPhase.Exchange) return "combat_idle";

            // Idle path — but if the unit is rotating significantly toward a
            // facing target, swap to a turn-in-place clip so the legs step
            // through the rotation instead of the body silently swinging.
            if (band == Band.Idle)
            {
                string turn = ResolveTurnInPlaceId(engaged);
                if (turn != null) return turn;
                return engaged ? "combat_idle" : "idle_relaxed";
            }

            // Combat-stance has NO run / sprint clips — Kubold ships only
            // walk-pace combat loops (KB_WalkFwd1 / 2 ≈ 0.6–0.9 m/s). When
            // velocity exceeds the walk band, fall back to standing-run /
            // sprint regardless of phase. Otherwise the body flies forward
            // at 5 m/s while the legs play a 0.9 m/s walk loop (foot slide).
            // The brain's phase-speed clamp keeps AI under walk speed; this
            // path mostly fires for manual control / WASD testing.
            if (band == Band.Run || band == Band.Sprint)
                return ResolveStandingId(band, dir);

            if (engaged) return ResolveCombatId(band, dir, speed);
            return ResolveStandingId(band, dir);
        }

        private string ResolveTurnInPlaceId(bool engaged)
        {
            if (_movement == null) return null;
            float pending = _movement.PendingTurnAngleDegrees;
            float a = Mathf.Abs(pending);
            if (a < _turnInPlaceThreshold) return null;
            // Big turn (≥ 120°) → 180° clip; else 90° clip.
            string suffix = a >= _turn180Threshold
                ? (pending > 0f ? "180_r" : "180_l")
                : (pending > 0f ? "90_r"  : "90_l");
            string prefix = engaged ? "combat_turn_" : "loco_turn_";
            // Library id naming: combat_turn_l90, combat_turn_r90, combat_turn_l180, combat_turn_r180.
            // Reorder our suffix accordingly: "90_r" → "r90", etc.
            string sideAndAngle = suffix == "90_r"  ? "r90"
                                : suffix == "90_l"  ? "l90"
                                : suffix == "180_r" ? "r180"
                                :                     "l180";
            return prefix + sideAndAngle;
        }

        private string ResolveCombatId(Band band, Direction dir, float speed)
        {
            // Combat units don't run/sprint — clamp to walk band.
            // (In practice the phase max-speed clamp keeps them under
            // walk speed, but if anything pushes them above we still
            // play a walk loop rather than slip into standing-run.)
            switch (dir)
            {
                case Direction.Forward:
                    return speed >= _combatWalkFastThreshold
                        ? "combat_walk_fwd_fast"
                        : "combat_walk_fwd_loop";
                case Direction.Backward:     return "combat_walk_bwd_loop";
                case Direction.Left:         return "combat_sidestep_l";
                case Direction.Right:        return "combat_sidestep_r";
                case Direction.ForwardLeft:  return "combat_walk_l45_loop";
                case Direction.ForwardRight: return "combat_walk_r45_loop";
                case Direction.BackLeft:     return "combat_walk_l135_loop";
                case Direction.BackRight:    return "combat_walk_r135_loop";
            }
            return "combat_idle";
        }

        private string ResolveStandingId(Band band, Direction dir)
        {
            switch (band)
            {
                case Band.Walk: return ResolveStandingWalk(dir);
                case Band.Run:  return ResolveStandingRun(dir);
                case Band.Sprint: return "loco_sprint_loop";
            }
            return "idle_relaxed";
        }

        private string ResolveStandingWalk(Direction dir)
        {
            // Standing walk has full 8-direction coverage now that l135 / r45
            // are wired in the library (StrafeLeft135Loop from the
            // RunStrafeUpdate pack and StrafeRight45Loop from the same pack;
            // l45 / r135 from MovementAnimsetPro_Additionals).
            switch (dir)
            {
                case Direction.Forward:       return "loco_walk_fwd_loop";
                case Direction.Backward:      return "loco_walk_bwd_loop";
                case Direction.Left:          return "loco_strafe_l_loop";
                case Direction.Right:         return "loco_strafe_r_loop";
                case Direction.ForwardLeft:   return "loco_strafe_l45_loop";
                case Direction.ForwardRight:  return "loco_strafe_r45_loop";
                case Direction.BackLeft:      return "loco_strafe_l135_loop";
                case Direction.BackRight:     return "loco_strafe_r135_loop";
            }
            return "loco_walk_fwd_loop";
        }

        private string ResolveStandingRun(Direction dir)
        {
            // Standing run has 6-direction coverage (fwd/bwd/l/r + 45° diagonals).
            // 135° back-diagonals fall back to plain backward run.
            switch (dir)
            {
                case Direction.Forward:       return "loco_run_fwd_loop";
                case Direction.Backward:      return "loco_run_bwd_loop";
                case Direction.Left:          return "loco_run_strafe_l_loop";
                case Direction.Right:         return "loco_run_strafe_r_loop";
                case Direction.ForwardLeft:   return "loco_run_strafe_l45_loop";
                case Direction.ForwardRight:  return "loco_run_strafe_r45_loop";
                case Direction.BackLeft:
                case Direction.BackRight:     return "loco_run_bwd_loop";
            }
            return "loco_run_fwd_loop";
        }

        private string FallbackFor(string desired)
        {
            // Combat ids fall back to standing equivalents
            switch (desired)
            {
                case "combat_idle":           return "idle";
                case "combat_walk_fwd_loop":
                case "combat_walk_fwd_fast":  return "loco_walk_fwd_loop";
                case "combat_walk_bwd_loop":  return "loco_walk_bwd_loop";
                case "combat_sidestep_l":
                case "combat_walk_l45_loop":
                case "combat_walk_l135_loop": return "loco_strafe_l_loop";
                case "combat_sidestep_r":
                case "combat_walk_r45_loop":
                case "combat_walk_r135_loop": return "loco_strafe_r_loop";

                // Standing run-strafe diagonals fall back to plain run
                case "loco_run_strafe_l45_loop":
                case "loco_run_strafe_l_loop":  return "loco_run_fwd_loop";
                case "loco_run_strafe_r45_loop":
                case "loco_run_strafe_r_loop":  return "loco_run_fwd_loop";
                case "loco_run_bwd_loop":       return "loco_run_fwd_loop";

                // Standing walk fallback: relaxed walk → idle
                case "loco_walk_fwd_loop":      return "walk_forward";
                case "loco_walk_bwd_loop":      return "walk_back";
                case "loco_strafe_l_loop":      return "walk_strafe_L";
                case "loco_strafe_r_loop":      return "walk_strafe_R";

                // Standing walk diagonals fall back to the nearer cardinal
                // if the new diagonal clip isn't in a partially-populated
                // library (re-running KuboldClipLibrarySetup picks them up).
                case "loco_strafe_l45_loop":    return "loco_walk_fwd_loop";
                case "loco_strafe_r45_loop":    return "loco_walk_fwd_loop";
                case "loco_strafe_l135_loop":   return "loco_walk_bwd_loop";
                case "loco_strafe_r135_loop":   return "loco_walk_bwd_loop";

                // Sprint fallback to run
                case "loco_sprint_loop":        return "loco_run_fwd_loop";

                case "idle_relaxed":            return "idle";

                // Turn-in-place fallbacks: combat → standing → idle.
                case "combat_turn_l90":         return "loco_turn_l90";
                case "combat_turn_r90":         return "loco_turn_r90";
                case "combat_turn_l180":        return "loco_turn_l180";
                case "combat_turn_r180":        return "loco_turn_r180";
                case "loco_turn_l90":
                case "loco_turn_r90":
                case "loco_turn_l180":
                case "loco_turn_r180":          return "combat_idle";

                // Pivot-start fallbacks: degrade to the generic _start clip,
                // which itself falls back to the loop.
                case "loco_walk_fwd_start_l90":
                case "loco_walk_fwd_start_r90":
                case "loco_walk_fwd_start_l135":
                case "loco_walk_fwd_start_r135":
                case "loco_walk_fwd_start_l180":
                case "loco_walk_fwd_start_r180": return "loco_walk_fwd_start";
                case "loco_run_fwd_start_l90":
                case "loco_run_fwd_start_r90":
                case "loco_run_fwd_start_l135":
                case "loco_run_fwd_start_r135":
                case "loco_run_fwd_start_l180":
                case "loco_run_fwd_start_r180":  return "loco_run_fwd_start";

                // Start/Stop transition fallbacks: degrade to the matching loop
                // (or idle for stops) if a transition clip isn't in the library.
                case "loco_walk_fwd_start":     return "loco_walk_fwd_loop";
                case "loco_walk_bwd_start":     return "loco_walk_bwd_loop";
                case "loco_strafe_l_start":     return "loco_strafe_l_loop";
                case "loco_strafe_r_start":     return "loco_strafe_r_loop";
                case "loco_run_fwd_start":      return "loco_run_fwd_loop";
                case "loco_walk_fwd_stop_l":
                case "loco_walk_fwd_stop_r":    return "idle_relaxed";
                case "loco_walk_bwd_stop_l":
                case "loco_walk_bwd_stop_r":    return "idle_relaxed";
                case "loco_strafe_l_stop_l":
                case "loco_strafe_l_stop_r":    return "idle_relaxed";
                case "loco_strafe_r_stop_l":
                case "loco_strafe_r_stop_r":    return "idle_relaxed";
                case "loco_run_fwd_stop_l":
                case "loco_run_fwd_stop_r":     return "idle_relaxed";
            }
            return null;
        }
    }
}
