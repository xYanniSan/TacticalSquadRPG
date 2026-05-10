using UnityEngine;

namespace TacticalRPG.DataModels
{
    /// <summary>
    /// Combat preset that bundles a hero's decision biases — speed-spend
    /// threshold, target priority, dodge willingness, movement preference.
    /// One per default stance (Onslaught, Tempest, Stalwart, Tactician,
    /// Wraith, Sentinel, Conduit). Selected per-hero per-mission via the Hero
    /// Config menu (per `06_HERO_CONFIG.md`).
    ///
    /// See `Docs/Design/COMBAT_DESIGN.md` "Stances — combat presets".
    /// </summary>
    [CreateAssetMenu(menuName = "TacticalRPG/Stance Definition")]
    public class StanceDefinition : ScriptableObject
    {
        [Header("Identity")]
        public StanceId id;
        public string displayName;
        [TextArea] public string description;

        [Header("Behavior bias")]
        public BehaviorType behaviorBias = BehaviorType.Balanced;

        [Header("Speed strategy")]
        [Tooltip("Speed threshold above which the brain will commit a Big Combo. Lower = more aggressive spending.")]
        [Range(20f, 100f)] public float speedThresholdBigCombo = 65f;
        [Tooltip("HP fraction below which the unit prefers to disengage and rebuild.")]
        [Range(0.1f, 0.9f)] public float hpThresholdDisengage = 0.50f;
        [Tooltip("Reserve speed floor — won't spend speed on combos below this value (Sentinel-style hoarding).")]
        [Range(0f, 100f)] public float speedReserveFloor = 0f;

        [Header("Targeting")]
        public TargetPriority targetPriority = TargetPriority.Nearest;

        [Header("Movement")]
        public MovementIntent preferredIntent = MovementIntent.Close;
        [Tooltip("Willingness to spend speed on a dodge when current speed is low.")]
        [Range(0f, 1f)] public float dodgeWillingnessAtLowSpeed = 0.5f;

        [Header("Engagement")]
        [Tooltip("Sentinel-style stances may hold backline longer; Onslaught engages immediately.")]
        [Range(0f, 5f)] public float engagementDelaySeconds = 0f;

        [Header("H2H — exchange tuning")]
        [Tooltip("Added to the unit's base separation chance after each exchange (0.15 default for Aggressive, 0.6 for Defensive). See HAND_TO_HAND_COMBAT.md §3.3.")]
        [Range(-0.5f, 0.6f)] public float separationChanceModifier = 0f;
        [Tooltip("Added to initiative when this stance enters an exchange. Aggressive +20, Defensive -20.")]
        [Range(-30f, 30f)] public int initiativeBonus = 0;
        [Tooltip("Multiplier on chase distance willingness during Approach. >1 = pursues fleeing harder.")]
        [Range(0.25f, 2f)] public float pursuitAggression = 1f;
    }
}
