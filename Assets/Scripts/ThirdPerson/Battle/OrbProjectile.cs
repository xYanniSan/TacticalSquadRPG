using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Attached to the orb prefab.
    /// Two modes:
    ///   Orbiting — circles the owner unit waiting to be fired.
    ///   Flying   — arcs toward a target then applies damage on arrival.
    /// </summary>
    public class OrbProjectile : MonoBehaviour
    {
        [Header("Orbit")]
        [Tooltip("How fast the orb spins around the unit (degrees per second).")]
        [SerializeField] private float orbitSpeed = 180f;
        [Tooltip("Radius of the orbit circle around the unit.")]
        [SerializeField] private float orbitRadius = 1.2f;
        [Tooltip("Centre height above the unit's root position.")]
        [SerializeField] private float orbitHeight = 1.4f;
        [Tooltip("Amplitude of the vertical sine wave (Syndra-style bob).")]
        [SerializeField] private float orbitBobAmplitude = 0.35f;
        [Tooltip("Speed multiplier of the vertical bob (relative to orbit speed).")]
        [SerializeField] private float orbitBobFrequency = 2f;

        [Header("Flight")]
        [Tooltip("How fast the orb flies toward the target (units per second).")]
        [SerializeField] private float flySpeed = 12f;
        [Tooltip("Arc height when flying toward the target.")]
        [SerializeField] private float flyArcHeight = 1.5f;

        [Header("Ray Mode")]
        [Tooltip("How long the ray beam stays visible after firing.")]
        [SerializeField] private float rayBeamDuration = 0.18f;
        [Tooltip("Width of the ray beam line.")]
        [SerializeField] private float rayBeamWidth = 0.12f;
        [Tooltip("Color of the ray beam line.")]
        [SerializeField] private Color rayBeamColor = new Color(0.9f, 0.7f, 1f, 1f);

        // ── Runtime state ────────────────────────────────────────────
        private Transform  _owner;
        private float      _orbitAngle;   // current angle in degrees around the owner
        private float      _bobPhase;     // per-orb phase offset for the vertical sine

        private bool           _flying;
        private TerrainBattleUnit _target;
        private int            _damage;
        private Vector3        _flyStart;
        private Vector3        _flyTarget;
        private float          _flyProgress;  // 0→1

        // ── Public API ───────────────────────────────────────────────

        /// <summary>Set the unit this orb orbits. Called by OrbBuffHandler on spawn.</summary>
        public void StartOrbit(Transform owner, float startAngleDeg)
        {
            _owner      = owner;
            _orbitAngle = startAngleDeg;
            _bobPhase   = startAngleDeg * Mathf.Deg2Rad;   // stagger bob per orb
            _flying     = false;
        }

        /// <summary>
        /// Detach from orbit and fly at the target.
        /// Called by OrbBuffHandler when a punch lands.
        /// </summary>
        public void Fire(TerrainBattleUnit target, int damage)
        {
            _flying      = true;
            _target      = target;
            _damage      = damage;
            _flyStart    = transform.position;
            _flyProgress = 0f;
            transform.SetParent(null);   // detach from any parent so it moves freely
        }

        /// <summary>
        /// Fire an instant ray at the target from this orb's current position.
        /// Applies damage immediately, draws a brief beam line, then despawns.
        /// Used by the OrbRay skill.
        /// </summary>
        public void FireRay(TerrainBattleUnit target, int damage)
        {
            if (target == null || target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            transform.SetParent(null);
            Vector3 origin = transform.position;
            Vector3 hit    = target.transform.position + Vector3.up * 1.0f;

            target.ApplyDamage(damage);
            CombatLogger.Instance?.Log(CombatLogger.CAT_DMG, "OrbRay",
                $"ray hit {target.Unit?.DisplayName} for {damage}");

            var beam = new GameObject("OrbRayBeam");
            beam.transform.position = origin;
            var lr = beam.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, origin);
            lr.SetPosition(1, hit);
            lr.startWidth = rayBeamWidth;
            lr.endWidth   = rayBeamWidth * 0.4f;
            lr.material   = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = rayBeamColor;
            lr.endColor   = new Color(rayBeamColor.r, rayBeamColor.g, rayBeamColor.b, 0f);
            Destroy(beam, rayBeamDuration);

            Destroy(gameObject);
        }

        // ── Unity ────────────────────────────────────────────────────

        private void Update()
        {
            if (_flying)
                UpdateFlight();
            else
                UpdateOrbit();
        }

        private void UpdateOrbit()
        {
            if (_owner == null) { Destroy(gameObject); return; }

            _orbitAngle += orbitSpeed * Time.deltaTime;
            _bobPhase   += orbitSpeed * orbitBobFrequency * Mathf.Deg2Rad * Time.deltaTime;

            float rad  = _orbitAngle * Mathf.Deg2Rad;
            float bob  = Mathf.Sin(_bobPhase) * orbitBobAmplitude;
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * orbitRadius,
                orbitHeight + bob,
                Mathf.Sin(rad) * orbitRadius);

            transform.position = _owner.position + offset;

            // Face the direction of travel around the circle
            Vector3 tangent = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            if (tangent != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(tangent);
        }

        private void UpdateFlight()
        {
            if (_target == null || _target.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            _flyTarget    = _target.transform.position + Vector3.up * 1.0f;
            float dist    = Vector3.Distance(_flyStart, _flyTarget);
            float step    = flySpeed * Time.deltaTime;
            _flyProgress  = Mathf.MoveTowards(_flyProgress, 1f, step / Mathf.Max(dist, 0.1f));

            // Arc: lerp position + sine arc on Y
            Vector3 pos = Vector3.Lerp(_flyStart, _flyTarget, _flyProgress);
            pos.y += flyArcHeight * Mathf.Sin(_flyProgress * Mathf.PI);
            transform.position = pos;

            // Face direction of travel
            Vector3 dir = (_flyTarget - _flyStart).normalized;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);

            if (_flyProgress >= 1f)
                OnArrival();
        }

        private void OnArrival()
        {
            _target.ApplyDamage(_damage);
            CombatLogger.Instance?.Log(CombatLogger.CAT_DMG, "Orb",
                $"hit {_target.Unit?.DisplayName} for {_damage}");
            Destroy(gameObject);
        }
    }
}
