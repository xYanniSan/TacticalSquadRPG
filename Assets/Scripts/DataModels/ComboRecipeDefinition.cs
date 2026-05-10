using UnityEngine;

namespace TacticalRPG.DataModels
{
    /// <summary>
    /// A single combo recipe as a ScriptableObject — editable in the Unity Inspector.
    /// Drag into a ComboLibraryAsset to wire it into the battle system.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCombo", menuName = "TacticalRPG/Combo Recipe")]
    public class ComboRecipeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string recipeName;
        public string description;

        [Header("Trigger — exact action ID sequence (order matters)")]
        public string[] actionIds;

        [Header("Result")]
        public TechniqueType techType;
        public ElementType element;

        [Header("Power")]
        [Tooltip("Multiplied against the sum of all action basePower values.")]
        public float powerMultiplier = 1.0f;

        [Header("Cast Behaviour")]
        public CastType castType;

        [Header("Speed (kinetic resource — see COMBAT_DESIGN \"Speed\")")]
        [Tooltip("Speed required when this combo fires. 0 = free.")]
        public float speedCost;
        [Tooltip("Speed granted when this combo fires (kinetic skills). 0 = none.")]
        public float speedGain;
        [Tooltip("Damage multiplier per 100 speed for this combo. 0 = no scaling.")]
        public float speedScaling;
        [Tooltip("Minimum caster speed required for this combo to fire. 0 = no gate.")]
        public float speedGate;

        [Header("CC (Phase 10 — see COMBAT_DESIGN \"CC catalog\")")]
        [Tooltip("CC effect applied to the target on landed strike. None = no CC.")]
        public CCEffectType ccType = CCEffectType.None;
        [Tooltip("CC duration in seconds. 0 = no CC.")]
        public float ccDuration;
        [Range(0f, 1f)]
        [Tooltip("Chance the CC applies on a landed strike. 0 = never, 1 = always.")]
        public float ccChance;
        [Tooltip("Magnitude (e.g. slow strength as a 0-1 multiplier reduction). Default 1 for binary effects.")]
        public float ccMagnitude = 1f;

        [Header("Tactical denial (Phase 12)")]
        [Tooltip("Flat amount of Speed drained from the target on a landed strike. 0 = no shatter.")]
        public float targetSpeedShatter;
        [Tooltip("Soft-cap modifier applied to the target on landed strike (temporary cap drop). 0 = no modifier.")]
        public float targetSoftCapOverride;
        [Tooltip("Soft-cap modifier applied to the caster on cast (e.g. Flow State raises caster's cap). 0 = no modifier.")]
        public float casterSoftCapOverride;
        [Tooltip("Duration of cap modifiers in seconds.")]
        public float speedCapModifierDuration;

        [Header("Multi-strike (gives combos a flurry cadence)")]
        [Tooltip("How many strikes this combo plays out. 1 = single swing. Damage is partitioned across strikes.")]
        [Range(1, 8)] public int strikeCount = 1;
        [Tooltip("Seconds between strikes during a multi-strike combo.")]
        [Range(0.05f, 1f)] public float strikeInterval = 0.25f;

        [Header("Attack archetype (pairs with defender reaction)")]
        [Tooltip("Drives DefenderReactionTable lookup. The defender's reaction is decided by this " +
                 "archetype crossed with their stance and speed band. Default: Light.")]
        public AttackArchetype attackArchetype = AttackArchetype.Light;

        /// <summary>Converts to the runtime ComboRecipe used by ComboLibrary.</summary>
        public ComboRecipe ToRecipe()
        {
            return new ComboRecipe(actionIds, recipeName, techType, element, powerMultiplier, castType,
                speedCost, speedGain, speedScaling, speedGate,
                ccType, ccDuration, ccChance, ccMagnitude,
                targetSpeedShatter, targetSoftCapOverride, casterSoftCapOverride, speedCapModifierDuration,
                strikeCount, strikeInterval, attackArchetype);
        }
    }
}
