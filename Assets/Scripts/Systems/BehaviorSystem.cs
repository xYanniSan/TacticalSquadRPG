using System.Collections.Generic;
using TacticalRPG.DataModels;

namespace TacticalRPG.Systems
{
    public class BehaviorSystem
    {
        private const int BasicAttackRange = 1; // Manhattan distance (adjacent = 1)

        private readonly MovementSystem _movement = new MovementSystem();

        // Generates an intent for every living unit and stores it on unit.currentIntent.
        public List<UnitIntent> GenerateAllIntents(BattleState state)
        {
            var intents = new List<UnitIntent>();
            var allLiving = new List<UnitRuntime>(state.GetLivingUnits(UnitTeam.Player));
            allLiving.AddRange(state.GetLivingUnits(UnitTeam.Enemy));
            foreach (UnitRuntime unit in allLiving)
            {
                UnitIntent intent = GenerateIntent(unit, state);
                unit.currentIntent = intent;
                intents.Add(intent);
            }
            return intents;
        }

        // Generates a single intent for one unit based on its BehaviorType.
        public UnitIntent GenerateIntent(UnitRuntime unit, BattleState state)
        {
            if (unit == null || unit.isDead)
                return MakeWait(unit);

            List<UnitRuntime> enemies = GetLivingEnemies(unit, state);
            if (enemies.Count == 0)
                return MakeWait(unit);

            switch (unit.behavior.behaviorType)
            {
                case BehaviorType.Aggressive: return GenerateAggressiveIntent(unit, state, enemies);
                case BehaviorType.Defensive:  return GenerateDefensiveIntent(unit, state, enemies);
                case BehaviorType.Balanced:   return GenerateBalancedIntent(unit, state, enemies);
                default:                      return MakeWait(unit);
            }
        }

        // ── Strategies ────────────────────────────────────────────────────

        // Aggressive: always close distance and attack. Prioritises nearest enemy.
        private UnitIntent GenerateAggressiveIntent(
            UnitRuntime unit, BattleState state, List<UnitRuntime> enemies)
        {
            UnitRuntime target = SelectNearestEnemy(unit, enemies);

            if (IsInAttackRange(unit, target))
                return MakeAttack(unit, target);

            GridPosition movePos = GetBestMovePositionToward(unit, target, state);
            if (!movePos.Equals(unit.position))
                return MakeMove(unit, target, movePos);

            return MakeWait(unit);
        }

        // Defensive: hold position. Only attacks enemies that are already adjacent.
        private UnitIntent GenerateDefensiveIntent(
            UnitRuntime unit, BattleState state, List<UnitRuntime> enemies)
        {
            UnitRuntime adjacentEnemy = GetAdjacentEnemy(unit, enemies);
            if (adjacentEnemy != null)
                return MakeAttack(unit, adjacentEnemy);

            return MakeWait(unit);
        }

        // Balanced: advance when out of range, attack when in range.
        // Picks lowest-HP enemy when multiple are reachable.
        private UnitIntent GenerateBalancedIntent(
            UnitRuntime unit, BattleState state, List<UnitRuntime> enemies)
        {
            UnitRuntime target = SelectLowestHPEnemy(enemies);

            if (IsInAttackRange(unit, target))
                return MakeAttack(unit, target);

            GridPosition movePos = GetBestMovePositionToward(unit, target, state);
            if (!movePos.Equals(unit.position))
                return MakeMove(unit, target, movePos);

            return MakeWait(unit);
        }

        // ── Target Selection ──────────────────────────────────────────────

        private List<UnitRuntime> GetLivingEnemies(UnitRuntime unit, BattleState state)
        {
            UnitTeam enemyTeam = unit.team == UnitTeam.Player ? UnitTeam.Enemy : UnitTeam.Player;
            return state.GetLivingUnits(enemyTeam);
        }

        private UnitRuntime SelectNearestEnemy(UnitRuntime unit, List<UnitRuntime> enemies)
        {
            UnitRuntime nearest = null;
            int minDist = int.MaxValue;

            foreach (UnitRuntime enemy in enemies)
            {
                int dist = unit.position.ManhattanDistanceTo(enemy.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private UnitRuntime SelectLowestHPEnemy(List<UnitRuntime> enemies)
        {
            UnitRuntime weakest = null;
            int lowestHP = int.MaxValue;

            foreach (UnitRuntime enemy in enemies)
            {
                if (enemy.currentHP < lowestHP)
                {
                    lowestHP = enemy.currentHP;
                    weakest = enemy;
                }
            }

            return weakest;
        }

        private UnitRuntime GetAdjacentEnemy(UnitRuntime unit, List<UnitRuntime> enemies)
        {
            foreach (UnitRuntime enemy in enemies)
            {
                if (unit.position.IsAdjacent(enemy.position))
                    return enemy;
            }
            return null;
        }

        // ── Movement Helpers ──────────────────────────────────────────────

        // Returns the next single tile on the path toward the target (1-step movement).
        // Returns the unit's current position if no path exists.
        private GridPosition GetBestMovePositionToward(
            UnitRuntime unit, UnitRuntime target, BattleState state)
        {
            return _movement.GetNextStepToward(state.grid, unit.position, target.position);
        }

        private bool IsInAttackRange(UnitRuntime unit, UnitRuntime target)
        {
            return unit.position.ManhattanDistanceTo(target.position) <= BasicAttackRange;
        }

        // ── Intent Factories ──────────────────────────────────────────────

        private UnitIntent MakeWait(UnitRuntime unit) =>
            new UnitIntent { actor = unit, type = IntentType.Wait };

        private UnitIntent MakeAttack(UnitRuntime unit, UnitRuntime target) =>
            new UnitIntent { actor = unit, type = IntentType.BasicAttack, target = target };

        private UnitIntent MakeMove(UnitRuntime unit, UnitRuntime target, GridPosition pos) =>
            new UnitIntent
            {
                actor          = unit,
                type           = IntentType.Move,
                target         = target,
                targetPosition = pos
            };
    }
}
