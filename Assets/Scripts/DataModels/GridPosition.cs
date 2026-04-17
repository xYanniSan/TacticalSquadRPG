using System;
using UnityEngine;

namespace TacticalRPG.DataModels
{
    [Serializable]
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int x;
        public int y;

        public GridPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int ManhattanDistanceTo(GridPosition other)
        {
            return Mathf.Abs(x - other.x) + Mathf.Abs(y - other.y);
        }

        public float EuclideanDistanceTo(GridPosition other)
        {
            int dx = x - other.x;
            int dy = y - other.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public float DistanceTo(GridPosition other)
        {
            return ManhattanDistanceTo(other);
        }

        public bool IsAdjacent(GridPosition other)
        {
            return ManhattanDistanceTo(other) == 1;
        }

        public bool Equals(GridPosition other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);
        public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);

        public static GridPosition Zero => new GridPosition(0, 0);

        public override string ToString() => $"({x}, {y})";
    }
}
