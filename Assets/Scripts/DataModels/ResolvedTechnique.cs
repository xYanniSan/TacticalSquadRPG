using System.Collections.Generic;

namespace TacticalRPG.DataModels
{
    public class ResolvedTechnique
    {
        public string techniqueName;                    // e.g., "Earth Fist", "Triple Sign"
        public TechniqueType type;                      // Attack, Buff, Heal, Summon, etc.
        public ElementType element;                     // Dominant element of the chain

        public int power;                               // Final damage or healing amount
        public TargetPattern targetPattern;             // Single, AOE, Self, etc.

        public List<ActionDefinition> sourceActions;    // The actions that created this

        // Combo system
        public bool isCombo;                            // True if matched a recipe in ComboLibrary
        public CastType castType;                       // Melee / Mobile / Rooted
    }
}
