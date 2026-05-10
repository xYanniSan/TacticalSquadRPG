using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Owns all CharacterController movement for a battle unit.
    /// TerrainBattleUnit and UnitBrainAI call the public API;
    /// they never touch CharacterController or _verticalVelocity directly.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class UnitMovementController : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private float gravity = -15f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 8f;

        private CharacterController _cc;

        // Gravity state
        private float _verticalVelocity;

        // Lunge (flying kick / attack dash)
        private bool    _lunging;
        private Vector3 _lungeStart;
        private Vector3 _lungeEnd;
        private float   _lungeDuration;
        private float   _lungeTimer;

        // Dodge arc
        private bool    _dodgeMoving;
        private Vector3 _dodgeStart;
        private Vector3 _dodgeEnd;
        private float   _dodgeArcHeight;
        private float   _dodgeDuration;
        private float   _dodgeTimer;

        // Re-engage ramp after knockback/stagger
        private bool  _reengaging;
        private float _reengageSpeed;

        // ── Public state ────────────────────────────────────────────
        public float CurrentMoveSpeed { get; private set; }
        public bool  IsGrounded       => _cc != null && _cc.isGrounded;
        public bool  IsLunging        => _lunging;
        public bool  IsDodgeMoving    => _dodgeMoving;

        // ── Lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        public void Initialize()
        {
            _cc = GetComponent<CharacterController>();
        }

        // ── Gravity ───────────────────────────────────────────────────

        /// <summary>
        /// Call once per frame before any horizontal movement.
        /// Self-managed states (dodge, lunge, orbit) fold vertical velocity inline
        /// and should pass selfManaged=true to skip the standalone gravity move.
        /// </summary>
        public void TickGravity(bool selfManaged)
        {
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            if (!selfManaged)
                _cc.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }

        // ── Direct movement ───────────────────────────────────────────

        /// <summary>
        /// Move toward a world position at the given speed.
        /// Returns true if still moving (distance > stopRadius).
        /// </summary>
        public bool MoveToward(Vector3 targetPos, float speed, float stopRadius = 0.1f)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist <= stopRadius) return false;

            dir /= dist; // normalize without re-computing magnitude
            Vector3 move = dir * speed + Vector3.up * _verticalVelocity;
            _cc.Move(move * Time.deltaTime);
            CurrentMoveSpeed = speed;
            return true;
        }

        /// <summary>
        /// Move in a direction with folded gravity (used for orbit/chase).
        /// </summary>
        public void MoveDirection(Vector3 horizontalDir, float speed)
        {
            Vector3 move = horizontalDir.normalized * speed + Vector3.up * _verticalVelocity;
            _cc.Move(move * Time.deltaTime);
            CurrentMoveSpeed = speed;
        }

        /// <summary>Apply an external delta directly (knockback).</summary>
        public void ApplyExternalDelta(Vector3 delta)
        {
            _cc.Move(delta);
        }

        /// <summary>
        /// Move-based engine move. Caller passes a world-space horizontal
        /// delta already scaled to the engine's tick (e.g. m/s × tickSeconds);
        /// CurrentMoveSpeed is set for the animator blend tree based on the
        /// requested instantaneous speed. Differs from MoveDirection which
        /// scales the speed by Unity's Time.deltaTime — that math is wrong
        /// for fixed engine ticks.
        /// </summary>
        public void EngineMove(Vector3 horizontalDelta, float blendTreeSpeed)
        {
            // Fold gravity into the move so a per-tick CC.Move keeps the
            // unit grounded. _verticalVelocity is updated by TickGravity
            // each Unity frame; we apply the gravity delta here scaled to
            // tick.
            float dt = Time.deltaTime; // small slice; gravity negligible per tick
            Vector3 move = horizontalDelta + Vector3.up * (_verticalVelocity * dt);
            _cc.Move(move);
            CurrentMoveSpeed = blendTreeSpeed;
        }

        /// <summary>
        /// Hard-teleport the unit to the given world position. Disables the
        /// CharacterController for one frame to avoid Unity warning when
        /// changing transform.position while CC is active.
        /// </summary>
        public void Teleport(Vector3 destination)
        {
            bool wasEnabled = _cc.enabled;
            _cc.enabled = false;
            transform.position = destination;
            _cc.enabled = wasEnabled;
            _verticalVelocity = 0f;
        }

        // ── Rotation ─────────────────────────────────────────────────

        public void FaceTarget(Transform target)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        public void FaceDirection(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        public void FaceTargetSnap(Transform target)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        // ── Re-engage ramp ────────────────────────────────────────────

        public void StartReengage() { _reengaging = true; _reengageSpeed = 0f; }
        public void ClearReengage() { _reengaging = false; }

        /// <summary>
        /// Returns the chase speed to use, applying the re-engage ramp if active.
        /// Automatically clears the ramp once full speed is reached.
        /// </summary>
        public float GetChaseSpeed(float fullSpeed)
        {
            if (!_reengaging) return fullSpeed;
            _reengageSpeed = Mathf.MoveTowards(_reengageSpeed, fullSpeed, fullSpeed / 1.5f * Time.deltaTime);
            if (_reengageSpeed >= fullSpeed) _reengaging = false;
            return _reengageSpeed;
        }

        // ── Lunge (attack dash / flying kick) ─────────────────────────

        /// <summary>
        /// Begin a smooth lunge from current position toward lungeEnd over duration seconds.
        /// </summary>
        public void StartLunge(Vector3 lungeEnd, float duration)
        {
            _lungeStart    = transform.position;
            _lungeEnd      = lungeEnd;
            _lungeEnd.y    = _lungeStart.y;
            _lungeDuration = duration;
            _lungeTimer    = 0f;
            _lunging       = true;
        }

        /// <summary>
        /// Tick the active lunge. Returns true while still lunging, false when complete.
        /// </summary>
        public bool TickLunge()
        {
            if (!_lunging) return false;

            _lungeTimer += Time.deltaTime;
            float t       = Mathf.Clamp01(_lungeTimer / _lungeDuration);
            float tSmooth = t * t * (3f - 2f * t);

            Vector3 desired = Vector3.Lerp(_lungeStart, _lungeEnd, tSmooth);
            Vector3 delta   = desired - transform.position;
            delta.y = _verticalVelocity * Time.deltaTime;
            _cc.Move(delta);

            if (t >= 1f)
            {
                _lunging = false;
                return false;
            }
            return true;
        }

        public void StopLunge() { _lunging = false; }

        // ── Dodge arc ─────────────────────────────────────────────────

        /// <summary>
        /// Begin a dodge arc jump from current position to dodgeEnd.
        /// </summary>
        public void StartDodge(Vector3 dodgeEnd, float arcHeight, float duration)
        {
            _dodgeStart     = transform.position;
            _dodgeEnd       = dodgeEnd;
            _dodgeArcHeight = arcHeight;
            _dodgeDuration  = duration;
            _dodgeTimer     = 0f;
            _dodgeMoving    = true;
        }

        /// <summary>
        /// Tick the active dodge arc. Returns true while still moving, false when landed.
        /// </summary>
        public bool TickDodge()
        {
            if (!_dodgeMoving) return false;

            _dodgeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_dodgeTimer / _dodgeDuration);

            Vector3 pos = Vector3.Lerp(_dodgeStart, _dodgeEnd, t);
            pos.y += _dodgeArcHeight * Mathf.Sin(t * Mathf.PI);

            Vector3 delta = pos - transform.position;
            _cc.Move(delta);

            if (t >= 1f)
            {
                _dodgeMoving = false;
                return false;
            }
            return true;
        }

        public void StopDodge() { _dodgeMoving = false; }
    }
}
