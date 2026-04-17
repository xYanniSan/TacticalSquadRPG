using System.Collections.Generic;
using TacticalRPG.DataModels;

namespace TacticalRPG.Systems
{
    public class SkillSystem
    {
        // Resolves a SkillSlot's action chain into a ResolvedTechnique for the given caster.
        public ResolvedTechnique ResolveSkill(SkillSlot slot, UnitRuntime caster)
        {
            List<ActionDefinition> actions = CollectActions(slot);

            float rawPower          = SumBasePower(actions);
            ElementType element     = GetDominantElement(actions);
            ActionType actionType   = GetDominantActionType(actions);
            TechniqueType techType  = GetTechniqueType(actionType, actions);

            float profMultiplier    = GetProficiencyMultiplier(caster, element, actionType, techType);
            float attackMultiplier  = caster.currentStats.attack / 10f;

            int finalPower          = (int)(rawPower * attackMultiplier * profMultiplier);
            TargetPattern pattern   = techType == TechniqueType.Buff ? TargetPattern.Self : TargetPattern.Single;
            string name             = BuildTechniqueName(element, techType);

            return new ResolvedTechnique
            {
                techniqueName = name,
                type          = techType,
                element       = element,
                power         = finalPower,
                targetPattern = pattern,
                sourceActions = actions
            };
        }

        // ── Resolution Steps ──────────────────────────────────────────────

        private List<ActionDefinition> CollectActions(SkillSlot slot)
        {
            var actions = new List<ActionDefinition>();
            foreach (ActionSlot actionSlot in slot.actionSequence)
            {
                if (actionSlot.action != null)
                    actions.Add(actionSlot.action);
            }
            return actions;
        }

        private float SumBasePower(List<ActionDefinition> actions)
        {
            float total = 0f;
            foreach (ActionDefinition action in actions)
                total += action.basePower;
            return total;
        }

        // Returns the most frequently occurring non-None element.
        // Ties are broken by whichever was counted first.
        private ElementType GetDominantElement(List<ActionDefinition> actions)
        {
            var counts = new Dictionary<ElementType, int>();
            foreach (ActionDefinition action in actions)
            {
                if (action.element == ElementType.None) continue;
                counts.TryGetValue(action.element, out int c);
                counts[action.element] = c + 1;
            }

            ElementType dominant = ElementType.None;
            int max = 0;
            foreach (var kvp in counts)
            {
                if (kvp.Value > max)
                {
                    max      = kvp.Value;
                    dominant = kvp.Key;
                }
            }
            return dominant;
        }

        private ActionType GetDominantActionType(List<ActionDefinition> actions)
        {
            var counts = new Dictionary<ActionType, int>();
            foreach (ActionDefinition action in actions)
            {
                counts.TryGetValue(action.actionType, out int c);
                counts[action.actionType] = c + 1;
            }

            ActionType dominant = ActionType.Physical;
            int max = 0;
            foreach (var kvp in counts)
            {
                if (kvp.Value > max)
                {
                    max      = kvp.Value;
                    dominant = kvp.Key;
                }
            }
            return dominant;
        }

        private TechniqueType GetTechniqueType(ActionType dominant, List<ActionDefinition> actions)
        {
            // Detect summon pattern: chain has BOTH Elemental AND Support actions
            bool hasElemental = false;
            bool hasSupport = false;
            foreach (var action in actions)
            {
                if (action.actionType == ActionType.Elemental) hasElemental = true;
                if (action.actionType == ActionType.Support) hasSupport = true;
            }
            if (hasElemental && hasSupport)
                return TechniqueType.Summon;

            switch (dominant)
            {
                case ActionType.Physical:
                case ActionType.Elemental:
                    return TechniqueType.Attack;
                case ActionType.Support:
                    return TechniqueType.Buff;
                default:
                    return TechniqueType.Attack;
            }
        }

        private float GetProficiencyMultiplier(
            UnitRuntime caster,
            ElementType element,
            ActionType actionType,
            TechniqueType techType)
        {
            if (caster.definition == null) return 1.0f;

            ProficiencySet profs = caster.definition.proficiencies;
            return profs.GetProficiencyBonus(element)
                 * profs.GetProficiencyBonus(actionType)
                 * profs.GetProficiencyBonus(techType);
        }

        private string BuildTechniqueName(ElementType element, TechniqueType type)
        {
            string prefix = element == ElementType.None ? "" : element.ToString() + " ";
            switch (type)
            {
                case TechniqueType.Attack: return $"{prefix}Strike";
                case TechniqueType.Buff:   return $"{prefix}Focus";
                case TechniqueType.Heal:   return "Mend";
                case TechniqueType.Summon: return $"{prefix}Summoning";
                default:                   return $"{prefix}Technique";
            }
        }
    }
}
