using System.Collections.Generic;
using TacticalRPG.DataModels;

namespace TacticalRPG.Systems
{
    public static class UnitFactory
    {
        private static int _nextRuntimeId = 1;

        public static void ResetIds()
        {
            _nextRuntimeId = 1;
        }

        public static int CreateSummonId()
        {
            return _nextRuntimeId++;
        }

        public static UnitRuntime CreateFromDefinition(UnitDefinition definition, UnitTeam team)
        {
            var unit = new UnitRuntime();

            unit.definition    = definition;
            unit.runtimeId     = _nextRuntimeId++;
            unit.team          = team;
            unit.currentStats  = definition.baseStats;
            unit.maxHP         = definition.baseStats.maxHP;
            unit.currentHP     = unit.maxHP;
            unit.behavior      = new BehaviorLoadout(definition.defaultBehavior);
            unit.equippedSkills = new List<SkillSlot>();
            unit.activeEffects  = new List<StatusEffect>();
            unit.isDead        = false;

            return unit;
        }
    }
}
