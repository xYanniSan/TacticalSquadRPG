using System;

namespace TacticalRPG.DataModels
{
    [Serializable]
    public class BehaviorLoadout
    {
        public BehaviorType behaviorType;

        public BehaviorLoadout(BehaviorType type = BehaviorType.Balanced)
        {
            behaviorType = type;
        }
    }
}
