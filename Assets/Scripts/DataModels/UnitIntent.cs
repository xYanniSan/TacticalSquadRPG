namespace TacticalRPG.DataModels
{
    public class UnitIntent
    {
        public UnitRuntime actor;
        public IntentType type;
        public UnitRuntime target;           // Target unit (if applicable)
        public GridPosition targetPosition;  // Target position (if applicable)
        public SkillSlot skillToUse;         // Which skill to execute (if applicable)
    }
}
