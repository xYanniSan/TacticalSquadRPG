using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Tests
{
    public class BattleStateTest : MonoBehaviour
    {
        private void Start()
        {
            RunAllTests();
        }

        private void RunAllTests()
        {
            Debug.Log("===== BATTLE STATE SYSTEM TEST START =====");

            Test_Initialization();
            Test_PhaseTransitions();
            Test_UnitTracking();
            Test_LivingUnitQueries();
            Test_TeamDefeatedCheck();
            Test_WinAndLoseConditions();
            Test_StatBlock();

            Debug.Log("===== BATTLE STATE SYSTEM TEST COMPLETE =====");
        }

        private void Test_Initialization()
        {
            BattleState state = new BattleState("battle_001", 8, 6);
            Debug.Assert(state.battleId == "battle_001",          "Battle ID should be set");
            Debug.Assert(state.currentPhase == BattlePhase.NotStarted, "Phase should start as NotStarted");
            Debug.Assert(state.playerUnits != null,               "Player units list should exist");
            Debug.Assert(state.enemyUnits != null,                "Enemy units list should exist");
            Debug.Assert(state.grid != null,                      "Grid should be initialized");
            Debug.Assert(state.grid.width == 8,                   "Grid width should be 8");
            Debug.Assert(state.grid.height == 6,                  "Grid height should be 6");
            Debug.Assert(!state.isBattleOver,                     "Battle should not be over at start");
            Debug.Assert(state.outcome == BattleOutcome.None,     "Outcome should start as None");
            Debug.Log("  [PASS] Initialization");
        }

        private void Test_PhaseTransitions()
        {
            BattleState state = new BattleState("battle_002", 8, 6);
            Debug.Assert(state.currentPhase == BattlePhase.NotStarted, "Should start as NotStarted");

            state.StartBattle();
            Debug.Assert(state.currentPhase == BattlePhase.Combat, "Should be Combat after StartBattle");
            Debug.Assert(state.battleTime == 0f,                   "Battle time should reset to 0");
            Debug.Assert(state.currentTick == 0,                   "Tick should reset to 0");

            state.EndBattle(BattleOutcome.Victory);
            Debug.Assert(state.currentPhase == BattlePhase.Victory, "Should be Victory after win");

            BattleState state2 = new BattleState("battle_003", 8, 6);
            state2.StartBattle();
            state2.EndBattle(BattleOutcome.Defeat);
            Debug.Assert(state2.currentPhase == BattlePhase.Defeat, "Should be Defeat after loss");
            Debug.Log("  [PASS] Phase transitions");
        }

        private void Test_UnitTracking()
        {
            BattleState state = new BattleState("battle_004", 8, 6);

            UnitRuntime player1 = new UnitRuntime { runtimeId = 1, team = UnitTeam.Player, currentHP = 100, maxHP = 100 };
            UnitRuntime player2 = new UnitRuntime { runtimeId = 2, team = UnitTeam.Player, currentHP = 80,  maxHP = 80  };
            UnitRuntime enemy1  = new UnitRuntime { runtimeId = 3, team = UnitTeam.Enemy,  currentHP = 60,  maxHP = 60  };

            state.playerUnits.Add(player1);
            state.playerUnits.Add(player2);
            state.enemyUnits.Add(enemy1);

            Debug.Assert(state.playerUnits.Count == 2, "Should have 2 player units");
            Debug.Assert(state.enemyUnits.Count == 1,  "Should have 1 enemy unit");

            List<UnitRuntime> all = state.GetAllUnits();
            Debug.Assert(all.Count == 3, "GetAllUnits should return 3 units");
            Debug.Log("  [PASS] Unit tracking");
        }

        private void Test_LivingUnitQueries()
        {
            BattleState state = new BattleState("battle_005", 8, 6);

            UnitRuntime alive = new UnitRuntime { runtimeId = 1, team = UnitTeam.Player, isDead = false };
            UnitRuntime dead  = new UnitRuntime { runtimeId = 2, team = UnitTeam.Player, isDead = true  };
            state.playerUnits.Add(alive);
            state.playerUnits.Add(dead);

            List<UnitRuntime> living = state.GetLivingUnits(UnitTeam.Player);
            Debug.Assert(living.Count == 1,        "Should return only 1 living unit");
            Debug.Assert(living[0] == alive,       "Living unit should be the alive one");
            Debug.Log("  [PASS] Living unit queries");
        }

        private void Test_TeamDefeatedCheck()
        {
            BattleState state = new BattleState("battle_006", 8, 6);

            UnitRuntime enemy1 = new UnitRuntime { runtimeId = 1, team = UnitTeam.Enemy, isDead = false };
            UnitRuntime enemy2 = new UnitRuntime { runtimeId = 2, team = UnitTeam.Enemy, isDead = false };
            state.enemyUnits.Add(enemy1);
            state.enemyUnits.Add(enemy2);

            Debug.Assert(!state.IsTeamDefeated(UnitTeam.Enemy), "Team should not be defeated while units are alive");

            enemy1.isDead = true;
            Debug.Assert(!state.IsTeamDefeated(UnitTeam.Enemy), "Team should not be defeated while one unit is alive");

            enemy2.isDead = true;
            Debug.Assert(state.IsTeamDefeated(UnitTeam.Enemy),  "Team should be defeated when all units are dead");

            Debug.Assert(!state.IsTeamDefeated(UnitTeam.Player), "Empty player team should not count as defeated");
            Debug.Log("  [PASS] Team defeated check");
        }

        private void Test_WinAndLoseConditions()
        {
            BattleState state = new BattleState("battle_007", 8, 6);
            state.StartBattle();

            state.EndBattle(BattleOutcome.Victory);
            Debug.Assert(state.isBattleOver,                     "Battle should be over");
            Debug.Assert(state.outcome == BattleOutcome.Victory, "Outcome should be Victory");
            Debug.Assert(state.currentPhase == BattlePhase.Victory, "Phase should be Victory");

            BattleState state2 = new BattleState("battle_008", 8, 6);
            state2.StartBattle();
            state2.EndBattle(BattleOutcome.Defeat);
            Debug.Assert(state2.isBattleOver,                      "Battle should be over");
            Debug.Assert(state2.outcome == BattleOutcome.Defeat,   "Outcome should be Defeat");
            Debug.Assert(state2.currentPhase == BattlePhase.Defeat, "Phase should be Defeat");
            Debug.Log("  [PASS] Win and lose conditions");
        }

        private void Test_StatBlock()
        {
            StatBlock stats = new StatBlock(100, 15, 8, 3.5f);
            Debug.Assert(stats.maxHP == 100,       "maxHP should be 100");
            Debug.Assert(stats.attack == 15,       "attack should be 15");
            Debug.Assert(stats.defense == 8,       "defense should be 8");
            Debug.Assert(stats.moveSpeed == 3.5f,  "moveSpeed should be 3.5");

            StatBlock defaults = StatBlock.Default;
            Debug.Assert(defaults.maxHP > 0,       "Default stats should have positive HP");
            Debug.Log("  [PASS] StatBlock");
        }
    }
}
