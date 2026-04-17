using System.Collections.Generic;
using UnityEngine;

namespace TacticalRPG.Hex
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour
    {
        private List<Vector3> _vertices;
        private List<int> _triangles;
        private List<Color> _colors;
        private Mesh _mesh;

        public void Initialize()
        {
            _mesh = new Mesh();
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            GetComponent<MeshFilter>().mesh = _mesh;

            // Use a vertex-color shader so each hex gets its own color
            var renderer = GetComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public void BuildMesh(HexCell[,] cells, int cols, int rows)
        {
            int cellCount = cols * rows;
            _vertices  = new List<Vector3>(cellCount * 7);
            _triangles = new List<int>(cellCount * 18);
            _colors    = new List<Color>(cellCount * 7);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    AddHex(cells[c, r]);
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.SetColors(_colors);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        // Rebuild a single hex's colors without rebuilding the entire mesh
        public void UpdateCellColor(int col, int row, int cols, Color color)
        {
            int cellIndex = row * cols + col;
            int vertStart = cellIndex * 7;

            var colorArray = new List<Color>();
            _mesh.GetColors(colorArray);

            if (vertStart + 7 > colorArray.Count) return;

            for (int i = 0; i < 7; i++)
            {
                // Darken corners slightly for a subtle edge effect
                Color c = (i == 0) ? color : Color.Lerp(color, Color.black, 0.08f);
                colorArray[vertStart + i] = c;
            }

            _mesh.SetColors(colorArray);
        }

        private void AddHex(HexCell cell)
        {
            Vector3 center = cell.worldPosition;
            int vertIndex = _vertices.Count;

            // Center vertex
            _vertices.Add(center);
            _colors.Add(cell.color);

            // 6 corner vertices
            Vector3[] corners = HexMetrics.Corners;
            for (int i = 0; i < 6; i++)
            {
                _vertices.Add(center + corners[i]);
                // Darken corners slightly for subtle hex outlines
                _colors.Add(Color.Lerp(cell.color, Color.black, 0.08f));
            }

            // 6 triangles (center → corner i → corner i+1)
            for (int i = 0; i < 6; i++)
            {
                _triangles.Add(vertIndex);            // center
                _triangles.Add(vertIndex + 1 + i);    // corner i
                _triangles.Add(vertIndex + 1 + (i + 1) % 6); // corner i+1
            }
        }
    }
}
