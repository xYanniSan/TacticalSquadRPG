using TacticalRPG.DataModels;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace TacticalRPG.Editor
{
    public static class SkillDataCreator
    {
        [MenuItem("TacticalRPG/Create Action Definitions")]
        public static void CreateAllActions() => CreateActions(forceRecreate: false);

        [MenuItem("TacticalRPG/Recreate Action Definitions (force)")]
        public static void ForceRecreateAllActions() => CreateActions(forceRecreate: true);

        [MenuItem("TacticalRPG/Create Combo Library")]
        public static void CreateComboLibrary()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Combos"))
                AssetDatabase.CreateFolder("Assets/Data", "Combos");

            var recipeAssets = new List<ComboRecipeDefinition>();

            // ── 5-action combos (check before all shorter ones) ────
            recipeAssets.Add(MakeRecipe("Combo_OrbStrike",
                "Orb Strike", "Summons 3 orbiting orbs that fire on each punch. (A→A→A→B→B)",
                new[] { "handsign_a","handsign_a","handsign_a","handsign_b","handsign_b" },
                TechniqueType.OrbSummon, ElementType.None, 1.0f, CastType.Rooted));

            recipeAssets.Add(MakeRecipe("Combo_OrbRay",
                "Orb Ray",
                "Spawns 3 orbs that immediately fire homing rays at the target. " +
                "Teleports 20 m if the caster is in melee range. (A→A→A→A→B)",
                new[] { "handsign_a","handsign_a","handsign_a","handsign_a","handsign_b" },
                TechniqueType.OrbRay, ElementType.None, 1.0f, CastType.Rooted));

            // ── 4-action combos ────────────────────────────────────
            recipeAssets.Add(MakeRecipe("Combo_Summoning",
                "Summoning", "Summon a guardian. Rooted. (A→B→C→Focus)",
                new[] { "handsign_a","handsign_b","handsign_c","focus" },
                TechniqueType.Summon, ElementType.None, 3.0f, CastType.Rooted));

            recipeAssets.Add(MakeRecipe("Combo_ElementalFist",
                "Elemental Fist", "Massive all-element punch — 4-strike Earth flurry. (A→B→C→Punch)",
                new[] { "handsign_a","handsign_b","handsign_c","punch" },
                TechniqueType.Attack, ElementType.Earth, 2.5f, CastType.Melee,
                speedCost: 35f, speedScaling: 0.5f, speedGate: 50f,
                ccType: CCEffectType.Slow, ccDuration: 1.5f, ccChance: 0.5f, ccMagnitude: 0.3f,
                targetSpeedShatter: 15f,
                strikeCount: 4, strikeInterval: 0.20f));

            recipeAssets.Add(MakeRecipe("Combo_ElementalStorm",
                "Elemental Storm", "Massive Lightning kick combo — 4-strike, can stun. (A→B→C→Kick)",
                new[] { "handsign_a","handsign_b","handsign_c","kick" },
                TechniqueType.Attack, ElementType.Lightning, 2.5f, CastType.Melee,
                speedCost: 35f, speedScaling: 0.4f, speedGate: 50f,
                ccType: CCEffectType.Stun, ccDuration: 0.4f, ccChance: 0.6f,
                strikeCount: 4, strikeInterval: 0.18f));

            // ── 3-action combos ────────────────────────────────────
            recipeAssets.Add(MakeRecipe("Combo_TripleSign",
                "Triple Sign", "High elemental damage. Rooted. (A→B→C)",
                new[] { "handsign_a","handsign_b","handsign_c" },
                TechniqueType.Attack, ElementType.Fire, 2.0f, CastType.Rooted));

            // ── 2-action elemental combos (mobile casts) ───────────
            recipeAssets.Add(MakeRecipe("Combo_Geomagnetic",
                "Geomagnetic", "Earth+Lightning 2-strike mobile. (A→B)",
                new[] { "handsign_a","handsign_b" },
                TechniqueType.Attack, ElementType.Earth, 1.4f, CastType.Mobile,
                speedCost: 10f, strikeCount: 2, strikeInterval: 0.22f));

            recipeAssets.Add(MakeRecipe("Combo_Thunderstorm",
                "Thunderstorm", "Lightning+Water 2-strike with stun chance. (B→C)",
                new[] { "handsign_b","handsign_c" },
                TechniqueType.Attack, ElementType.Lightning, 1.4f, CastType.Mobile,
                speedCost: 10f,
                ccType: CCEffectType.Stun, ccDuration: 0.3f, ccChance: 0.25f,
                strikeCount: 2, strikeInterval: 0.20f));

            recipeAssets.Add(MakeRecipe("Combo_Mudslide",
                "Mudslide", "Earth+Water 2-strike, applies Slow. (A→C)",
                new[] { "handsign_a","handsign_c" },
                TechniqueType.Attack, ElementType.Water, 1.4f, CastType.Mobile,
                speedCost: 10f,
                ccType: CCEffectType.Slow, ccDuration: 1.0f, ccChance: 0.4f, ccMagnitude: 0.25f,
                strikeCount: 2, strikeInterval: 0.22f));

            // ── 2-action physical combos ───────────────────────────
            recipeAssets.Add(MakeRecipe("Combo_ComboStrike",
                "Combo Strike", "Physical 2-strike. (Punch→Kick)",
                new[] { "punch","kick" },
                TechniqueType.Attack, ElementType.None, 1.3f, CastType.Melee,
                speedCost: 10f, strikeCount: 2, strikeInterval: 0.22f));

            recipeAssets.Add(MakeRecipe("Combo_PowerStrike",
                "Power Strike", "Focus-enhanced single heavy hit, scales with speed. (Focus→Punch)",
                new[] { "focus","punch" },
                TechniqueType.Attack, ElementType.None, 1.5f, CastType.Melee,
                speedCost: 15f, speedScaling: 0.5f,
                ccType: CCEffectType.Slow, ccDuration: 1.0f, ccChance: 0.4f, ccMagnitude: 0.25f,
                strikeCount: 1));

            recipeAssets.Add(MakeRecipe("Combo_CrescentKick",
                "Crescent Kick", "Launch finisher: launch + aerial flurry + cinematic knockback. (Focus→Kick)",
                new[] { "focus","kick" },
                TechniqueType.LaunchCombo, ElementType.None, 2.0f, CastType.Melee,
                speedCost: 25f, speedScaling: 0.7f, speedGate: 30f,
                ccType: CCEffectType.Stun, ccDuration: 0.5f, ccChance: 0.3f,
                targetSpeedShatter: 25f,
                strikeCount: 4, strikeInterval: 0.18f,
                attackArchetype: AttackArchetype.Launch));

            // ── Sign + Physical 2-strike combos ────────────────────
            recipeAssets.Add(MakeRecipe("Combo_EarthFist",
                "Earth Fist", "Earth 2-strike punch combo. (A→Punch)",
                new[] { "handsign_a","punch" },
                TechniqueType.Attack, ElementType.Earth, 1.2f, CastType.Melee,
                speedCost: 15f, strikeCount: 2, strikeInterval: 0.22f));

            recipeAssets.Add(MakeRecipe("Combo_ThunderFist",
                "Thunder Fist", "Lightning 2-strike punch with stun chance. (B→Punch)",
                new[] { "handsign_b","punch" },
                TechniqueType.Attack, ElementType.Lightning, 1.2f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Stun, ccDuration: 0.3f, ccChance: 0.25f,
                strikeCount: 2, strikeInterval: 0.20f));

            recipeAssets.Add(MakeRecipe("Combo_WaterFist",
                "Water Fist", "Water 2-strike punch with slow chance. (C→Punch)",
                new[] { "handsign_c","punch" },
                TechniqueType.Attack, ElementType.Water, 1.2f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Slow, ccDuration: 0.8f, ccChance: 0.30f, ccMagnitude: 0.20f,
                strikeCount: 2, strikeInterval: 0.22f));

            recipeAssets.Add(MakeRecipe("Combo_TremorKick",
                "Tremor Kick", "Earth 2-strike kick with slow. (A→Kick)",
                new[] { "handsign_a","kick" },
                TechniqueType.Attack, ElementType.Earth, 1.3f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Slow, ccDuration: 1.0f, ccChance: 0.30f, ccMagnitude: 0.25f,
                strikeCount: 2, strikeInterval: 0.22f));

            recipeAssets.Add(MakeRecipe("Combo_ThunderSweep",
                "Thunder Sweep", "Lightning 2-strike kick with stun chance. (B→Kick)",
                new[] { "handsign_b","kick" },
                TechniqueType.Attack, ElementType.Lightning, 1.3f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Stun, ccDuration: 0.35f, ccChance: 0.30f,
                strikeCount: 2, strikeInterval: 0.20f));

            recipeAssets.Add(MakeRecipe("Combo_TidalSweep",
                "Tidal Sweep", "Water 2-strike kick with strong slow. (C→Kick)",
                new[] { "handsign_c","kick" },
                TechniqueType.Attack, ElementType.Water, 1.3f, CastType.Melee,
                speedCost: 15f,
                ccType: CCEffectType.Slow, ccDuration: 1.2f, ccChance: 0.40f, ccMagnitude: 0.30f,
                strikeCount: 2, strikeInterval: 0.22f));

            // ── Create library asset ───────────────────────────────
            string libPath = "Assets/Data/Combos/ComboLibrary.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ComboLibraryAsset>(libPath);
            if (existing != null) AssetDatabase.DeleteAsset(libPath);

            var library = ScriptableObject.CreateInstance<ComboLibraryAsset>();
            library.recipes = recipeAssets;
            AssetDatabase.CreateAsset(library, libPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SkillDataCreator] Created ComboLibrary with {recipeAssets.Count} recipes at {libPath}");
        }

        private static ComboRecipeDefinition MakeRecipe(
            string fileName, string recipeName, string description,
            string[] actionIds, TechniqueType techType, ElementType element,
            float powerMult, CastType castType,
            float speedCost = 0f, float speedScaling = 0f, float speedGate = 0f,
            CCEffectType ccType = CCEffectType.None, float ccDuration = 0f, float ccChance = 0f, float ccMagnitude = 1f,
            float targetSpeedShatter = 0f,
            int strikeCount = 1, float strikeInterval = 0.25f,
            AttackArchetype attackArchetype = AttackArchetype.Light)
        {
            string path = $"Assets/Data/Combos/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ComboRecipeDefinition>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var def = ScriptableObject.CreateInstance<ComboRecipeDefinition>();
            def.recipeName      = recipeName;
            def.description     = description;
            def.actionIds       = actionIds;
            def.techType        = techType;
            def.element         = element;
            def.powerMultiplier = powerMult;
            def.castType        = castType;
            def.speedCost       = speedCost;
            def.speedScaling    = speedScaling;
            def.speedGate       = speedGate;
            def.ccType          = ccType;
            def.ccDuration      = ccDuration;
            def.ccChance        = ccChance;
            def.ccMagnitude     = ccMagnitude;
            def.targetSpeedShatter = targetSpeedShatter;
            def.strikeCount     = Mathf.Max(1, strikeCount);
            def.strikeInterval  = Mathf.Max(0.05f, strikeInterval);
            def.attackArchetype = attackArchetype;

            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static void CreateActions(bool forceRecreate)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Actions"))
                AssetDatabase.CreateFolder("Assets/Data", "Actions");

            // ── Physical Actions ─────────────────────────────────────
            CreateAction("Punch", new ActionData
            {
                actionId         = "punch",
                displayName      = "Punch",
                actionType       = ActionType.Physical,
                basePower        = 10f,
                element          = ElementType.None,
                energyCost       = 0f,
                selfBuffDamage   = 0,
                selfBuffCharges  = 0,
                powerBoostPct    = 0f
            }, forceRecreate);

            CreateAction("Kick", new ActionData
            {
                actionId         = "kick",
                displayName      = "Kick",
                actionType       = ActionType.Physical,
                basePower        = 15f,
                element          = ElementType.None,
                energyCost       = 0f,
                selfBuffDamage   = 0,
                selfBuffCharges  = 0,
                powerBoostPct    = 0f
            }, forceRecreate);

            // ── Elemental Actions (Hand Signs) ───────────────────────
            CreateAction("HandSignA", new ActionData
            {
                actionId         = "handsign_a",
                displayName      = "Hand Sign A",
                actionType       = ActionType.Elemental,
                basePower        = 12f,
                element          = ElementType.Earth,
                energyCost       = 10f,
                selfBuffDamage   = 20,  // +20 Earth dmg per hit
                selfBuffCharges  = 10,  // lasts 10 hits
                powerBoostPct    = 0f
            }, forceRecreate);

            CreateAction("HandSignB", new ActionData
            {
                actionId         = "handsign_b",
                displayName      = "Hand Sign B",
                actionType       = ActionType.Elemental,
                basePower        = 12f,
                element          = ElementType.Lightning,
                energyCost       = 10f,
                selfBuffDamage   = 20,  // +20 Lightning dmg per hit
                selfBuffCharges  = 10,
                powerBoostPct    = 0f
            }, forceRecreate);

            CreateAction("HandSignC", new ActionData
            {
                actionId         = "handsign_c",
                displayName      = "Hand Sign C",
                actionType       = ActionType.Elemental,
                basePower        = 12f,
                element          = ElementType.Water,
                energyCost       = 10f,
                selfBuffDamage   = 20,  // +20 Water dmg per hit
                selfBuffCharges  = 10,
                powerBoostPct    = 0f
            }, forceRecreate);

            // ── Orb Summon Actions ───────────────────────────────────
            CreateAction("OrbSummon_B", new ActionData
            {
                actionId         = "orb_summon_b",
                displayName      = "Orb Summon",
                actionType       = ActionType.OrbSummon,
                basePower        = 0f,
                element          = ElementType.None,
                energyCost       = 0f,
                selfBuffDamage   = 0,
                selfBuffCharges  = 0,
                powerBoostPct    = 0f,
                orbCount         = 3,
                orbDamage        = 15
            }, forceRecreate);

            // ── Support Actions ──────────────────────────────────────
            CreateAction("Focus", new ActionData
            {
                actionId         = "focus",
                displayName      = "Focus",
                actionType       = ActionType.Support,
                basePower        = 8f,
                element          = ElementType.None,
                energyCost       = 15f,
                selfBuffDamage   = 0,
                selfBuffCharges  = 0,
                powerBoostPct    = 0.20f  // +20% damage to next skill
            }, forceRecreate);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SkillDataCreator] Action definitions updated in Assets/Data/Actions/");
        }

        private static void CreateAction(string fileName, ActionData data, bool forceRecreate)
        {
            string path = $"Assets/Data/Actions/{fileName}.asset";

            if (forceRecreate)
            {
                var existing = AssetDatabase.LoadAssetAtPath<ActionDefinition>(path);
                if (existing != null)
                    AssetDatabase.DeleteAsset(path);
            }
            else if (AssetDatabase.LoadAssetAtPath<ActionDefinition>(path) != null)
            {
                Debug.Log($"  Skipped {fileName} (already exists — use Recreate to overwrite)");
                return;
            }

            ActionDefinition def   = ScriptableObject.CreateInstance<ActionDefinition>();
            def.actionId           = data.actionId;
            def.displayName        = data.displayName;
            def.actionType         = data.actionType;
            def.basePower          = data.basePower;
            def.element            = data.element;
            def.energyCost         = data.energyCost;
            def.selfBuffDamage     = data.selfBuffDamage;
            def.selfBuffCharges    = data.selfBuffCharges;
            def.powerBoostPercent  = data.powerBoostPct;
            def.orbCount           = data.orbCount;
            def.orbDamage          = data.orbDamage;

            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"  {(forceRecreate ? "Recreated" : "Created")} {fileName}");
        }

        private struct ActionData
        {
            public string      actionId;
            public string      displayName;
            public ActionType  actionType;
            public float       basePower;
            public ElementType element;
            public float       energyCost;
            public int         selfBuffDamage;
            public int         selfBuffCharges;
            public float       powerBoostPct;
            public int         orbCount;
            public int         orbDamage;
        }
    }
}
