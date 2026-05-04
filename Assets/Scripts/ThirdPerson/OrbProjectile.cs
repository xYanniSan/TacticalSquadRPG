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
        [Tooltip("Height above the unit's root position to orbit at.")]
        [SerializeField] private float orbitHeight = 1.4f;

        [Header("Flight")]
        [Tooltip("How fast the orb flies toward the target (units per second).")]
        [SerializeField] private float flySpeed = 12f;
        [Tooltip("Arc height when flying toward the target.")]
        [SerializeField] private float flyArcHeight = 1.5f;

        // ── Runtime state ────────────────────────────────────────────
        private Transform  _owner;
        private float      _orbitAngle;   // current angle in degrees around the owner

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

            float rad = _orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * orbitRadius,
                orbitHeight,
                Mathf.Sin(rad) * orbitRadius);

            transform.position = _owner.position + offset;
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
