using System.Collections.Generic;
using UnityEngine;

namespace TacticalRPG.DataModels
{
    public class GridMap
    {
        public int width;
        public int height;
        private GridTile[,] tiles;

        public GridMap(int width, int height)
        {
            this.width = width;
            this.height = height;
            tiles = new GridTile[width, height];
            InitializeTiles();
        }

        private void InitializeTiles()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[x, y] = new GridTile(new GridPosition(x, y), isWalkable: true);
                }
            }
        }

        public bool IsInBounds(GridPosition pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        public GridTile GetTile(GridPosition pos)
        {
            if (!IsInBounds(pos))
            {
                Debug.LogWarning($"GridMap.GetTile: Position {pos} is out of bounds.");
                return null;
            }
            return tiles[pos.x, pos.y];
        }

        public bool IsWalkable(GridPosition pos)
        {
            GridTile tile = GetTile(pos);
            return tile != null && tile.isWalkable;
        }

        public bool IsOccupied(GridPosition pos)
        {
            GridTile tile = GetTile(pos);
            return tile != null && tile.occupyingUnit != null;
        }

        public UnitRuntime GetUnitAt(GridPosition pos)
        {
            GridTile tile = GetTile(pos);
            return tile?.occupyingUnit;
        }

        public bool PlaceUnit(UnitRuntime unit, GridPosition pos)
        {
            if (!IsInBounds(pos) || IsOccupied(pos) || !IsWalkable(pos))
                return false;

            tiles[pos.x, pos.y].occupyingUnit = unit;
            unit.position = pos;
            return true;
        }

        public bool MoveUnit(GridPosition from, GridPosition to)
        {
            if (!IsInBounds(from) || !IsInBounds(to)) return false;
            if (!IsWalkable(to) || IsOccupied(to)) return false;

            UnitRuntime unit = GetUnitAt(from);
            if (unit == null) return false;

            tiles[from.x, from.y].occupyingUnit = null;
            tiles[to.x, to.y].occupyingUnit = unit;
            unit.position = to;
            return true;
        }

        public void RemoveUnit(GridPosition pos)
        {
            GridTile tile = GetTile(pos);
            if (tile != null)
                tile.occupyingUnit = null;
        }

        public List<GridPosition> GetNeighbors(GridPosition pos)
        {
            List<GridPosition> neighbors = new List<GridPosition>();
            GridPosition[] directions =
            {
                new GridPosition(0,  1),
                new GridPosition(0, -1),
                new GridPosition( 1, 0),
                new GridPosition(-1, 0)
            };

            foreach (var dir in directions)
            {
                GridPosition neighbor = new GridPosition(pos.x + dir.x, pos.y + dir.y);
                if (IsInBounds(neighbor))
                    neighbors.Add(neighbor);
            }

            return neighbors;
        }

        public List<GridPosition> GetWalkableNeighbors(GridPosition pos)
        {
            List<GridPosition> neighbors = GetNeighbors(pos);
            neighbors.RemoveAll(p => !IsWalkable(p));
            return neighbors;
        }
    }
}
