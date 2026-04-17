using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Hex
{
    public class HexCell
    {
        public int col;
        public int row;
        public Vector3 worldPosition;
        public Color color;
        public bool isWalkable = true;
        public UnitRuntime occupyingUnit;

        public HexCell(int col, int row)
        {
            this.col = col;
            this.row = row;
            this.worldPosition = HexMetrics.OffsetToWorld(col, row);
            this.color = GenerateGrassColor();
        }

        private Color GenerateGrassColor()
        {
            // Base grass green with per-hex random variation
            float r = 0.35f + Random.Range(-0.05f, 0.05f);
            float g = 0.65f + Random.Range(-0.08f, 0.08f);
            float b = 0.25f + Random.Range(-0.04f, 0.04f);
            return new Color(r, g, b);
        }
    }
}
