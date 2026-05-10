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

        // Phase 11 — SPD-modulated execution timing.
        // Computed by SkillSystem when resolving for a specific caster (see
        // CombatTimingFormula). 0 means "use the ability's default hold time."
        public float executionTime;

        // Phase 4 — speed properties resolved from the recipe (or summed from
        // standalone actions when no combo matches).
        public float speedCost;
        public float speedGain;
        public float speedScaling;
        public float speedGate;

        // Phase 10 — CC effect applied to target on landed strike.
        public CCEffectType ccType;
        public float ccDuration;
        public float ccChance;
        public float ccMagnitude;

        // Phase 12 — tactical denial.
        public float targetSpeedShatter;
        public float targetSoftCapOverride;
        public float casterSoftCapOverride;
        public float speedCapModifierDuration;

        // Multi-strike cadence.
        public int   strikeCount;
        public float strikeInterval;

        // Paired-reaction attack archetype — drives DefenderReactionTable lookup.
        public AttackArchetype attackArchetype;
    }
}
