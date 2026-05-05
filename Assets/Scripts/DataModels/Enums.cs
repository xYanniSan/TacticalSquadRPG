namespace TacticalRPG.DataModels
{
    public enum UnitTeam
    {
        Player,
        Enemy
    }

    public enum BattlePhase
    {
        NotStarted,
        Placement,
        Combat,
        Victory,
        Defeat
    }

    public enum BattleOutcome
    {
        None,
        Victory,
        Defeat
    }

    public enum BehaviorType
    {
        Aggressive,
        Defensive,
        Balanced
    }

    public enum IntentType
    {
        Wait,
        Move,
        BasicAttack,
        UseSkill,
        Retreat
    }

    public enum ActionType
    {
        Physical,
        Elemental,
        Support,
        Movement,
        OrbSummon
    }

    public enum ElementType
    {
        None,
        Fire,
        Water,
        Earth,
        Lightning,
        Wind
    }

    public enum TechniqueType
    {
        Attack,
        Heal,
        Buff,
        Debuff,
        Utility,
        Summon,
        OrbSummon,
        OrbRay
    }

    public enum TargetPattern
    {
        Single,
        AOE,
        Line,
        Self,
        AllAllies,
        AllEnemies
    }

    public enum EffectType
    {
        Buff,
        Debuff,
        Neutral
    }

    public enum ModifierSource
    {
        BaseStats,
        StatusEffect,
        Item,
        Passive,
        SkillTree,
        Behavior
    }

    public enum ModifierType
    {
        Additive,
        Multiplicative,
        Override
    }

    public enum GamePhase
    {
        PreBattle,
        Combat,
        Result
    }

    public enum CastType
    {
        Melee,
        Mobile,
        Rooted
    }

    public enum ActionMovementMode
    {
        InPlace,
        ShortLunge,
        ScriptedTravel,
        Dash,
        Leap
    }

    public enum CombatRole
    {
        Free,       // no exchange in progress — unit attacks freely
        Attacker,   // this unit swings first in the current exchange
        Defender    // this unit waits and braces while the attacker swings
    }

    public enum UnitCombatState
    {
        Backline,
        Engage,
        Decide,
        Melee,
        CastMobile,
        CastRooted,
        AttackDash,   // closing the last gap before a hit lands
        Execute,
        Recover,
        Stagger,      // briefly stunned after significant knockback
        Dodging,
        Dead
    }
}
