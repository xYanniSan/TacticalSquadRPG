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
            float rawPower = SumBasePower(actions);
            float attackMultiplier = caster.currentStats.attack / 10f;

            // ── Try combo library first ───────────────────────────────────────
            var actionIds = actions.ConvertAll(a => a.actionId);
            ComboRecipe combo = ComboLibrary.TryMatch(actionIds);

            if (combo != null)
            {
                float profMultiplier = GetProficiencyMultiplier(
                    caster, combo.element, ActionType.Physical, combo.techType);

                int finalPower = (int)(rawPower * combo.powerMultiplier
                                                * attackMultiplier
                                                * profMultiplier);

                return new ResolvedTechnique
                {
                    techniqueName = combo.name,
                    type          = combo.techType,
                    element       = combo.element,
                    power         = finalPower,
                    targetPattern = combo.techType == TechniqueType.Buff
                                    ? TargetPattern.Self : TargetPattern.Single,
                    sourceActions = actions,
                    isCombo       = true,
                    castType      = combo.castType,
                    // Phase 4 — recipe-defined speed properties win for combos
                    speedCost     = combo.speedCost,
                    speedGain     = combo.speedGain,
                    speedScaling  = combo.speedScaling,
                    speedGate     = combo.speedGate,
                    // Phase 10 — recipe-defined CC properties
                    ccType        = combo.ccType,
                    ccDuration    = combo.ccDuration,
                    ccChance      = combo.ccChance,
                    ccMagnitude   = combo.ccMagnitude,
                    // Phase 12 — tactical denial
                    targetSpeedShatter       = combo.targetSpeedShatter,
                    targetSoftCapOverride    = combo.targetSoftCapOverride,
                    casterSoftCapOverride    = combo.casterSoftCapOverride,
                    speedCapModifierDuration = combo.speedCapModifierDuration,
                    // Multi-strike cadence
                    strikeCount    = combo.strikeCount,
                    strikeInterval = combo.strikeInterval,
                    // Paired-reaction archetype
                    attackArchetype = combo.attackArchetype
                };
            }

            // ── No combo matched — individual actions will fire separately ────
            ElementType element    = GetDominantElement(actions);
            ActionType actionType  = GetDominantActionType(actions);
            TechniqueType techType = GetTechniqueType(actionType, actions);

            // castType derived from dominant action type
            CastType castType = CastType.Mobile;
            bool hasPhysical = false;
            bool hasSupport  = false;
            foreach (var a in actions)
            {
                if (a.actionType == ActionType.Physical) hasPhysical = true;
                if (a.actionType == ActionType.Support)  hasSupport  = true;
            }
            if (hasSupport)  castType = CastType.Rooted;
            else if (hasPhysical) castType = CastType.Melee;

            // For unmatched chains, sum action-level speed properties so a
            // single-action skill (e.g. standalone Kick) still has a cost.
            float sumSpeedCost = 0f, sumSpeedGain = 0f;
            float maxSpeedScaling = 0f, maxSpeedGate = 0f;
            foreach (var a in actions)
            {
                sumSpeedCost     += a.speedCost;
                sumSpeedGain     += a.speedGain;
                if (a.speedScaling > maxSpeedScaling) maxSpeedScaling = a.speedScaling;
                if (a.speedGate    > maxSpeedGate)    maxSpeedGate    = a.speedGate;
            }

            return new ResolvedTechnique
            {
                techniqueName = "Individual",
                type          = techType,
                element       = element,
                power         = 0,
                targetPattern = TargetPattern.Single,
                sourceActions = actions,
                isCombo       = false,
                castType      = castType,
                speedCost     = sumSpeedCost,
                speedGain     = sumSpeedGain,
                speedScaling  = maxSpeedScaling,
                speedGate     = maxSpeedGate
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
            // Orb summon: any action in chain is OrbSummon
            foreach (var action in actions)
                if (action.actionType == ActionType.OrbSummon)
                    return TechniqueType.OrbSummon;

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
