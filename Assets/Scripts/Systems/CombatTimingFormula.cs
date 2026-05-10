using TacticalRPG.DataModels;
using UnityEngine;

namespace TacticalRPG.Systems
{
    /// <summary>
    /// Phase 11 — final action execution time, derived from SPD attribute,
    /// proficiency, and current speed band. See
    /// Docs/Design/COMBAT_DESIGN.md "How SPD and proficiency combine".
    ///
    ///     final_time = base_time × (10 / SPD) × (1 / proficiency) × speed_band_modifier
    ///
    /// Reference SPD = 10, reference proficiency = 1.0, reference band = Engaged (1.0).
    /// </summary>
    public static class CombatTimingFormula
    {
        // Per spec: cosmetic-only band modifier on visuals. We expose it here too
        // so resolvers can use the same numbers when computing gameplay-truth time.
        public static float BandModifier(SpeedBand band)
        {
            switch (band)
            {
                case SpeedBand.Sluggish: return 0.85f;
                case SpeedBand.Engaged:  return 1.00f;
                case SpeedBand.Sharp:    return 1.10f;
                case SpeedBand.Primed:   return 1.20f;
                default:                 return 1.00f;
            }
        }

        public static float ComputeExecutionTime(
            float baseTime,
            UnitRuntime caster,
            ResolvedTechnique tech,
            SpeedBand band)
        {
            if (caster == null || tech == null) return baseTime;

            float spd        = Mathf.Max(1f, caster.currentStats.moveSpeed);
            float proficiency = 1.0f;
            if (caster.definition != null && tech.element != ElementType.None)
                proficiency = caster.definition.proficiencies.GetProficiencyBonus(tech.element);

            float bandMod = BandModifier(band);

            // Faster band = less time (1/bandMod). Higher SPD = less time.
            float t = baseTime * (10f / spd) * (1f / Mathf.Max(0.1f, proficiency)) * (1f / bandMod);

            // Tight clamp — combat should feel snappy. Anime-fast on the
            // high end, never sluggish enough to break tempo on the low end.
            return Mathf.Clamp(t, baseTime * 0.5f, baseTime * 1.8f);
        }
    }
}
