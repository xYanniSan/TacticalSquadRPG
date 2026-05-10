#if UNITY_EDITOR
using System.IO;
using TacticalRPG.DataModels;
using UnityEditor;
using UnityEngine;

namespace TacticalRPG.Editor
{
    /// <summary>
    /// Creates the seven default stance assets in `Assets/Data/Stances/`.
    /// Idempotent — re-running rewrites the assets to spec values, so this is
    /// the canonical "rebuild stances from spec" tool.
    /// </summary>
    public static class StanceDataCreator
    {
        private const string Folder = "Assets/Data/Stances";

        [MenuItem("TacticalRPG/Create Default Stances (force)")]
        public static void Create()
        {
            EnsureFolder(Folder);

            CreateStance(StanceId.Onslaught, "Onslaught",
                "Constant pressure, high risk, looks for kills. Spend liberally, low commit threshold.",
                BehaviorType.Aggressive, 50f, 0.30f, 0f, TargetPriority.Nearest,
                MovementIntent.Close, 0.7f, 0f);

            CreateStance(StanceId.Tempest, "Tempest",
                "Burst skirmisher; closes fast, commits hard. Builds aggressively for big bursts.",
                BehaviorType.Aggressive, 60f, 0.40f, 0f, TargetPriority.BacklineFirst,
                MovementIntent.Close, 0.6f, 0f);

            CreateStance(StanceId.Stalwart, "Stalwart",
                "Protector role; holds position, supports allies. Hoards speed, commits only when primed.",
                BehaviorType.Defensive, 80f, 0.60f, 25f, TargetPriority.AttackerOfAlly,
                MovementIntent.Hold, 0.3f, 0.5f);

            CreateStance(StanceId.Tactician, "Tactician",
                "Smart finisher; picks fights they can win. Reads situation adaptively.",
                BehaviorType.Balanced, 65f, 0.50f, 10f, TargetPriority.LowestHP,
                MovementIntent.Circle, 0.45f, 0f);

            CreateStance(StanceId.Wraith, "Wraith",
                "Hit-and-run assassin; never commits to standing fights. High mobility, dodge-heavy.",
                BehaviorType.Aggressive, 40f, 0.45f, 0f, TargetPriority.BacklineFirst,
                MovementIntent.Circle, 0.85f, 0f);

            CreateStance(StanceId.Sentinel, "Sentinel",
                "Static defender; one target, holds the line. Builds slowly, never spends below 30.",
                BehaviorType.Defensive, 85f, 0.65f, 30f, TargetPriority.Marked,
                MovementIntent.Hold, 0.2f, 1.0f);

            CreateStance(StanceId.Conduit, "Conduit",
                "Caster role; energy-focused, range-focused. Speed reserved for emergency dodges only.",
                BehaviorType.Balanced, 75f, 0.55f, 20f, TargetPriority.Furthest,
                MovementIntent.Disengage, 0.6f, 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[StanceDataCreator] Created/refreshed 7 default stances in " + Folder);
        }

        private static void CreateStance(StanceId id, string displayName, string description,
            BehaviorType bias, float speedThresholdBigCombo, float hpThresholdDisengage,
            float speedReserveFloor, TargetPriority targetPriority,
            MovementIntent preferredIntent, float dodgeWillingnessAtLowSpeed,
            float engagementDelaySeconds)
        {
            string path = $"{Folder}/Stance_{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<StanceDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StanceDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.id = id;
            asset.displayName = displayName;
            asset.description = description;
            asset.behaviorBias = bias;
            asset.speedThresholdBigCombo = speedThresholdBigCombo;
            asset.hpThresholdDisengage = hpThresholdDisengage;
            asset.speedReserveFloor = speedReserveFloor;
            asset.targetPriority = targetPriority;
            asset.preferredIntent = preferredIntent;
            asset.dodgeWillingnessAtLowSpeed = dodgeWillingnessAtLowSpeed;
            asset.engagementDelaySeconds = engagementDelaySeconds;

            EditorUtility.SetDirty(asset);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf   = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
