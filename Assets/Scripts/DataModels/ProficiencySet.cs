using System;
using System.Collections.Generic;

namespace TacticalRPG.DataModels
{
    [Serializable]
    public class ProficiencySet
    {
        public Dictionary<ActionType, float>    actionProficiencies    = new Dictionary<ActionType, float>();
        public Dictionary<ElementType, float>   elementProficiencies   = new Dictionary<ElementType, float>();
        public Dictionary<TechniqueType, float> techniqueProficiencies = new Dictionary<TechniqueType, float>();

        public float GetProficiencyBonus(ActionType action)
        {
            return actionProficiencies.TryGetValue(action, out float bonus) ? bonus : 1.0f;
        }

        public float GetProficiencyBonus(ElementType element)
        {
            return elementProficiencies.TryGetValue(element, out float bonus) ? bonus : 1.0f;
        }

        public float GetProficiencyBonus(TechniqueType technique)
        {
            return techniqueProficiencies.TryGetValue(technique, out float bonus) ? bonus : 1.0f;
        }
    }
}
