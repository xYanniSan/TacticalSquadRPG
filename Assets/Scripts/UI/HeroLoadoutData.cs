using System.Collections.Generic;
using TacticalRPG.DataModels;

namespace TacticalRPG.UI
{
    /// <summary>
    /// Static data store that survives scene transitions.
    /// The hero config menu writes here; the battle scene reads.
    /// </summary>
    public static class HeroLoadoutData
    {
        /// <summary>Key = UnitDefinition.unitId, Value = configured skill slots.</summary>
        public static Dictionary<string, List<SkillSlot>> Loadouts = new Dictionary<string, List<SkillSlot>>();

        /// <summary>Ordered list of heroes the player selected for battle.</summary>
        public static List<UnitDefinition> SelectedHeroes = new List<UnitDefinition>();

        public static void Clear()
        {
            Loadouts.Clear();
            SelectedHeroes.Clear();
        }

        /// <summary>
        /// Saves a hero's current skill config. Replaces any previous entry.
        /// </summary>
        public static void SaveLoadout(UnitDefinition hero, List<SkillSlot> skills)
        {
            Loadouts[hero.unitId] = skills;

            if (!SelectedHeroes.Contains(hero))
                SelectedHeroes.Add(hero);
        }

        /// <summary>
        /// Returns the saved skill slots for a hero, or an empty list.
        /// </summary>
        public static List<SkillSlot> GetLoadout(string unitId)
        {
            if (Loadouts.TryGetValue(unitId, out var slots))
                return slots;
            return new List<SkillSlot>();
        }

        /// <summary>True if at least one hero has at least one non-empty skill slot.</summary>
        public static bool HasAnyLoadout()
        {
            foreach (var kvp in Loadouts)
            {
                foreach (var slot in kvp.Value)
                {
                    if (slot.actionSequence.Count > 0)
                        return true;
                }
            }
            return false;
        }
    }
}
