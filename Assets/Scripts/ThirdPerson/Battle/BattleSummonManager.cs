using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Handles spawning and tracking of summoned units.
    /// Sits on the same GameObject as TerrainBattleManager.
    /// </summary>
    public class BattleSummonManager : MonoBehaviour
    {
        private Dictionary<int, TerrainBattleUnit> _activeSummons = new Dictionary<int, TerrainBattleUnit>();

        private List<TerrainBattleUnit> _playerUnits;
        private List<TerrainBattleUnit> _enemyUnits;
        private List<TerrainBattleUnit> _allUnits;

        public void Initialize(
            List<TerrainBattleUnit> playerUnits,
            List<TerrainBattleUnit> enemyUnits,
            List<TerrainBattleUnit> allUnits)
        {
            _playerUnits = playerUnits;
            _enemyUnits  = enemyUnits;
            _allUnits    = allUnits;
        }

        public bool HasActiveSummon(int casterId)
        {
            return _activeSummons.TryGetValue(casterId, out TerrainBattleUnit s)
                && s != null && !s.IsDead;
        }

        public void TrySummon(TerrainBattleUnit caster, ResolvedTechnique tech)
        {
            int casterId = caster.Unit.runtimeId;

            if (_activeSummons.TryGetValue(casterId, out TerrainBattleUnit existing)
                && existing != null && !existing.IsDead)
            {
                Debug.Log($"  {caster.Unit.DisplayName} tries [{tech.techniqueName}] — summon already active!");
                return;
            }

            var summonUnit = new UnitRuntime
            {
                definition  = null,
                runtimeId   = UnitFactory.CreateSummonId(),
                team        = caster.Unit.team,
                currentStats = new StatBlock(
                    tech.power * 2,
                    tech.power / 2,
                    caster.Unit.currentStats.defense / 2,
                    caster.Unit.currentStats.moveSpeed * 1.2f),
                maxHP    = tech.power * 2,
                currentHP = tech.power * 2,
                behavior = new BehaviorLoadout(BehaviorType.Aggressive),
                equippedSkills = new System.Collections.Generic.List<SkillSlot>(),
                activeEffects  = new System.Collections.Generic.List<StatusEffect>(),
                isDead         = false,
                overrideDisplayName = $"{caster.Unit.DisplayName}'s Summon"
            };

            // Raycast to find actual terrain height at spawn point
            Vector3 basePos = caster.transform.position + caster.transform.forward * 3f;
            float groundY   = basePos.y;
            if (Physics.Raycast(basePos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                groundY = hit.point.y;
            Vector3 spawnPos = new Vector3(basePos.x, groundY + 2f, basePos.z);

            Color summonColor = caster.Unit.team == UnitTeam.Player
                ? new Color(0.4f, 0.8f, 1f)
                : new Color(1f, 0.5f, 0.3f);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"{caster.Unit.DisplayName}'s Summon";
            go.transform.position = spawnPos;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = summonColor;

            go.AddComponent<CharacterController>();
            go.AddComponent<HealthSystem>();
            var battleUnit = go.AddComponent<TerrainBattleUnit>();
            battleUnit.Initialize(summonUnit);

            // Primitive capsule pivot is at center — override CC center back to zero
            var summonCC = go.GetComponent<CharacterController>();
            if (summonCC != null) summonCC.center = Vector3.zero;

            go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            summonUnit.visualInstance = go;

            var list = caster.Unit.team == UnitTeam.Player ? _playerUnits : _enemyUnits;
            list.Add(battleUnit);
            _allUnits.Add(battleUnit);

            _activeSummons[casterId] = battleUnit;

            Debug.Log($"  {caster.Unit.DisplayName} uses [{tech.techniqueName}] — SUMMONED! " +
                      $"(HP:{summonUnit.maxHP} ATK:{summonUnit.currentStats.attack})");
        }
    }
}
