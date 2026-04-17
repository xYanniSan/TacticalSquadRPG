using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Systems
{
    public class GridSystem
    {
        private GridMap _grid;

        public int Width => _grid.width;
        public int Height => _grid.height;

        public GridSystem(int width, int height)
        {
            _grid = new GridMap(width, height);
            Debug.Log($"Grid initialized: {width}x{height}");
        }

        public GridMap GetGridMap() => _grid;

        public bool IsInBounds(GridPosition pos) => _grid.IsInBounds(pos);
        public bool IsWalkable(GridPosition pos) => _grid.IsWalkable(pos);
        public bool IsOccupied(GridPosition pos) => _grid.IsOccupied(pos);
        public GridTile GetTile(GridPosition pos) => _grid.GetTile(pos);
        public UnitRuntime GetUnitAt(GridPosition pos) => _grid.GetUnitAt(pos);
        public List<GridPosition> GetNeighbors(GridPosition pos) => _grid.GetNeighbors(pos);
        public List<GridPosition> GetWalkableNeighbors(GridPosition pos) => _grid.GetWalkableNeighbors(pos);

        public bool PlaceUnit(UnitRuntime unit, GridPosition pos) => _grid.PlaceUnit(unit, pos);
        public bool MoveUnit(GridPosition from, GridPosition to) => _grid.MoveUnit(from, to);
        public void RemoveUnit(GridPosition pos) => _grid.RemoveUnit(pos);

        public List<GridPosition> GetPositionsInRange(GridPosition center, int range)
        {
            List<GridPosition> positions = new List<GridPosition>();
            for (int x = center.x - range; x <= center.x + range; x++)
            {
                for (int y = center.y - range; y <= center.y + range; y++)
                {
                    GridPosition pos = new GridPosition(x, y);
                    if (_grid.IsInBounds(pos) && center.ManhattanDistanceTo(pos) <= range)
                        positions.Add(pos);
                }
            }
            return positions;
        }
    }
}
