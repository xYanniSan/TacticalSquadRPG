using UnityEngine;

namespace TacticalRPG.DataModels
{
    [CreateAssetMenu(fileName = "NewUnit", menuName = "TacticalRPG/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string unitId;
        public string displayName;
        public Sprite portrait;
        public GameObject visualPrefab;

        [Header("Base Stats")]
        public StatBlock baseStats;

        [Header("Behavior")]
        public BehaviorType defaultBehavior;

        // Not serialized — set in code or via editor tools in future
        [System.NonSerialized]
        private ProficiencySet _proficiencies;
        public ProficiencySet proficiencies
        {
            get
            {
                if (_proficiencies == null)
                    _proficiencies = new ProficiencySet();
                return _proficiencies;
            }
        }

        // Future: public List<SkillSlot> defaultSkills;
    }
}
