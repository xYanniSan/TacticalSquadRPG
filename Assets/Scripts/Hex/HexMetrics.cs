using UnityEngine;

namespace TacticalRPG.Hex
{
    public static class HexMetrics
    {
        // Outer radius: center to vertex
        public const float OuterRadius = 0.5f;

        // Inner radius: center to edge midpoint
        public static readonly float InnerRadius = OuterRadius * Mathf.Sqrt(3f) / 2f;

        // Spacing between hex centers
        public static readonly float HorizontalSpacing = Mathf.Sqrt(3f) * OuterRadius;
        public static readonly float VerticalSpacing = 1.5f * OuterRadius;

        // 6 corner offsets for a pointy-top hex (relative to center, at y=0)
        private static Vector3[] _corners;

        public static Vector3[] Corners
        {
            get
            {
                if (_corners == null)
                {
                    _corners = new Vector3[6];
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = (60f * i - 30f) * Mathf.Deg2Rad;
                        _corners[i] = new Vector3(
                            OuterRadius * Mathf.Cos(angle),
                            0f,
                            OuterRadius * Mathf.Sin(angle));
                    }
                }
                return _corners;
            }
        }

        // Offset (col, row) → world position (even-r, pointy-top)
        public static Vector3 OffsetToWorld(int col, int row)
        {
            float x = (col + 0.5f * (row & 1)) * HorizontalSpacing;
            float z = row * VerticalSpacing;
            return new Vector3(x, 0f, z);
        }

        // World position → nearest offset (col, row)
        public static void WorldToOffset(Vector3 world, out int col, out int row)
        {
            // Approximate row from z
            row = Mathf.RoundToInt(world.z / VerticalSpacing);

            // Account for odd-row shift when computing col
            float offset = 0.5f * (row & 1);
            col = Mathf.RoundToInt(world.x / HorizontalSpacing - offset);

            // Check this cell and its neighbors, pick the closest center
            int bestCol = col;
            int bestRow = row;
            float bestDist = float.MaxValue;

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    int testCol = col + dc;
                    int testRow = row + dr;
                    Vector3 center = OffsetToWorld(testCol, testRow);
                    float dist = (new Vector3(world.x, 0f, world.z) - center).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestCol = testCol;
                        bestRow = testRow;
                    }
                }
            }

            col = bestCol;
            row = bestRow;
        }

        // Even-r neighbor offsets: [direction][col_offset, row_offset]
        private static readonly int[,] EvenRowOffsets =
        {
            { -1, -1 }, {  0, -1 },
            { -1,  0 }, {  1,  0 },
            { -1,  1 }, {  0,  1 }
        };

        private static readonly int[,] OddRowOffsets =
        {
            {  0, -1 }, {  1, -1 },
            { -1,  0 }, {  1,  0 },
            {  0,  1 }, {  1,  1 }
        };

        // Returns the 6 neighbor (col, row) pairs for a given cell
        public static void GetNeighbor(int col, int row, int direction, out int nCol, out int nRow)
        {
            var offsets = (row & 1) == 0 ? EvenRowOffsets : OddRowOffsets;
            nCol = col + offsets[direction, 0];
            nRow = row + offsets[direction, 1];
        }
    }
}
