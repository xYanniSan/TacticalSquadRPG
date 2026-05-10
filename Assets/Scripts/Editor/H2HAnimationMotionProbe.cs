#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TacticalRPG.EditorTools
{
    /// <summary>
    /// One-shot probe for every Kubold AnimationClip — reports each clip's
    /// effective root-motion delta (forward, lateral, vertical, rotation)
    /// under the current FBX import settings. Output is written to
    /// `Docs/Design/ANIMATION_MOTION_PROBE.md` as a markdown table so the
    /// inventory doc can reference real numbers when picking the right
    /// "step-in" vs "in-place" clip variant for combo wiring.
    ///
    /// Re-run after changing any FBX import option (lockRootPositionXZ,
    /// keepOriginalPositionXZ, etc.) — the numbers reflect the *effective*
    /// motion as the runtime sees it, not the raw mocap.
    /// </summary>
    public static class H2HAnimationMotionProbe
    {
        private const string OutputPath = "Docs/Design/ANIMATION_MOTION_PROBE.md";

        private static readonly string[] FbxRoots =
        {
            "Assets/Art/FightingAnimsetPro/Animations",
            "Assets/Art/MovementAnimsetPro/Animations",
        };

        // Clips below this displacement count as "in-place".
        private const float MotionThresholdMeters = 0.10f;
        private const float RotationThresholdDegrees = 5f;
        private const float VerticalThresholdMeters = 0.15f;

        [MenuItem("TacticalRPG/H2H/Probe Clip Motion")]
        public static void Run()
        {
            var rows = new List<Row>();
            int skipped = 0;

            foreach (var dir in FbxRoots)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    Debug.LogWarning($"[H2HAnimationMotionProbe] Folder not found: {dir}");
                    continue;
                }

                var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { dir });
                foreach (var guid in fbxGuids)
                {
                    var fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                    var subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                    foreach (var obj in subAssets)
                    {
                        if (obj is AnimationClip clip
                            && !clip.name.StartsWith("__preview__"))
                        {
                            rows.Add(Probe(fbxPath, clip));
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                }
            }

            rows.Sort((a, b) =>
            {
                int c = string.Compare(a.fbxPath, b.fbxPath, System.StringComparison.Ordinal);
                if (c != 0) return c;
                return string.Compare(a.clipName, b.clipName, System.StringComparison.Ordinal);
            });

            int moves = 0, inPlace = 0, airborne = 0;
            foreach (var r in rows)
            {
                if (r.verdict == "in-place") inPlace++;
                else if (r.verdict.Contains("air")) airborne++;
                else moves++;
            }

            var sb = new StringBuilder();
            sb.AppendLine("# ANIMATION_MOTION_PROBE.md");
            sb.AppendLine();
            sb.AppendLine("> **Auto-generated.** Run `TacticalRPG → H2H → Probe Clip Motion` in the Unity Editor to refresh. Reports the effective root-motion delta of every Kubold AnimationClip under the current FBX import settings. Re-run after changing any import option.");
            sb.AppendLine();
            sb.AppendLine($"**Threshold:** ≥ {MotionThresholdMeters:0.00}m forward/lateral, ≥ {VerticalThresholdMeters:0.00}m vertical, or ≥ {RotationThresholdDegrees:0.0}° rotation = **moves**. Otherwise **in-place**.");
            sb.AppendLine();
            sb.AppendLine($"**Totals:** {rows.Count} clips probed → {inPlace} in-place, {moves} ground-motion, {airborne} airborne.");
            sb.AppendLine();
            sb.AppendLine("| FBX | Clip | Length (s) | Forward (m) | Lateral (m) | Vertical (m) | Rotation (°) | Verdict |");
            sb.AppendLine("|---|---|---:|---:|---:|---:|---:|---|");

            string lastFbx = null;
            foreach (var r in rows)
            {
                string fbxShort = Path.GetFileNameWithoutExtension(r.fbxPath);
                // Visual divider when the FBX changes
                if (fbxShort != lastFbx)
                {
                    lastFbx = fbxShort;
                }
                sb.AppendLine($"| {fbxShort} | `{r.clipName}` | {r.length:0.00} | {Format(r.forward)} | {Format(r.lateral)} | {Format(r.vertical)} | {FormatDeg(r.rotationDeg)} | {r.verdict} |");
            }

            string fullPath = Path.Combine(Application.dataPath, "..", OutputPath).Replace('\\', '/');
            string outputDir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            File.WriteAllText(fullPath, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"[H2HAnimationMotionProbe] Probed {rows.Count} clips → {OutputPath} (in-place: {inPlace}, moves: {moves}, airborne: {airborne}).");
        }

        private struct Row
        {
            public string fbxPath, clipName, verdict;
            public float length, forward, lateral, vertical, rotationDeg;
        }

        private static Row Probe(string fbxPath, AnimationClip clip)
        {
            float len = clip.length;
            // averageSpeed is reported in clip-local space: .z forward, .x lateral, .y vertical
            Vector3 vel = clip.averageSpeed;
            float angVel = clip.averageAngularSpeed; // rad/s around the up axis

            float fwd = vel.z * len;
            float lat = vel.x * len;
            float vert = vel.y * len;
            float rotDeg = angVel * len * Mathf.Rad2Deg;

            string verdict = ResolveVerdict(fwd, lat, vert, rotDeg);

            return new Row
            {
                fbxPath = fbxPath,
                clipName = clip.name,
                length = len,
                forward = fwd,
                lateral = lat,
                vertical = vert,
                rotationDeg = rotDeg,
                verdict = verdict,
            };
        }

        private static string ResolveVerdict(float fwd, float lat, float vert, float rotDeg)
        {
            bool airborne = Mathf.Abs(vert) >= VerticalThresholdMeters;
            bool moves = Mathf.Abs(fwd) >= MotionThresholdMeters
                      || Mathf.Abs(lat) >= MotionThresholdMeters;
            bool rotates = Mathf.Abs(rotDeg) >= RotationThresholdDegrees;

            if (airborne && moves) return "airborne+moves";
            if (airborne)          return "airborne";

            if (moves && rotates)
            {
                return DirectionLabel(fwd, lat) + "+rotates";
            }
            if (moves)   return DirectionLabel(fwd, lat);
            if (rotates) return rotDeg > 0 ? "rotates right" : "rotates left";
            return "in-place";
        }

        private static string DirectionLabel(float fwd, float lat)
        {
            // Pick the dominant axis for a quick verdict word
            if (Mathf.Abs(fwd) >= Mathf.Abs(lat))
                return fwd >= 0 ? "moves fwd" : "moves bwd";
            return lat >= 0 ? "moves right" : "moves left";
        }

        private static string Format(float meters)
        {
            // Always show two decimals with sign for nonzero so columns align nicely
            if (Mathf.Abs(meters) < 0.005f) return "0.00";
            return (meters >= 0 ? "+" : "") + meters.ToString("0.00");
        }

        private static string FormatDeg(float deg)
        {
            if (Mathf.Abs(deg) < 0.05f) return "0.0";
            return (deg >= 0 ? "+" : "") + deg.ToString("0.0");
        }
    }
}
#endif
