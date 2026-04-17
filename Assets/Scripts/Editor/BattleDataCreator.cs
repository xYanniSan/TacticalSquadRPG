using TacticalRPG.DataModels;
using UnityEditor;
using UnityEngine;

namespace TacticalRPG.Editor
{
    public static class BattleDataCreator
    {
        [MenuItem("TacticalRPG/Create Battle Data")]
        public static void CreateAllBattleData()
        {
            // Create folder
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            // ── Player Heroes ─────────────────────────────────────────

            CreateUnit("Hero_Kai", new UnitData
            {
                unitId      = "hero_kai",
                displayName = "Kai",
                maxHP       = 100,
                attack      = 10,
                defense     = 5,
                moveSpeed   = 3f,
                behavior    = BehaviorType.Aggressive
            });

            CreateUnit("Hero_Mira", new UnitData
            {
                unitId      = "hero_mira",
                displayName = "Mira",
                maxHP       = 80,
                attack      = 12,
                defense     = 3,
                moveSpeed   = 4f,
                behavior    = BehaviorType.Aggressive
            });

            // ── Enemies ───────────────────────────────────────────────

            CreateUnit("Enemy_Grunt_A", new UnitData
            {
                unitId      = "enemy_a",
                displayName = "Grunt A",
                maxHP       = 60,
                attack      = 8,
                defense     = 3,
                moveSpeed   = 3f,
                behavior    = BehaviorType.Aggressive
            });

            CreateUnit("Enemy_Grunt_B", new UnitData
            {
                unitId      = "enemy_b",
                displayName = "Grunt B",
                maxHP       = 70,
                attack      = 7,
                defense     = 4,
                moveSpeed   = 2f,
                behavior    = BehaviorType.Defensive
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BattleDataCreator] Created 4 unit definitions in Assets/Data/");
        }

        private static void CreateUnit(string fileName, UnitData data)
        {
            string path = $"Assets/Data/{fileName}.asset";

            // Skip if already exists
            if (AssetDatabase.LoadAssetAtPath<UnitDefinition>(path) != null)
            {
                Debug.Log($"  Skipped {fileName} (already exists)");
                return;
            }

            UnitDefinition def = ScriptableObject.CreateInstance<UnitDefinition>();
            def.unitId          = data.unitId;
            def.displayName     = data.displayName;
            def.baseStats       = new StatBlock(data.maxHP, data.attack, data.defense, data.moveSpeed);
            def.defaultBehavior = data.behavior;

            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"  Created {fileName}");
        }

        private struct UnitData
        {
            public string unitId;
            public string displayName;
            public int maxHP;
            public int attack;
            public int defense;
            public float moveSpeed;
            public BehaviorType behavior;
        }
    }
}
