using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Owns all CharacterController motion for an `H2HUnit`. The brain
    /// declares intent (`SetMoveIntent(direction, maxSpeed)`); this
    /// controller smoothly accelerates/decelerates toward that intent and
    /// applies `cc.Move` once per frame.
    ///
    /// This is the layer the legacy `H2HUnitBrain` was bypassing — the
    /// brain used to call `_unit.CC.Move(step)` every frame on conditional
    /// branches, which produced binary velocity blips at the
    /// `CharacterController` and made the locomotion driver flicker
    /// between idle and walk/strafe loops every frame. Putting the
    /// physics layer here means the brain just describes "I want to go
    /// this way at this speed" and lets the controller handle continuity.
    ///
    /// Public state (`Velocity`, `Speed`, `IsStarting`, `IsStopping`)
    /// drives `KuboldLocomotionDriver`'s clip selection — it reads
    /// from us, not from `cc.velocity`, so values are smoothed at the
    /// source.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class H2HMovementController : MonoBehaviour
    {
        // ── Tunables ──────────────────────────────────────────────────
        [Header("Acceleration (m/s²)")]
        [Tooltip("Ramp rate when target speed is in walk band.")]
        [SerializeField] private float _walkAccel = 8f;
        [Tooltip("Ramp rate when target speed is in run band.")]
        [SerializeField] private float _runAccel = 12f;
        [Tooltip("Brake rate when intent goes to zero.")]
        [SerializeField] private float _decelOnStop = 16f;
        [Tooltip("Brake rate when intent reverses against current velocity (sharper to feel responsive).")]
        [SerializeField] private float _decelOnReverse = 24f;

        [Header("Speed boundary used for accel-curve selection")]
        [SerializeField] private float _walkRunBoundary = 1.8f;

        [Header("Safety")]
        [SerializeField] private float _maxSpeedHardCap = 8f;

        [Header("Rotation")]
        [Tooltip("Higher = snappier turn. 0 = no smoothing, instant face.")]
        [SerializeField] private float _rotationLerp = 10f;

        [Header("Physics")]
        [SerializeField] private float _gravity = -15f;

        // ── Runtime ──────────────────────────────────────────────────
        private CharacterController _cc;

        private Vector3 _velocity;          // current world-space horizontal velocity
        private Vector3 _intentDir;         // unit vector or zero
        private float   _intentSpeed;       // 0 = stop
        private float   _verticalVelocity;

        private bool    _hasFacingTarget;
        private Vector3 _facingTarget;
        private bool    _facingLocked;       // suspends rotation lerp (e.g. during exchange)

        private float   _suppressUntil;      // motion paused for a one-shot

        // ── Public state (read by the locomotion driver) ─────────────
        // Velocity fallback: if our internal smoothed velocity is near
        // zero AND we have no active intent, defer to `CharacterController.velocity`
        // so external movers (TrainingPlayerController, knockback, root-
        // motion overlays) are visible to the locomotion driver. Without
        // this, anything that calls `cc.Move` directly while the brain
        // is idle ends up sliding through an idle clip.
        public Vector3 Velocity
        {
            get
            {
                if (_velocity.sqrMagnitude > 0.0025f || _intentSpeed > 0.05f)
                    return _velocity;
                if (_cc != null)
                {
                    var v = _cc.velocity; v.y = 0f;
                    return v;
                }
                return Vector3.zero;
            }
        }
        public float   Speed
        {
            get
            {
                var v = Velocity;
                return new Vector3(v.x, 0f, v.z).magnitude;
            }
        }
        public Vector3 IntentDir       => _intentDir;
        public float   IntentSpeed     => _intentSpeed;
        public bool    HasIntent       => _intentSpeed > 0.01f;
        public bool    IsStarting      => HasIntent && Speed < 0.10f;
        public bool    IsStopping      => !HasIntent && Speed > 0.10f;
        public bool    IsAccelerating  => HasIntent && Speed + 0.05f < _intentSpeed;
        public bool    IsDecelerating  => Speed > _intentSpeed + 0.05f;
        public bool    IsSuppressed    => Time.time < _suppressUntil;

        /// <summary>
        /// Signed angle (degrees) between the unit's current forward and
        /// the direction toward `_facingTarget`. Positive = the unit needs
        /// to rotate **right** (clockwise from above); negative = left.
        /// Returns 0 when there's no facing target. The locomotion driver
        /// uses this to pick turn-in-place clips (`*_turn_l90/r90/l180/r180`)
        /// when the unit is standing still but rotating.
        /// </summary>
        public float PendingTurnAngleDegrees
        {
            get
            {
                if (!_hasFacingTarget) return 0f;
                Vector3 dir = _facingTarget - transform.position; dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) return 0f;
                Vector3 fwd = transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f) return 0f;
                return Vector3.SignedAngle(fwd.normalized, dir.normalized, Vector3.up);
            }
        }

        // ── Lifecycle ────────────────────────────────────────────────
        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        // ── Public API ────────────────────────────────────────────────
        /// <summary>
        /// Tell the controller to accelerate toward `worldDir * maxSpeed`.
        /// `worldDir` is a non-normalized world-space vector (Y is dropped);
        /// pass `Vector3.zero` to stop, or use <see cref="Stop"/>. Magnitude
        /// of `worldDir` does not matter — only its direction is used.
        /// </summary>
        public void SetMoveIntent(Vector3 worldDir, float maxSpeed)
        {
            worldDir.y = 0f;
            float mag = worldDir.magnitude;
            if (mag < 0.0001f || maxSpeed < 0.001f)
            {
                Stop();
                return;
            }
            _intentDir = worldDir / mag;
            _intentSpeed = Mathf.Clamp(maxSpeed, 0f, _maxSpeedHardCap);
        }

        /// <summary>Decelerates the unit to a stop (intent → 0).</summary>
        public void Stop()
        {
            _intentDir = Vector3.zero;
            _intentSpeed = 0f;
        }

        /// <summary>Smoothly rotate to face `worldPos` (Y is dropped). Set every
        /// frame the brain wants the unit aimed at the target — re-calling is cheap.</summary>
        public void FaceTowards(Vector3 worldPos)
        {
            _facingTarget = worldPos;
            _hasFacingTarget = true;
        }

        /// <summary>Snap-rotate to face `worldPos` immediately. Used at scene
        /// setup or when re-orienting after a 180° spin attack.</summary>
        public void SnapFace(Vector3 worldPos)
        {
            Vector3 dir = worldPos - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        public void ClearFacing()           => _hasFacingTarget = false;
        public void LockFacing()            => _facingLocked = true;
        public void UnlockFacing()          => _facingLocked = false;
        public bool IsFacingLocked          => _facingLocked;

        /// <summary>
        /// Set by the locomotion driver when a turn-in-place animation
        /// clip is playing with root motion enabled. While true, this
        /// controller suspends its rotation Slerp so the clip's baked
        /// rotation owns the transform — otherwise the clip's root motion
        /// AND the Slerp would both rotate the unit, doubling the result.
        /// </summary>
        public void SetTurnInPlaceActive(bool active) => _turnInPlaceActive = active;
        public bool IsTurnInPlaceActive                => _turnInPlaceActive;
        private bool _turnInPlaceActive;

        /// <summary>Pause physics for `seconds` so a strike clip's root motion
        /// (or a scripted lunge) can take over. Only the controller's own
        /// `cc.Move` is paused — current velocity is preserved and resumes
        /// when the suppression expires.</summary>
        public void SuppressFor(float seconds)
        {
            float until = Time.time + Mathf.Max(0f, seconds);
            if (until > _suppressUntil) _suppressUntil = until;
        }

        public void ClearSuppression() => _suppressUntil = 0f;

        /// <summary>Hard-set position (use only at scene setup / reset).</summary>
        public void Teleport(Vector3 worldPos)
        {
            if (_cc != null) _cc.enabled = false;
            transform.position = worldPos;
            _velocity = Vector3.zero;
            _verticalVelocity = 0f;
            if (_cc != null) _cc.enabled = true;
        }

        // ── Update ────────────────────────────────────────────────────
        private void Update()
        {
            if (_cc == null || !_cc.enabled) return;
            if (IsSuppressed) { TickRotation(); return; }

            // Apply gravity.
            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += _gravity * Time.deltaTime;

            // Compute desired horizontal velocity from intent.
            Vector3 targetV = _intentDir * _intentSpeed;

            // Pick the right accel rate for this transition.
            float rate = ResolveAccelRate(targetV);

            // Lerp velocity toward target by `rate * dt` (capped at the gap).
            Vector3 delta = targetV - _velocity;
            float deltaMag = delta.magnitude;
            if (deltaMag > 0.001f)
            {
                float step = rate * Time.deltaTime;
                _velocity = step >= deltaMag
                    ? targetV
                    : _velocity + (delta / deltaMag) * step;
            }

            // Hard speed cap (defensive — tunables shouldn't allow exceeding).
            float hSpeed = new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
            if (hSpeed > _maxSpeedHardCap)
                _velocity = (_velocity / hSpeed) * _maxSpeedHardCap;

            // Apply the move (gravity folded in so a single CC.Move handles both).
            Vector3 step3 = new Vector3(_velocity.x, _verticalVelocity, _velocity.z) * Time.deltaTime;
            _cc.Move(step3);

            TickRotation();
        }

        private void TickRotation()
        {
            // Yield to root motion while a turn-in-place clip drives the
            // transform — otherwise the clip's baked rotation AND this
            // Slerp both fire toward the same target and the unit rotates
            // ~360° instead of 180° (the "doing a 360" gameplay bug).
            if (_turnInPlaceActive)        return;
            if (!_hasFacingTarget || _facingLocked) return;
            Vector3 dir = _facingTarget - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = _rotationLerp <= 0f
                ? target
                : Quaternion.Slerp(transform.rotation, target, _rotationLerp * Time.deltaTime);
        }

        private float ResolveAccelRate(Vector3 targetV)
        {
            // Target is zero → braking.
            if (targetV.sqrMagnitude < 0.0001f)
            {
                return _velocity.sqrMagnitude < 0.0001f ? 0f : _decelOnStop;
            }

            // Intent reverses against current velocity → harder brake.
            // (Use a small negative threshold so near-orthogonal turns
            // don't engage the reverse-decel rate; that should feel like
            // an accel into the new direction, not a stop.)
            if (_velocity.sqrMagnitude > 0.04f && Vector3.Dot(targetV, _velocity) < -0.2f)
                return _decelOnReverse;

            // Pure acceleration: pick walk vs run rate based on target speed.
            return targetV.magnitude < _walkRunBoundary ? _walkAccel : _runAccel;
        }
    }
}
