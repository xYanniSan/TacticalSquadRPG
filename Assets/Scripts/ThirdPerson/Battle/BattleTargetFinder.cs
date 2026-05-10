using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Handles target finding for units on the battlefield. Phase 7 added
    /// stance-driven target priority — `GetTarget(unit)` consults the unit's
    /// stance and returns the matching priority target. `GetNearestEnemy` is
    /// the legacy entry point and remains as the fallback.
    /// </summary>
    public class BattleTargetFinder : MonoBehaviour
    {
        private List<TerrainBattleUnit> _playerUnits;
        private List<TerrainBattleUnit> _enemyUnits;

        public void Initialize(
            List<TerrainBattleUnit> playerUnits,
            List<TerrainBattleUnit> enemyUnits)
        {
            _playerUnits = playerUnits;
            _enemyUnits  = enemyUnits;
        }

        public TerrainBattleUnit GetNearestEnemy(TerrainBattleUnit unit)
        {
            return GetTarget(unit, TargetPriority.Nearest);
        }

        /// <summary>
        /// Stance-driven targeting (Phase 7). Unknown / unmarked priorities
        /// fall back to nearest. Doesn't fail-soft on backline-first if no
        /// backline candidates exist — it relaxes to nearest.
        /// </summary>
        public TerrainBattleUnit GetTarget(TerrainBattleUnit unit, TargetPriority priority)
        {
            var enemies = unit.Unit.team == UnitTeam.Player ? _enemyUnits : _playerUnits;
            if (enemies == null || enemies.Count == 0) return null;

            switch (priority)
            {
                case TargetPriority.LowestHP:
                    return PickLowestHP(enemies);
                case TargetPriority.Furthest:
                    return PickByDistance(unit, enemies, prefer: TargetPriority.Furthest);
                case TargetPriority.BacklineFirst:
                    {
                        var t = PickBacklineFirst(unit, enemies);
                        return t ?? PickByDistance(unit, enemies, prefer: TargetPriority.Nearest);
                    }
                case TargetPriority.AttackerOfAlly:
                    {
                        var t = PickAttackerOfAlly(unit);
                        return t ?? PickByDistance(unit, enemies, prefer: TargetPriority.Nearest);
                    }
                case TargetPriority.Marked:
                    // Marking system not yet implemented — fall back to nearest.
                    return PickByDistance(unit, enemies, prefer: TargetPriority.Nearest);
                case TargetPriority.Nearest:
                default:
                    return PickByDistance(unit, enemies, prefer: TargetPriority.Nearest);
            }
        }

        // ── Selection strategies ─────────────────────────────────────

        private static TerrainBattleUnit PickLowestHP(List<TerrainBattleUnit> enemies)
        {
            TerrainBattleUnit best = null;
            int bestHP = int.MaxValue;
            foreach (var e in enemies)
            {
                if (e.IsDead || e.Unit == null) continue;
                if (e.Unit.currentHP < bestHP)
                {
                    bestHP = e.Unit.currentHP;
                    best   = e;
                }
            }
            return best;
        }

        private static TerrainBattleUnit PickByDistance(TerrainBattleUnit unit,
            List<TerrainBattleUnit> enemies, TargetPriority prefer)
        {
            TerrainBattleUnit best = null;
            float metric = prefer == TargetPriority.Furthest ? -1f : float.MaxValue;
            foreach (var e in enemies)
            {
                if (e.IsDead) continue;
                float d = Vector3.Distance(unit.transform.position, e.transform.position);
                bool wins = prefer == TargetPriority.Furthest ? d > metric : d < metric;
                if (wins) { metric = d; best = e; }
            }
            return best;
        }

        private static TerrainBattleUnit PickBacklineFirst(TerrainBattleUnit unit,
            List<TerrainBattleUnit> enemies)
        {
            // "Backline" = the enemy unit currently in `Backline` state, or
            // failing that, the one furthest from this unit (proxy).
            foreach (var e in enemies)
            {
                if (e.IsDead) continue;
                if (e.CombatState == UnitCombatState.Backline) return e;
            }
            return null;
        }

        private TerrainBattleUnit PickAttackerOfAlly(TerrainBattleUnit unit)
        {
            // Find an ally currently in Recover/Defender role and target whoever
            // is attacking them.
            var allies = unit.Unit.team == UnitTeam.Player ? _playerUnits : _enemyUnits;
            foreach (var ally in allies)
            {
                if (ally == unit || ally.IsDead) continue;
                if (ally.CombatRole == CombatRole.Defender && ally.CurrentTarget != null
                    && !ally.CurrentTarget.IsDead)
                {
                    return ally.CurrentTarget;
                }
            }
            return null;
        }
    }
}
