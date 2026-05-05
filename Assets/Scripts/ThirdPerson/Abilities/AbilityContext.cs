using UnityEngine;
using TacticalRPG.DataModels;

namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Shared references injected into every ability so they don't need
    /// to use GetComponent or static singletons themselves.
    /// </summary>
    public class AbilityContext
    {
        public TerrainBattleUnit    Unit;
        public UnitMovementController Mover;
        public UnitAnimationDriver  Anim;
        public UnitAnimancerDriver  Animancer;    // optional — present when the unit prefab has Animancer wired
        public TerrainBattleUnit    Target;       // may be null for self-cast abilities
        public ResolvedTechnique    Technique;    // null for basic attacks
        public SkillSlot            Skill;        // null for basic attacks
    }
}
