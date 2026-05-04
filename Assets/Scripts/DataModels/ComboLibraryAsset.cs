using System.Collections.Generic;
using UnityEngine;

namespace TacticalRPG.DataModels
{
    /// <summary>
    /// Container ScriptableObject that holds all combo recipes for a battle ruleset.
    /// Assign to TerrainBattleManager to override the hardcoded combo list.
    /// </summary>
    [CreateAssetMenu(fileName = "ComboLibrary", menuName = "TacticalRPG/Combo Library")]
    public class ComboLibraryAsset : ScriptableObject
    {
        [Tooltip("All combo recipes available in battle. Order matters — longer combos must come before shorter sub-sequences.")]
        public List<ComboRecipeDefinition> recipes = new List<ComboRecipeDefinition>();
    }
}
