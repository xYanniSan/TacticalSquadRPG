using System.Collections.Generic;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Describes an in-flight knockback impulse applied to a unit.
    /// Direction is calculated from attacker → defender at the moment of impact.
    /// </summary>
    public struct KnockbackImpulse
    {
        /// <summary>World-space direction (normalised) the unit is knocked toward.</summary>
        public Vector3 direction;

        /// <summary>Total distance the unit should travel over the lifetime.</summary>
        public float distance;

        /// <summary>How quickly the impulse decays (units/s²).</summary>
        public float deceleration;

        /// <summary>Remaining distance still to travel this frame.</summary>
        public float remaining;

        /// <summary>Whether the impulse includes upward arc (e.g. launch).</summary>
        public bool hasVertical;

        public static KnockbackImpulse Create(
            Vector3 from, Vector3 to,
            float distance, float deceleration,
            bool addArc = false)
        {
            Vector3 horizontal = (to - from);
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.001f) horizontal = Vector3.forward;
            horizontal.Normalize();

            Vector3 dir = horizontal;
            if (addArc) dir = (horizontal + Vector3.up * 0.45f).normalized;

            return new KnockbackImpulse
            {
                direction    = dir,
                distance     = distance,
                deceleration = deceleration,
                remaining    = distance,
                hasVertical  = addArc
            };
        }
    }

    /// <summary>
    /// Strength presets mapping damage thresholds to knockback feel.
    /// </summary>
    public enum KnockbackStrength
    {
        None    = 0,
        Nudge   = 1,   // light tick-back
        Stumble = 2,   // noticeable stagger
        Launch  = 3,   // sends the target flying
        Smash   = 4    // hard send + arc
    }

    /// <summary>
    /// Applies and steps directional knockback impulses to units every frame.
    /// Sits on the same GameObject as TerrainBattleManager.
    /// </summary>
    public class BattleKnockbackSystem : MonoBehaviour
    {
        // Distance (units) per strength preset
        private static readonly float[] Distances     = { 0f, 0.8f, 2.0f, 4.0f, 7.0f };
        private static readonly float[] Decelerations = { 0f, 6.0f, 5.0f, 4.5f, 4.0f };

        // Active impulses keyed by target unit
        private Dictionary<TerrainBattleUnit, KnockbackImpulse> _impulses
            = new Dictionary<TerrainBattleUnit, KnockbackImpulse>();

        // Stagger durations per strength — Nudge doesn't stagger at all
        private static readonly float[] StaggerDurations = { 0f, 0f, 0.4f, 0.75f, 1.2f };

        // ── Public API ────────────────────────────────────────────────

        /// <summary>
        /// Automatically selects knockback strength from damage dealt.
        /// </summary>
        public void ApplyFromDamage(
            TerrainBattleUnit attacker,
            TerrainBattleUnit defender,
            int damage)
        {
            KnockbackStrength strength = DamageToStrength(damage);
            if (strength == KnockbackStrength.None) return;
            Apply(attacker, defender, strength);
        }

        /// <summary>
        /// Applies a specific strength knockback regardless of damage.
        /// </summary>
        public void Apply(
            TerrainBattleUnit attacker,
            TerrainBattleUnit defender,
            KnockbackStrength strength)
        {
            if (strength == KnockbackStrength.None) return;
            if (defender == null || defender.IsDead) return;

            bool addArc = strength >= KnockbackStrength.Launch;
            float dist  = Distances[(int)strength];
            float decel = Decelerations[(int)strength];

            var impulse = KnockbackImpulse.Create(
                attacker.transform.position,
                defender.transform.position,
                dist, decel, addArc);

            // Overwrite any existing impulse — stronger hit wins
            _impulses[defender] = impulse;

            // Stagger the defender so it stops trying to chase/attack while flying back
            float staggerDur = StaggerDurations[(int)strength];
            if (staggerDur > 0f)
                defender.EnterStagger(staggerDur);
        }

        // ── Step impulses every frame ─────────────────────────────────

        private void Update()
        {
            if (_impulses.Count == 0) return;

            float dt = Time.deltaTime;

            // Collect updates and removals separately — can't modify the dictionary mid-enumeration
            List<TerrainBattleUnit> toRemove = null;
            List<(TerrainBattleUnit unit, KnockbackImpulse impulse)> toUpdate = null;

            foreach (var kvp in _impulses)
            {
                TerrainBattleUnit unit = kvp.Key;

                if (unit == null || unit.IsDead)
                {
                    (toRemove ??= new List<TerrainBattleUnit>()).Add(unit);
                    continue;
                }

                KnockbackImpulse imp = kvp.Value;

                float speed = imp.deceleration * (imp.remaining / imp.distance);
                speed = Mathf.Max(speed, 0.5f);

                float step = Mathf.Min(speed * dt, imp.remaining);
                unit.ApplyKnockbackMove(imp.direction * step);

                imp.remaining -= step;

                if (imp.remaining <= 0f)
                    (toRemove ??= new List<TerrainBattleUnit>()).Add(unit);
                else
                    (toUpdate ??= new List<(TerrainBattleUnit, KnockbackImpulse)>()).Add((unit, imp));
            }

            // Apply updates
            if (toUpdate != null)
                foreach (var (unit, imp) in toUpdate)
                    _impulses[unit] = imp;

            // Remove finished or dead
            if (toRemove != null)
                foreach (var u in toRemove)
                    _impulses.Remove(u);
        }

        // ── Damage → Strength mapping ────────────────────────────────

        public static KnockbackStrength DamageToStrength(int damage)
        {
            if (damage <= 0)  return KnockbackStrength.None;
            if (damage < 8)   return KnockbackStrength.Nudge;
            if (damage < 20)  return KnockbackStrength.Stumble;
            if (damage < 40)  return KnockbackStrength.Launch;
            return KnockbackStrength.Smash;
        }
    }
}
