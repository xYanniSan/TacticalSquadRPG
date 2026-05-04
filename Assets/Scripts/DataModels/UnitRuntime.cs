using System.Collections.Generic;
using UnityEngine;

namespace TacticalRPG.DataModels
{
    public class UnitRuntime
    {
        // Reference to static definition
        public UnitDefinition definition;

        // Battle Identity
        public int runtimeId;
        public UnitTeam team;

        // Current State
        public int currentHP;
        public int maxHP;
        public GridPosition position;
        public bool isDead;

        // Energy
        public float currentEnergy;
        public float maxEnergy = 100f;

        // Current Stats (modified by buffs/items)
        public StatBlock currentStats;

        // Loadout (configured pre-battle)
        public BehaviorLoadout behavior;
        public List<SkillSlot> equippedSkills;

        // Active Effects
        public List<StatusEffect> activeEffects;

        // Active Buffs (terrain combat)
        public List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

        // Pending power boost — set by standalone Focus, consumed on next attack
        public float pendingPowerBoost;

        // Combat State
        public UnitIntent currentIntent;
        public UnitRuntime currentTarget;

        // Visual Reference (non-gameplay)
        public GameObject visualInstance;

        // Override display name (used by summons)
        public string overrideDisplayName;

        public string DisplayName => overrideDisplayName ?? (definition != null ? definition.displayName : $"Unit_{runtimeId}");

        public void TakeDamage(int amount)
        {
            currentHP = Mathf.Max(0, currentHP - amount);
            if (currentHP <= 0)
                isDead = true;
        }

        public void Heal(int amount)
        {
            currentHP = Mathf.Min(maxHP, currentHP + amount);
        }

        public bool SpendEnergy(float amount)
        {
            if (currentEnergy < amount) return false;
            currentEnergy -= amount;
            return true;
        }

        public void RegenEnergy(float amount)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        }
    }
}
