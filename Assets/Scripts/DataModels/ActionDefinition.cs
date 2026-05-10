using UnityEngine;

namespace TacticalRPG.DataModels
{
    [CreateAssetMenu(fileName = "NewAction", menuName = "TacticalRPG/Action Definition")]
    public class ActionDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string actionId;
        public string displayName;
        public ActionType actionType;
        public Sprite icon;

        [Header("Combat")]
        public float basePower;        // Contribution to technique power
        public ElementType element;    // None = non-elemental

        [Header("Casting")]
        public float energyCost;       // 0 = free (Physical), 10+ = costs energy

        [Header("Speed (kinetic resource — see COMBAT_DESIGN \"Speed\")")]
        [Tooltip("Speed required to use this action standalone. 0 = free.")]
        public float speedCost;
        [Tooltip("Speed granted on use (kinetic skills like Wind Burst). 0 = none.")]
        public float speedGain;
        [Tooltip("Damage multiplier per 100 speed for damaging actions. 0 = no scaling.")]
        public float speedScaling;
        [Tooltip("Minimum current speed required to use this action. 0 = no gate.")]
        public float speedGate;

        [Header("Standalone Buff (Elemental actions — when not part of a combo)")]
        public int selfBuffDamage = 20;    // Bonus flat damage added per hit
        public int selfBuffCharges = 10;   // How many hits the buff lasts

        [Header("Power Boost (Support actions — when not part of a combo)")]
        [Range(0f, 1f)]
        public float powerBoostPercent = 0.20f; // e.g. 0.2 = +20% to next skill's damage

        [Header("Orb Summon (OrbSummon actions)")]
        public int orbCount = 3;          // how many orbs to spawn
        public int orbDamage = 15;        // flat damage each orb deals on hit
    }
}
