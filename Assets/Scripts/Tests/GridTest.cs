using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.Tests
{
    public class GridTest : MonoBehaviour
    {
        private void Start()
        {
            RunAllTests();
        }

        private void RunAllTests()
        {
            Debug.Log("===== GRID SYSTEM TEST START =====");

            Test_GridInitialization();
            Test_BoundsChecking();
            Test_DistanceCalculations();
            Test_NeighborQueries();
            Test_UnitPlacement();
            Test_UnitMovement();
            Test_RangeQuery();

            Debug.Log("===== GRID SYSTEM TEST COMPLETE =====");
        }

        private void Test_GridInitialization()
        {
            GridSystem grid = new GridSystem(8, 6);
            Debug.Assert(grid.Width == 8, "Width should be 8");
            Debug.Assert(grid.Height == 6, "Height should be 6");
            Debug.Assert(grid.IsWalkable(new GridPosition(0, 0)), "All tiles should start walkable");
            Debug.Log("  [PASS] Grid initialization");
        }

        private void Test_BoundsChecking()
        {
            GridSystem grid = new GridSystem(8, 6);
            Debug.Assert(grid.IsInBounds(new GridPosition(0, 0)),   "(0,0) should be in bounds");
            Debug.Assert(grid.IsInBounds(new GridPosition(7, 5)),   "(7,5) should be in bounds");
            Debug.Assert(!grid.IsInBounds(new GridPosition(-1, 0)), "(-1,0) should be out of bounds");
            Debug.Assert(!grid.IsInBounds(new GridPosition(8, 0)),  "(8,0) should be out of bounds");
            Debug.Assert(!grid.IsInBounds(new GridPosition(0, 6)),  "(0,6) should be out of bounds");
            Debug.Log("  [PASS] Bounds checking");
        }

        private void Test_DistanceCalculations()
        {
            GridPosition a = new GridPosition(0, 0);
            GridPosition b = new GridPosition(3, 4);
            Debug.Assert(a.ManhattanDistanceTo(b) == 7,                      "Manhattan distance (0,0)->(3,4) should be 7");
            Debug.Assert(a.IsAdjacent(new GridPosition(1, 0)),               "(0,0) and (1,0) should be adjacent");
            Debug.Assert(a.IsAdjacent(new GridPosition(0, 1)),               "(0,0) and (0,1) should be adjacent");
            Debug.Assert(!a.IsAdjacent(new GridPosition(1, 1)),              "Diagonal should NOT be adjacent");
            Debug.Assert(!a.IsAdjacent(new GridPosition(2, 0)),              "(0,0) and (2,0) should NOT be adjacent");
            Debug.Assert(new GridPosition(2, 2) == new GridPosition(2, 2),   "Equal positions should be equal");
            Debug.Assert(new GridPosition(1, 0) != new GridPosition(0, 1),   "Different positions should not be equal");
            Debug.Log("  [PASS] Distance calculations");
        }

        private void Test_NeighborQueries()
        {
            GridSystem grid = new GridSystem(8, 6);
            List<GridPosition> centerNeighbors = grid.GetNeighbors(new GridPosition(3, 3));
            Debug.Assert(centerNeighbors.Count == 4, "Center tile should have 4 neighbors");

            List<GridPosition> cornerNeighbors = grid.GetNeighbors(new GridPosition(0, 0));
            Debug.Assert(cornerNeighbors.Count == 2, "Corner tile should have 2 neighbors");

            List<GridPosition> edgeNeighbors = grid.GetNeighbors(new GridPosition(0, 3));
            Debug.Assert(edgeNeighbors.Count == 3, "Edge tile should have 3 neighbors");
            Debug.Log("  [PASS] Neighbor queries");
        }

        private void Test_UnitPlacement()
        {
            GridSystem grid = new GridSystem(8, 6);
            UnitRuntime unit = new UnitRuntime { runtimeId = 1, team = UnitTeam.Player };

            bool placed = grid.PlaceUnit(unit, new GridPosition(2, 2));
            Debug.Assert(placed,                                               "Unit should place successfully");
            Debug.Assert(grid.IsOccupied(new GridPosition(2, 2)),             "Tile (2,2) should be occupied");
            Debug.Assert(grid.GetUnitAt(new GridPosition(2, 2)) == unit,      "GetUnitAt should return correct unit");
            Debug.Assert(unit.position == new GridPosition(2, 2),             "Unit position should be updated");

            bool placedAgain = grid.PlaceUnit(new UnitRuntime(), new GridPosition(2, 2));
            Debug.Assert(!placedAgain, "Should not place two units on same tile");

            grid.RemoveUnit(new GridPosition(2, 2));
            Debug.Assert(!grid.IsOccupied(new GridPosition(2, 2)), "Tile should be empty after removal");
            Debug.Log("  [PASS] Unit placement");
        }

        private void Test_UnitMovement()
        {
            GridSystem grid = new GridSystem(8, 6);
            UnitRuntime unit = new UnitRuntime { runtimeId = 2, team = UnitTeam.Enemy };

            grid.PlaceUnit(unit, new GridPosition(1, 1));
            bool moved = grid.MoveUnit(new GridPosition(1, 1), new GridPosition(1, 2));
            Debug.Assert(moved,                                              "Unit should move successfully");
            Debug.Assert(!grid.IsOccupied(new GridPosition(1, 1)),          "Old tile should be empty");
            Debug.Assert(grid.IsOccupied(new GridPosition(1, 2)),           "New tile should be occupied");
            Debug.Assert(unit.position == new GridPosition(1, 2),           "Unit position should update after move");

            bool movedOutOfBounds = grid.MoveUnit(new GridPosition(1, 2), new GridPosition(-1, 0));
            Debug.Assert(!movedOutOfBounds, "Should not move out of bounds");
            Debug.Log("  [PASS] Unit movement");
        }

        private void Test_RangeQuery()
        {
            GridSystem grid = new GridSystem(8, 6);
            List<GridPosition> range1 = grid.GetPositionsInRange(new GridPosition(3, 3), 1);
            Debug.Assert(range1.Count == 5, "Range 1 from center should return 5 tiles (center + 4 neighbors)");

            List<GridPosition> range2 = grid.GetPositionsInRange(new GridPosition(3, 3), 2);
            Debug.Assert(range2.Count == 13, "Range 2 from center should return 13 tiles");
            Debug.Log("  [PASS] Range query");
        }
    }
}
