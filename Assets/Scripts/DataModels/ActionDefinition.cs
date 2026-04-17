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
    }
}
