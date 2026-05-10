using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Movement-primitives library — the verbs the brain composes into
    /// combat sentences. See `Docs/Design/COMBAT_DESIGN.md` "Spatial combat
    /// choreography" for the design and reference moments.
    ///
    /// The choreography system does NOT own translation (UnitMovementController
    /// still moves bodies). It owns the *intent of motion* — running a
    /// primitive instructs the unit's mover what to do each frame for the
    /// primitive's duration. Primitives respect the speed-band budget.
    ///
    /// Phase 14 — first primitives shipped: OrbitTarget, RangeBand reads.
    /// More primitives (TeleportFlank, GhostTrail, PoseAttack, LaunchAttack…)
    /// land as the spec is filled in.
    /// </summary>
    public class BattleMovementChoreography : MonoBehaviour
    {
        // Range-band thresholds (see spec table). Configurable for tuning.
        [Header("Range bands")]
        [SerializeField] private float farMinDistance   = 8f;
        [SerializeField] private float midMinDistance   = 3f;
        [SerializeField] private float closeMinDistance = 1f;

        private readonly Dictionary<TerrainBattleUnit, ActivePrimitive> _active
            = new Dictionary<TerrainBattleUnit, ActivePrimitive>();

        // ── Range bands ─────────────────────────────────────────────

        public RangeBand GetRangeBand(TerrainBattleUnit unit, TerrainBattleUnit target)
        {
            if (unit == null || target == null) return RangeBand.Far;
            float d = Vector3.Distance(unit.transform.position, target.transform.position);
            if (d > farMinDistance)   return RangeBand.Far;
            if (d > midMinDistance)   return RangeBand.Mid;
            if (d > closeMinDistance) return RangeBand.Close;
            return RangeBand.Locked;
        }

        public RangeBand GetRangeBand(TerrainBattleUnit unit)
        {
            return GetRangeBand(unit, unit?.CurrentTarget);
        }

        // ── Primitive: OrbitTarget (Reference 2 — Lee circling Gaara) ─

        /// <summary>
        /// Sustained circle around target at given distance. The canonical
        /// speed-build primitive — units in Engaged+ band running this should
        /// see speed climb steadily (BattleMovementSystem reports `Circle` intent
        /// while orbiting). Direction +1 = clockwise, -1 = counter-clockwise.
        /// </summary>
        public void StartOrbitTarget(TerrainBattleUnit unit, TerrainBattleUnit target,
            float distance, float direction, float durationSec)
        {
            if (unit == null || target == null || unit.IsDead) return;
            _active[unit] = new ActivePrimitive
            {
                kind          = PrimitiveKind.Orbit,
                target        = target,
                paramA        = Mathf.Max(0.5f, distance),
                paramB        = Mathf.Sign(direction == 0f ? 1f : direction),
                remaining     = Mathf.Max(0.5f, durationSec),
                totalDuration = Mathf.Max(0.5f, durationSec)
            };

            // Brain pushes Circle intent so BattleSpeedSystem grants +6/sec.
            TerrainBattleManager.Instance?.Movement?.SetIntent(unit, MovementIntent.Circle);
        }

        public bool IsRunning(TerrainBattleUnit unit, PrimitiveKind kind)
        {
            return unit != null
                && _active.TryGetValue(unit, out var p)
                && p.kind == kind
                && p.remaining > 0f;
        }

        public bool IsRunningAny(TerrainBattleUnit unit)
        {
            return unit != null
                && _active.TryGetValue(unit, out var p)
                && p.remaining > 0f;
        }

        public void Cancel(TerrainBattleUnit unit)
        {
            if (unit == null) return;
            _active.Remove(unit);
        }

        // ── Primitive: TeleportFlank (Reference 1 — Lee fade-strike) ─

        /// <summary>
        /// Instant relocate to a flank position around the target with brief
        /// alpha-flicker on entry/exit and a `GhostTrail` of fading silhouettes
        /// between the source and destination. Primed-band only — caller is
        /// responsible for the gate.
        ///
        /// `flankAngleDegrees` 0 = behind, 90 = right side, etc. (target-local)
        /// </summary>
        public void TeleportFlank(TerrainBattleUnit unit, TerrainBattleUnit target,
            float flankAngleDegrees, float orbitDistance, int ghostCount = 4)
        {
            if (unit == null || target == null || unit.IsDead) return;

            Vector3 from = unit.transform.position;
            Vector3 forward = target.transform.forward;
            Quaternion rot = Quaternion.AngleAxis(flankAngleDegrees, Vector3.up);
            Vector3 dir = (rot * forward).normalized;
            Vector3 to = target.transform.position + dir * orbitDistance;

            // Snap-relocate via UnitMovementController.Teleport (preserves
            // CharacterController integrity).
            var mover = unit.GetComponent<UnitMovementController>();
            if (mover != null) mover.Teleport(to);
            else unit.transform.position = to;

            // Face the target post-teleport.
            mover?.FaceTargetSnap(target.transform);

            // Spawn the ghost trail.
            SpawnGhostTrail(unit, from, to, ghostCount);
        }

        // ── Primitive: KnockbackFar (References 3,4 — Maki / Sasuke flung) ─

        /// <summary>
        /// Cinematic-scale knockback that physically slides the target a long
        /// distance away from the attacker. Distinct from
        /// `BattleKnockbackSystem`'s contextual hit-knockback — this one is for
        /// finishers and sends the defender Close → Far in one beat.
        /// </summary>
        public void KnockbackFar(TerrainBattleUnit attacker, TerrainBattleUnit defender,
            float distance, float durationSec)
        {
            if (attacker == null || defender == null || defender.IsDead) return;

            Vector3 toDefender = defender.transform.position - attacker.transform.position;
            toDefender.y = 0f;
            if (toDefender.sqrMagnitude < 0.001f) toDefender = attacker.transform.forward;
            Vector3 dir = toDefender.normalized;
            Vector3 from = defender.transform.position;
            Vector3 to   = from + dir * distance;

            _active[defender] = new ActivePrimitive
            {
                kind          = PrimitiveKind.KnockbackFar,
                target        = attacker,           // keep ref so we can face it on land
                paramA        = distance,
                paramB        = 0f,
                remaining     = Mathf.Max(0.05f, durationSec),
                totalDuration = Mathf.Max(0.05f, durationSec),
                fromPos       = from,
                toPos         = to
            };
        }

        // ── Primitive: LaunchUp (Reference 3 — Naruto upper) ──────

        /// <summary>
        /// Launches the target straight up over `riseSec`, holds, then drops.
        /// Used as the lead-in for an aerial follow-up combo. Target's gravity
        /// is suspended for the duration via `ApplyExternalDelta` ticks.
        /// </summary>
        public void LaunchUp(TerrainBattleUnit defender, float launchHeight, float riseSec)
        {
            if (defender == null || defender.IsDead) return;

            _active[defender] = new ActivePrimitive
            {
                kind          = PrimitiveKind.LaunchUp,
                target        = null,
                paramA        = launchHeight,
                paramB        = 0f,
                remaining     = Mathf.Max(0.05f, riseSec),
                totalDuration = Mathf.Max(0.05f, riseSec),
                fromPos       = defender.transform.position,
                toPos         = defender.transform.position + Vector3.up * launchHeight
            };
        }

        // ── Primitive: DashTo (Initiation dash) ───────────────────

        /// <summary>
        /// Fast straight-line dash to a position relative to a target — used
        /// by the Initiation phase of an exchange so attacks have a visible
        /// approach instead of starting from in-place.
        /// `closeDistance` = final distance from target after dash.
        /// </summary>
        public void DashToTarget(TerrainBattleUnit unit, TerrainBattleUnit target, float closeDistance, float durationSec)
        {
            if (unit == null || target == null || unit.IsDead) return;

            Vector3 toUnit = unit.transform.position - target.transform.position;
            toUnit.y = 0f;
            if (toUnit.sqrMagnitude < 0.001f) toUnit = -target.transform.forward;
            Vector3 finalPos = target.transform.position + toUnit.normalized * closeDistance;

            _active[unit] = new ActivePrimitive
            {
                kind          = PrimitiveKind.DashLine,
                target        = target,
                paramA        = closeDistance,
                paramB        = 0f,
                remaining     = Mathf.Max(0.05f, durationSec),
                totalDuration = Mathf.Max(0.05f, durationSec),
                fromPos       = unit.transform.position,
                toPos         = finalPos
            };
        }

        // ── Primitive: BackstepAway (DisengageBackstep) ───────────

        public void BackstepAway(TerrainBattleUnit unit, TerrainBattleUnit target, float distance, float durationSec)
        {
            if (unit == null || unit.IsDead) return;

            Vector3 from = unit.transform.position;
            Vector3 to = from;
            if (target != null)
            {
                Vector3 awayDir = (unit.transform.position - target.transform.position);
                awayDir.y = 0f;
                if (awayDir.sqrMagnitude < 0.001f) awayDir = -unit.transform.forward;
                awayDir.Normalize();
                to = from + awayDir * distance;
            }

            _active[unit] = new ActivePrimitive
            {
                kind          = PrimitiveKind.BackstepAway,
                target        = target,
                paramA        = distance,
                paramB        = 0f,
                remaining     = Mathf.Max(0.05f, durationSec),
                totalDuration = Mathf.Max(0.05f, durationSec),
                fromPos       = from,
                toPos         = to
            };
        }

        // ── GhostTrail visual (Reference 1 — Lee fade silhouettes) ────

        /// <summary>
        /// Spawns N brief fading silhouettes between two world positions.
        /// Each ghost is a simple capsule mesh with a translucent material
        /// — placeholder for proper mesh-clone-of-current-pose. Ghosts
        /// destroy themselves after lifetime expires.
        /// </summary>
        public void SpawnGhostTrail(TerrainBattleUnit sourceUnit, Vector3 from, Vector3 to, int ghostCount)
        {
            if (ghostCount < 1) return;
            for (int i = 0; i < ghostCount; i++)
            {
                float t = (i + 1f) / (ghostCount + 1f);
                Vector3 pos = Vector3.Lerp(from, to, t);
                SpawnSingleGhost(sourceUnit, pos, lifeSec: 0.30f, alpha: 0.45f * (1f - t));
            }
        }

        private void SpawnSingleGhost(TerrainBattleUnit sourceUnit, Vector3 pos, float lifeSec, float alpha)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "GhostTrail";
            go.transform.position = pos;
            go.transform.rotation = sourceUnit != null
                ? sourceUnit.transform.rotation
                : Quaternion.identity;

            // Strip collider — pure visual.
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Cyan-tint translucent material, cheap.
                var mat = new Material(Shader.Find("Sprites/Default"))
                {
                    color = new Color(0.4f, 0.95f, 1f, alpha)
                };
                renderer.material = mat;
            }

            // Auto-fade by reducing alpha over lifetime.
            var fader = go.AddComponent<GhostFade>();
            fader.lifeSeconds = lifeSec;
            fader.startAlpha  = alpha;

            Destroy(go, lifeSec);
        }

        // ── Tick ────────────────────────────────────────────────────

        private void Update()
        {
            if (_active.Count == 0) return;
            float dt = Time.deltaTime;

            var snapshot = new List<TerrainBattleUnit>(_active.Keys);
            foreach (var unit in snapshot)
            {
                if (unit == null || unit.IsDead) { _active.Remove(unit); continue; }
                var p = _active[unit];
                p.remaining -= dt;
                if (p.remaining <= 0f) { _active.Remove(unit); continue; }
                _active[unit] = p;

                switch (p.kind)
                {
                    case PrimitiveKind.Orbit:
                        TickOrbit(unit, p, dt);
                        break;
                    case PrimitiveKind.KnockbackFar:
                        TickKnockbackFar(unit, p, dt);
                        break;
                    case PrimitiveKind.LaunchUp:
                        TickLaunchUp(unit, p, dt);
                        break;
                    case PrimitiveKind.BackstepAway:
                        TickBackstepAway(unit, p, dt);
                        break;
                    case PrimitiveKind.DashLine:
                        TickDashLine(unit, p, dt);
                        break;
                }
            }
        }

        private void TickDashLine(TerrainBattleUnit unit, ActivePrimitive p, float dt)
        {
            float t = 1f - Mathf.Clamp01(p.remaining / p.totalDuration);
            // Quadratic ease-out — fast initial, slow finish, like a sprint stop.
            float eased = 1f - (1f - t) * (1f - t);
            Vector3 worldPos = Vector3.Lerp(p.fromPos, p.toPos, eased);
            var mover = unit.GetComponent<UnitMovementController>();
            if (mover != null)
            {
                Vector3 delta = worldPos - unit.transform.position;
                delta.y = 0f;
                mover.ApplyExternalDelta(delta);
                if (p.target != null) mover.FaceTarget(p.target.transform);
            }
        }

        private void TickKnockbackFar(TerrainBattleUnit unit, ActivePrimitive p, float dt)
        {
            float t = 1f - Mathf.Clamp01(p.remaining / p.totalDuration);
            // Eased curve — fast start, slow finish, like sliding to a halt.
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            Vector3 worldPos = Vector3.Lerp(p.fromPos, p.toPos, eased);
            var mover = unit.GetComponent<UnitMovementController>();
            if (mover != null)
            {
                Vector3 delta = worldPos - unit.transform.position;
                delta.y = 0f;
                mover.ApplyExternalDelta(delta);
            }
            else
            {
                unit.transform.position = worldPos;
            }
        }

        private void TickLaunchUp(TerrainBattleUnit unit, ActivePrimitive p, float dt)
        {
            float t = 1f - Mathf.Clamp01(p.remaining / p.totalDuration);
            // Sine arc — smooth rise.
            float eased = Mathf.Sin(t * Mathf.PI * 0.5f);
            Vector3 worldPos = Vector3.Lerp(p.fromPos, p.toPos, eased);
            var mover = unit.GetComponent<UnitMovementController>();
            if (mover != null)
            {
                Vector3 delta = worldPos - unit.transform.position;
                mover.ApplyExternalDelta(delta);
            }
            else
            {
                unit.transform.position = worldPos;
            }
        }

        private void TickBackstepAway(TerrainBattleUnit unit, ActivePrimitive p, float dt)
        {
            float t = 1f - Mathf.Clamp01(p.remaining / p.totalDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 worldPos = Vector3.Lerp(p.fromPos, p.toPos, eased);
            var mover = unit.GetComponent<UnitMovementController>();
            if (mover != null)
            {
                Vector3 delta = worldPos - unit.transform.position;
                delta.y = 0f;
                mover.ApplyExternalDelta(delta);
                if (p.target != null) mover.FaceTarget(p.target.transform);
            }
            else
            {
                unit.transform.position = worldPos;
            }
        }

        private void TickOrbit(TerrainBattleUnit unit, ActivePrimitive p, float dt)
        {
            if (p.target == null || p.target.IsDead) { _active.Remove(unit); return; }

            var mover = unit.GetComponent<UnitMovementController>();
            if (mover == null) return;

            // Angular-velocity orbit. The unit completes ~one revolution every
            // ~3.3s (110°/sec). Each frame: advance the polar angle, smoothly
            // approach the target radius, move to the resulting world position.
            // This produces a clean ring orbit regardless of starting distance,
            // unlike the previous tangent-correction approach which tended to
            // snap or drift.
            const float AngularSpeedDegPerSec = 110f;

            float elapsed = p.totalDuration - p.remaining;
            float rampIn  = Mathf.Clamp01(elapsed / 0.4f);

            Vector3 toUnit = unit.transform.position - p.target.transform.position;
            toUnit.y = 0f;
            if (toUnit.sqrMagnitude < 0.001f) toUnit = unit.transform.right;

            float currentRadius = toUnit.magnitude;
            float targetRadius  = p.paramA;
            float radius = Mathf.MoveTowards(currentRadius, targetRadius, dt * 6f * rampIn);

            float currentAngleRad = Mathf.Atan2(toUnit.z, toUnit.x);
            float angularRad      = AngularSpeedDegPerSec * Mathf.Deg2Rad * p.paramB * rampIn;
            float newAngleRad     = currentAngleRad + angularRad * dt;

            Vector3 desiredPos = p.target.transform.position + new Vector3(
                Mathf.Cos(newAngleRad) * radius,
                0f,
                Mathf.Sin(newAngleRad) * radius);

            Vector3 dir = (desiredPos - unit.transform.position);
            dir.y = 0f;
            float distToDesired = dir.magnitude;
            if (distToDesired > 0.001f)
            {
                dir.Normalize();
                // Move at a speed proportional to required arc-distance per
                // frame so we hit the desired point. UnitMovementController
                // expects a speed value (units/sec) and direction.
                float moveSpeed = distToDesired / Mathf.Max(0.0001f, dt);
                // Cap at a reasonable max so we don't snap on big numerical
                // jumps (e.g. first frame after teleport).
                moveSpeed = Mathf.Min(moveSpeed, unit.Unit != null
                    ? unit.Unit.currentStats.moveSpeed * 2.0f : 8f);
                mover.MoveDirection(dir, moveSpeed);
            }
            mover.FaceTarget(p.target.transform);
        }

        // ── State ────────────────────────────────────────────────────

        public enum PrimitiveKind
        {
            None,
            Orbit,
            Teleport,           // single teleport with fade
            DashLine,           // straight-line dash
            KnockbackFar,       // forced cinematic-scale knockback
            LaunchUp,           // launch target into the air
            BackstepAway        // disengage backstep
        }

        private struct ActivePrimitive
        {
            public PrimitiveKind     kind;
            public TerrainBattleUnit target;
            public float             paramA;
            public float             paramB;
            public float             remaining;
            public float             totalDuration;
            public Vector3           fromPos;
            public Vector3           toPos;
        }
    }

    /// <summary>
    /// Per-frame fade-out for ghost-trail capsules.
    /// </summary>
    public class GhostFade : MonoBehaviour
    {
        public float lifeSeconds = 0.3f;
        public float startAlpha  = 0.4f;

        private float _elapsed;
        private Renderer _renderer;
        private Material _mat;

        private void Start()
        {
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null) _mat = _renderer.material;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_mat == null) return;
            float a = startAlpha * (1f - Mathf.Clamp01(_elapsed / lifeSeconds));
            Color c = _mat.color;
            c.a = a;
            _mat.color = c;
        }
    }
}
