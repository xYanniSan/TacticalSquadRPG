using TacticalRPG.DataModels;
using UnityEditor;
using UnityEngine;

namespace TacticalRPG.Editor
{
    public static class SkillDataCreator
    {
        [MenuItem("TacticalRPG/Create Action Definitions")]
        public static void CreateAllActions()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Actions"))
                AssetDatabase.CreateFolder("Assets/Data", "Actions");

            // ── Physical Actions ─────────────────────────────────────
            CreateAction("Punch", new ActionData
            {
                actionId   = "punch",
                displayName = "Punch",
                actionType = ActionType.Physical,
                basePower  = 10f,
                element    = ElementType.None
            });

            CreateAction("Kick", new ActionData
            {
                actionId   = "kick",
                displayName = "Kick",
                actionType = ActionType.Physical,
                basePower  = 15f,
                element    = ElementType.None
            });

            // ── Elemental Actions ────────────────────────────────────
            CreateAction("HandSignA", new ActionData
            {
                actionId   = "handsign_a",
                displayName = "Hand Sign A",
                actionType = ActionType.Elemental,
                basePower  = 12f,
                element    = ElementType.Fire
            });

            CreateAction("HandSignB", new ActionData
            {
                actionId   = "handsign_b",
                displayName = "Hand Sign B",
                actionType = ActionType.Elemental,
                basePower  = 10f,
                element    = ElementType.Fire
            });

            CreateAction("HandSignC", new ActionData
            {
                actionId   = "handsign_c",
                displayName = "Hand Sign C",
                actionType = ActionType.Elemental,
                basePower  = 8f,
                element    = ElementType.Earth
            });

            // ── Support Actions ──────────────────────────────────────
            CreateAction("Focus", new ActionData
            {
                actionId   = "focus",
                displayName = "Focus",
                actionType = ActionType.Support,
                basePower  = 8f,
                element    = ElementType.None
            });

            CreateAction("Meditate", new ActionData
            {
                actionId   = "meditate",
                displayName = "Meditate",
                actionType = ActionType.Support,
                basePower  = 5f,
                element    = ElementType.None
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SkillDataCreator] Created 7 action definitions in Assets/Data/Actions/");
        }

        private static void CreateAction(string fileName, ActionData data)
        {
            string path = $"Assets/Data/Actions/{fileName}.asset";

            if (AssetDatabase.LoadAssetAtPath<ActionDefinition>(path) != null)
            {
                Debug.Log($"  Skipped {fileName} (already exists)");
                return;
            }

            ActionDefinition def = ScriptableObject.CreateInstance<ActionDefinition>();
            def.actionId    = data.actionId;
            def.displayName = data.displayName;
            def.actionType  = data.actionType;
            def.basePower   = data.basePower;
            def.element     = data.element;

            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"  Created {fileName}");
        }

        private struct ActionData
        {
            public string actionId;
            public string displayName;
            public ActionType actionType;
            public float basePower;
            public ElementType element;
        }
    }
}
