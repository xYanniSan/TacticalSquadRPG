using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Handles target finding for units on the battlefield.
    /// Sits on the same GameObject as TerrainBattleManager.
    /// </summary>
    public class BattleTargetFinder : MonoBehaviour
    {
        private System.Collections.Generic.List<TerrainBattleUnit> _playerUnits;
        private System.Collections.Generic.List<TerrainBattleUnit> _enemyUnits;

        public void Initialize(
            System.Collections.Generic.List<TerrainBattleUnit> playerUnits,
            System.Collections.Generic.List<TerrainBattleUnit> enemyUnits)
        {
            _playerUnits = playerUnits;
            _enemyUnits  = enemyUnits;
        }

        public TerrainBattleUnit GetNearestEnemy(TerrainBattleUnit unit)
        {
            var enemies = unit.Unit.team == TacticalRPG.DataModels.UnitTeam.Player
                ? _enemyUnits : _playerUnits;

            TerrainBattleUnit nearest = null;
            float minDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead) continue;
                float dist = Vector3.Distance(unit.transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }
    }
}
