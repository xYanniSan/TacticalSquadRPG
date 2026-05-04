using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Visual
{
    public class GridVisualizer : MonoBehaviour
    {
        private static readonly Color LightTile = new Color(0.78f, 0.82f, 0.70f);
        private static readonly Color DarkTile  = new Color(0.58f, 0.62f, 0.50f);

        private int _width;
        private int _height;

        public void CreateGrid(GridMap grid)
        {
            _width  = grid.width;
            _height = grid.height;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.parent     = transform;
                    tile.transform.localScale = new Vector3(0.95f, 0.1f, 0.95f);
                    tile.transform.position   = GridToWorld(new GridPosition(x, y));

                    bool isLight = (x + y) % 2 == 0;
                    tile.GetComponent<Renderer>().material.color = isLight ? LightTile : DarkTile;

                    Destroy(tile.GetComponent<Collider>());
                }
            }
        }

        // Converts a GridPosition to a world-space Vector3 (x -> x, y -> z).
        public static Vector3 GridToWorld(GridPosition pos)
        {
            return new Vector3(pos.x, 0f, pos.y);
        }

        public Vector3 GetGridCenter()
        {
            return new Vector3((_width - 1) * 0.5f, 0f, (_height - 1) * 0.5f);
        }
    }
}
