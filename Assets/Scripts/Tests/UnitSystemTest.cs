using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.Tests
{
    public class UnitSystemTest : MonoBehaviour
    {
        private void Start()
        {
            RunAllTests();
        }

        private void RunAllTests()
        {
            Debug.Log("===== UNIT SYSTEM TEST START =====");

            Test_BehaviorLoadout();
            Test_ProficiencySet();
            Test_UnitRuntimeManual();
            Test_UnitFactory();
            Test_UnitHP();

            Debug.Log("===== UNIT SYSTEM TEST COMPLETE =====");
        }

        private void Test_BehaviorLoadout()
        {
            BehaviorLoadout aggressive = new BehaviorLoadout(BehaviorType.Aggressive);
            Debug.Assert(aggressive.behaviorType == BehaviorType.Aggressive, "Should be Aggressive");

            BehaviorLoadout defaultLoadout = new BehaviorLoadout();
            Debug.Assert(defaultLoadout.behaviorType == BehaviorType.Balanced, "Default should be Balanced");

            BehaviorLoadout defensive = new BehaviorLoadout(BehaviorType.Defensive);
            Debug.Assert(defensive.behaviorType == BehaviorType.Defensive, "Should be Defensive");
            Debug.Log("  [PASS] BehaviorLoadout");
        }

        private void Test_ProficiencySet()
        {
            ProficiencySet profs = new ProficiencySet();

            Debug.Assert(profs.GetProficiencyBonus(ActionType.Physical)  == 1.0f, "Default physical should be 1.0");
            Debug.Assert(profs.GetProficiencyBonus(ElementType.Fire)     == 1.0f, "Default fire should be 1.0");
            Debug.Assert(profs.GetProficiencyBonus(TechniqueType.Attack) == 1.0f, "Default attack should be 1.0");

            profs.actionProficiencies[ActionType.Physical]  = 1.3f;
            profs.elementProficiencies[ElementType.Fire]    = 0.8f;
            profs.techniqueProficiencies[TechniqueType.Heal] = 1.5f;

            Debug.Assert(profs.GetProficiencyBonus(ActionType.Physical)   == 1.3f, "Physical bonus should be 1.3");
            Debug.Assert(profs.GetProficiencyBonus(ElementType.Fire)      == 0.8f, "Fire penalty should be 0.8");
            Debug.Assert(profs.GetProficiencyBonus(TechniqueType.Heal)    == 1.5f, "Heal bonus should be 1.5");
            Debug.Assert(profs.GetProficiencyBonus(ElementType.Lightning) == 1.0f, "Unset element should be 1.0");
            Debug.Log("  [PASS] ProficiencySet");
        }

        private void Test_UnitRuntimeManual()
        {
            UnitRuntime unit = new UnitRuntime
            {
                runtimeId  = 99,
                team       = UnitTeam.Enemy,
                currentHP  = 50,
                maxHP      = 100,
                isDead     = false
            };

            Debug.Assert(unit.runtimeId == 99,           "runtimeId should be 99");
            Debug.Assert(unit.team == UnitTeam.Enemy,    "Team should be Enemy");
            Debug.Assert(unit.currentHP == 50,           "currentHP should be 50");
            Debug.Assert(!unit.isDead,                   "Should not be dead");
            Debug.Assert(unit.DisplayName == "Unit_99",  "DisplayName without definition should use fallback");
            Debug.Log("  [PASS] UnitRuntime manual creation");
        }

        private void Test_UnitFactory()
        {
            UnitFactory.ResetIds();

            UnitDefinition def = ScriptableObject.CreateInstance<UnitDefinition>();
            def.unitId        = "hero_test";
            def.displayName   = "Test Hero";
            def.baseStats     = new StatBlock(150, 25, 12, 4f);
            def.defaultBehavior = BehaviorType.Aggressive;

            UnitRuntime unit = UnitFactory.CreateFromDefinition(def, UnitTeam.Player);

            Debug.Assert(unit.runtimeId == 1,                                    "First unit ID should be 1");
            Debug.Assert(unit.team == UnitTeam.Player,                           "Team should be Player");
            Debug.Assert(unit.maxHP == 150,                                      "maxHP should match definition");
            Debug.Assert(unit.currentHP == 150,                                  "Should start at full HP");
            Debug.Assert(unit.currentStats.attack == 25,                         "Attack should match definition");
            Debug.Assert(unit.behavior.behaviorType == BehaviorType.Aggressive,  "Behavior should be Aggressive");
            Debug.Assert(unit.definition == def,                                 "Definition reference should match");
            Debug.Assert(unit.equippedSkills != null,                            "equippedSkills should be initialized");
            Debug.Assert(unit.activeEffects != null,                             "activeEffects should be initialized");
            Debug.Assert(!unit.isDead,                                           "Should not start dead");
            Debug.Assert(unit.DisplayName == "Test Hero",                        "DisplayName should come from definition");

            UnitRuntime unit2 = UnitFactory.CreateFromDefinition(def, UnitTeam.Enemy);
            Debug.Assert(unit2.runtimeId == 2,           "Second unit ID should be 2");
            Debug.Assert(unit2.team == UnitTeam.Enemy,   "Second unit should be Enemy");

            Object.Destroy(def);
            Debug.Log("  [PASS] UnitFactory");
        }

        private void Test_UnitHP()
        {
            UnitRuntime unit = new UnitRuntime { currentHP = 100, maxHP = 100 };

            unit.TakeDamage(30);
            Debug.Assert(unit.currentHP == 70, "HP should be 70 after 30 damage");
            Debug.Assert(!unit.isDead,         "Should not be dead at 70 HP");

            unit.TakeDamage(80);
            Debug.Assert(unit.currentHP == 0,  "HP should not go below 0");
            Debug.Assert(unit.isDead,          "Should be dead at 0 HP");

            unit.isDead   = false;
            unit.currentHP = 50;
            unit.Heal(30);
            Debug.Assert(unit.currentHP == 80,  "HP should be 80 after healing 30");

            unit.Heal(100);
            Debug.Assert(unit.currentHP == 100, "HP should not exceed maxHP");
            Debug.Log("  [PASS] Unit HP system");
        }
    }
}
