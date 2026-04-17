using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalRPG.DataModels;

namespace TacticalRPG.UI
{
    /// <summary>
    /// One circular action slot inside a SkillSlotUI row.
    /// Click to select, then pick an action from the pool to assign.
    /// Right-click to clear.
    /// </summary>
    public class ActionSlotUI : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Button button;
        [SerializeField] private Image selectionBorder;

        private ActionDefinition _action;
        private SkillSlotUI _parentSlot;
        private int _index;
        private bool _selected;

        private static readonly Color EmptyColor = new Color(0.25f, 0.25f, 0.25f, 0.6f);
        private static readonly Color SelectedColor = new Color(1f, 0.85f, 0.2f, 1f);

        public ActionDefinition Action => _action;
        public int Index => _index;

        public void Initialize(SkillSlotUI parent, int index)
        {
            _parentSlot = parent;
            _index = index;
            _action = null;

            button.onClick.AddListener(OnClick);
            if (selectionBorder != null)
                selectionBorder.enabled = false;

            Refresh();
        }

        public void SetAction(ActionDefinition action)
        {
            _action = action;
            Refresh();
        }

        public void ClearAction()
        {
            _action = null;
            Refresh();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (selectionBorder != null)
                selectionBorder.enabled = selected;
        }

        private void OnClick()
        {
            _parentSlot.OnActionSlotClicked(this);
        }

        /// <summary>
        /// Called by SkillSlotUI when a right-click is detected via
        /// the EventTrigger on this slot.
        /// </summary>
        public void OnRightClick()
        {
            if (_action != null)
                _parentSlot.OnActionSlotRightClicked(this);
        }

        private void Refresh()
        {
            if (_action != null)
            {
                backgroundImage.color = GetTypeColor(_action.actionType);

                if (iconImage != null)
                {
                    iconImage.enabled = _action.icon != null;
                    iconImage.sprite = _action.icon;
                }

                if (labelText != null)
                    labelText.text = _action.displayName;
            }
            else
            {
                backgroundImage.color = EmptyColor;
                if (iconImage != null) iconImage.enabled = false;
                if (labelText != null) labelText.text = "";
            }
        }

        public static Color GetTypeColor(ActionType type)
        {
            switch (type)
            {
                case ActionType.Physical:  return new Color(0.5f, 0.5f, 0.55f);
                case ActionType.Elemental: return new Color(0.85f, 0.45f, 0.15f);
                case ActionType.Support:   return new Color(0.25f, 0.7f, 0.35f);
                case ActionType.Movement:  return new Color(0.3f, 0.55f, 0.85f);
                default:                   return Color.grey;
            }
        }
    }
}
