using System.Collections.Generic;

namespace TacticalRPG.DataModels
{
    /// <summary>
    /// A named combo triggered by an exact sequence of action IDs in a skill slot.
    /// </summary>
    public class ComboRecipe
    {
        public readonly string[] actionIds;
        public readonly string name;
        public readonly TechniqueType techType;
        public readonly ElementType element;
        public readonly float powerMultiplier;
        public readonly CastType castType;

        public ComboRecipe(string[] ids, string name, TechniqueType type,
            ElementType element, float mult, CastType cast)
        {
            actionIds       = ids;
            this.name       = name;
            techType        = type;
            this.element    = element;
            powerMultiplier = mult;
            castType        = cast;
        }
    }

    /// <summary>
    /// Static library of all combo recipes. Matched by exact action ID sequence.
    /// Longer recipes are listed first so they are tested before shorter sub-sequences.
    /// Call SetLibrary() at battle start to override with a ComboLibraryAsset SO.
    /// </summary>
    public static class ComboLibrary
    {
        // ── Runtime override (loaded from ComboLibraryAsset SO) ─────
        private static List<ComboRecipe> _loadedRecipes;

        /// <summary>
        /// Load recipes from a ComboLibraryAsset ScriptableObject.
        /// Pass null to revert to the built-in hardcoded list.
        /// </summary>
        public static void SetLibrary(ComboLibraryAsset asset)
        {
            if (asset == null || asset.recipes == null)
            {
                _loadedRecipes = null;
                return;
            }

            _loadedRecipes = new List<ComboRecipe>();
            foreach (var def in asset.recipes)
            {
                if (def != null)
                    _loadedRecipes.Add(def.ToRecipe());
            }
        }
        private static readonly List<ComboRecipe> Recipes = new List<ComboRecipe>
        {
            // ── 5-action combos (check before all shorter ones) ────────────────
            new ComboRecipe(
                new[] { "handsign_a","handsign_a","handsign_a","handsign_b","handsign_b" },
                "Orb Strike", TechniqueType.OrbSummon, ElementType.None, 1.0f, CastType.Rooted),

            new ComboRecipe(
                new[] { "handsign_a","handsign_a","handsign_a","handsign_a","handsign_b" },
                "Orb Ray", TechniqueType.OrbRay, ElementType.None, 1.0f, CastType.Rooted),

            // ── 4-action combos (check before shorter ones) ────────────────────
            new ComboRecipe(
                new[] { "handsign_a", "handsign_b", "handsign_c", "focus" },
                "Summoning", TechniqueType.Summon, ElementType.None, 3.0f, CastType.Rooted),

            new ComboRecipe(
                new[] { "handsign_a", "handsign_b", "handsign_c", "punch" },
                "Elemental Fist", TechniqueType.Attack, ElementType.Earth, 2.5f, CastType.Melee),

            new ComboRecipe(
                new[] { "handsign_a", "handsign_b", "handsign_c", "kick" },
                "Elemental Storm", TechniqueType.Attack, ElementType.Lightning, 2.5f, CastType.Melee),

            // ── 3-action combos ────────────────────────────────────────────────
            new ComboRecipe(
                new[] { "handsign_a", "handsign_b", "handsign_c" },
                "Triple Sign", TechniqueType.Attack, ElementType.Fire, 2.0f, CastType.Rooted),

            // ── 2-action elemental combos ──────────────────────────────────────
            new ComboRecipe(
                new[] { "handsign_a", "handsign_b" },
                "Geomagnetic", TechniqueType.Attack, ElementType.Earth, 1.4f, CastType.Mobile),

            new ComboRecipe(
                new[] { "handsign_b", "handsign_c" },
                "Thunderstorm", TechniqueType.Attack, ElementType.Lightning, 1.4f, CastType.Mobile),

            new ComboRecipe(
                new[] { "handsign_a", "handsign_c" },
                "Mudslide", TechniqueType.Attack, ElementType.Water, 1.4f, CastType.Mobile),

            // ── 2-action physical combos ───────────────────────────────────────
            new ComboRecipe(
                new[] { "punch", "kick" },
                "Combo Strike", TechniqueType.Attack, ElementType.None, 1.3f, CastType.Melee),

            new ComboRecipe(
                new[] { "focus", "punch" },
                "Power Strike", TechniqueType.Attack, ElementType.None, 1.5f, CastType.Melee),

            new ComboRecipe(
                new[] { "focus", "kick" },
                "Crescent Kick", TechniqueType.Attack, ElementType.None, 2.0f, CastType.Melee),

            // ── Sign + Physical combos ─────────────────────────────────────────
            new ComboRecipe(
                new[] { "handsign_a", "punch" },
                "Earth Fist", TechniqueType.Attack, ElementType.Earth, 1.2f, CastType.Melee),

            new ComboRecipe(
                new[] { "handsign_b", "punch" },
                "Thunder Fist", TechniqueType.Attack, ElementType.Lightning, 1.2f, CastType.Melee),

            new ComboRecipe(
                new[] { "handsign_c", "punch" },
                "Water Fist", TechniqueType.Attack, ElementType.Water, 1.2f, CastType.Melee),

            new ComboRecipe(
                new[] { "handsign_a", "kick" },
                "Tremor Kick", TechniqueType.Attack, ElementType.Earth, 1.3f, CastType.Melee),

            new ComboRecipe(
                new[] { "handsign_b", "kick" },
                "Thunder Sweep", TechniqueType.Attack, ElementType.Lightning, 1.3f, CastType.Melee),

            new ComboRecipe(
                new[] { "handsign_c", "kick" },
                "Tidal Sweep", TechniqueType.Attack, ElementType.Water, 1.3f, CastType.Melee),
        };

        /// <summary>
        /// Returns the first matching recipe for the given action ID sequence, or null.
        /// Uses the loaded SO library if one was set, otherwise falls back to built-in list.
        /// </summary>
        public static ComboRecipe TryMatch(List<string> actionIds)
        {
            var library = _loadedRecipes ?? Recipes;

            foreach (var recipe in library)
            {
                if (recipe.actionIds.Length != actionIds.Count) continue;

                bool match = true;
                for (int i = 0; i < recipe.actionIds.Length; i++)
                {
                    if (recipe.actionIds[i] != actionIds[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match) return recipe;
            }
            return null;
        }
    }
}
