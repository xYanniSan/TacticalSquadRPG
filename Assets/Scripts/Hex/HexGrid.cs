using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Hex
{
    public class HexGrid : MonoBehaviour
    {
        [Header("Grid Size")]
        [SerializeField] private int cols = 70;
        [SerializeField] private int rows = 60;

        private HexCell[,] _cells;
        private HexMesh _hexMesh;
        private bool _initialized;

        public int Cols => cols;
        public int Rows => rows;

        public HexCell GetCell(int col, int row)
        {
            if (col < 0 || col >= cols || row < 0 || row >= rows)
                return null;
            return _cells[col, row];
        }

        public HexCell GetCellAtWorld(Vector3 worldPos)
        {
            HexMetrics.WorldToOffset(worldPos, out int col, out int row);
            return GetCell(col, row);
        }

        private void Start()
        {
            if (!_initialized)
                Initialize(cols, rows);
        }

        public void Initialize(int newCols, int newRows)
        {
            cols = newCols;
            rows = newRows;
            CreateCells();
            CreateMesh();
            SetupCamera();
            _initialized = true;
        }

        private void CreateCells()
        {
            _cells = new HexCell[cols, rows];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    _cells[c, r] = new HexCell(c, r);
                }
            }

            AddTopologyVariation();
        }

        private void AddTopologyVariation()
        {
            // Scatter some darker patches (dirt/mud)
            int dirtPatches = (cols * rows) / 50;
            for (int i = 0; i < dirtPatches; i++)
            {
                int centerCol = Random.Range(2, cols - 2);
                int centerRow = Random.Range(2, rows - 2);
                int radius = Random.Range(1, 4);

                Color dirtBase = new Color(
                    0.45f + Random.Range(-0.03f, 0.03f),
                    0.40f + Random.Range(-0.03f, 0.03f),
                    0.25f + Random.Range(-0.02f, 0.02f));

                PaintPatch(centerCol, centerRow, radius, dirtBase);
            }

            // Scatter some lighter grass patches
            int lightPatches = (cols * rows) / 80;
            for (int i = 0; i < lightPatches; i++)
            {
                int centerCol = Random.Range(1, cols - 1);
                int centerRow = Random.Range(1, rows - 1);
                int radius = Random.Range(1, 3);

                Color lightGrass = new Color(
                    0.40f + Random.Range(-0.03f, 0.03f),
                    0.75f + Random.Range(-0.05f, 0.05f),
                    0.30f + Random.Range(-0.03f, 0.03f));

                PaintPatch(centerCol, centerRow, radius, lightGrass);
            }
        }

        private void PaintPatch(int centerCol, int centerRow, int radius, Color baseColor)
        {
            for (int dr = -radius; dr <= radius; dr++)
            {
                for (int dc = -radius; dc <= radius; dc++)
                {
                    int c = centerCol + dc;
                    int r = centerRow + dr;
                    if (c < 0 || c >= cols || r < 0 || r >= rows)
                        continue;

                    // Rough circular shape
                    float dist = Mathf.Sqrt(dc * dc + dr * dr);
                    if (dist > radius + 0.5f) continue;

                    // Blend: cells at edge keep more of their original color
                    float blend = 1f - (dist / (radius + 1f));
                    _cells[c, r].color = Color.Lerp(_cells[c, r].color, baseColor, blend * 0.7f);
                }
            }
        }

        private void CreateMesh()
        {
            GameObject meshObj = new GameObject("HexMesh");
            meshObj.transform.parent = transform;
            meshObj.AddComponent<MeshFilter>();
            meshObj.AddComponent<MeshRenderer>();

            _hexMesh = meshObj.AddComponent<HexMesh>();
            _hexMesh.Initialize();
            _hexMesh.BuildMesh(_cells, cols, rows);
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Center of the grid in world space
            Vector3 center = HexMetrics.OffsetToWorld(cols / 2, rows / 2);

            // Pull camera up and back to see the whole arena
            float gridWorldWidth = cols * HexMetrics.HorizontalSpacing;
            float gridWorldHeight = rows * HexMetrics.VerticalSpacing;
            float maxExtent = Mathf.Max(gridWorldWidth, gridWorldHeight);

            // Position camera high enough to see everything
            float camHeight = maxExtent * 0.65f;
            float camBack = maxExtent * 0.35f;

            cam.transform.position = center + new Vector3(0f, camHeight, -camBack);
            cam.transform.LookAt(center);

            // Adjust far clip plane for large grids
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, camHeight * 3f);
        }

        // ── Battle Integration ───────────────────────────────────────

        public bool PlaceUnit(UnitRuntime unit, int col, int row)
        {
            HexCell cell = GetCell(col, row);
            if (cell == null || !cell.isWalkable || cell.occupyingUnit != null)
                return false;

            cell.occupyingUnit = unit;
            return true;
        }

        public void RemoveUnit(int col, int row)
        {
            HexCell cell = GetCell(col, row);
            if (cell != null)
                cell.occupyingUnit = null;
        }

        public bool MoveUnit(int fromCol, int fromRow, int toCol, int toRow)
        {
            HexCell from = GetCell(fromCol, fromRow);
            HexCell to = GetCell(toCol, toRow);
            if (from == null || to == null) return false;
            if (to.occupyingUnit != null) return false;
            if (!to.isWalkable) return false;

            to.occupyingUnit = from.occupyingUnit;
            from.occupyingUnit = null;
            return true;
        }

        public bool IsOccupied(int col, int row)
        {
            HexCell cell = GetCell(col, row);
            return cell != null && cell.occupyingUnit != null;
        }

        // BFS pathfinding on hex grid
        public List<(int col, int row)> FindPath(int fromCol, int fromRow, int toCol, int toRow)
        {
            var start = (col: fromCol, row: fromRow);
            var end = (col: toCol, row: toRow);

            HexCell endCell = GetCell(toCol, toRow);
            if (endCell == null || !endCell.isWalkable) return null;

            var visited = new HashSet<(int, int)>();
            var parent = new Dictionary<(int, int), (int, int)>();
            var queue = new Queue<(int col, int row)>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.col == end.col && current.row == end.row)
                    return ReconstructPath(parent, start, end);

                for (int dir = 0; dir < 6; dir++)
                {
                    HexMetrics.GetNeighbor(current.col, current.row, dir, out int nc, out int nr);
                    var neighbor = (col: nc, row: nr);

                    if (visited.Contains(neighbor)) continue;

                    HexCell cell = GetCell(nc, nr);
                    if (cell == null || !cell.isWalkable) continue;

                    // Allow destination even if occupied, block other occupied cells
                    if (cell.occupyingUnit != null && !(nc == toCol && nr == toRow))
                        continue;

                    visited.Add(neighbor);
                    parent[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return null;
        }

        // Returns the first step toward the target. Returns current pos if no path.
        public (int col, int row) GetNextStepToward(
            int fromCol, int fromRow, int toCol, int toRow)
        {
            var path = FindPath(fromCol, fromRow, toCol, toRow);
            if (path == null || path.Count < 2)
                return (fromCol, fromRow);

            var next = path[1];
            HexCell cell = GetCell(next.col, next.row);
            if (cell != null && cell.occupyingUnit != null)
                return (fromCol, fromRow);

            return next;
        }

        // Hex distance using cube coordinate conversion (even-r offset)
        public static int HexDistance(int col1, int row1, int col2, int row2)
        {
            int q1 = col1 - (row1 - (row1 & 1)) / 2;
            int r1 = row1;
            int q2 = col2 - (row2 - (row2 & 1)) / 2;
            int r2 = row2;

            return (Mathf.Abs(q1 - q2)
                  + Mathf.Abs(q1 + r1 - q2 - r2)
                  + Mathf.Abs(r1 - r2)) / 2;
        }

        public static bool IsAdjacent(int col1, int row1, int col2, int row2)
        {
            return HexDistance(col1, row1, col2, row2) == 1;
        }

        private List<(int col, int row)> ReconstructPath(
            Dictionary<(int, int), (int, int)> parent,
            (int col, int row) start,
            (int col, int row) end)
        {
            var path = new List<(int col, int row)>();
            var current = end;

            while (!(current.col == start.col && current.row == start.row))
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
