using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

/// <summary>
/// Builds the UnitAnimator controller from scratch.
/// Run via Tools > Build Unit Animator Controller.
/// Safe to re-run — it overwrites the existing controller each time.
/// </summary>
public class UnitAnimatorBuilder : Editor
{
    private const string ControllerPath = "Assets/Art/Characters/UnitAnimator.controller";
    private const string ClipBasePath   = "Assets/Art/Characters/";

    // ── Clip names exactly as they appear in the Project window ──────────────
    // Key = friendly name used in this script, Value = FBX filename (without .fbx)
    private static readonly Dictionary<string, string> ClipSources = new Dictionary<string, string>
    {
        { "Idle",           "Ch24_nonPBR@Idle"              },
        { "FightIdle",      "Ch24_nonPBR@Fight Idle"        },
        { "Running",        "Ch24_nonPBR@Running"           },
        { "Punching",       "Ch24_nonPBR@Punching"          },
        { "CrossPunch",     "Ch24_nonPBR@Cross Punch"       },
        { "ElbowPunch",     "Ch24_nonPBR@Elbow Punching"    },
        { "Kick",           "Ch24_nonPBR@Kicking"           },
        { "Reaction",       "Ch24_nonPBR@Reaction"          },
        { "Stunned",        "Ch24_nonPBR@Stunned"           },
        { "BodyBlock",      "Ch24_nonPBR@Body Block"        },
        { "Dodging",        "Ch24_nonPBR@Dodging"           },
        { "SpellCasting",   "Ch24_nonPBR@Spell Casting"     },
        { "KnockedOut",     "Ch24_nonPBR@Knocked Out"       },
        { "DyingBackwards", "Ch24_nonPBR@Dying Backwards"   },
        { "GettingUp",      "Ch24_nonPBR@Getting Up"        },
    };

    [MenuItem("Tools/Build Unit Animator Controller")]
    public static void Build()
    {
        // ── Delete the old controller so no stale transitions survive ────────
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AssetDatabase.Refresh();
        }

        // ── Load clips ───────────────────────────────────────────────────────
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var kv in ClipSources)
        {
            AnimationClip clip = LoadClip(kv.Value);
            if (clip == null)
                Debug.LogWarning($"[UnitAnimatorBuilder] Could not find clip in '{kv.Value}.fbx' — state will have no motion.");
            clips[kv.Key] = clip;
        }

        // ── Create controller ────────────────────────────────────────────────
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // ── Parameters ──────────────────────────────────────────────────────
        ctrl.AddParameter("Speed",       AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack",      AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit",         AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Cast",        AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Death",       AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("IsRecovering",AnimatorControllerParameterType.Bool);

        AnimatorStateMachine root = ctrl.layers[0].stateMachine;

        // ── Locomotion blend tree (default state) ────────────────────────────
        AnimatorState locoState = root.AddState("Locomotion");
        locoState.tag = "Locomotion";

        var locoTree = new BlendTree();
        AssetDatabase.AddObjectToAsset(locoTree, ctrl);
        locoTree.name            = "LocoTree";
        locoTree.blendType       = BlendTreeType.Simple1D;
        locoTree.blendParameter  = "Speed";
        locoTree.useAutomaticThresholds = false;
        locoTree.AddChild(clips.GetValueOrDefault("FightIdle"), 0f);
        locoTree.AddChild(clips.GetValueOrDefault("Running"),   1f);
        locoState.motion = locoTree;

        root.defaultState = locoState;

        // ── Attack state (blend tree picks a random punch each time) ─────────
        ctrl.AddParameter("AttackVariant", AnimatorControllerParameterType.Float);

        AnimatorState attackState = root.AddState("Attack");
        attackState.tag = "Attack";

        var attackTree = new BlendTree();
        AssetDatabase.AddObjectToAsset(attackTree, ctrl);
        attackTree.name           = "AttackTree";
        attackTree.blendType      = BlendTreeType.Simple1D;
        attackTree.blendParameter = "AttackVariant";
        attackTree.useAutomaticThresholds = false;
        attackTree.AddChild(clips.GetValueOrDefault("Punching"),   0f);
        attackTree.AddChild(clips.GetValueOrDefault("CrossPunch"), 0.5f);
        attackTree.AddChild(clips.GetValueOrDefault("ElbowPunch"), 1f);
        attackState.motion = attackTree;

        // ── Hit / Stagger state ──────────────────────────────────────────────
        AnimatorState hitState = root.AddState("Hit");
        hitState.tag    = "Hit";
        hitState.motion = clips.GetValueOrDefault("Reaction");

        // ── Stunned state (heavy stagger) ────────────────────────────────────
        AnimatorState stunnedState = root.AddState("Stunned");
        stunnedState.tag    = "Stunned";
        stunnedState.motion = clips.GetValueOrDefault("Stunned");

        // ── Block / Defend state ─────────────────────────────────────────────
        AnimatorState blockState = root.AddState("Block");
        blockState.tag    = "Block";
        blockState.motion = clips.GetValueOrDefault("BodyBlock");

        // ── Recover state ────────────────────────────────────────────────────
        AnimatorState recoverState = root.AddState("Recover");
        recoverState.tag    = "Recover";
        recoverState.motion = clips.GetValueOrDefault("FightIdle");  // settle back into stance

        // ── Cast state ───────────────────────────────────────────────────────
        AnimatorState castState = root.AddState("Cast");
        castState.tag    = "Cast";
        castState.motion = clips.GetValueOrDefault("SpellCasting");

        // ── Dodge state ──────────────────────────────────────────────────────
        AnimatorState dodgeState = root.AddState("Dodge");
        dodgeState.tag    = "Dodge";
        dodgeState.motion = clips.GetValueOrDefault("Dodging");

        // ── Death state ──────────────────────────────────────────────────────
        AnimatorState deathState = root.AddState("Death");
        deathState.tag    = "Death";
        deathState.motion = clips.GetValueOrDefault("KnockedOut");

        // ════════════════════════════════════════════════════════════════════
        //  TRANSITIONS  — hub model
        //  Any State → action states (triggers)
        //  Every action state → Locomotion (exit time)
        // ════════════════════════════════════════════════════════════════════

        // Helper: Any State → target on a trigger
        void AnyTrigger(AnimatorState to, string trigger, float duration = 0.1f)
        {
            var t = root.AddAnyStateTransition(to);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.hasExitTime         = false;
            t.duration            = duration;
            t.canTransitionToSelf = false;
        }

        // Helper: state → Locomotion after clip finishes
        void BackToLoco(AnimatorState from, float exitTime = 0.9f, float duration = 0.15f)
        {
            var t = from.AddTransition(locoState);
            t.hasExitTime  = true;
            t.exitTime     = exitTime;
            t.duration     = duration;
            t.canTransitionToSelf = false;
        }

        // ── Any State → action states ────────────────────────────────────────
        AnyTrigger(attackState, "Attack",  0.05f);
        AnyTrigger(hitState,    "Hit",     0.05f);
        AnyTrigger(castState,   "Cast",    0.10f);
        AnyTrigger(deathState,  "Death",   0.10f);

        // ── Every action state → Locomotion ──────────────────────────────────
        BackToLoco(attackState,  0.85f, 0.15f);   // punch finishes then return
        BackToLoco(hitState,     0.90f, 0.15f);
        BackToLoco(castState,    0.95f, 0.15f);
        BackToLoco(dodgeState,   0.95f, 0.15f);
        BackToLoco(stunnedState, 0.95f, 0.15f);

        // Recover → Locomotion: condition first, exit-time fallback
        {
            var t = recoverState.AddTransition(locoState);
            t.hasExitTime         = false;
            t.duration            = 0.2f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRecovering");
        }
        BackToLoco(recoverState, 1f, 0.2f);

        // Death has no exit — unit stays dead

        // ── Assign controller to all prefabs that have an Animator ──────────
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art/Characters" });
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            Animator anim = prefab.GetComponentInChildren<Animator>();
            if (anim == null) continue;

            anim.runtimeAnimatorController = ctrl;
            EditorUtility.SetDirty(prefab);
            Debug.Log($"[UnitAnimatorBuilder] Assigned controller to prefab: {prefab.name}");
        }

        // ── Save ─────────────────────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[UnitAnimatorBuilder] ✅ UnitAnimator.controller rebuilt successfully.");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = ctrl;
    }

    // ── Finds the first AnimationClip inside an FBX asset ────────────────────
    private static AnimationClip LoadClip(string fbxName)
    {
        string path = $"{ClipBasePath}{fbxName}.fbx";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets == null) return null;

        foreach (var asset in assets)
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;

        return null;
    }
}
