using System.Collections.Generic;
using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Runtime fallback that gives a unit a varied skill loadout when its
    /// `equippedSkills` is empty after spawn. Without this, units bypass the
    /// Hero Config UI and end up with no skills, so combos never match and
    /// combat falls through to single basic-attack hits.
    ///
    /// Loads ActionDefinition assets via Resources.Load — requires the
    /// actions to be present at `Assets/Resources/Actions/*.asset`. If
    /// Resources lookup fails (action assets aren't in Resources/), the
    /// builder is a no-op and the unit stays bare.
    /// </summary>
    public static class DefaultLoadoutBuilder
    {
        // Cached lookup so we hit Resources.Load only once per ID.
        private static readonly Dictionary<string, ActionDefinition> _cache
            = new Dictionary<string, ActionDefinition>();

        public static void Apply(UnitRuntime unit)
        {
            if (unit == null) return;
            if (unit.equippedSkills != null && unit.equippedSkills.Count > 0) return;

            // Equip four varied combos so the brain has different speed
            // costs / CC types / strike counts to choose between.
            //   Earth Fist     (handsign_a + punch)         — basic 2-strike
            //   Crescent Kick  (focus + kick)               — LaunchCombo finisher
            //   Power Strike   (focus + punch)              — speed-scaling single
            //   Tidal Sweep    (handsign_c + kick)          — Slow CC kick
            TryAddSkill(unit, "handsign_a", "punch");
            TryAddSkill(unit, "focus", "kick");
            TryAddSkill(unit, "focus", "punch");
            TryAddSkill(unit, "handsign_c", "kick");

            if (unit.equippedSkills.Count > 0)
                Debug.Log($"[DefaultLoadoutBuilder] {unit.DisplayName} auto-equipped {unit.equippedSkills.Count} skills (no loadout was set).");
        }

        private static void TryAddSkill(UnitRuntime unit, params string[] actionIds)
        {
            var slot = new SkillSlot(unit.equippedSkills.Count);
            foreach (var id in actionIds)
            {
                var action = LoadAction(id);
                if (action == null)
                {
                    Debug.LogWarning($"[DefaultLoadoutBuilder] Action '{id}' not found in Resources/Actions/. Skipping skill.");
                    return;
                }
                slot.AddAction(action);
            }
            unit.equippedSkills.Add(slot);
        }

        private static ActionDefinition LoadAction(string actionId)
        {
            if (_cache.TryGetValue(actionId, out var cached)) return cached;

            string fileName = actionId switch
            {
                "punch"      => "Punch",
                "kick"       => "Kick",
                "handsign_a" => "HandSignA",
                "handsign_b" => "HandSignB",
                "handsign_c" => "HandSignC",
                "focus"      => "Focus",
                _            => actionId
            };

            // Try Resources first (works in builds).
            var action = Resources.Load<ActionDefinition>($"Actions/{fileName}");

#if UNITY_EDITOR
            // Editor fallback — assets live in Assets/Data/Actions/ rather
            // than a Resources folder. Find by AssetDatabase so the dev
            // workflow doesn't require copying the assets twice.
            if (action == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets(
                    $"{fileName} t:ActionDefinition", new[] { "Assets/Data/Actions" });
                if (guids != null && guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    action = UnityEditor.AssetDatabase.LoadAssetAtPath<ActionDefinition>(path);
                }
            }
#endif
            if (action != null) _cache[actionId] = action;
            return action;
        }
    }
}
