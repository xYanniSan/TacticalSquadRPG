using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalRPG.Visual
{
    public class PreBattleUI : MonoBehaviour
    {
        private BattleManager _battle;

        // Hero data
        private List<UnitDefinition> _heroes;
        private List<ActionDefinition> _actions;

        // Per-hero config
        private int[] _behaviorIndex;            // index into BehaviorType enum
        private int[][] _actionSlotIndex;        // [hero][slot] -> index into _actions (-1 = empty)
        private GridPosition[] _placements;
        private bool[] _placed;

        // Placement mode
        private int _placingHero = -1;
        private List<GameObject> _ghostMarkers  = new List<GameObject>();
        private List<GameObject> _highlightTiles = new List<GameObject>();

        // UI labels (built from data)
        private string[] _actionLabels;

        public void Initialize(
            BattleManager battle,
            List<UnitDefinition> heroes,
            List<ActionDefinition> actions)
        {
            _battle  = battle;
            _heroes  = heroes ?? new List<UnitDefinition>();
            _actions = actions ?? new List<ActionDefinition>();

            int count = _heroes.Count;
            _behaviorIndex   = new int[count];
            _actionSlotIndex = new int[count][];
            _placements      = new GridPosition[count];
            _placed          = new bool[count];

            for (int i = 0; i < count; i++)
            {
                _behaviorIndex[i] = (int)_heroes[i].defaultBehavior;
                _actionSlotIndex[i] = new int[] { -1, -1, -1 };
                _placed[i] = false;
            }

            // Build action labels: index 0 = empty, 1..N = action names
            _actionLabels = new string[_actions.Count + 1];
            _actionLabels[0] = "\u2014"; // em-dash for "empty"
            for (int i = 0; i < _actions.Count; i++)
                _actionLabels[i + 1] = _actions[i].displayName;
        }

        // ── Grid Click for Placement ─────────────────────────────────

        private void Update()
        {
            if (_battle == null || _battle.CurrentPhase != GamePhase.PreBattle) return;

            if (_placingHero >= 0 && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Ignore clicks on the left UI panel area
                if (Mouse.current.position.ReadValue().x < Screen.width * 0.25f) return;

                HandlePlacementClick();
            }
        }

        private void HandlePlacementClick()
        {
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!groundPlane.Raycast(ray, out float distance)) return;

            Vector3 worldPoint = ray.GetPoint(distance);
            int x = Mathf.RoundToInt(worldPoint.x);
            int z = Mathf.RoundToInt(worldPoint.z);

            // Must be in placement zone (left 3 columns)
            if (x < 0 || x > 2) return;
            if (z < 0 || z >= _battle.GridHeight) return;

            GridPosition pos = new GridPosition(x, z);

            // Can't overlap another placed hero
            for (int i = 0; i < _placed.Length; i++)
            {
                if (i == _placingHero) continue;
                if (_placed[i] && _placements[i].x == pos.x && _placements[i].y == pos.y) return;
            }

            // Can't overlap an enemy
            if (_battle.State.grid.IsOccupied(pos)) return;

            _placements[_placingHero] = pos;
            _placed[_placingHero] = true;
            _placingHero = -1;

            UpdateGhostMarkers();
            ClearHighlights();
        }

        // ── OnGUI ────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_battle == null || _battle.CurrentPhase != GamePhase.PreBattle) return;

            // Title
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 24,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            GUI.Box(new Rect(Screen.width / 2 - 150, 10, 300, 40), "");
            GUI.Label(new Rect(Screen.width / 2 - 150, 10, 300, 40), "PREPARE FOR BATTLE", titleStyle);

            // Hero config panels (left side)
            float panelW = Mathf.Max(260f, Screen.width * 0.22f);
            float panelY = 60f;

            for (int i = 0; i < _heroes.Count; i++)
            {
                float h = DrawHeroPanel(i, 10f, panelY, panelW);
                panelY += h + 10f;
            }

            // Placement instructions
            if (_placingHero >= 0)
            {
                var instrStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 15,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                instrStyle.normal.textColor = Color.yellow;
                GUI.Label(
                    new Rect(Screen.width / 2 - 200, Screen.height - 90, 400, 30),
                    $"Click a tile (left 3 columns) to place {_heroes[_placingHero].displayName}",
                    instrStyle);
            }

            // Start Battle button
            bool allPlaced = true;
            for (int i = 0; i < _placed.Length; i++)
                if (!_placed[i]) { allPlaced = false; break; }

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 20,
                fontStyle = FontStyle.Bold
            };

            GUI.enabled = allPlaced;
            if (GUI.Button(
                new Rect(Screen.width / 2 - 110, Screen.height - 55, 220, 45),
                "START BATTLE!", btnStyle))
            {
                LaunchBattle();
            }
            GUI.enabled = true;

            if (!allPlaced)
            {
                var hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 12,
                    alignment = TextAnchor.MiddleCenter
                };
                hintStyle.normal.textColor = Color.gray;
                GUI.Label(
                    new Rect(Screen.width / 2 - 150, Screen.height - 15, 300, 20),
                    "Place all heroes on the grid to begin", hintStyle);
            }
        }

        // ── Hero Panel ───────────────────────────────────────────────

        private float DrawHeroPanel(int heroIdx, float x, float y, float w)
        {
            UnitDefinition hero = _heroes[heroIdx];
            float cy = y;
            float labelH = 22f;
            float pad = 6f;

            // Estimate total height for background box
            float estimatedH = 20f + labelH * 3 + 30f + pad * 4;
            if (_actions.Count > 0) estimatedH += (labelH + 32f) * 3 + pad;
            estimatedH += 35f + pad;

            GUI.Box(new Rect(x, y, w, estimatedH), "");
            cy += 6f;

            // Hero name
            var nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(x, cy, w, labelH + 4), hero.displayName, nameStyle);
            cy += labelH + 4;

            // Stats
            GUI.Label(new Rect(x + 10, cy, w - 20, labelH),
                $"HP: {hero.baseStats.maxHP}   ATK: {hero.baseStats.attack}");
            cy += labelH;
            GUI.Label(new Rect(x + 10, cy, w - 20, labelH),
                $"DEF: {hero.baseStats.defense}   SPD: {hero.baseStats.moveSpeed}");
            cy += labelH + pad;

            // ── Behavior ──
            GUI.Label(new Rect(x + 10, cy, w - 20, labelH), "Behavior:");
            cy += labelH;

            string[] behaviorNames = { "Aggressive", "Defensive", "Balanced" };
            _behaviorIndex[heroIdx] = GUI.Toolbar(
                new Rect(x + 8, cy, w - 16, 26),
                _behaviorIndex[heroIdx],
                behaviorNames);
            cy += 30f + pad;

            // ── Skill Actions ──
            if (_actions.Count > 0)
            {
                GUI.Label(new Rect(x + 10, cy, w - 20, labelH), "Skill Slot:");
                cy += labelH;

                int cols = Mathf.Min(_actionLabels.Length, 4);

                for (int s = 0; s < 3; s++)
                {
                    // Current selection: -1 => index 0 (empty), 0 => index 1, etc.
                    int current = _actionSlotIndex[heroIdx][s] + 1;
                    int rows = Mathf.CeilToInt((float)_actionLabels.Length / cols);
                    float gridH = rows * 22f;

                    GUI.Label(new Rect(x + 10, cy, 50, 20), $"  [{s + 1}]");
                    int newVal = GUI.SelectionGrid(
                        new Rect(x + 45, cy, w - 55, gridH),
                        current,
                        _actionLabels,
                        cols);
                    _actionSlotIndex[heroIdx][s] = newVal - 1;
                    cy += gridH + 4f;
                }
                cy += pad;
            }

            // ── Placement button ──
            bool isPlacing = _placingHero == heroIdx;
            bool isPlaced  = _placed[heroIdx];

            if (isPlacing) GUI.backgroundColor = Color.yellow;
            else if (isPlaced) GUI.backgroundColor = Color.green;

            string placeLabel = isPlacing
                ? "Placing... (click grid)"
                : isPlaced
                    ? $"Placed at ({_placements[heroIdx].x}, {_placements[heroIdx].y})"
                    : "Place on Grid";

            if (GUI.Button(new Rect(x + 10, cy, w - 20, 30), placeLabel))
            {
                if (isPlacing)
                {
                    _placingHero = -1;
                    ClearHighlights();
                }
                else
                {
                    _placingHero = heroIdx;
                    ShowPlacementHighlights();
                }
            }
            GUI.backgroundColor = Color.white;
            cy += 35f;

            return cy - y;
        }

        // ── Placement Visuals ────────────────────────────────────────

        private void ShowPlacementHighlights()
        {
            ClearHighlights();

            for (int gx = 0; gx < 3; gx++)
            {
                for (int gz = 0; gz < _battle.GridHeight; gz++)
                {
                    GridPosition pos = new GridPosition(gx, gz);

                    // Skip occupied grid tiles (enemies)
                    if (_battle.State.grid.IsOccupied(pos)) continue;

                    // Skip tiles with another placed hero
                    bool heroThere = false;
                    for (int i = 0; i < _placed.Length; i++)
                    {
                        if (i == _placingHero) continue;
                        if (_placed[i] && _placements[i].x == pos.x && _placements[i].y == pos.y)
                        {
                            heroThere = true;
                            break;
                        }
                    }
                    if (heroThere) continue;

                    // Create glowing tile overlay
                    GameObject hl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    hl.name = $"Highlight_{gx}_{gz}";
                    hl.transform.position   = GridVisualizer.GridToWorld(pos) + new Vector3(0f, 0.06f, 0f);
                    hl.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
                    hl.GetComponent<Renderer>().material.color = new Color(0.3f, 0.85f, 1f, 0.6f);
                    Destroy(hl.GetComponent<Collider>());
                    _highlightTiles.Add(hl);
                }
            }
        }

        private void ClearHighlights()
        {
            foreach (GameObject h in _highlightTiles)
                if (h != null) Destroy(h);
            _highlightTiles.Clear();
        }

        private void UpdateGhostMarkers()
        {
            foreach (GameObject g in _ghostMarkers)
                if (g != null) Destroy(g);
            _ghostMarkers.Clear();

            for (int i = 0; i < _placed.Length; i++)
            {
                if (!_placed[i]) continue;

                GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                ghost.name = $"Ghost_{_heroes[i].displayName}";

                Vector3 worldPos = GridVisualizer.GridToWorld(_placements[i]);
                ghost.transform.position   = worldPos + new Vector3(0f, 0.6f, 0f);
                ghost.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

                ghost.GetComponent<Renderer>().material.color = new Color(0.3f, 0.55f, 1f, 0.8f);
                Destroy(ghost.GetComponent<Collider>());

                _ghostMarkers.Add(ghost);
            }
        }

        // ── Launch ───────────────────────────────────────────────────

        private void LaunchBattle()
        {
            ClearHighlights();
            foreach (GameObject g in _ghostMarkers)
                if (g != null) Destroy(g);
            _ghostMarkers.Clear();

            // Build hero configs
            var configs = new List<BattleManager.HeroConfig>();

            for (int i = 0; i < _heroes.Count; i++)
            {
                var cfg = new BattleManager.HeroConfig();
                cfg.definition = _heroes[i];
                cfg.behavior   = (BehaviorType)_behaviorIndex[i];
                cfg.position   = _placements[i];
                cfg.actions    = new List<ActionDefinition>();

                for (int s = 0; s < 3; s++)
                {
                    int idx = _actionSlotIndex[i][s];
                    if (idx >= 0 && idx < _actions.Count)
                        cfg.actions.Add(_actions[idx]);
                }

                configs.Add(cfg);
            }

            _battle.StartCombatWithConfig(configs);

            Destroy(gameObject);
        }
    }
}
