using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Hex;
using TacticalRPG.Visual;
using UnityEngine;

namespace TacticalRPG.Systems
{
    public class HexBattleManager : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private int gridCols = 70;
        [SerializeField] private int gridRows = 60;

        [Header("Player Units")]
        [SerializeField] private List<UnitDefinition> playerDefinitions;

        [Header("Enemy Units")]
        [SerializeField] private List<UnitDefinition> enemyDefinitions;

        [Header("Timing")]
        [SerializeField] private float tickInterval = 0.05f;

        [Header("Unit Speed (ticks between actions)")]
        [SerializeField] private int moveTickCost = 3;
        [SerializeField] private int attackTickCost = 10;

        private HexGrid _grid;
        private CombatResolutionSystem _combat;
        private SkillSystem _skill;

        private List<UnitRuntime> _playerUnits = new List<UnitRuntime>();
        private List<UnitRuntime> _enemyUnits = new List<UnitRuntime>();
        private List<UnitVisualizer> _unitVis = new List<UnitVisualizer>();
        private Dictionary<int, int> _cooldowns = new Dictionary<int, int>();

        private float _tickTimer;
        private int _currentTick;
        private bool _battleOver;
        private BattleOutcome _outcome = BattleOutcome.None;

        private void Start()
        {
            _combat = new CombatResolutionSystem();
            _skill = new SkillSystem();
            UnitFactory.ResetIds();

            // Tell visualizers to use hex positions
            UnitVisualizer.WorldPositionProvider =
                (pos) => HexMetrics.OffsetToWorld(pos.x, pos.y);

            // Create hex grid
            var gridObj = new GameObject("HexGrid");
            _grid = gridObj.AddComponent<HexGrid>();
            _grid.Initialize(gridCols, gridRows);

            // Spawn units on opposite sides
            SpawnTeam(playerDefinitions, UnitTeam.Player, _playerUnits, 3);
            SpawnTeam(enemyDefinitions, UnitTeam.Enemy, _enemyUnits, gridCols - 4);

            CreateUnitVisuals();

            Debug.Log($"[HexBattle] {_playerUnits.Count}v{_enemyUnits.Count} " +
                      $"on {gridCols}x{gridRows} hex grid - GO!");
        }

        private void Update()
        {
            if (_battleOver) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer -= tickInterval;
                RunTick();
            }
        }

        private void OnGUI()
        {
            if (!_battleOver) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor =
                _outcome == BattleOutcome.Victory ? Color.green : Color.red;

            string msg = _outcome == BattleOutcome.Victory ? "VICTORY!" : "DEFEAT!";
            GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100), msg, style);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(
                new Rect(Screen.width / 2 - 100, Screen.height / 2 + 60, 200, 45),
                "RESTART", btnStyle))
            {
                UnitVisualizer.WorldPositionProvider = null;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        // ── Spawning ─────────────────────────────────────────────────

        private void SpawnTeam(List<UnitDefinition> defs, UnitTeam team,
            List<UnitRuntime> list, int spawnCol)
        {
            if (defs == null) return;

            int rowSpacing = gridRows / (defs.Count + 1);

            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i] == null) continue;

                UnitRuntime unit = UnitFactory.CreateFromDefinition(defs[i], team);
                unit.behavior = new BehaviorLoadout(defs[i].defaultBehavior);

                int row = rowSpacing * (i + 1);
                unit.position = new GridPosition(spawnCol, row);
                _grid.PlaceUnit(unit, spawnCol, row);

                list.Add(unit);
                _cooldowns[unit.runtimeId] = 0;

                Debug.Log($"  Spawned {unit.DisplayName} ({team}) at hex ({spawnCol}, {row})");
            }
        }

        // ── Tick Loop ────────────────────────────────────────────────

        private void RunTick()
        {
            _currentTick++;

            foreach (UnitRuntime unit in GetAllLiving())
            {
                if (unit.isDead) continue;

                // Check cooldown
                if (_cooldowns.TryGetValue(unit.runtimeId, out int cd) && cd > 0)
                {
                    _cooldowns[unit.runtimeId] = cd - 1;
                    continue;
                }

                UnitRuntime target = FindNearestEnemy(unit);
                if (target == null) continue;

                int ux = unit.position.x;
                int uy = unit.position.y;
                int tx = target.position.x;
                int ty = target.position.y;

                if (HexGrid.IsAdjacent(ux, uy, tx, ty))
                {
                    ExecuteAttack(unit, target);
                    _cooldowns[unit.runtimeId] = attackTickCost;
                }
                else
                {
                    var (nextCol, nextRow) = _grid.GetNextStepToward(ux, uy, tx, ty);
                    if (nextCol != ux || nextRow != uy)
                    {
                        if (_grid.MoveUnit(ux, uy, nextCol, nextRow))
                        {
                            unit.position = new GridPosition(nextCol, nextRow);
                            _cooldowns[unit.runtimeId] = moveTickCost;
                        }
                    }
                }
            }

            RefreshVisuals();
            CheckWinCondition();
        }

        // ── Combat ───────────────────────────────────────────────────

        private void ExecuteAttack(UnitRuntime attacker, UnitRuntime target)
        {
            if (target.isDead) return;

            CombatContext ctx;

            if (attacker.equippedSkills != null
                && attacker.equippedSkills.Count > 0
                && attacker.equippedSkills[0].actionSequence.Count > 0)
            {
                ResolvedTechnique tech = _skill.ResolveSkill(
                    attacker.equippedSkills[0], attacker);
                ctx = _combat.ResolveTechnique(attacker, target, tech);
            }
            else
            {
                ctx = _combat.ResolveBasicAttack(attacker, target);
            }

            if (target.isDead)
            {
                _grid.RemoveUnit(target.position.x, target.position.y);
                Debug.Log($"  ** {target.DisplayName} defeated by {attacker.DisplayName}! **");
            }
        }

        // ── Target Selection ─────────────────────────────────────────

        private UnitRuntime FindNearestEnemy(UnitRuntime unit)
        {
            var enemies = unit.team == UnitTeam.Player ? _enemyUnits : _playerUnits;
            UnitRuntime nearest = null;
            int minDist = int.MaxValue;

            foreach (UnitRuntime enemy in enemies)
            {
                if (enemy.isDead) continue;
                int dist = HexGrid.HexDistance(
                    unit.position.x, unit.position.y,
                    enemy.position.x, enemy.position.y);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        // ── Win Condition ────────────────────────────────────────────

        private void CheckWinCondition()
        {
            if (_battleOver) return;

            bool allEnemiesDead = true;
            foreach (UnitRuntime e in _enemyUnits)
                if (!e.isDead) { allEnemiesDead = false; break; }

            bool allPlayersDead = true;
            foreach (UnitRuntime p in _playerUnits)
                if (!p.isDead) { allPlayersDead = false; break; }

            if (allEnemiesDead)
            {
                _battleOver = true;
                _outcome = BattleOutcome.Victory;
                Debug.Log("=== VICTORY! ===");
            }
            else if (allPlayersDead)
            {
                _battleOver = true;
                _outcome = BattleOutcome.Defeat;
                Debug.Log("=== DEFEAT! ===");
            }
        }

        // ── Visuals ──────────────────────────────────────────────────

        private void CreateUnitVisuals()
        {
            var allUnits = new List<UnitRuntime>(_playerUnits);
            allUnits.AddRange(_enemyUnits);

            foreach (UnitRuntime unit in allUnits)
            {
                var obj = new GameObject($"Unit_{unit.DisplayName}");
                var vis = obj.AddComponent<UnitVisualizer>();
                vis.Initialize(unit);
                _unitVis.Add(vis);
            }
        }

        private void RefreshVisuals()
        {
            foreach (UnitVisualizer vis in _unitVis)
                vis.UpdateVisual();
        }

        private List<UnitRuntime> GetAllLiving()
        {
            var list = new List<UnitRuntime>();
            foreach (UnitRuntime u in _playerUnits)
                if (!u.isDead) list.Add(u);
            foreach (UnitRuntime u in _enemyUnits)
                if (!u.isDead) list.Add(u);
            return list;
        }
    }
}
