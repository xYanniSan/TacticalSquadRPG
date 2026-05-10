#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TacticalRPG.EditorTools
{
    /// <summary>
    /// One-shot bootstrapper for the UpperBody humanoid AvatarMask used by
    /// `BattleAnimancerDriver`'s layered playback. Saves to
    /// `Assets/Animation/Masks/UpperBody.mask`. Re-runnable: refreshes the
    /// existing asset's body-part flags in place if the file already exists,
    /// so the menu can be used to reset the mask after manual edits.
    ///
    /// Body-part configuration (per `Docs/Design/ENGAGEMENT_ANIMATIONS.md`
    /// §4.1 and the spec in this task):
    ///
    ///   Enabled  → upper layer drives:
    ///     - Body          (spine + chest — drives torso twist for punches)
    ///     - Head          (head pose for casting / aiming)
    ///     - LeftArm       (full left arm chain)
    ///     - RightArm      (full right arm chain)
    ///     - LeftFingers   (fingers — important for hand signs and grips)
    ///     - RightFingers
    ///     - LeftHandIK    (so hand IK targets layer the upper layer)
    ///     - RightHandIK
    ///
    ///   Disabled → base layer keeps control:
    ///     - Root          (root motion belongs to base / locomotion)
    ///     - LeftLeg       (leg swing during walk/run/sprint stays on base)
    ///     - RightLeg
    ///     - LeftFootIK    (foot IK belongs to base / H2HFootIK)
    ///     - RightFootIK
    ///
    /// **Caveat (documented for the report):** Unity's Humanoid AvatarMask
    /// has no separate "Hips" toggle — `Body` includes the hips bone *and*
    /// the spine chain. Enabling Body means the upper layer drives both,
    /// which can dampen the natural hip swing of the walk/run loops.
    /// Trade-off accepted: the alternative (Body off) makes punches look
    /// stiff because the torso doesn't twist with the strike. We'll
    /// re-evaluate per-bone Generic mask if this becomes a problem.
    /// </summary>
    public static class UpperBodyMaskCreator
    {
        public const string MaskFolder = "Assets/Animation/Masks";
        public const string MaskPath   = "Assets/Animation/Masks/UpperBody.mask";

        [MenuItem("TacticalRPG/Animation/Create UpperBody Mask")]
        public static void CreateOrRefresh()
        {
            EnsureFolder(MaskFolder);

            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            bool created = false;
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, MaskPath);
                created = true;
            }

            // Set humanoid body-part flags (13 entries).
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root,         false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body,         true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head,         true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg,      false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg,     false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm,      true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm,     true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers,  true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK,   false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK,  false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK,   true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK,  true);

            EditorUtility.SetDirty(mask);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UpperBodyMaskCreator] {(created ? "Created" : "Refreshed")} {MaskPath}.\n" +
                      "Enabled: Body, Head, LeftArm, RightArm, LeftFingers, RightFingers, LeftHandIK, RightHandIK\n" +
                      "Disabled: Root, LeftLeg, RightLeg, LeftFootIK, RightFootIK");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
