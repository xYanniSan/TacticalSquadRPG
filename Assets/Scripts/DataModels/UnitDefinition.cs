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

        [Header("Stance")]
        [Tooltip("Default combat stance for this unit. May be overridden per-mission via Hero Config.")]
        public StanceDefinition defaultStance;

        [Header("H2H — ranges (meters)")]
        [Tooltip("Range at which hostiles are detected and Spotting phase begins.")]
        public float spottingRangeMeters = 8f;
        [Tooltip("Range at which Engagement phase begins (combat circling speed kicks in).")]
        public float engagementRangeMeters = 2f;
        [Tooltip("Range at which Exchange phase can begin (within striking distance).")]
        public float strikeRangeMeters = 1.5f;
        [Tooltip("Distance the unit must reach during Separation before re-engaging.")]
        public float separationDistanceMeters = 3f;

        [Header("H2H — reaction timing (seconds)")]
        [Range(0f, 2f)] public float spottingMinTime = 0.3f;
        [Range(0f, 2f)] public float spottingMaxTime = 0.7f;
        [Range(0f, 2f)] public float decisionLagMin = 0.2f;
        [Range(0f, 2f)] public float decisionLagMax = 0.5f;

        [Header("H2H — movement speeds (m/s)")]
        [Tooltip("Speed during Engagement (combat-context circling — much slower than traversal).")]
        public float combatMovementSpeed = 1.5f;
        [Tooltip("Speed during Approach (running into engagement range).")]
        public float traversalSpeed = 6f;
        [Tooltip("Speed during Separation (or 0 to use animation root motion).")]
        public float disengageSpeed = 3.5f;

        [Header("H2H — separation phase")]
        [Tooltip("Min seconds spent in Separation before re-engaging.")]
        [Range(0.2f, 3f)] public float separationMinDuration = 1f;
        [Tooltip("Max seconds spent in Separation before re-engaging.")]
        [Range(0.2f, 3f)] public float separationMaxDuration = 1.5f;

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
