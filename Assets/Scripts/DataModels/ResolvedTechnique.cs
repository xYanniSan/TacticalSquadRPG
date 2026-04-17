using System.Collections.Generic;

namespace TacticalRPG.DataModels
{
    public class ResolvedTechnique
    {
        public string techniqueName;                    // e.g., "Fire Strike", "Focus"
        public TechniqueType type;                      // Attack, Buff, Heal, etc.
        public ElementType element;                     // Dominant element of the chain

        public int power;                               // Final damage or healing amount
        public TargetPattern targetPattern;             // Single, AOE, Self, etc.

        public List<ActionDefinition> sourceActions;    // The actions that created this
    }
}
