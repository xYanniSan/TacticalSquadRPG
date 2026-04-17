using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalRPG.DataModels;

namespace TacticalRPG.UI
{
    /// <summary>
    /// Grid of available actions the player can drag/click into skill slots.
    /// Each action is a button showing icon + name + type tag.
    /// </summary>
    public class ActionPoolUI : MonoBehaviour
    {
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject actionButtonPrefab;

        private HeroConfigManager _manager;

        public void Initialize(HeroConfigManager manager, ActionDefinition[] actions)
        {
            _manager = manager;

            // Clear any existing children
            foreach (Transform child in gridContainer)
                Destroy(child.gameObject);

            foreach (var action in actions)
            {
                if (action == null) continue;

                var go = Instantiate(actionButtonPrefab, gridContainer);
                SetupButton(go, action);
            }
        }

        private void SetupButton(GameObject go, ActionDefinition action)
        {
            // Background color by type
            var bg = go.GetComponent<Image>();
            if (bg != null)
                bg.color = ActionSlotUI.GetTypeColor(action.actionType);

            // Icon
            var icon = go.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.enabled = action.icon != null;
                icon.sprite = action.icon;
            }

            // Name label
            var nameLabel = go.transform.Find("NameLabel")?.GetComponent<TMP_Text>();
            if (nameLabel != null)
                nameLabel.text = action.displayName;

            // Type tag
            var typeLabel = go.transform.Find("TypeLabel")?.GetComponent<TMP_Text>();
            if (typeLabel != null)
            {
                string elementStr = action.element != ElementType.None ? $":{action.element}" : "";
                typeLabel.text = $"{action.actionType}{elementStr}";
            }

            // Click → assign to selected slot
            var button = go.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => _manager.OnActionPoolClicked(action));
        }
    }
}
