using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Pairs a UnitDefinition with its action loadout for the Inspector.
    /// </summary>
    [System.Serializable]
    public class HeroLoadout
    {
        public UnitDefinition unit;
        public List<ActionDefinition> actions;
    }

    /// <summary>
    /// Spawns two teams of AI units on the terrain, wires them to the existing
    /// CombatResolutionSystem and SkillSystem, manages win/loss.
    /// Attach to an empty GameObject in the scene.
    /// </summary>
    public class TerrainBattleManager : MonoBehaviour
    {
        public static TerrainBattleManager Instance { get; private set; }

        public BattleExchangeCoordinator ExchangeCoordinator => _exchangeCoordinator;
        public BattleMeleeTokenSystem     MeleeTokens         => _meleeTokens;

        [Header("Player Heroes (each with their own skills)")]
        [SerializeField] private List<HeroLoadout> playerHeroes;

        [Header("Enemy Units")]
        [SerializeField] private List<UnitDefinition> enemyTeam;

        [Header("Enemy Default Attack (shared)")]
        [SerializeField] private ActionDefinition enemyDefaultAttack;

        [Header("Spawn Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform playerSpawnCenter;
        [SerializeField] private Transform enemySpawnCenter;
        [SerializeField] private float spawnSpreadRadius = 5f;

        [Header("Battle Start")]
        [SerializeField] private float battleStartDelay = 3f;

        [Header("Combo Library (optional — overrides hardcoded recipes)")]
        [SerializeField] private ComboLibraryAsset comboLibrary;

        [Header("Camera")]
        [SerializeField] private bool autoFollowCamera = true;

        [Header("Debug")]
        [SerializeField] private bool knockbackEnabled = true;
        [SerializeField] private bool dodgeEnabled     = true;
        [SerializeField] private bool blockEnabled     = true;

        [Header("Team Colors")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.4f, 0.9f);
        [SerializeField] private Color enemyColor = new Color(0.9f, 0.2f, 0.2f);

        private Systems.CombatResolutionSystem _combat;
        private Systems.SkillSystem _skill;

        // Sub-systems (added as components in Awake)
        private BattleCombatResolver    _resolver;
        private BattleEngagementManager _engagement;
        private BattleSummonManager     _summons;
        private BattleTargetFinder      _targets;
        private BattleHitStopSystem          _hitStop;
        private BattleKnockbackSystem        _knockback;
        private BattleExchangeCoordinator    _exchangeCoordinator;
        private BattleMeleeTokenSystem       _meleeTokens;

        private List<TerrainBattleUnit> _playerUnits = new List<TerrainBattleUnit>();
        private List<TerrainBattleUnit> _enemyUnits  = new List<TerrainBattleUnit>();
        private List<TerrainBattleUnit> _allUnits    = new List<TerrainBattleUnit>();

        private bool _battleOver;
        private BattleOutcome _outcome = BattleOutcome.None;
        private ThirdPersonCamera _cam;

        private float _battleStartTimer;
        private bool _battleStarted;

        private void Awake()
        {
            Instance = this;

            // Attach sub-systems as sibling components
            _resolver   = gameObject.AddComponent<BattleCombatResolver>();
            _engagement = gameObject.AddComponent<BattleEngagementManager>();
            _summons    = gameObject.AddComponent<BattleSummonManager>();
            _targets    = gameObject.AddComponent<BattleTargetFinder>();
            _hitStop              = gameObject.AddComponent<BattleHitStopSystem>();
            _knockback            = gameObject.AddComponent<BattleKnockbackSystem>();
            _exchangeCoordinator  = gameObject.AddComponent<BattleExchangeCoordinator>();
            _meleeTokens          = gameObject.AddComponent<BattleMeleeTokenSystem>();
        }

        private void Start()
        {
            _combat = new Systems.CombatResolutionSystem();
            _skill  = new Systems.SkillSystem();
            Systems.UnitFactory.ResetIds();

            // Load combo library SO (falls back to built-in if not assigned)
            ComboLibrary.SetLibrary(comboLibrary);

            Cursor.lockState = CursorLockMode.Locked;

            SpawnPlayerHeroes();
            SpawnEnemyTeam();

            // Wire sub-systems that depend on the unit lists
            _summons.Initialize(_playerUnits, _enemyUnits, _allUnits);
            _targets.Initialize(_playerUnits, _enemyUnits);
            _resolver.Initialize(_combat, _skill);

            _battleStartTimer = battleStartDelay;
            _battleStarted    = battleStartDelay <= 0f;

            // Camera follows the first player unit
            if (autoFollowCamera && _playerUnits.Count > 0)
            {
                _cam = FindAnyObjectByType<ThirdPersonCamera>();
                if (_cam != null)
                    SetCameraTarget(_playerUnits[0].transform);
            }

            Debug.Log($"[TerrainBattle] {_playerUnits.Count}v{_enemyUnits.Count} — FIGHT!");
        }

        private void Update()
        {
            if (_battleOver) return;

            // Countdown before units engage
            if (!_battleStarted)
            {
                _battleStartTimer -= Time.deltaTime;
                if (_battleStartTimer <= 0f)
                {
                        _battleStarted = true;
                            _engagement.SetBattleStarted(true);
                            Debug.Log("[TerrainBattle] *** FIGHT! ***");
                        }
                        return;
                    }

            CheckWinCondition();

            // Tab to cycle camera between player units
            if (Keyboard.current.tabKey.wasPressedThisFrame && _playerUnits.Count > 0)
                CycleCameraTarget();

            // Escape to toggle cursor
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
        }

        // ── Spawning ─────────────────────────────────────────────────

        private void SpawnPlayerHeroes()
        {
            // Prefer HeroLoadoutData (from config menu) over Inspector fields
            if (HeroLoadoutData.SelectedHeroes.Count > 0)
            {
                SpawnFromLoadoutData();
                return;
            }

            if (playerHeroes == null || playerSpawnCenter == null) return;

            for (int i = 0; i < playerHeroes.Count; i++)
            {
                var loadout = playerHeroes[i];
                if (loadout?.unit == null) continue;

                UnitRuntime unit = Systems.UnitFactory.CreateFromDefinition(loadout.unit, UnitTeam.Player);
                unit.behavior = new BehaviorLoadout(loadout.unit.defaultBehavior);

                // Equip this hero's personal actions
                if (loadout.actions != null && loadout.actions.Count > 0)
                {
                    var skill = new SkillSlot(0);
                    foreach (var action in loadout.actions)
                    {
                        if (action != null)
                            skill.AddAction(action);
                    }
                    unit.equippedSkills.Add(skill);
                }

                SpawnUnit(unit, i, playerHeroes.Count, playerSpawnCenter, playerColor);
            }
        }

        /// <summary>
        /// Spawns heroes using loadouts configured in the Hero Config Menu.
        /// </summary>
        private void SpawnFromLoadoutData()
        {
            if (playerSpawnCenter == null) return;

            var heroes = HeroLoadoutData.SelectedHeroes;
            for (int i = 0; i < heroes.Count; i++)
            {
                var heroDef = heroes[i];
                if (heroDef == null) continue;

                UnitRuntime unit = Systems.UnitFactory.CreateFromDefinition(heroDef, UnitTeam.Player);
                unit.behavior = new BehaviorLoadout(heroDef.defaultBehavior);

                // Equip skill slots from config menu
                var slots = HeroLoadoutData.GetLoadout(heroDef.unitId);
                foreach (var slot in slots)
                    unit.equippedSkills.Add(slot);

                SpawnUnit(unit, i, heroes.Count, playerSpawnCenter, playerColor);
            }

            Debug.Log($"[TerrainBattle] Spawned {heroes.Count} heroes from HeroLoadoutData");
        }

        private void SpawnEnemyTeam()
        {
            if (enemyTeam == null || enemySpawnCenter == null) return;

            for (int i = 0; i < enemyTeam.Count; i++)
            {
                if (enemyTeam[i] == null) continue;

                UnitRuntime unit = Systems.UnitFactory.CreateFromDefinition(enemyTeam[i], UnitTeam.Enemy);
                unit.behavior = new BehaviorLoadout(enemyTeam[i].defaultBehavior);

                // Enemies all use the same default attack (e.g. Punch)
                if (enemyDefaultAttack != null)
                {
                    var skill = new SkillSlot(0);
                    skill.AddAction(enemyDefaultAttack);
                    unit.equippedSkills.Add(skill);
                }

                SpawnUnit(unit, i, enemyTeam.Count, enemySpawnCenter, enemyColor);
            }
        }

        private void SpawnUnit(UnitRuntime unit, int index, int total,
            Transform spawnCenter, Color color)
        {
            float angle = (360f / Mathf.Max(1, total)) * index * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * spawnSpreadRadius,
                0f,
                Mathf.Sin(angle) * spawnSpreadRadius);
            Vector3 basePos = spawnCenter.position + offset;
            float groundY = basePos.y;
            if (Physics.Raycast(basePos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                groundY = hit.point.y;
            Vector3 spawnPos = new Vector3(basePos.x, groundY + 2f, basePos.z);

            GameObject go;
            GameObject prefab = unit.team == UnitTeam.Player ? playerPrefab : enemyPrefab;
            
            // If team-specific prefab is not set, try to use the unit definition's visual prefab
            if (prefab == null && unit.definition != null)
                prefab = unit.definition.visualPrefab;

            if (prefab != null)
            {
                go = Instantiate(prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.position = spawnPos;

                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = color;
            }

            go.name = $"{unit.DisplayName} ({unit.team})";

            var cc = go.GetComponent<CharacterController>();
            if (cc == null) cc = go.AddComponent<CharacterController>();

            var health = go.GetComponent<HealthSystem>();
            if (health == null) health = go.AddComponent<HealthSystem>();

            var battleUnit = go.GetComponent<TerrainBattleUnit>();
            if (battleUnit == null) battleUnit = go.AddComponent<TerrainBattleUnit>();

            battleUnit.Initialize(unit);

            unit.visualInstance = go;

            var list = unit.team == UnitTeam.Player ? _playerUnits : _enemyUnits;
            list.Add(battleUnit);
            _allUnits.Add(battleUnit);

            // All units start in Backline — RequestFrontlineSlot promotes the first 3 per side

            string skillInfo = unit.equippedSkills.Count > 0
                ? $"Skill({unit.equippedSkills[0].actionSequence.Count} actions)"
                : "Basic Attack only";
            Debug.Log($"  Spawned {unit.DisplayName} ({unit.team}) " +
                      $"HP:{unit.maxHP} ATK:{unit.currentStats.attack} DEF:{unit.currentStats.defense} " +
                      $"[{skillInfo}]");
        }

        // ── Combat Resolution (delegated to BattleCombatResolver) ───

        /// <summary>
        /// Called from TerrainBattleUnit.UpdateDecide — pre-resolves the skill
        /// so the unit knows cast type before committing to a state.
        /// </summary>
        public ResolvedTechnique ResolveForDecide(SkillSlot skill, UnitRuntime caster)
            => _resolver.ResolveForDecide(skill, caster);

        public void ResolveBasicAttack(TerrainBattleUnit attacker, TerrainBattleUnit defender)
            => _resolver.ResolveBasicAttack(attacker, defender);

        public void ResolveSkillAttack(TerrainBattleUnit attacker, TerrainBattleUnit defender,
            ResolvedTechnique tech)
            => _resolver.ResolveSkillAttack(attacker, defender, tech);

        public void ExecuteIndividualActions(TerrainBattleUnit attacker, TerrainBattleUnit defender,
            SkillSlot skill)
            => _resolver.ExecuteIndividualActions(attacker, defender, skill);

        // ── Knockback (delegated to BattleKnockbackSystem) ───────────

        public void ApplyKnockback(TerrainBattleUnit attacker, TerrainBattleUnit defender, int damage)
        {
            if (knockbackEnabled)
                _knockback.ApplyFromDamage(attacker, defender, damage);
        }

        public bool IsDodgeEnabled  => dodgeEnabled;
        public bool IsBlockEnabled  => blockEnabled;

        // ── Engagement Limits (delegated to BattleEngagementManager) ─

        /// <summary>
        /// Called by TerrainBattleUnit in Backline state. Returns true if a
        /// frontline slot is available and the unit should engage.
        /// </summary>
        public bool RequestFrontlineSlot(TerrainBattleUnit unit)
            => _engagement.RequestFrontlineSlot(unit);

        /// <summary>
        /// Called when a unit dies so the frontline set is updated.
        /// </summary>
        public void OnUnitDied(TerrainBattleUnit unit)
            => _engagement.OnUnitDied(unit);

        /// <summary>
        /// Returns true if the caster already has a live summon on the field.
        /// </summary>
        public bool HasActiveSummon(int casterId)
            => _summons.HasActiveSummon(casterId);

        // ── Target Finding (delegated to BattleTargetFinder) ─────────

        public TerrainBattleUnit GetNearestEnemy(TerrainBattleUnit unit)
            => _targets.GetNearestEnemy(unit);

        // ── Camera ───────────────────────────────────────────────────

        private int _cameraTargetIndex = 0;

        private void CycleCameraTarget()
        {
            // Find next living player unit
            for (int i = 0; i < _playerUnits.Count; i++)
            {
                _cameraTargetIndex = (_cameraTargetIndex + 1) % _playerUnits.Count;
                if (!_playerUnits[_cameraTargetIndex].IsDead)
                {
                    SetCameraTarget(_playerUnits[_cameraTargetIndex].transform);
                    return;
                }
            }
        }

        private void SetCameraTarget(Transform target)
        {
            if (_cam == null)
                _cam = FindAnyObjectByType<ThirdPersonCamera>();
            if (_cam != null)
                _cam.SetTarget(target);
        }

        // ── Win Condition ────────────────────────────────────────────

        private void CheckWinCondition()
        {
            bool allEnemiesDead = true;
            foreach (var e in _enemyUnits)
                if (!e.IsDead) { allEnemiesDead = false; break; }

            bool allPlayersDead = true;
            foreach (var p in _playerUnits)
                if (!p.IsDead) { allPlayersDead = false; break; }

            if (allEnemiesDead)
            {
                _battleOver = true;
                _outcome = BattleOutcome.Victory;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log("=== VICTORY! ===");
            }
            else if (allPlayersDead)
            {
                _battleOver = true;
                _outcome = BattleOutcome.Defeat;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log("=== DEFEAT! ===");
            }
        }

        // ── UI ───────────────────────────────────────────────────────

        private void OnGUI()
        {
            // ── Countdown ────────────────────────────────────────────
            if (!_battleStarted && !_battleOver)
            {
                var countStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 96,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                int seconds = Mathf.CeilToInt(_battleStartTimer);
                countStyle.normal.textColor = seconds <= 1 ? Color.red : Color.white;
                string countText = seconds > 0 ? seconds.ToString() : "FIGHT!";
                GUI.Label(new Rect(0, Screen.height / 2 - 70, Screen.width, 140), countText, countStyle);
                return;
            }

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

            var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            if (GUI.Button(
                new Rect(Screen.width / 2 - 100, Screen.height / 2 + 60, 200, 45),
                "RESTART", btnStyle))
            {
                Instance = null;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }
    }
}
