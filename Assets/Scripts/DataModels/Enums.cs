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
        Movement
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
        Summon
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
}
