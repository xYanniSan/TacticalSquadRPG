#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TacticalRPG.DataModels;

namespace TacticalRPG.Editor
{
    /// <summary>
    /// Creates the Tier 1 MoveDefinition assets defined in
    /// `Docs/Design/MOVES_CATALOG.md` "Authoring priorities → Tier 1".
    ///
    /// One menu item: TacticalRPG → Combat → Create Tier 1 Move Catalog.
    /// Idempotent — re-running updates frame data on existing assets
    /// without overwriting Inspector edits to other fields.
    ///
    /// Assets land at Assets/Resources/Moves/ so MoveCatalog can pick them
    /// up via Resources.LoadAll. Cancel-into chains are wired after all
    /// assets exist.
    /// </summary>
    public static class Tier1MoveCatalogCreator
    {
        private const string FolderPath = "Assets/Resources/Moves";

        [MenuItem("TacticalRPG/Combat/Create Tier 1 Move Catalog")]
        public static void Create()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(FolderPath);

            var assets = new Dictionary<string, MoveDefinition>();

            // Idle ------------------------------------------------------
            assets["idle"] = Build("idle",
                cat: MoveCategory.Idle,
                startup: 0, active: 0, recovery: 4,   // 200ms idle "loop"
                cancelWindow: 4,
                facing: FacingPolicy.FaceTarget);

            // Locomotion -----------------------------------------------
            assets["locomotion_walk_forward"] = Build("locomotion_walk_forward",
                cat: MoveCategory.Locomotion,
                startup: 0, active: 0, recovery: 4,
                cancelWindow: 4,
                forwardMps: 2.5f);

            assets["locomotion_run"] = Build("locomotion_run",
                cat: MoveCategory.Locomotion,
                startup: 0, active: 0, recovery: 4,
                cancelWindow: 4,
                forwardMps: 5f);

            // Light attacks --------------------------------------------
            assets["attack_punch_jab"] = Build("attack_punch_jab",
                cat: MoveCategory.LightAttack,
                startup: 2, active: 1, recovery: 4,
                cancelWindow: 3,
                damage: 8, range: 2.0f, angle: 45f,
                archetype: AttackArchetype.Light, react: ReactionTag.LightHit,
                isAttack: true);

            assets["attack_punch_hook"] = Build("attack_punch_hook",
                cat: MoveCategory.LightAttack,
                startup: 3, active: 1, recovery: 5,
                cancelWindow: 3,
                damage: 10, range: 2.0f, angle: 50f,
                archetype: AttackArchetype.Light, react: ReactionTag.LightHit,
                isAttack: true);

            // Tier 2 — combo identity ----------------------------------

            assets["attack_punch_uppercut"] = Build("attack_punch_uppercut",
                cat: MoveCategory.HeavyAttack,
                startup: 4, active: 1, recovery: 6,
                cancelWindow: 4,
                damage: 12, range: 1.8f, angle: 50f,
                archetype: AttackArchetype.Launch, react: ReactionTag.Launch,
                isAttack: true,
                speedCost: 10f);

            assets["attack_kick_low"] = Build("attack_kick_low",
                cat: MoveCategory.LightAttack,
                startup: 3, active: 2, recovery: 5,
                cancelWindow: 3,
                damage: 9, range: 2.2f, angle: 40f,
                archetype: AttackArchetype.Sweep, react: ReactionTag.Sweep,
                isAttack: true);

            assets["attack_kick_crescent"] = Build("attack_kick_crescent",
                cat: MoveCategory.HeavyAttack,
                startup: 6, active: 2, recovery: 10,
                cancelWindow: 5,
                damage: 18, range: 2.5f, angle: 60f,
                archetype: AttackArchetype.Launch, react: ReactionTag.Launch,
                isAttack: true,
                speedCost: 15f, speedGate: 20f);

            assets["attack_power_strike"] = Build("attack_power_strike",
                cat: MoveCategory.HeavyAttack,
                startup: 8, active: 1, recovery: 12,
                cancelWindow: 4,
                damage: 22, range: 2.2f, angle: 45f,
                archetype: AttackArchetype.Heavy, react: ReactionTag.Heavy,
                isAttack: true,
                speedCost: 20f, speedGate: 25f);

            // Side dodges (Sharp+ band) — keep range, evade laterally
            assets["defend_dodge_side_left"] = Build("defend_dodge_side_left",
                cat: MoveCategory.Dodge,
                startup: 2, active: 3, recovery: 3,
                cancelWindow: 0,
                lateralMps: -6f,
                iFrameStart: 0, iFrameEnd: 2);

            assets["defend_dodge_side_right"] = Build("defend_dodge_side_right",
                cat: MoveCategory.Dodge,
                startup: 2, active: 3, recovery: 3,
                cancelWindow: 0,
                lateralMps: 6f,
                iFrameStart: 0, iFrameEnd: 2);

            // Tier 3 — stance signatures
            // Bob/weave: in-place narrow-window evasion (Sharp+ band).
            assets["defend_bob_weave"] = Build("defend_bob_weave",
                cat: MoveCategory.Dodge,
                startup: 1, active: 2, recovery: 2,
                cancelWindow: 0,
                iFrameStart: 0, iFrameEnd: 1);

            // Parry: brief active window where incoming attacks are
            // converted into "parried" — attacker forced into Daze.
            assets["defend_parry"] = Build("defend_parry",
                cat: MoveCategory.Parry,
                startup: 0, active: 3, recovery: 8,
                cancelWindow: 0,
                isParry: true);

            // Static anchor: -75% incoming damage, no movement (Sentinel).
            assets["defend_static_anchor"] = Build("defend_static_anchor",
                cat: MoveCategory.Block,
                startup: 0, active: 6, recovery: 0,
                cancelWindow: 0,
                isBlock: true,
                incomingMult: 0.25f);

            // Fade-out: Wraith-only Primed teleport. Implemented as a
            // dodge with strong backward displacement during active i-frames.
            // (Visual ghosting handled by the Animator clip when authored.)
            assets["defend_fade_out"] = Build("defend_fade_out",
                cat: MoveCategory.Dodge,
                startup: 1, active: 4, recovery: 2,
                cancelWindow: 0,
                forwardMps: -10f,             // 4 active × 50ms × 10 = 2.0m back
                facing: FacingPolicy.Lock,
                iFrameStart: 0, iFrameEnd: 3,
                speedCost: 25f, speedGate: 50f);

            // Tier 2 hit reactions
            assets["react_hit_sweep"] = Build("react_hit_sweep",
                cat: MoveCategory.HitReact,
                startup: 0, active: 8, recovery: 0,
                cancelWindow: 0,
                forwardMps: -1f,
                facing: FacingPolicy.Lock);

            assets["react_launch_airborne"] = Build("react_launch_airborne",
                cat: MoveCategory.HitReact,
                startup: 0, active: 16, recovery: 0,
                cancelWindow: 0,
                forwardMps: -2f,
                facing: FacingPolicy.Lock);

            assets["react_knockdown_back"] = Build("react_knockdown_back",
                cat: MoveCategory.Knockdown,
                startup: 0, active: 30, recovery: 8,
                cancelWindow: 0,
                forwardMps: -2f,
                facing: FacingPolicy.Lock,
                iFrameStart: 5, iFrameEnd: 25); // grace window so they aren't pinned

            // Defensive ------------------------------------------------
            var blockReact = Build("defend_block_react",
                cat: MoveCategory.Block,
                startup: 1, active: 4, recovery: 4,
                cancelWindow: 0,
                isBlock: true,
                incomingMult: 0.5f);
            assets["defend_block_react"] = blockReact;

            var dodgeBack = Build("defend_dodge_back",
                cat: MoveCategory.Dodge,
                startup: 2, active: 4, recovery: 4,
                cancelWindow: 0,
                forwardMps: -7f,                      // backstep — short and crisp
                iFrameStart: 0, iFrameEnd: 3);
            assets["defend_dodge_back"] = dodgeBack;

            // Hit reactions --------------------------------------------
            // Forward speed is negative — defender slides back along their
            // facing (toward attacker), creating spacing after a hit. The
            // back half of the active window is i-framed so a same-target
            // cancel chain (jab → hook → jab) can't combo-lock the
            // defender forever; the second swing whiffs and the defender
            // gets a window to escape or counter.
            assets["react_hit_light"] = Build("react_hit_light",
                cat: MoveCategory.HitReact,
                startup: 0, active: 6, recovery: 2,
                cancelWindow: 0,
                forwardMps: -3.5f,
                facing: FacingPolicy.Lock,
                iFrameStart: 3, iFrameEnd: 5);

            assets["react_hit_heavy"] = Build("react_hit_heavy",
                cat: MoveCategory.HitReact,
                startup: 0, active: 12, recovery: 4,
                cancelWindow: 0,
                forwardMps: -5f,
                facing: FacingPolicy.Lock,
                iFrameStart: 6, iFrameEnd: 10);

            // Death ----------------------------------------------------
            assets["death_collapse"] = Build("death_collapse",
                cat: MoveCategory.Death,
                startup: 0, active: 30, recovery: 0,
                cancelWindow: 0);

            // Save all (creates new or overwrites in-place via SetDirty).
            foreach (var kv in assets)
                SaveAsset(kv.Key, kv.Value);

            // Wire cancel chains now that all assets exist.
            WireCancels(assets);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Tier1MoveCatalogCreator] Created/updated {assets.Count} Tier 1 moves under {FolderPath}/");
        }

        // ── Builders ────────────────────────────────────────────────

        private static MoveDefinition Build(string id,
            MoveCategory cat,
            int startup, int active, int recovery,
            int cancelWindow,
            int damage = 0, float range = 0f, float angle = 45f,
            AttackArchetype archetype = AttackArchetype.Light,
            ReactionTag react = ReactionTag.None,
            bool isAttack = false,
            float forwardMps = 0f,
            float lateralMps = 0f,
            FacingPolicy facing = FacingPolicy.FaceTarget,
            bool isBlock = false,
            bool isParry = false,
            float incomingMult = 1f,
            int iFrameStart = 0, int iFrameEnd = 0,
            float speedCost = 0f, float energyCost = 0f, float speedGate = 0f)
        {
            // Try to load existing.
            string path = $"{FolderPath}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MoveDefinition>(path);
            var mv = existing != null ? existing : ScriptableObject.CreateInstance<MoveDefinition>();
            mv.id = id;
            mv.animationName = id;
            mv.category = cat;
            mv.startupFrames  = startup;
            mv.activeFrames   = active;
            mv.recoveryFrames = recovery;
            mv.cancelWindowFrames = Mathf.Min(cancelWindow, recovery);
            mv.damage = damage;
            mv.range  = range;
            mv.angleDegrees = angle;
            mv.archetype = archetype;
            mv.reactionTag = react;
            mv.isAttack = isAttack;
            mv.forwardSpeedMetersPerSecond = forwardMps;
            mv.lateralSpeedMetersPerSecond = lateralMps;
            mv.facing = facing;
            mv.isBlock = isBlock;
            mv.isParry = isParry;
            mv.incomingDamageMultiplier = incomingMult;
            mv.iFrameStart = iFrameStart;
            mv.iFrameEnd = iFrameEnd;
            mv.speedCost = speedCost;
            mv.energyCost = energyCost;
            mv.speedGate = speedGate;
            return mv;
        }

        private static void SaveAsset(string id, MoveDefinition mv)
        {
            string path = $"{FolderPath}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<MoveDefinition>(path) == null)
                AssetDatabase.CreateAsset(mv, path);
            else
                EditorUtility.SetDirty(mv);
        }

        private static void WireCancels(Dictionary<string, MoveDefinition> a)
        {
            // Punch combo ladder: jab → hook → uppercut → kick_crescent (Launch)
            Wire(a, "attack_punch_jab",      onHit: new[] { "attack_punch_hook", "attack_punch_uppercut" });
            Wire(a, "attack_punch_hook",     onHit: new[] { "attack_punch_uppercut", "attack_punch_jab" });
            Wire(a, "attack_punch_uppercut", onHit: new[] { "attack_kick_crescent", "attack_kick_axe" });
            Wire(a, "attack_kick_low",       onHit: new[] { "attack_kick_crescent", "attack_punch_uppercut" });
            Wire(a, "attack_kick_crescent",  onHit: new string[0]);  // launch ends the combo
            Wire(a, "attack_power_strike",   onHit: new string[0]);  // committed heavy
            // Locomotion / idle / blocks have no cancel chains — brains pick neutral.
        }

        private static void Wire(Dictionary<string, MoveDefinition> a, string id, string[] onHit = null, string[] onWhiff = null)
        {
            if (!a.TryGetValue(id, out var mv) || mv == null) return;
            if (onHit != null)
            {
                mv.cancelIntoOnHit.Clear();
                foreach (var n in onHit)
                    if (a.TryGetValue(n, out var t) && t != null) mv.cancelIntoOnHit.Add(t);
            }
            if (onWhiff != null)
            {
                mv.cancelIntoOnWhiff.Clear();
                foreach (var n in onWhiff)
                    if (a.TryGetValue(n, out var t) && t != null) mv.cancelIntoOnWhiff.Add(t);
            }
            EditorUtility.SetDirty(mv);
        }

        // ── Folder helpers ─────────────────────────────────────────

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            int slash = assetPath.LastIndexOf('/');
            string parent = assetPath.Substring(0, slash);
            string leaf   = assetPath.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ── Diagnostic ─────────────────────────────────────────────

        [MenuItem("TacticalRPG/Combat/Dump Move Catalog")]
        public static void DumpCatalog()
        {
            var moves = AssetDatabase.FindAssets("t:MoveDefinition");
            Debug.Log($"[MoveCatalog] Found {moves.Length} MoveDefinition assets:");
            foreach (var guid in moves)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mv = AssetDatabase.LoadAssetAtPath<MoveDefinition>(path);
                if (mv == null) continue;
                Debug.Log($"  {mv.id}  ({mv.category}, total={mv.TotalFrames}f, " +
                          $"start={mv.startupFrames}/act={mv.activeFrames}/rec={mv.recoveryFrames}, " +
                          $"dmg={mv.damage}, range={mv.range:F1}, react={mv.reactionTag})  {path}");
            }
        }
    }
}
#endif
