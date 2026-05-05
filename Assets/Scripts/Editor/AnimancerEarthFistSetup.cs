#if UNITY_EDITOR
using Animancer;
using TacticalRPG.DataModels;
using UnityEditor;
using UnityEngine;

namespace TacticalRPG.EditorTools
{
    /// <summary>
    /// One-shot bootstrapper for the Earth Fist Animancer proof-of-concept.
    /// Creates Transition_EarthPunch.asset (TransitionAsset wrapping the punch
    /// AnimationClip) and AttackProfile_EarthFist.asset under Assets/Data/Profiles.
    /// Idempotent — re-running updates the existing assets.
    /// </summary>
    public static class AnimancerEarthFistSetup
    {
        private const string ProfileDir     = "Assets/Data/Profiles";
        private const string TransitionPath = "Assets/Data/Profiles/Transition_EarthPunch.asset";
        private const string ProfilePath    = "Assets/Data/Profiles/AttackProfile_EarthFist.asset";
        private const string PunchFbxPath   = "Assets/Art/Characters/Ch24_nonPBR@Punching.fbx";

        [MenuItem("TacticalRPG/Animancer/Setup Earth Fist Profile")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(ProfileDir))
                AssetDatabase.CreateFolder("Assets/Data", "Profiles");

            AnimationClip punchClip = null;
            foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(PunchFbxPath))
            {
                if (sub is AnimationClip ac && !ac.name.StartsWith("__preview__"))
                {
                    punchClip = ac;
                    break;
                }
            }
            if (punchClip == null)
            {
                Debug.LogError($"[EarthFistSetup] No AnimationClip found in {PunchFbxPath}");
                return;
            }
            Debug.Log($"[EarthFistSetup] Found punch clip: {punchClip.name} (length={punchClip.length:F2}s)");

            var transition = AssetDatabase.LoadAssetAtPath<TransitionAsset>(TransitionPath);
            bool createdTransition = false;
            if (transition == null)
            {
                transition = ScriptableObject.CreateInstance<TransitionAsset>();
                var clip = new ClipTransition { Clip = punchClip };
                transition.Transition = clip;
                AssetDatabase.CreateAsset(transition, TransitionPath);
                createdTransition = true;
            }
            else if (transition.Transition is ClipTransition existing)
            {
                existing.Clip = punchClip;
                EditorUtility.SetDirty(transition);
            }
            else
            {
                var clip = new ClipTransition { Clip = punchClip };
                transition.Transition = clip;
                EditorUtility.SetDirty(transition);
            }
            Debug.Log(createdTransition
                ? $"[EarthFistSetup] Created TransitionAsset at {TransitionPath}"
                : $"[EarthFistSetup] Updated TransitionAsset at {TransitionPath}");

            var profile = AssetDatabase.LoadAssetAtPath<AttackProfile>(ProfilePath);
            bool createdProfile = false;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AttackProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
                createdProfile = true;
            }

            profile.techniqueName            = "Earth Fist";
            profile.transition               = transition;
            profile.impactEventName          = null; // legacy clip events forwarded by relay
            profile.minStartRange            = 0f;
            profile.idealStartRange          = 2f;
            profile.maxStartRange            = 3f;
            profile.desiredImpactDistance    = 1.5f;
            profile.allowedAngleDegrees      = 30f;
            profile.requiresPreAlign         = true;
            profile.requiresEngagementSlot   = true;
            profile.canUseIfTooClose         = true;
            profile.movementMode             = ActionMovementMode.InPlace;
            profile.useRootMotion            = false;
            profile.lockMovementDuringCommit = true;
            profile.lockRotationDuringImpact = true;
            profile.scriptedTravelDistance   = 0f;
            profile.causesKnockback          = false;
            profile.knockbackDistance        = 0f;
            EditorUtility.SetDirty(profile);

            Debug.Log(createdProfile
                ? $"[EarthFistSetup] Created AttackProfile at {ProfilePath}"
                : $"[EarthFistSetup] Updated AttackProfile at {ProfilePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EarthFistSetup] Asset setup complete.");
        }
    }
}
#endif
