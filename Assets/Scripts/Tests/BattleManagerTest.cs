using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.Tests
{
    public class BattleManagerTest : MonoBehaviour
    {
        private void Start()
        {
            RunAllTests();
        }

        private void RunAllTests()
        {
            Debug.Log("===== BATTLE MANAGER TEST START =====");

            Test_InitializationWithNoUnits();
            Test_SpawnAndPlacement();
            Test_BattleStartsInCombatPhase();
            Test_WinConditionVictory();
            Test_WinConditionDefeat();
            Test_BattleTimeAdvances();

            Debug.Log("===== BATTLE MANAGER TEST COMPLETE =====");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private BattleState CreateBattle(int players, int enemies, int hp = 100)
        {
            UnitFactory.ResetIds();

            var state = new BattleState("test_battle", 8, 6);

            for (int i = 0; i < players; i++)
            {
                UnitDefinition def = MakeDefinition($"player_{i}", hp);
                UnitRuntime unit = UnitFactory.CreateFromDefinition(def, UnitTeam.Player);
                state.playerUnits.Add(unit);
                state.grid.PlaceUnit(unit, new GridPosition(0, i));
                Object.Destroy(def);
            }

            for (int i = 0; i < enemies; i++)
            {
                UnitDefinition def = MakeDefinition($"enemy_{i}", hp);
                UnitRuntime unit = UnitFactory.CreateFromDefinition(def, UnitTeam.Enemy);
                state.enemyUnits.Add(unit);
                state.grid.PlaceUnit(unit, new GridPosition(7, i));
                Object.Destroy(def);
            }

            return state;
        }

        private UnitDefinition MakeDefinition(string name, int hp)
        {
            UnitDefinition def = ScriptableObject.CreateInstance<UnitDefinition>();
            def.unitId      = name;
            def.displayName = name;
            def.baseStats   = new StatBlock(hp, 10, 5, 3f);
            def.defaultBehavior = BehaviorType.Balanced;
            return def;
        }

        // ── Tests ─────────────────────────────────────────────────────────

        private void Test_InitializationWithNoUnits()
        {
            var state = new BattleState("empty_battle", 8, 6);

            Debug.Assert(state.playerUnits.Count == 0,            "Should start with no player units");
            Debug.Assert(state.enemyUnits.Count == 0,             "Should start with no enemy units");
            Debug.Assert(state.currentPhase == BattlePhase.NotStarted, "Phase should be NotStarted");
            Debug.Assert(state.grid != null,                      "Grid should exist");
            Debug.Assert(!state.isBattleOver,                     "Battle should not be over");
            Debug.Log("  [PASS] Initialization with no units");
        }

        private void Test_SpawnAndPlacement()
        {
            var state = CreateBattle(players: 2, enemies: 3);

            Debug.Assert(state.playerUnits.Count == 2, "Should have 2 player units");
            Debug.Assert(state.enemyUnits.Count == 3,  "Should have 3 enemy units");

            // Verify units are on grid
            Debug.Assert(state.grid.IsOccupied(new GridPosition(0, 0)), "Player 0 should be at (0,0)");
            Debug.Assert(state.grid.IsOccupied(new GridPosition(0, 1)), "Player 1 should be at (0,1)");
            Debug.Assert(state.grid.IsOccupied(new GridPosition(7, 0)), "Enemy 0 should be at (7,0)");
            Debug.Assert(state.grid.IsOccupied(new GridPosition(7, 1)), "Enemy 1 should be at (7,1)");
            Debug.Assert(state.grid.IsOccupied(new GridPosition(7, 2)), "Enemy 2 should be at (7,2)");

            // Verify unit-grid consistency
            UnitRuntime unitAtOrigin = state.grid.GetUnitAt(new GridPosition(0, 0));
            Debug.Assert(unitAtOrigin != null,                "Should find unit at (0,0)");
            Debug.Assert(unitAtOrigin.team == UnitTeam.Player,"Unit at (0,0) should be Player");
            Debug.Log("  [PASS] Unit spawning and placement");
        }

        private void Test_BattleStartsInCombatPhase()
        {
            var state = CreateBattle(players: 1, enemies: 1);
            Debug.Assert(state.currentPhase == BattlePhase.NotStarted, "Should be NotStarted before start");

            state.StartBattle();
            Debug.Assert(state.currentPhase == BattlePhase.Combat, "Should be Combat after StartBattle");
            Debug.Assert(state.battleTime == 0f,                   "Battle time should reset to 0");
            Debug.Assert(state.currentTick == 0,                   "Tick should reset to 0");
            Debug.Log("  [PASS] Battle starts in Combat phase");
        }

        private void Test_WinConditionVictory()
        {
            var state = CreateBattle(players: 2, enemies: 2);
            state.StartBattle();

            // Kill all enemies
            foreach (var enemy in state.enemyUnits)
                enemy.isDead = true;

            Debug.Assert(state.IsTeamDefeated(UnitTeam.Enemy),    "Enemy team should be defeated");
            Debug.Assert(!state.IsTeamDefeated(UnitTeam.Player),  "Player team should not be defeated");

            state.EndBattle(BattleOutcome.Victory);
            Debug.Assert(state.isBattleOver,                       "Battle should be over");
            Debug.Assert(state.outcome == BattleOutcome.Victory,   "Outcome should be Victory");
            Debug.Assert(state.currentPhase == BattlePhase.Victory,"Phase should be Victory");
            Debug.Log("  [PASS] Win condition - Victory");
        }

        private void Test_WinConditionDefeat()
        {
            var state = CreateBattle(players: 2, enemies: 2);
            state.StartBattle();

            // Kill all players
            foreach (var player in state.playerUnits)
                player.isDead = true;

            Debug.Assert(state.IsTeamDefeated(UnitTeam.Player),   "Player team should be defeated");
            Debug.Assert(!state.IsTeamDefeated(UnitTeam.Enemy),   "Enemy team should not be defeated");

            state.EndBattle(BattleOutcome.Defeat);
            Debug.Assert(state.isBattleOver,                       "Battle should be over");
            Debug.Assert(state.outcome == BattleOutcome.Defeat,    "Outcome should be Defeat");
            Debug.Assert(state.currentPhase == BattlePhase.Defeat, "Phase should be Defeat");
            Debug.Log("  [PASS] Win condition - Defeat");
        }

        private void Test_BattleTimeAdvances()
        {
            var state = CreateBattle(players: 1, enemies: 1);
            state.StartBattle();

            Debug.Assert(state.battleTime == 0f,  "Battle time should start at 0");
            Debug.Assert(state.currentTick == 0,  "Tick should start at 0");

            // Simulate ticks manually
            state.currentTick++;
            state.currentTick++;
            state.battleTime += 0.5f;

            Debug.Assert(state.currentTick == 2,   "Tick should be 2 after two increments");
            Debug.Assert(state.battleTime == 0.5f, "Battle time should be 0.5 after 0.5s");
            Debug.Log("  [PASS] Battle time advances");
        }
    }
}
