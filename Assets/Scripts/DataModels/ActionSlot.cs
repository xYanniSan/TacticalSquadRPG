using System;

namespace TacticalRPG.DataModels
{
    [Serializable]
    public struct ActionSlot
    {
        public int subSlotIndex;        // 0-4 position within the skill
        public ActionDefinition action; // The action in this slot (can be null = empty)

        public ActionSlot(int subSlotIndex, ActionDefinition action)
        {
            this.subSlotIndex = subSlotIndex;
            this.action       = action;
        }
    }
}
