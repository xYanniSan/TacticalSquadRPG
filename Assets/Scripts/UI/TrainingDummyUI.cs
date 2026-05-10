using TacticalRPG.ThirdPerson;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalRPG.UI
{
    /// <summary>
    /// HUD for the TrainingDummy test scene. Shows the TestSubject's last
    /// button-driven action, the active Animancer clip, and (when battle
    /// components are enabled) the legacy combat state. AI is intentionally
    /// off in this scene — `State` will read "TEST MODE" until production AI
    /// is layered in.
    /// </summary>
    public class TrainingDummyUI : MonoBehaviour
    {
        [SerializeField] private TrainingDummyController _subject;
        [SerializeField] private Text _actionLabel;
        [SerializeField] private Text _animLabel;
        [SerializeField] private Text _stateLabel;

        private void Update()
        {
            if (_subject == null) return;

            if (_actionLabel != null)
                _actionLabel.text = $"Action: {_subject.CurrentAction}";

            if (_animLabel != null)
                _animLabel.text = $"Anim: {_subject.CurrentAnimation}";

            if (_stateLabel != null)
            {
                var battleUnit = _subject.GetComponent<TerrainBattleUnit>();
                _stateLabel.text = (battleUnit != null && battleUnit.enabled)
                    ? $"State: {battleUnit.CombatState}"
                    : "State: TEST MODE (no AI)";
            }
        }
    }
}
