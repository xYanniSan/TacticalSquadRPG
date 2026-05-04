using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.Tests
{
    public class BehaviorSystemTest : MonoBehaviour
    {
        private void Start()
        {
            RunAllTests();
        }

        private void RunAllTests()
        {
            Debug.Log("===== BEHAVIOR SYSTEM TEST START =====");

            Test_Aggressive_MovesTowardEnemy();
            Test_Aggressive_AttacksWhenAdjacent();
            Test_Defensive_WaitsWhenNotAdjacent();
            Test_Defensive_AttacksWhenAdjacent();
            Test_Balanced_MovesTowardEnemy();
            Test_Balanced_AttacksWhenAdjacent();
            Test_Balanced_PicksLowestHPTarget();
            Test_NoEnemies_ReturnsWait();
            Test_GenerateAllIntents();

            Debug.Log("===== BEHAVIOR SYSTEM TEST COMPLETE =====");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private (BattleState state, BehaviorSystem behavior) MakeBattle()
        {
            UnitFactory.ResetIds();
            var state    = new BattleState("test", 8, 6);
            var behavior = new BehaviorSystem();
            state.StartBattle();
            return (state, behavior);
        }

        private UnitRuntime MakeUnit(BattleState state, UnitTeam team, GridPosition pos,
            BehaviorType behaviorType = BehaviorType.Aggressive, int hp = 100, int moveSpeed = 3)
        {
            UnitDefinition def = ScriptableObject.CreateInstance<UnitDefinition>();
            def.unitId      = $"{team}_{pos.x}_{pos.y}";
            def.displayName = def.unitId;
            def.baseStats   = new StatBlock(hp, 10, 5, moveSpeed);
            def.defaultBehavior = behaviorType;

            UnitRuntime unit = UnitFactory.CreateFromDefinition(def, team);
            unit.behavior = new BehaviorLoadout(behaviorType);
            state.grid.PlaceUnit(unit, pos);

            if (team == UnitTeam.Player) state.playerUnits.Add(unit);
            else                         state.enemyUnits.Add(unit);

            Object.Destroy(def);
            return unit;
        }

        // ── Tests ─────────────────────────────────────────────────────────

        private void Test_Aggressive_MovesTowardEnemy()
        {
            var (state, behavior) = MakeBattle();
            UnitRuntime player = MakeUnit(state, UnitTeam.Player, new GridPosition(0, 0), BehaviorType.Aggressive);
            UnitRuntime enemy  = MakeUnit(state, UnitTeam.Enemy,  new GridPosition(5, 0));

            UnitIntent intent = behavior.GenerateIntent(player, state);

            Debug.Assert(intent.type == IntentType.Move,        "Aggressive unit should Move when not adjacent");
            Debug.Assert(intent.target == enemy,                 "Target should be the enemy");
            Debug.Assert(intent.targetPosition.x > player.position.x, "Should move closer (x increases)");
            Debug.Log("  [PASS] Aggressive - moves toward enemy");
        }

        private void Test_Aggressive_AttacksWhenAdjacent()
        {
            var (state, behavior) = MakeBattle();
            UnitRuntime player = MakeUnit(state, UnitTeam.Player, new GridPosition(0, 0), BehaviorType.Aggressive);
            UnitRuntime enemy  = MakeUnit(state, UnitTeam.Enemy,  new GridPosition(1, 0));

            UnitIntent intent = behavior.GenerateIntent(player, state);

            Debug.Assert(intent.type == IntentType.BasicAttack, "Aggressive unit should attack when adjacent");
            Debug.Assert(intent.target == enemy,                 "Should target the adjacent enemy");
            Debug.Log("  [PASS] Aggressive - attacks when adjacent");
        }

        private void Test_Defensive_WaitsWhenNotAdjacent()
        {
            var (state, behavior) = MakeBattle();
            UnitRuntime player = MakeUnit(state, UnitTeam.Player, new GridPosition(0, 0), BehaviorType.Defensive);
            MakeUnit(state, UnitTeam.Enemy, new GridPosition(5, 0));

            UnitIntent intent = behavior.GenerateIntent(player, state);

            Debug.Assert(intent.type == IntentType.Wait, "Defensive unit should Wait when enemy is not adjacent");
            Debug.Log("  [PASS] Defensive - waits when not adjacent");
        }

        private void Test_Defensive_AttacksWhenAdjacent()
        {
            var (state, behavior) = MakeBattle();
            UnitRuntime player = MakeUnit(state, UnitTeam.Player, new GridPosition(0, 0), BehaviorType.Defensive);
            UnitRuntime enemy  = MakeUnit(state, UnitTeam.Enemy,  new GridPosition(1, 0));

            UnitIntent intent = behavior.GenerateIntent(player, state);

            Debug.Assert(intent.type == IntentType.BasicAttack, "Defensive unit should attack adjacent enemy");
            Debug.Assert(intent.target == enemy,                 "Should target the adjacent enemy");
            Debug.Log("  [PASS] Defensive - attacks when adjacent");
        }

        private void Test_Balanced_MovesTowardEnemy()
        {
            var (state, behavior) = MakeBattle();
            UnitRuntime player = MakeUnit(state, UnitTeam.Player, new GridPosition(0, 0), BehaviorType.Balanced);
            MakeUnit(state, UnitTeam.Enemy, new GridPosition(5, 0));

            UnitIntent intent = behavior.GenerateIntent(player, state);

            Debug.Assert(intent.type == IntentType.Move, "Balanced unit should Move when not adjacent");
            Debug.Log("  [PASS] Balanced - moves toward enemy");
        }

        private void Test_Balanced_AttacksWhenAdjacent()
        {
            var (state, behavior) = MakeBattle();
            UnitRuntime player = MakeUnit(state, UnitTeam.Player, new GridPosition(0, 0), BehaviorType.Balanced);
            UnitRuntime enemy  = MakeUnit(state, UnitTeam.Enemy,  new GridPosition(1, 0));

            UnitIntent intent = behavior.GenerateIntent(player, state);

            Debug.Assert(intent.type == IntentType.BasicAttack, "Balanced unit should attack when adjacent");
            Debug.Assert(intent.target == enemy,                 "Should target the adjacent enemy");
            Debug.Log("  [PASS] Balanced - attacks when adjacent");
        }

        private void Test_Balanced_PicksLowestHPTarget()
        {
            var (state, behavior) = MakeBattle();
            UnitRuntime player      = MakeUnit(state, UnitTeam.Player, new GridPosition(3, 0), BehaviorType.Balanced);
            UnitRuntime highHPEnemy = MakeUnit(state, UnitTeam.Enemy,  new GridPosition(4, 0), hp: 80);
            UnitRuntime lowHPEnemy  = MakeUnit(state, UnitTeam.Enemy,  new GridPosition(3, 1), hp: 20);

            // Both enemies are adjacent — balanced should target the lower HP one
            UnitIntent intent = behavior.GenerateIntent(player, state);

            Debug.Assert(intent.type == IntentType.BasicAttack, "Should attack (both enemies adjacent)");
            Debug.Assert(intent.target == lowHPEnemy,            "Should target the lower HP enemy");
            Debug.Log("  [PASS] Balanced - picks lowest HP target");
        }

        private void Test_NoEnemies_ReturnsWait()
        {
            var (state, behavior) = MakeBattle();
            UnitRuntime player = MakeUnit(state, UnitTeam.Player, new GridPosition(0, 0), BehaviorType.Aggressive);

            UnitIntent intent = behavior.GenerateIntent(player, state);

            Debug.Assert(intent.type == IntentType.Wait, "Should Wait when there are no enemies");
            Debug.Log("  [PASS] No enemies - returns Wait");
        }

        private void Test_GenerateAllIntents()
        {
            var (state, behavior) = MakeBattle();
            MakeUnit(state, UnitTeam.Player, new GridPosition(0, 0), BehaviorType.Aggressive);
            MakeUnit(state, UnitTeam.Player, new GridPosition(0, 1), BehaviorType.Defensive);
            MakeUnit(state, UnitTeam.Enemy,  new GridPosition(7, 0));
            MakeUnit(state, UnitTeam.Enemy,  new GridPosition(7, 1));

            List<UnitIntent> intents = behavior.GenerateAllIntents(state);

            Debug.Assert(intents.Count == 4, $"Should generate 4 intents (got {intents.Count})");

            foreach (UnitIntent intent in intents)
            {
                Debug.Assert(intent != null,        "Each intent should not be null");
                Debug.Assert(intent.actor != null,  "Each intent should have an actor");
                Debug.Assert(intent.actor.currentIntent == intent, "Intent should be stored on unit");
            }

            Debug.Log("  [PASS] GenerateAllIntents - all units get intents");
        }
    }
}
