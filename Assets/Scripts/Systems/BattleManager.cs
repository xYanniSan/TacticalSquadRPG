using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Visual;
using UnityEngine;

namespace TacticalRPG.Systems
{
    public class BattleManager : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private int gridWidth  = 14;
        [SerializeField] private int gridHeight = 10;

        [Header("Timing")]
        [SerializeField] private float tickInterval = 0.5f;

        [Header("Player Units")]
        [SerializeField] private List<UnitDefinition> playerDefinitions;

        [Header("Enemy Units")]
        [SerializeField] private List<UnitDefinition> enemyDefinitions;

        [Header("Available Actions")]
        [SerializeField] private List<ActionDefinition> availableActions;

        [Header("Hero Skill Loadouts (Auto-Start Only)")]
        [Tooltip("Drag Punch, Punch, Kick here for Triple Strike")]
        [SerializeField] private List<ActionDefinition> heroKaiActions;
        [Tooltip("Drag HandSignA, HandSignB, Focus here for Fire Strike")]
        [SerializeField] private List<ActionDefinition> heroMiraActions;

        [Header("Flow")]
        [SerializeField] private bool autoStartCombat = false;

        private BehaviorSystem          _behavior;
        private MovementSystem          _movement;
        private CombatResolutionSystem  _combat;
        private SkillSystem             _skill;

        private BattleState _state;
        private float       _tickTimer;
        private GamePhase   _gamePhase = GamePhase.PreBattle;

        private GridVisualizer        _gridVis;
        private List<UnitVisualizer>  _unitVis = new List<UnitVisualizer>();

        // ── Public Accessors ─────────────────────────────────────────
        public GamePhase CurrentPhase => _gamePhase;
        public BattleState State     => _state;
        public bool        IsRunning => _gamePhase == GamePhase.Combat && _state != null && !_state.isBattleOver;
        public int GridWidth  => gridWidth;
        public int GridHeight => gridHeight;
        public List<UnitDefinition>   PlayerDefinitions => playerDefinitions;
        public List<ActionDefinition> AvailableActions  => availableActions;

        // ── Hero Config ──────────────────────────────────────────────
        public struct HeroConfig
        {
            public UnitDefinition definition;
            public BehaviorType behavior;
            public List<ActionDefinition> actions;
            public GridPosition position;
        }

        private void Start()
        {
            _behavior = new BehaviorSystem();
            _movement = new MovementSystem();
            _combat   = new CombatResolutionSystem();
            _skill    = new SkillSystem();

            UnitFactory.ResetIds();
            _state = new BattleState("battle", gridWidth, gridHeight);

            GameObject gridObj = new GameObject("GridVisual");
            _gridVis = gridObj.AddComponent<GridVisualizer>();
            _gridVis.CreateGrid(_state.grid);
            SetupCamera();

            if (autoStartCombat)
            {
                SpawnTeam(playerDefinitions, UnitTeam.Player);
                SpawnTeam(enemyDefinitions,  UnitTeam.Enemy);
                EquipHeroSkills();
                CreateUnitVisuals();
                _state.StartBattle();
                _gamePhase = GamePhase.Combat;
                Debug.Log("[Battle] GO!");
            }
            else
            {
                SpawnTeam(enemyDefinitions, UnitTeam.Enemy);
                CreateUnitVisuals();

                var uiObj = new GameObject("PreBattleUI");
                var ui = uiObj.AddComponent<PreBattleUI>();
                ui.Initialize(this, playerDefinitions, availableActions);

                _gamePhase = GamePhase.PreBattle;
                Debug.Log("[Game] Pre-battle phase - configure your heroes!");
            }
        }

        private void Update()
        {
            if (_gamePhase != GamePhase.Combat) return;
            if (_state == null || _state.isBattleOver) return;

            _state.battleTime += Time.deltaTime;
            _tickTimer        += Time.deltaTime;

            if (_tickTimer >= tickInterval)
            {
                _tickTimer -= tickInterval;
                RunBattleTick();
                RefreshVisuals();
            }
        }

        private void OnGUI()
        {
            if (_state == null || _gamePhase != GamePhase.Result) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 48,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor =
                _state.outcome == BattleOutcome.Victory ? Color.green : Color.red;

            string msg = _state.outcome == BattleOutcome.Victory ? "VICTORY!" : "DEFEAT!";
            GUI.Label(new Rect(0, Screen.height / 2 - 50, Screen.width, 100), msg, style);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 20,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 60, 200, 45),
                           "RESTART", btnStyle))
            {
                RestartBattle();
            }
        }

        // ── Pre-Battle API ───────────────────────────────────────────

        public void StartCombatWithConfig(List<HeroConfig> configs)
        {
            foreach (HeroConfig cfg in configs)
            {
                UnitRuntime unit = UnitFactory.CreateFromDefinition(cfg.definition, UnitTeam.Player);
                unit.behavior = new BehaviorLoadout(cfg.behavior);

                if (cfg.actions != null && cfg.actions.Count > 0)
                {
                    SkillSlot slot = new SkillSlot(0);
                    foreach (ActionDefinition action in cfg.actions)
                    {
                        if (action != null)
                            slot.AddAction(action);
                    }
                    unit.equippedSkills.Add(slot);
                }

                _state.playerUnits.Add(unit);
                _state.grid.PlaceUnit(unit, cfg.position);

                Debug.Log($"  Spawned {unit.DisplayName} ({cfg.behavior}) at {cfg.position}" +
                          $" with {unit.equippedSkills.Count} skill(s)");
            }

            ClearUnitVisuals();
            CreateUnitVisuals();

            _state.StartBattle();
            _gamePhase = GamePhase.Combat;
            Debug.Log($"[Battle] {_state.playerUnits.Count}v{_state.enemyUnits.Count} - GO!");
        }

        public void RestartBattle()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void SpawnTeam(List<UnitDefinition> defs, UnitTeam team)
        {
            if (defs == null) return;

            var list   = team == UnitTeam.Player ? _state.playerUnits : _state.enemyUnits;
            int column = team == UnitTeam.Player ? 0 : gridWidth - 1;

            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i] == null) continue;

                UnitRuntime unit = UnitFactory.CreateFromDefinition(defs[i], team);
                list.Add(unit);

                GridPosition pos = new GridPosition(column, i);
                _state.grid.PlaceUnit(unit, pos);
                Debug.Log($"  Spawned {unit.DisplayName} ({team}) at {pos}");
            }
        }

        private void EquipHeroSkills()
        {
            foreach (UnitRuntime unit in _state.playerUnits)
            {
                List<ActionDefinition> actions = null;

                if (unit.definition.unitId == "hero_kai")
                    actions = heroKaiActions;
                else if (unit.definition.unitId == "hero_mira")
                    actions = heroMiraActions;

                if (actions == null || actions.Count == 0) continue;

                SkillSlot slot = new SkillSlot(0);
                foreach (ActionDefinition action in actions)
                {
                    if (action != null)
                        slot.AddAction(action);
                }
                unit.equippedSkills.Add(slot);
                Debug.Log($"  Equipped {unit.DisplayName} with {slot.actionSequence.Count} actions");
            }
        }

        private void RunBattleTick()
        {
            if (_state.isBattleOver) return;

            _state.currentTick++;
            Debug.Log($"--- Tick {_state.currentTick} ---");

            _behavior.GenerateAllIntents(_state);
            ExecuteMovement();
            ExecuteCombat();
            CheckWinCondition();
        }

        private void ExecuteMovement()
        {
            foreach (UnitRuntime unit in GetAllLiving())
            {
                if (unit.currentIntent == null) continue;
                if (unit.currentIntent.type != IntentType.Move) continue;

                GridPosition from = unit.position;
                if (_movement.MoveUnit(_state, unit, unit.currentIntent.targetPosition))
                    Debug.Log($"  {unit.DisplayName} moves {from} -> {unit.position}");
            }
        }

        private void ExecuteCombat()
        {
            foreach (UnitRuntime unit in GetAllLiving())
            {
                if (unit.isDead) continue;
                if (unit.currentIntent == null) continue;
                if (unit.currentIntent.type != IntentType.BasicAttack) continue;

                UnitRuntime target = unit.currentIntent.target;
                if (target == null || target.isDead) continue;
                if (!unit.position.IsAdjacent(target.position)) continue;

                CombatContext ctx;

                if (unit.equippedSkills != null
                    && unit.equippedSkills.Count > 0
                    && unit.equippedSkills[0].actionSequence.Count > 0)
                {
                    ResolvedTechnique tech = _skill.ResolveSkill(unit.equippedSkills[0], unit);
                    ctx = _combat.ResolveTechnique(unit, target, tech);
                    Debug.Log($"  {unit.DisplayName} uses {tech.techniqueName} on " +
                              $"{target.DisplayName} -> {ctx.finalDamage} dmg " +
                              $"(HP:{target.currentHP}/{target.maxHP})");
                }
                else
                {
                    ctx = _combat.ResolveBasicAttack(unit, target);
                    Debug.Log($"  {unit.DisplayName} attacks {target.DisplayName} -> " +
                              $"{ctx.finalDamage} dmg (HP:{target.currentHP}/{target.maxHP})");
                }

                if (target.isDead)
                {
                    _state.grid.RemoveUnit(target.position);
                    Debug.Log($"  ** {target.DisplayName} defeated! **");
                }
            }
        }

        private void CheckWinCondition()
        {
            if (_state.isBattleOver) return;

            if (_state.IsTeamDefeated(UnitTeam.Enemy))
            {
                _state.EndBattle(BattleOutcome.Victory);
                _gamePhase = GamePhase.Result;
                Debug.Log("=== VICTORY! ===");
            }
            else if (_state.IsTeamDefeated(UnitTeam.Player))
            {
                _state.EndBattle(BattleOutcome.Defeat);
                _gamePhase = GamePhase.Result;
                Debug.Log("=== DEFEAT! ===");
            }
        }

        private void CreateUnitVisuals()
        {
            foreach (UnitRuntime unit in _state.GetAllUnits())
            {
                GameObject obj = new GameObject($"Unit_{unit.DisplayName}");
                UnitVisualizer vis = obj.AddComponent<UnitVisualizer>();
                vis.Initialize(unit);
                _unitVis.Add(vis);
            }
        }

        private void ClearUnitVisuals()
        {
            foreach (UnitVisualizer vis in _unitVis)
            {
                if (vis != null) Destroy(vis.gameObject);
            }
            _unitVis.Clear();
        }

        private void RefreshVisuals()
        {
            foreach (UnitVisualizer vis in _unitVis)
                vis.UpdateVisual();
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 center = _gridVis.GetGridCenter();
            cam.transform.position = center + new Vector3(0f, 14f, -8f);
            cam.transform.LookAt(center);
        }

        private List<UnitRuntime> GetAllLiving()
        {
            var list = new List<UnitRuntime>(_state.GetLivingUnits(UnitTeam.Player));
            list.AddRange(_state.GetLivingUnits(UnitTeam.Enemy));
            return list;
        }

        public List<UnitRuntime> GetLivingPlayers() => _state?.GetLivingUnits(UnitTeam.Player);
        public List<UnitRuntime> GetLivingEnemies()  => _state?.GetLivingUnits(UnitTeam.Enemy);
        public GridMap           GetGrid()           => _state?.grid;
    }
}
