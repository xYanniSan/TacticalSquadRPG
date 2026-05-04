using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.Tests
{
    public class CombatResolutionSystemTest : MonoBehaviour
    {
        private void Start()
        {
            RunAllTests();
        }

        private void RunAllTests()
        {
            Debug.Log("===== COMBAT RESOLUTION TEST START =====");

            Test_BasicAttack_DealsDamage();
            Test_BasicAttack_MinimumOneDamage();
            Test_BasicAttack_CanKill();
            Test_TechniqueAttack_DealsDamage();
            Test_TechniqueAttack_UsesDefense();

            Debug.Log("===== COMBAT RESOLUTION TEST COMPLETE =====");
        }

        // ── Helpers ───────────────────────────────────────────────────

        private UnitRuntime MakeUnit(int hp = 100, int attack = 10, int defense = 5)
        {
            UnitFactory.ResetIds();
            UnitDefinition def = ScriptableObject.CreateInstance<UnitDefinition>();
            def.unitId      = "test";
            def.displayName = "Test";
            def.baseStats   = new StatBlock(hp, attack, defense, 3f);

            UnitRuntime unit = UnitFactory.CreateFromDefinition(def, UnitTeam.Player);
            Object.Destroy(def);
            return unit;
        }

        // ── Tests ─────────────────────────────────────────────────────

        private void Test_BasicAttack_DealsDamage()
        {
            var system   = new CombatResolutionSystem();
            var attacker = MakeUnit(attack: 15, defense: 5);
            var defender = MakeUnit(hp: 100, defense: 5);

            CombatContext ctx = system.ResolveBasicAttack(attacker, defender);

            // 15 attack - 5 defense = 10 damage
            Debug.Assert(ctx.finalDamage == 10,     $"Expected 10 dmg, got {ctx.finalDamage}");
            Debug.Assert(defender.currentHP == 90,   $"Expected 90 HP, got {defender.currentHP}");
            Debug.Assert(ctx.IsBasicAttack,          "Should be basic attack");
            Debug.Log("  [PASS] Basic attack deals damage");
        }

        private void Test_BasicAttack_MinimumOneDamage()
        {
            var system   = new CombatResolutionSystem();
            var attacker = MakeUnit(attack: 3);
            var defender = MakeUnit(hp: 100, defense: 50);

            CombatContext ctx = system.ResolveBasicAttack(attacker, defender);

            Debug.Assert(ctx.finalDamage == 1,       $"Expected min 1 dmg, got {ctx.finalDamage}");
            Debug.Assert(defender.currentHP == 99,   $"Expected 99 HP, got {defender.currentHP}");
            Debug.Log("  [PASS] Basic attack minimum 1 damage");
        }

        private void Test_BasicAttack_CanKill()
        {
            var system   = new CombatResolutionSystem();
            var attacker = MakeUnit(attack: 50);
            var defender = MakeUnit(hp: 10, defense: 0);

            system.ResolveBasicAttack(attacker, defender);

            Debug.Assert(defender.isDead,            "Defender should be dead");
            Debug.Assert(defender.currentHP == 0,    "HP should be 0");
            Debug.Log("  [PASS] Basic attack can kill");
        }

        private void Test_TechniqueAttack_DealsDamage()
        {
            var system   = new CombatResolutionSystem();
            var attacker = MakeUnit(attack: 10);
            var defender = MakeUnit(hp: 100, defense: 5);

            var technique = new ResolvedTechnique
            {
                techniqueName = "Fire Strike",
                type          = TechniqueType.Attack,
                element       = ElementType.Fire,
                power         = 30,
                targetPattern = TargetPattern.Single
            };

            CombatContext ctx = system.ResolveTechnique(attacker, defender, technique);

            // 30 power - 5 defense = 25 damage
            Debug.Assert(ctx.finalDamage == 25,      $"Expected 25 dmg, got {ctx.finalDamage}");
            Debug.Assert(defender.currentHP == 75,   $"Expected 75 HP, got {defender.currentHP}");
            Debug.Assert(!ctx.IsBasicAttack,         "Should not be basic attack");
            Debug.Log("  [PASS] Technique attack deals damage");
        }

        private void Test_TechniqueAttack_UsesDefense()
        {
            var system   = new CombatResolutionSystem();
            var attacker = MakeUnit();
            var weakDef  = MakeUnit(hp: 100, defense: 5);
            var strongDef = MakeUnit(hp: 100, defense: 20);

            var tech = new ResolvedTechnique
            {
                techniqueName = "Strike",
                type          = TechniqueType.Attack,
                power         = 30,
                targetPattern = TargetPattern.Single
            };

            var ctx1 = system.ResolveTechnique(attacker, weakDef, tech);
            var ctx2 = system.ResolveTechnique(attacker, strongDef, tech);

            Debug.Assert(ctx1.finalDamage > ctx2.finalDamage,
                $"Low defense should take more dmg ({ctx1.finalDamage} vs {ctx2.finalDamage})");
            Debug.Log("  [PASS] Technique uses defender's defense");
        }
    }
}
