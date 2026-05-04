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

        /// <summary>Converts to the runtime ComboRecipe used by ComboLibrary.</summary>
        public ComboRecipe ToRecipe()
        {
            return new ComboRecipe(actionIds, recipeName, techType, element, powerMultiplier, castType);
        }
    }
}
