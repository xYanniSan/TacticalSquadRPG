using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.Tests
{
    public class MovementSystemTest : MonoBehaviour
    {
        private void Start()
        {
            RunAllTests();
        }

        private void RunAllTests()
        {
            Debug.Log("===== MOVEMENT SYSTEM TEST START =====");

            Test_FindPath_StraightLine();
            Test_FindPath_AroundObstacle();
            Test_FindPath_BlockedReturnsNull();
            Test_FindPath_SameStartAndEnd();
            Test_GetReachablePositions();
            Test_CanReach_WithinRange();
            Test_CanReach_OutOfRange();
            Test_MoveUnit_UpdatesGrid();
            Test_MoveUnit_DeadUnitCannotMove();

            Debug.Log("===== MOVEMENT SYSTEM TEST COMPLETE =====");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private (GridMap grid, MovementSystem movement) MakeGrid(int w = 8, int h = 6)
        {
            return (new GridMap(w, h), new MovementSystem());
        }

        private UnitRuntime MakeUnit(int moveSpeed = 3)
        {
            UnitFactory.ResetIds();
            UnitDefinition def = ScriptableObject.CreateInstance<UnitDefinition>();
            def.unitId      = "test_unit";
            def.displayName = "Test Unit";
            def.baseStats   = new StatBlock(100, 10, 5, moveSpeed);
            def.defaultBehavior = BehaviorType.Balanced;

            UnitRuntime unit = UnitFactory.CreateFromDefinition(def, UnitTeam.Player);
            Object.Destroy(def);
            return unit;
        }

        // ── Tests ─────────────────────────────────────────────────────────

        private void Test_FindPath_StraightLine()
        {
            var (grid, movement) = MakeGrid();
            GridPosition start = new GridPosition(0, 0);
            GridPosition end   = new GridPosition(3, 0);

            List<GridPosition> path = movement.FindPath(grid, start, end);

            Debug.Assert(path != null,          "Path should exist");
            Debug.Assert(path.Count == 4,       "Path should have 4 positions (0,0)-(1,0)-(2,0)-(3,0)");
            Debug.Assert(path[0].Equals(start), "Path should start at origin");
            Debug.Assert(path[path.Count - 1].Equals(end), "Path should end at destination");
            Debug.Log("  [PASS] FindPath - straight line");
        }

        private void Test_FindPath_AroundObstacle()
        {
            var (grid, movement) = MakeGrid();

            // Block the direct path: column x=1 is unwalkable for y=0,1,2
            grid.GetTile(new GridPosition(1, 0)).isWalkable = false;
            grid.GetTile(new GridPosition(1, 1)).isWalkable = false;
            grid.GetTile(new GridPosition(1, 2)).isWalkable = false;

            GridPosition start = new GridPosition(0, 0);
            GridPosition end   = new GridPosition(2, 0);

            List<GridPosition> path = movement.FindPath(grid, start, end);

            Debug.Assert(path != null,    "Path should exist around obstacle");
            Debug.Assert(path.Count > 3,  "Path should be longer than direct route");
            Debug.Assert(path[0].Equals(start),             "Path should start correctly");
            Debug.Assert(path[path.Count - 1].Equals(end),  "Path should end correctly");
            Debug.Log("  [PASS] FindPath - around obstacle");
        }

        private void Test_FindPath_BlockedReturnsNull()
        {
            var (grid, movement) = MakeGrid(3, 3);

            // Surround (1,1) completely
            grid.GetTile(new GridPosition(1, 0)).isWalkable = false;
            grid.GetTile(new GridPosition(1, 2)).isWalkable = false;
            grid.GetTile(new GridPosition(0, 1)).isWalkable = false;
            grid.GetTile(new GridPosition(2, 1)).isWalkable = false;

            GridPosition start = new GridPosition(0, 0);
            GridPosition end   = new GridPosition(1, 1);

            List<GridPosition> path = movement.FindPath(grid, start, end);

            Debug.Assert(path == null, "Path should be null when destination is unreachable");
            Debug.Log("  [PASS] FindPath - blocked returns null");
        }

        private void Test_FindPath_SameStartAndEnd()
        {
            var (grid, movement) = MakeGrid();
            GridPosition pos = new GridPosition(2, 2);

            List<GridPosition> path = movement.FindPath(grid, pos, pos);

            Debug.Assert(path != null,      "Path should exist when start equals end");
            Debug.Assert(path.Count == 1,   "Path should have exactly 1 position");
            Debug.Assert(path[0].Equals(pos), "Path should contain only the start/end position");
            Debug.Log("  [PASS] FindPath - same start and end");
        }

        private void Test_GetReachablePositions()
        {
            var (grid, movement) = MakeGrid();
            GridPosition start = new GridPosition(3, 3);
            int range = 2;

            List<GridPosition> reachable = movement.GetReachablePositions(grid, start, range);

            // Manhattan distance <= 2 from (3,3), excluding start itself
            Debug.Assert(reachable.Count == 12, $"Should have 12 reachable positions (got {reachable.Count})");
            Debug.Assert(!reachable.Contains(start), "Start position should not be in reachable list");

            // Spot check: (3,5) is 2 steps away, should be reachable
            Debug.Assert(reachable.Contains(new GridPosition(3, 5)), "(3,5) should be reachable");
            // (3,6) is 3 steps away, should not be reachable
            Debug.Assert(!reachable.Contains(new GridPosition(3, 6)), "(3,6) should not be reachable");
            Debug.Log("  [PASS] GetReachablePositions");
        }

        private void Test_CanReach_WithinRange()
        {
            var (grid, movement) = MakeGrid();
            GridPosition start = new GridPosition(0, 0);
            GridPosition end   = new GridPosition(3, 0);

            Debug.Assert(movement.CanReach(grid, start, end, 3), "Should reach (3,0) with range 3");
            Debug.Assert(movement.CanReach(grid, start, end, 5), "Should reach (3,0) with range 5");
            Debug.Log("  [PASS] CanReach - within range");
        }

        private void Test_CanReach_OutOfRange()
        {
            var (grid, movement) = MakeGrid();
            GridPosition start = new GridPosition(0, 0);
            GridPosition end   = new GridPosition(5, 0);

            Debug.Assert(!movement.CanReach(grid, start, end, 3), "Should not reach (5,0) with range 3");
            Debug.Log("  [PASS] CanReach - out of range");
        }

        private void Test_MoveUnit_UpdatesGrid()
        {
            var (grid, movement) = MakeGrid();
            UnitRuntime unit = MakeUnit(moveSpeed: 3);
            GridPosition start  = new GridPosition(0, 0);
            GridPosition target = new GridPosition(2, 0);

            grid.PlaceUnit(unit, start);

            BattleState state = new BattleState("test", 8, 6);
            state.playerUnits.Add(unit);
            // Replace state's grid with our test grid
            state.grid = grid;

            bool moved = movement.MoveUnit(state, unit, target);

            Debug.Assert(moved,                               "MoveUnit should return true");
            Debug.Assert(unit.position.Equals(target),        "Unit position should update to target");
            Debug.Assert(grid.IsOccupied(target),             "Target tile should be occupied");
            Debug.Assert(!grid.IsOccupied(start),             "Start tile should be vacated");
            Debug.Assert(grid.GetUnitAt(target) == unit,      "Grid should reference the unit at target");
            Debug.Log("  [PASS] MoveUnit - grid updates correctly");
        }

        private void Test_MoveUnit_DeadUnitCannotMove()
        {
            var (grid, movement) = MakeGrid();
            UnitRuntime unit = MakeUnit(moveSpeed: 3);
            GridPosition start  = new GridPosition(0, 0);
            GridPosition target = new GridPosition(1, 0);

            grid.PlaceUnit(unit, start);
            unit.isDead = true;

            BattleState state = new BattleState("test", 8, 6);
            state.playerUnits.Add(unit);
            state.grid = grid;

            bool moved = movement.MoveUnit(state, unit, target);

            Debug.Assert(!moved,                          "Dead unit should not be able to move");
            Debug.Assert(unit.position.Equals(start),     "Dead unit position should not change");
            Debug.Assert(grid.IsOccupied(start),          "Original tile should still be occupied");
            Debug.Log("  [PASS] MoveUnit - dead unit cannot move");
        }
    }
}
