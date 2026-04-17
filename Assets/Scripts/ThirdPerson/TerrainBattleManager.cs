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

        [Header("Player Heroes (each with their own skills)")]
        [SerializeField] private List<HeroLoadout> playerHeroes;

        [Header("Enemy Units")]
        [SerializeField] private List<UnitDefinition> enemyTeam;

        [Header("Enemy Default Attack (shared)")]
        [SerializeField] private ActionDefinition enemyDefaultAttack;

        [Header("Spawn Settings")]
        [SerializeField] private Transform playerSpawnCenter;
        [SerializeField] private Transform enemySpawnCenter;
        [SerializeField] private float spawnSpreadRadius = 5f;
        [SerializeField] private float spawnHeightOffset = 5f;

        [Header("Camera")]
        [SerializeField] private bool autoFollowCamera = true;

        [Header("Team Colors")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.4f, 0.9f);
        [SerializeField] private Color enemyColor = new Color(0.9f, 0.2f, 0.2f);

        private Systems.CombatResolutionSystem _combat;
        private Systems.SkillSystem _skill;

        private List<TerrainBattleUnit> _playerUnits = new List<TerrainBattleUnit>();
        private List<TerrainBattleUnit> _enemyUnits = new List<TerrainBattleUnit>();
        private List<TerrainBattleUnit> _allUnits = new List<TerrainBattleUnit>();

        private Dictionary<int, TerrainBattleUnit> _activeSummons = new Dictionary<int, TerrainBattleUnit>();

        private bool _battleOver;
        private BattleOutcome _outcome = BattleOutcome.None;
        private ThirdPersonCamera _cam;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _combat = new Systems.CombatResolutionSystem();
            _skill = new Systems.SkillSystem();
            Systems.UnitFactory.ResetIds();

            Cursor.lockState = CursorLockMode.Locked;

            SpawnPlayerHeroes();
            SpawnEnemyTeam();

            // Camera follows the first player unit
            if (autoFollowCamera && _playerUnits.Count > 0)
            {
                _cam = FindFirstObjectByType<ThirdPersonCamera>();
                if (_cam != null)
                    SetCameraTarget(_playerUnits[0].transform);
            }

            Debug.Log($"[TerrainBattle] {_playerUnits.Count}v{_enemyUnits.Count} — FIGHT!");
        }

        private void Update()
        {
            if (_battleOver) return;
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
                spawnHeightOffset,
                Mathf.Sin(angle) * spawnSpreadRadius);
            Vector3 spawnPos = spawnCenter.position + offset;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"{unit.DisplayName} ({unit.team})";
            go.transform.position = spawnPos;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;

            var cc = go.AddComponent<CharacterController>();
            var health = go.AddComponent<HealthSystem>();
            var battleUnit = go.AddComponent<TerrainBattleUnit>();
            battleUnit.Initialize(unit);

            unit.visualInstance = go;

            var list = unit.team == UnitTeam.Player ? _playerUnits : _enemyUnits;
            list.Add(battleUnit);
            _allUnits.Add(battleUnit);

            string skillInfo = unit.equippedSkills.Count > 0
                ? $"Skill({unit.equippedSkills[0].actionSequence.Count} actions)"
                : "Basic Attack only";
            Debug.Log($"  Spawned {unit.DisplayName} ({unit.team}) " +
                      $"HP:{unit.maxHP} ATK:{unit.currentStats.attack} DEF:{unit.currentStats.defense} " +
                      $"[{skillInfo}]");
        }

        // ── Combat Resolution (called by TerrainBattleUnit) ──────────

        public void ResolveAttack(TerrainBattleUnit attacker, TerrainBattleUnit defender)
        {
            if (attacker.IsDead || defender.IsDead) return;

            CombatContext ctx;
            string attackName = "Basic Attack";

            // Try skill-based attack first
            if (attacker.Unit.equippedSkills != null
                && attacker.Unit.equippedSkills.Count > 0
                && attacker.Unit.equippedSkills[0].actionSequence.Count > 0)
            {
                ResolvedTechnique tech = _skill.ResolveSkill(
                    attacker.Unit.equippedSkills[0], attacker.Unit);

                // ── Summon handling ──────────────────────────────
                if (tech.type == TechniqueType.Summon)
                {
                    TrySummon(attacker, tech);
                    return;
                }

                ctx = _combat.ResolveTechnique(attacker.Unit, defender.Unit, tech);
                attackName = tech.techniqueName;
            }
            else
            {
                ctx = _combat.ResolveBasicAttack(attacker.Unit, defender.Unit);
            }

            // Sync the 3D health system
            defender.ApplyDamage(ctx.finalDamage);

            Debug.Log($"  {attacker.Unit.DisplayName} uses [{attackName}] → {defender.Unit.DisplayName} " +
                      $"for {ctx.finalDamage} dmg (HP: {defender.Unit.currentHP}/{defender.Unit.maxHP})");

            if (defender.Unit.isDead)
                Debug.Log($"  ** {defender.Unit.DisplayName} DEFEATED! **");
        }

        // ── Summon System ────────────────────────────────────────────

        private void TrySummon(TerrainBattleUnit caster, ResolvedTechnique tech)
        {
            int casterId = caster.Unit.runtimeId;

            // If summon already alive, do nothing
            if (_activeSummons.TryGetValue(casterId, out TerrainBattleUnit existing)
                && existing != null && !existing.IsDead)
            {
                Debug.Log($"  {caster.Unit.DisplayName} tries [{tech.techniqueName}] — summon already active!");
                return;
            }

            // Create summon UnitRuntime
            var summonUnit = new UnitRuntime
            {
                definition = null,
                runtimeId = Systems.UnitFactory.CreateSummonId(),
                team = caster.Unit.team,
                currentStats = new StatBlock(
                    tech.power * 2,           // HP scales with technique power
                    tech.power / 2,           // ATK = half of technique power
                    caster.Unit.currentStats.defense / 2,  // DEF = half of caster
                    caster.Unit.currentStats.moveSpeed * 1.2f),  // Slightly faster
                maxHP = tech.power * 2,
                currentHP = tech.power * 2,
                behavior = new BehaviorLoadout(BehaviorType.Aggressive),
                equippedSkills = new System.Collections.Generic.List<SkillSlot>(),
                activeEffects = new System.Collections.Generic.List<StatusEffect>(),
                isDead = false,
                overrideDisplayName = $"{caster.Unit.DisplayName}'s Summon"
            };

            // Spawn capsule near caster
            Vector3 spawnPos = caster.transform.position
                + caster.transform.forward * 3f
                + Vector3.up * 2f;

            Color summonColor = caster.Unit.team == UnitTeam.Player
                ? new Color(0.4f, 0.8f, 1f)   // Light blue for player summons
                : new Color(1f, 0.5f, 0.3f);   // Orange for enemy summons

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"{caster.Unit.DisplayName}'s Summon";
            go.transform.position = spawnPos;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f); // Slightly smaller

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = summonColor;

            go.AddComponent<CharacterController>();
            go.AddComponent<HealthSystem>();
            var battleUnit = go.AddComponent<TerrainBattleUnit>();
            battleUnit.Initialize(summonUnit);

            summonUnit.visualInstance = go;

            var list = caster.Unit.team == UnitTeam.Player ? _playerUnits : _enemyUnits;
            list.Add(battleUnit);
            _allUnits.Add(battleUnit);

            _activeSummons[casterId] = battleUnit;

            Debug.Log($"  {caster.Unit.DisplayName} uses [{tech.techniqueName}] — SUMMONED! " +
                      $"(HP:{summonUnit.maxHP} ATK:{summonUnit.currentStats.attack})");
        }

        // ── Target Finding (called by TerrainBattleUnit) ─────────────

        public TerrainBattleUnit GetNearestEnemy(TerrainBattleUnit unit)
        {
            var enemies = unit.Unit.team == UnitTeam.Player ? _enemyUnits : _playerUnits;
            TerrainBattleUnit nearest = null;
            float minDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (enemy.IsDead) continue;
                float dist = Vector3.Distance(unit.transform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }

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
                _cam = FindFirstObjectByType<ThirdPersonCamera>();
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
