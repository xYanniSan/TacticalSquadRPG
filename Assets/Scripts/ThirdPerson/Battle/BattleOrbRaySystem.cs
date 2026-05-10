using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Owns the OrbRay skill mechanic: spawns N orbs around the caster,
    /// each immediately fires an instant ray at the resolved target, applying
    /// flat damage and a short beam VFX. If the caster is within melee range
    /// of the target at cast time, the unit teleports a fixed distance in a
    /// random horizontal direction first so the rays come from a safer angle.
    ///
    /// Sits as a MonoBehaviour on the TerrainBattleManager GameObject.
    /// Wired by TerrainBattleManager.Awake/Start (orbPrefab is shared with
    /// BattleCombatResolver — both read it via GetOrbPrefab()).
    /// </summary>
    public class BattleOrbRaySystem : MonoBehaviour
    {
        [Header("Defaults (data-drive when an action exposes orbCount/orbDamage)")]
        [Tooltip("Number of orbs spawned and rays fired by an OrbRay cast.")]
        [SerializeField] private int defaultOrbCount = 3;

        [Tooltip("Flat damage applied per ray when the technique provides no override.")]
        [SerializeField] private int defaultDamagePerRay = 15;

        [Header("Teleport on melee proximity")]
        [Tooltip("If the target is within this distance, the caster teleports before firing.")]
        [SerializeField] private float meleeProximityRadius = 3.0f;

        [Tooltip("Horizontal distance the caster teleports when triggering the proximity escape.")]
        [SerializeField] private float teleportDistance = 20f;

        [Tooltip("Layer mask used to ground-snap the teleport destination via downward raycast.")]
        [SerializeField] private LayerMask groundLayerMask = ~0;

        [Header("Orb Ring")]
        [Tooltip("Radius of the orb spawn ring around the caster at cast time.")]
        [SerializeField] private float orbSpawnRadius = 1.4f;

        [Tooltip("Vertical offset above the caster's root for orb spawn positions.")]
        [SerializeField] private float orbSpawnHeight = 1.4f;

        [Tooltip("Delay between each orb's ray, for staccato feel. 0 = simultaneous.")]
        [SerializeField] private float perOrbFireDelay = 0.05f;

        public void FireOrbRay(TerrainBattleUnit caster, ResolvedTechnique tech, GameObject orbPrefab)
        {
            if (caster == null || caster.IsDead) return;
            if (orbPrefab == null)
            {
                Debug.LogWarning("[BattleOrbRaySystem] orbPrefab not assigned — cannot fire OrbRay.");
                return;
            }

            TerrainBattleUnit target = TerrainBattleManager.Instance?.GetNearestEnemy(caster);
            if (target == null || target.IsDead)
            {
                Debug.Log($"[BattleOrbRaySystem] {caster.Unit?.DisplayName} cast OrbRay but no target available.");
                return;
            }

            // Resolve orb count / damage. Source action overrides defaults if present.
            int   orbCount      = defaultOrbCount;
            int   damagePerRay  = defaultDamagePerRay;
            if (tech?.sourceActions != null)
            {
                foreach (var a in tech.sourceActions)
                {
                    if (a != null && a.actionType == ActionType.OrbSummon)
                    {
                        if (a.orbCount  > 0) orbCount     = a.orbCount;
                        if (a.orbDamage > 0) damagePerRay = a.orbDamage;
                        break;
                    }
                }
            }

            // Optional melee-escape teleport.
            float dist = Vector3.Distance(caster.transform.position, target.transform.position);
            if (dist <= meleeProximityRadius)
            {
                TeleportInRandomDirection(caster);
            }

            // Orient toward the target so the orb ring fires from the front-arc.
            var mover = caster.GetComponent<UnitMovementController>();
            mover?.FaceTargetSnap(target.transform);

            CombatLogger.Instance?.Log(CombatLogger.CAT_DMG, caster.Unit?.DisplayName ?? caster.name,
                $"OrbRay fires ×{orbCount} ({damagePerRay} dmg each) at {target.Unit?.DisplayName}");

            for (int i = 0; i < orbCount; i++)
            {
                float angleDeg = (360f / orbCount) * i;
                float rad      = angleDeg * Mathf.Deg2Rad;
                Vector3 spawnPos = caster.transform.position
                    + new Vector3(Mathf.Cos(rad) * orbSpawnRadius,
                                  orbSpawnHeight,
                                  Mathf.Sin(rad) * orbSpawnRadius);

                var go = Instantiate(orbPrefab, spawnPos, Quaternion.identity);
                var orb = go.GetComponent<OrbProjectile>();
                if (orb == null)
                {
                    Debug.LogError("[BattleOrbRaySystem] Orb prefab is missing OrbProjectile component.");
                    Destroy(go);
                    continue;
                }

                if (perOrbFireDelay > 0f && i > 0)
                {
                    StartCoroutine(DelayedFire(orb, target, damagePerRay, perOrbFireDelay * i));
                }
                else
                {
                    orb.FireRay(target, damagePerRay);
                }
            }
        }

        private System.Collections.IEnumerator DelayedFire(
            OrbProjectile orb, TerrainBattleUnit target, int damage, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (orb != null) orb.FireRay(target, damage);
        }

        private void TeleportInRandomDirection(TerrainBattleUnit caster)
        {
            var mover = caster.GetComponent<UnitMovementController>();
            if (mover == null) return;

            float angle = Random.value * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 dest = caster.transform.position + dir * teleportDistance;

            // Ground-snap the destination so the unit doesn't end up airborne or buried.
            if (Physics.Raycast(dest + Vector3.up * 50f, Vector3.down,
                    out RaycastHit hit, 200f, groundLayerMask))
            {
                dest.y = hit.point.y;
            }

            mover.Teleport(dest);

            CombatLogger.Instance?.Log(CombatLogger.CAT_DMG, caster.Unit?.DisplayName ?? caster.name,
                $"OrbRay melee-escape teleport → {dest}");
        }
    }
}
