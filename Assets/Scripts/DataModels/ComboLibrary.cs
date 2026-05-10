using System.Collections.Generic;
using UnityEngine;

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

        // Phase 4 — speed properties. Defaults are 0 so the hardcoded recipes
        // below stay neutral until they're individually balanced.
        public readonly float speedCost;
        public readonly float speedGain;
        public readonly float speedScaling;
        public readonly float speedGate;

        // Phase 10 — CC properties. Defaults: no CC.
        public readonly CCEffectType ccType;
        public readonly float ccDuration;
        public readonly float ccChance;
        public readonly float ccMagnitude;

        // Phase 12 — tactical denial. Defaults zero (no effect).
        public readonly float targetSpeedShatter;
        public readonly float targetSoftCapOverride;
        public readonly float casterSoftCapOverride;
        public readonly float speedCapModifierDuration;

        // Multi-strike (cadence — 1 = single swing).
        public readonly int   strikeCount;
        public readonly float strikeInterval;

        // Paired-reaction attack archetype.
        public readonly AttackArchetype attackArchetype;

        public ComboRecipe(string[] ids, string name, TechniqueType type,
            ElementType element, float mult, CastType cast,
            float speedCost = 0f, float speedGain = 0f,
            float speedScaling = 0f, float speedGate = 0f,
            CCEffectType ccType = CCEffectType.None,
            float ccDuration = 0f, float ccChance = 0f, float ccMagnitude = 1f,
            float targetSpeedShatter = 0f,
            float targetSoftCapOverride = 0f,
            float casterSoftCapOverride = 0f,
            float speedCapModifierDuration = 0f,
            int   strikeCount = 1,
            float strikeInterval = 0.25f,
            AttackArchetype attackArchetype = AttackArchetype.Light)
        {
            actionIds       = ids;
            this.name       = name;
            techType        = type;
            this.element    = element;
            powerMultiplier = mult;
            castType        = cast;
            this.speedCost     = speedCost;
            this.speedGain     = speedGain;
            this.speedScaling  = speedScaling;
            this.speedGate     = speedGate;
            this.ccType        = ccType;
            this.ccDuration    = ccDuration;
            this.ccChance      = ccChance;
            this.ccMagnitude   = ccMagnitude;
            this.targetSpeedShatter       = targetSpeedShatter;
            this.targetSoftCapOverride    = targetSoftCapOverride;
            this.casterSoftCapOverride    = casterSoftCapOverride;
            this.speedCapModifierDuration = speedCapModifierDuration;
            this.strikeCount    = Mathf.Max(1, strikeCount);
            this.strikeInterval = Mathf.Max(0.05f, strikeInterval);
            this.attackArchetype = attackArchetype;
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
        // Spec-tuned recipe values per `Docs/Design/COMBAT_DESIGN.md` "Speed
        // cost matrix" (line 159+) and "Skill catalog — speed properties"
        // (line 1187+). Multi-strike counts produce the visible flurry cadence.
        // Named arguments highlight the populated fields.
        private static readonly List<ComboRecipe> Recipes = new List<ComboRecipe>
        {
            // ── 5-action combos ───────────────────────────────────────────────
            new ComboRecipe(
                new[] { "handsign_a","handsign_a","handsign_a","handsign_b","handsign_b" },
                "Orb Strike", TechniqueType.OrbSummon, ElementType.None, 1.0f, CastType.Rooted,
                speedCost: 0f),  // orbs are the offensive output, the cast itself is free

            new ComboRecipe(
                new[] { "handsign_a","handsign_a","handsign_a","handsign_a","handsign_b" },
                "Orb Ray", TechniqueType.OrbRay, ElementType.None, 1.0f, CastType.Rooted,
                speedCost: 0f),

            // ── 4-action combos ───────────────────────────────────────────────
            new ComboRecipe(
                new[] { "handsign_a", "handsign_b", "handsign_c", "focus" },
                "Summoning", TechniqueType.Summon, ElementType.None, 3.0f, CastType.Rooted,
                speedCost: 0f),

            // Elemental Fist — 4-strike Earth flurry, heavy commitment, applies Slow + shatter
            new ComboRecipe(
                new[] { "handsign_a", "handsign_b", "handsign_c", "punch" },
                "Elemental Fist", TechniqueType.Attack, ElementType.Earth, 2.5f, CastType.Melee,
                speedCost: 35f, speedScaling: 0.5f, speedGate: 50f,
                ccType: CCEffectType.Slow, ccDuration: 1.5f, ccChance: 0.5f, ccMagnitude: 0.3f,
                targetSpeedShatter: 15f,
                strikeCount: 4, strikeInterval: 0.20f,
                attackArchetype: AttackArchetype.Flurry),

            // Elemental Storm — 4-strike Lightning kick combo, applies Stun
            new ComboRecipe(
                new[] { "handsign_a", "handsign_b", "handsign_c", "kick" },
                "Elemental Storm", TechniqueType.Attack, ElementType.Lightning, 2.5f, CastType.Melee,
                speedCost: 35f, speedScaling: 0.4f, speedGate: 50f,
                ccType: CCEffectType.Stun, ccDuration: 0.4f, ccChance: 0.6f,
                strikeCount: 4, strikeInterval: 0.18f,
                attackArchetype: AttackArchetype.Heavy),

            // ── 3-action combos ───────────────────────────────────────────────
            new ComboRecipe(
                new[] { "handsign_a", "handsign_b", "handsign_c" },
                "Triple Sign", TechniqueType.Attack, ElementType.Fire, 2.0f, CastType.Rooted,
                speedCost: 0f, strikeCount: 1,
                attackArchetype: AttackArchetype.Sign),

            // ── 2-action elemental combos (mobile casts) ──────────────────────
            new ComboRecipe(
                new[] { "handsign_a", "handsign_b" },
                "Geomagnetic", TechniqueType.Attack, ElementType.Earth, 1.4f, CastType.Mobile,
                speedCost: 10f,
                strikeCount: 2, strikeInterval: 0.22f,
                attackArchetype: AttackArchetype.Sign),

            new ComboRecipe(
                new[] { "handsign_b", "handsign_c" },
                "Thunderstorm", TechniqueType.Attack, ElementType.Lightning, 1.4f, CastType.Mobile,
                speedCost: 10f,
                ccType: CCEffectType.Stun, ccDuration: 0.3f, ccChance: 0.25f,
                strikeCount: 2, strikeInterval: 0.20f,
                attackArchetype: AttackArchetype.Sign),

            new ComboRecipe(
                new[] { "handsign_a", "handsign_c" },
                "Mudslide", TechniqueType.Attack, ElementType.Water, 1.4f, CastType.Mobile,
                speedCost: 10f,
                ccType: CCEffectType.Slow, ccDuration: 1.0f, ccChance: 0.4f, ccMagnitude: 0.25f,
                strikeCount: 2, strikeInterval: 0.22f,
                attackArchetype: AttackArchetype.Sign),

            // ── 2-action physical combos ──────────────────────────────────────
            new ComboRecipe(
                new[] { "punch", "kick" },
                "Combo Strike", TechniqueType.Attack, ElementType.None, 1.3f, CastType.Melee,
                speedCost: 10f,
                strikeCount: 2, strikeInterval: 0.22f,
                attackArchetype: AttackArchetype.Light),

            // Power Strike — single heavy hit, scales hard with speed
            new ComboRecipe(
                new[] { "focus", "punch" },
                "Power Strike", TechniqueType.Attack, ElementType.None, 1.5f, CastType.Melee,
                speedCost: 15f, speedScaling: 0.5f,
                ccType: CCEffectType.Slow, ccDuration: 1.0f, ccChance: 0.4f, ccMagnitude: 0.25f,
                strikeCount: 1,
                attackArchetype: AttackArchetype.Heavy),

            // Crescent Kick — Reference 3: launch → aerial flurry → far
            // knockback. Naruto-style finisher. Drives the LaunchCombo
            // resolver branch + Airborne reaction.
            new ComboRecipe(
                new[] { "focus", "kick" },
                "Crescent Kick", TechniqueType.LaunchCombo, ElementType.None, 2.0f, CastType.Melee,
                speedCost: 25f, speedScaling: 0.7f, speedGate: 30f,
                ccType: CCEffectType.Stun, ccDuration: 0.5f, ccChance: 0.3f,
                targetSpeedShatter: 25f,
                strikeCount: 4, strikeInterval: 0.18f,
                attackArchetype: AttackArchetype.Launch),

            // ── Sign + Physical 2-strike combos ───────────────────────────────
            new ComboRecipe(
                new[] { "handsign_a", "punch" },
                "Earth Fist", TechniqueType.Attack, ElementType.Earth, 1.2f, CastType.Melee,
                speedCost: 15f,
                strikeCount: 2, strikeInterval: 0.22f,
                attackArchetype: AttackArchetype.Flurry),

            new ComboRecipe(
                new[] { "handsign_b", "punch" },
                "Thunder Fist", TechniqueType.Attack, ElementType.Lightning, 1.2f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Stun, ccDuration: 0.3f, ccChance: 0.25f,
                strikeCount: 2, strikeInterval: 0.20f,
                attackArchetype: AttackArchetype.Flurry),

            new ComboRecipe(
                new[] { "handsign_c", "punch" },
                "Water Fist", TechniqueType.Attack, ElementType.Water, 1.2f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Slow, ccDuration: 0.8f, ccChance: 0.30f, ccMagnitude: 0.20f,
                strikeCount: 2, strikeInterval: 0.22f,
                attackArchetype: AttackArchetype.Flurry),

            new ComboRecipe(
                new[] { "handsign_a", "kick" },
                "Tremor Kick", TechniqueType.Attack, ElementType.Earth, 1.3f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Slow, ccDuration: 1.0f, ccChance: 0.30f, ccMagnitude: 0.25f,
                strikeCount: 2, strikeInterval: 0.22f,
                attackArchetype: AttackArchetype.Sweep),

            new ComboRecipe(
                new[] { "handsign_b", "kick" },
                "Thunder Sweep", TechniqueType.Attack, ElementType.Lightning, 1.3f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Stun, ccDuration: 0.35f, ccChance: 0.30f,
                strikeCount: 2, strikeInterval: 0.20f,
                attackArchetype: AttackArchetype.Sweep),

            new ComboRecipe(
                new[] { "handsign_c", "kick" },
                "Tidal Sweep", TechniqueType.Attack, ElementType.Water, 1.3f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Slow, ccDuration: 1.2f, ccChance: 0.40f, ccMagnitude: 0.30f,
                strikeCount: 2, strikeInterval: 0.22f,
                attackArchetype: AttackArchetype.Sweep),
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
