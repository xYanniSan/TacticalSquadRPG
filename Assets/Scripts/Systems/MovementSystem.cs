using System.Collections.Generic;
using TacticalRPG.DataModels;

namespace TacticalRPG.Systems
{
    public class MovementSystem
    {
        // Returns the shortest walkable path from start to end, excluding occupied tiles
        // (except the destination, which the moving unit will occupy).
        // Returns null if no path exists.
        public List<GridPosition> FindPath(GridMap grid, GridPosition start, GridPosition end)
        {
            if (!grid.IsInBounds(start) || !grid.IsInBounds(end))
                return null;

            if (!grid.IsWalkable(end))
                return null;

            if (start.Equals(end))
                return new List<GridPosition> { start };

            // BFS
            var visited  = new HashSet<GridPosition>();
            var parent   = new Dictionary<GridPosition, GridPosition>();
            var queue    = new Queue<GridPosition>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                GridPosition current = queue.Dequeue();

                if (current.Equals(end))
                    return ReconstructPath(parent, start, end);

                foreach (GridPosition neighbor in grid.GetNeighbors(current))
                {
                    if (visited.Contains(neighbor))
                        continue;

                    if (!grid.IsWalkable(neighbor))
                        continue;

                    // Allow passing through the destination even if occupied,
                    // but block movement through other occupied tiles
                    if (grid.IsOccupied(neighbor) && !neighbor.Equals(end))
                        continue;

                    visited.Add(neighbor);
                    parent[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return null; // no path found
        }

        // Returns all positions reachable from start within moveRange steps (BFS flood fill).
        // Does not include the start position itself.
        // Blocked by unwalkable tiles and tiles occupied by other units.
        public List<GridPosition> GetReachablePositions(GridMap grid, GridPosition start, int moveRange)
        {
            var reachable = new List<GridPosition>();
            var visited   = new Dictionary<GridPosition, int>(); // position -> steps used
            var queue     = new Queue<GridPosition>();

            queue.Enqueue(start);
            visited[start] = 0;

            while (queue.Count > 0)
            {
                GridPosition current = queue.Dequeue();
                int steps = visited[current];

                if (steps >= moveRange)
                    continue;

                foreach (GridPosition neighbor in grid.GetNeighbors(current))
                {
                    if (visited.ContainsKey(neighbor))
                        continue;

                    if (!grid.IsWalkable(neighbor))
                        continue;

                    if (grid.IsOccupied(neighbor))
                        continue;

                    visited[neighbor] = steps + 1;
                    reachable.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            return reachable;
        }

        // Returns true if a unit can reach the target position within its move range.
        public bool CanReach(GridMap grid, GridPosition start, GridPosition end, int moveRange)
        {
            List<GridPosition> path = FindPath(grid, start, end);
            if (path == null)
                return false;

            int steps = path.Count - 1; // start is index 0, so steps = length - 1
            return steps <= moveRange;
        }

        // Moves a unit to the target position, updating the grid.
        // Returns true if the move succeeded.
        public bool MoveUnit(BattleState state, UnitRuntime unit, GridPosition targetPosition)
        {
            if (unit == null || unit.isDead)
                return false;

            int moveRange = (int)unit.currentStats.moveSpeed;

            if (!CanReach(state.grid, unit.position, targetPosition, moveRange))
                return false;

            return state.grid.MoveUnit(unit.position, targetPosition);
        }

        // Returns the first step on the path from 'from' toward 'toward'.
        // Returns 'from' if no valid step exists.
        public GridPosition GetNextStepToward(GridMap grid, GridPosition from, GridPosition toward)
        {
            List<GridPosition> path = FindPath(grid, from, toward);
            if (path == null || path.Count < 2)
                return from;

            GridPosition nextStep = path[1];
            if (grid.IsOccupied(nextStep))
                return from;

            return nextStep;
        }

        // ── Private Helpers ───────────────────────────────────────────────

        private List<GridPosition> ReconstructPath(
            Dictionary<GridPosition, GridPosition> parent,
            GridPosition start,
            GridPosition end)
        {
            var path = new List<GridPosition>();
            GridPosition current = end;

            while (!current.Equals(start))
            {
                path.Add(current);
                current = parent[current];
            }

            path.Add(start);
            path.Reverse();
            return path;
        }
    }
}
