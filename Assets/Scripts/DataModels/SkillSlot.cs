using System.Collections.Generic;

namespace TacticalRPG.DataModels
{
    public class SkillSlot
    {
        public int slotIndex;
        public List<ActionSlot> actionSequence = new List<ActionSlot>();

        public SkillSlot(int slotIndex = 0)
        {
            this.slotIndex = slotIndex;
        }

        // Appends an action to the next available sub-slot position.
        public void AddAction(ActionDefinition action)
        {
            actionSequence.Add(new ActionSlot(actionSequence.Count, action));
        }
    }
}
