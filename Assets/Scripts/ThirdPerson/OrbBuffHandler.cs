using System.Collections.Generic;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Attached to a TerrainBattleUnit at runtime to manage orbiting orbs.
    ///
    /// Lifecycle:
    ///   1. OrbBuffHandler.Spawn(unit, orbPrefab, count, damage) is called by the combat
    ///      resolver when an OrbSummon skill activates.
    ///   2. On each punch hit, TerrainBattleUnit calls TryConsumeOrb(target) which fires
    ///      the next available orb at the enemy.
    ///   3. When all orbs are consumed the component removes itself.
    /// </summary>
    public class OrbBuffHandler : MonoBehaviour
    {
        // ── Static helper ────────────────────────────────────────────

        /// <summary>
        /// Spawn orbs on a unit, replacing any existing ones.
        /// Returns the new handler (so callers can store it if needed).
        /// </summary>
        public static OrbBuffHandler Spawn(
            TerrainBattleUnit unit,
            GameObject orbPrefab,
            int count,
            int damagePerOrb)
        {
            // Remove existing handler first so we don't double-stack
            var existing = unit.GetComponent<OrbBuffHandler>();
            if (existing != null)
            {
                existing.DestroyAllOrbs();
                Destroy(existing);
            }

            var handler = unit.gameObject.AddComponent<OrbBuffHandler>();
            handler.Initialize(unit, orbPrefab, count, damagePerOrb);
            return handler;
        }

        // ── Fields ───────────────────────────────────────────────────

        private TerrainBattleUnit    _unit;
        private int                  _damagePerOrb;
        private List<OrbProjectile>  _orbs = new();

        // ── Init ─────────────────────────────────────────────────────

        private void Initialize(
            TerrainBattleUnit unit,
            GameObject orbPrefab,
            int count,
            int damagePerOrb)
        {
            _unit         = unit;
            _damagePerOrb = damagePerOrb;

            float angleStep = 360f / count;
            for (int i = 0; i < count; i++)
            {
                var go  = Instantiate(orbPrefab);
                var orb = go.GetComponent<OrbProjectile>();
                if (orb == null)
                {
                    Debug.LogError("[OrbBuffHandler] Orb prefab is missing OrbProjectile component!");
                    Destroy(go);
                    continue;
                }
                orb.StartOrbit(unit.transform, angleStep * i);
                _orbs.Add(orb);
            }

            CombatLogger.Instance?.Log(CombatLogger.CAT_INIT, unit.Unit?.DisplayName ?? name,
                $"Orb buff applied — {_orbs.Count} orbs");
        }

        // ── Public API ───────────────────────────────────────────────

        /// <returns>true if an orb was available and fired.</returns>
        public bool TryConsumeOrb(TerrainBattleUnit target)
        {
            // Find the first still-orbiting orb
            for (int i = 0; i < _orbs.Count; i++)
            {
                if (_orbs[i] == null) { _orbs.RemoveAt(i); i--; continue; }

                var orb = _orbs[i];
                _orbs.RemoveAt(i);
                orb.Fire(target, _damagePerOrb);

                CombatLogger.Instance?.Log(CombatLogger.CAT_INIT, _unit?.Unit?.DisplayName ?? name,
                    $"Orb fired → {target.Unit?.DisplayName} ({_orbs.Count} remaining)");

                if (_orbs.Count == 0)
                    Destroy(this);   // all consumed — remove handler

                return true;
            }

            Destroy(this);
            return false;
        }

        public bool HasOrbs => _orbs.Count > 0;

        // ── Cleanup ──────────────────────────────────────────────────

        public void DestroyAllOrbs()
        {
            foreach (var o in _orbs)
                if (o != null) Destroy(o.gameObject);
            _orbs.Clear();
        }

        private void OnDestroy() => DestroyAllOrbs();
    }
}
