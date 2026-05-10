using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Manages frontline/backline slot allocation and unit death cleanup.
    /// Sits on the same GameObject as TerrainBattleManager.
    /// </summary>
    public class BattleEngagementManager : MonoBehaviour
    {
        private const int MaxFrontline = 3;

        private HashSet<TerrainBattleUnit> _playerFrontline = new HashSet<TerrainBattleUnit>();
        private HashSet<TerrainBattleUnit> _enemyFrontline  = new HashSet<TerrainBattleUnit>();

        private bool _battleStarted;

        public void SetBattleStarted(bool started) => _battleStarted = started;

        public bool RequestFrontlineSlot(TerrainBattleUnit unit)
        {
            if (!_battleStarted) return false;

            var frontline = unit.Unit.team == UnitTeam.Player ? _playerFrontline : _enemyFrontline;
            frontline.RemoveWhere(u => u == null || u.IsDead);

            if (frontline.Count < MaxFrontline)
            {
                frontline.Add(unit);
                return true;
            }
            return false;
        }

        public void OnUnitDied(TerrainBattleUnit unit)
        {
            _playerFrontline.Remove(unit);
            _enemyFrontline.Remove(unit);
        }
    }
}
