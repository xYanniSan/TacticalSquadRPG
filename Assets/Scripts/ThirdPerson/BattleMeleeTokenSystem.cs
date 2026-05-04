using UnityEngine;
using TacticalRPG.DataModels;
using System.Collections.Generic;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Melee Token System — inspired by Halo 2's "proximity melee" and Dragon Age's
    /// engagement slot design. Only a fixed number of attackers can actively engage
    /// any single target at once. Others must orbit and wait for a slot to open.
    ///
    /// This prevents units piling onto the same target and causing overlap jitter.
    /// </summary>
    public class BattleMeleeTokenSystem : MonoBehaviour
    {
        [Header("Token Settings")]
        [Tooltip("Max attackers allowed on a single target simultaneously.")]
        [SerializeField] private int maxAttackersPerTarget = 2;

        [Tooltip("How far waiters orbit around their target while waiting for a token.")]
        [SerializeField] private float orbitRadius = 4f;

        [Tooltip("How fast waiters orbit (degrees per second).")]
        [SerializeField] private float orbitSpeed = 45f;

        // Tokens: target → list of units currently holding an attack token on them
        private readonly Dictionary<TerrainBattleUnit, List<TerrainBattleUnit>> _tokens
            = new Dictionary<TerrainBattleUnit, List<TerrainBattleUnit>>();

        // Orbit angles per waiting unit
        private readonly Dictionary<TerrainBattleUnit, float> _orbitAngles
            = new Dictionary<TerrainBattleUnit, float>();

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Called each frame by a unit in Melee state.
        /// Returns true if this unit holds a token and should actively attack.
        /// Returns false if all slots are full — unit should orbit instead.
        /// </summary>
        public bool RequestToken(TerrainBattleUnit requester, TerrainBattleUnit target)
        {
            if (target == null || target.IsDead)
            {
                ReleaseToken(requester);
                return false;
            }

            // Already holds a token on this target
            if (_tokens.TryGetValue(target, out var holders) && holders.Contains(requester))
                return true;

            // Release any token this unit holds on a different target
            ReleaseToken(requester);

            // Ensure entry exists
            if (!_tokens.ContainsKey(target))
                _tokens[target] = new List<TerrainBattleUnit>();

            var list = _tokens[target];

            // Clean up dead holders
            list.RemoveAll(u => u == null || u.IsDead);

            if (list.Count < maxAttackersPerTarget)
            {
                // Slot available — grant token
                list.Add(requester);
                _orbitAngles.Remove(requester); // no longer orbiting
                return true;
            }

            // No slot — assign an orbit angle if not already orbiting
            if (!_orbitAngles.ContainsKey(requester))
            {
                // Space waiters evenly around the target
                _orbitAngles[requester] = Random.Range(0f, 360f);
            }

            return false;
        }

        /// <summary>
        /// Called when a unit finishes its attack cycle, dies, or changes target.
        /// Frees its slot so a waiting unit can move in.
        /// </summary>
        public void ReleaseToken(TerrainBattleUnit unit)
        {
            foreach (var kv in _tokens)
                kv.Value.Remove(unit);

            _orbitAngles.Remove(unit);
        }

        /// <summary>
        /// Returns the world-space orbit position a waiting unit should move toward.
        /// Call each frame when RequestToken returns false.
        /// </summary>
        public Vector3 GetOrbitPosition(TerrainBattleUnit waiter, TerrainBattleUnit target)
        {
            if (!_orbitAngles.ContainsKey(waiter))
                _orbitAngles[waiter] = Random.Range(0f, 360f);

            // Advance angle
            _orbitAngles[waiter] += orbitSpeed * Time.deltaTime;

            float rad = _orbitAngles[waiter] * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * orbitRadius;
            return target.transform.position + offset;
        }

        private void Update()
        {
            // Clean up entries for dead targets
            var deadTargets = new List<TerrainBattleUnit>();
            foreach (var kv in _tokens)
                if (kv.Key == null || kv.Key.IsDead) deadTargets.Add(kv.Key);
            foreach (var t in deadTargets) _tokens.Remove(t);
        }
    }
}
