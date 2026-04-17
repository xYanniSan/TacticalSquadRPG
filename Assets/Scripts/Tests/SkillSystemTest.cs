using TacticalRPG.DataModels;
using TacticalRPG.Systems;
using UnityEngine;

namespace TacticalRPG.Tests
{
    public class SkillSystemTest : MonoBehaviour
    {
        private void Start()
        {
            RunAllTests();
        }

        private void RunAllTests()
        {
            Debug.Log("===== SKILL SYSTEM TEST START =====");

            Test_EmptySlot_ZeroPower();
            Test_SingleAction_CorrectPower();
            Test_MultipleActions_SumsPower();
            Test_DominantElement_Detected();
            Test_MixedElements_DominantWins();
            Test_AllNoneElement_ElementIsNone();
            Test_PhysicalActions_TypeIsAttack();
            Test_SupportActions_TypeIsBuff();
            Test_ElementProficiency_AffectsPower();
            Test_AttackStat_AffectsPower();
            Test_AttackTechnique_TargetIsSingle();
            Test_BuffTechnique_TargetIsSelf();

            Debug.Log("===== SKILL SYSTEM TEST COMPLETE =====");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private UnitRuntime MakeUnit(int attack = 10)
        {
            UnitFactory.ResetIds();
            UnitDefinition def = ScriptableObject.CreateInstance<UnitDefinition>();
            def.unitId      = "test";
            def.displayName = "Test";
            def.baseStats   = new StatBlock(100, attack, 5, 3f);
            def.defaultBehavior = BehaviorType.Balanced;

            UnitRuntime unit = UnitFactory.CreateFromDefinition(def, UnitTeam.Player);
            Object.Destroy(def);
            return unit;
        }

        private ActionDefinition MakeAction(float power, ActionType type, ElementType element = ElementType.None)
        {
            ActionDefinition def = ScriptableObject.CreateInstance<ActionDefinition>();
            def.actionId    = "test_action";
            def.displayName = "Test Action";
            def.basePower   = power;
            def.actionType  = type;
            def.element     = element;
            return def;
        }

        private SkillSlot MakeSlot(params ActionDefinition[] actions)
        {
            var slot = new SkillSlot(0);
            foreach (ActionDefinition action in actions)
                slot.AddAction(action);
            return slot;
        }

        // ── Tests ─────────────────────────────────────────────────────────

        private void Test_EmptySlot_ZeroPower()
        {
            var skill  = new SkillSystem();
            var slot   = new SkillSlot(0);
            var caster = MakeUnit(attack: 10);

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result != null,     "Should return a technique even for empty slot");
            Debug.Assert(result.power == 0,  "Empty slot should produce zero power");
            Debug.Log("  [PASS] Empty slot - zero power");
        }

        private void Test_SingleAction_CorrectPower()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit(attack: 10);
            // basePower=10, attack=10 → power = 10 * (10/10) = 10
            var slot   = MakeSlot(MakeAction(10f, ActionType.Physical));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result.power == 10, $"Expected power 10, got {result.power}");
            Debug.Log("  [PASS] Single action - correct power");
        }

        private void Test_MultipleActions_SumsPower()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit(attack: 10);
            // 3 x basePower=10, attack=10 → power = 30 * 1.0 = 30
            var slot   = MakeSlot(
                MakeAction(10f, ActionType.Physical),
                MakeAction(10f, ActionType.Physical),
                MakeAction(10f, ActionType.Physical));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result.power == 30, $"Expected power 30, got {result.power}");
            Debug.Assert(result.sourceActions.Count == 3, "Should have 3 source actions");
            Debug.Log("  [PASS] Multiple actions - sums power");
        }

        private void Test_DominantElement_Detected()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit();
            // 2 Fire + 1 Lightning → dominant = Fire
            var slot   = MakeSlot(
                MakeAction(10f, ActionType.Elemental, ElementType.Fire),
                MakeAction(10f, ActionType.Elemental, ElementType.Fire),
                MakeAction(10f, ActionType.Elemental, ElementType.Lightning));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result.element == ElementType.Fire,
                $"Expected Fire, got {result.element}");
            Debug.Log("  [PASS] Dominant element detected");
        }

        private void Test_MixedElements_DominantWins()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit();
            // 3 Earth + 1 Water → Earth
            var slot   = MakeSlot(
                MakeAction(5f, ActionType.Elemental, ElementType.Earth),
                MakeAction(5f, ActionType.Elemental, ElementType.Earth),
                MakeAction(5f, ActionType.Elemental, ElementType.Earth),
                MakeAction(5f, ActionType.Elemental, ElementType.Water));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result.element == ElementType.Earth,
                $"Expected Earth, got {result.element}");
            Debug.Log("  [PASS] Mixed elements - dominant wins");
        }

        private void Test_AllNoneElement_ElementIsNone()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit();
            var slot   = MakeSlot(
                MakeAction(10f, ActionType.Physical, ElementType.None),
                MakeAction(10f, ActionType.Physical, ElementType.None));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result.element == ElementType.None,
                $"Expected None, got {result.element}");
            Debug.Log("  [PASS] All None element - element is None");
        }

        private void Test_PhysicalActions_TypeIsAttack()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit();
            var slot   = MakeSlot(
                MakeAction(10f, ActionType.Physical),
                MakeAction(10f, ActionType.Physical));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result.type == TechniqueType.Attack,
                $"Expected Attack, got {result.type}");
            Debug.Log("  [PASS] Physical actions - type is Attack");
        }

        private void Test_SupportActions_TypeIsBuff()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit();
            var slot   = MakeSlot(
                MakeAction(5f, ActionType.Support),
                MakeAction(5f, ActionType.Support));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result.type == TechniqueType.Buff,
                $"Expected Buff, got {result.type}");
            Debug.Log("  [PASS] Support actions - type is Buff");
        }

        private void Test_ElementProficiency_AffectsPower()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit(attack: 10);

            // Give the caster 2x Fire proficiency
            caster.definition.proficiencies.elementProficiencies[ElementType.Fire] = 2.0f;

            var slot = MakeSlot(
                MakeAction(10f, ActionType.Elemental, ElementType.Fire),
                MakeAction(10f, ActionType.Elemental, ElementType.Fire));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            // rawPower=20, attack=10 → 20*1.0=20, then *2.0 proficiency = 40
            Debug.Assert(result.power == 40,
                $"Expected power 40 with 2x fire proficiency, got {result.power}");
            Debug.Log("  [PASS] Element proficiency affects power");
        }

        private void Test_AttackStat_AffectsPower()
        {
            var skill     = new SkillSystem();
            var weakUnit  = MakeUnit(attack: 10);
            var strongUnit = MakeUnit(attack: 20);

            var action = MakeAction(10f, ActionType.Physical);
            var slotA  = MakeSlot(action);
            var slotB  = MakeSlot(action);

            ResolvedTechnique weak   = skill.ResolveSkill(slotA, weakUnit);
            ResolvedTechnique strong = skill.ResolveSkill(slotB, strongUnit);

            Debug.Assert(strong.power > weak.power,
                $"Strong unit (atk 20) should deal more than weak unit (atk 10). Got {strong.power} vs {weak.power}");
            Debug.Assert(strong.power == weak.power * 2,
                $"Double attack should give double power. Got {strong.power} vs {weak.power}");
            Debug.Log("  [PASS] Attack stat affects power");
        }

        private void Test_AttackTechnique_TargetIsSingle()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit();
            var slot   = MakeSlot(MakeAction(10f, ActionType.Physical));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result.targetPattern == TargetPattern.Single,
                $"Expected Single, got {result.targetPattern}");
            Debug.Log("  [PASS] Attack technique - target is Single");
        }

        private void Test_BuffTechnique_TargetIsSelf()
        {
            var skill  = new SkillSystem();
            var caster = MakeUnit();
            var slot   = MakeSlot(MakeAction(5f, ActionType.Support));

            ResolvedTechnique result = skill.ResolveSkill(slot, caster);

            Debug.Assert(result.targetPattern == TargetPattern.Self,
                $"Expected Self, got {result.targetPattern}");
            Debug.Log("  [PASS] Buff technique - target is Self");
        }
    }
}
