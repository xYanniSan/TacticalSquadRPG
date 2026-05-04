using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TacticalRPG.DataModels;
using TacticalRPG.Systems;

namespace TacticalRPG.UI
{
    /// <summary>
    /// One skill slot row: 5 ActionSlotUI circles + a technique preview label.
    /// </summary>
    public class SkillSlotUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text slotLabel;
        [SerializeField] private TMP_Text previewLabel;
        [SerializeField] private Transform actionSlotsContainer;
        [SerializeField] private GameObject actionSlotPrefab;

        private int _slotIndex;
        private List<ActionSlotUI> _actionSlots = new List<ActionSlotUI>();
        private HeroConfigManager _manager;
        private SkillSystem _skillSystem = new SkillSystem();

        private const int MaxActions = 5;

        public void Initialize(HeroConfigManager manager, int slotIndex)
        {
            _manager = manager;
            _slotIndex = slotIndex;

            if (slotLabel != null)
                slotLabel.text = $"Slot {slotIndex + 1}";

            // Spawn 5 action slot circles
            for (int i = 0; i < MaxActions; i++)
            {
                var go = Instantiate(actionSlotPrefab, actionSlotsContainer);
                var slotUI = go.GetComponent<ActionSlotUI>();
                slotUI.Initialize(this, i);
                _actionSlots.Add(slotUI);
            }

            UpdatePreview();
        }

        /// <summary>
        /// Loads actions from an existing SkillSlot into the UI.
        /// </summary>
        public void LoadFromSkillSlot(SkillSlot skillSlot)
        {
            ClearAll();
            if (skillSlot == null) return;

            for (int i = 0; i < skillSlot.actionSequence.Count && i < MaxActions; i++)
            {
                if (skillSlot.actionSequence[i].action != null)
                    _actionSlots[i].SetAction(skillSlot.actionSequence[i].action);
            }
            UpdatePreview();
        }

        /// <summary>
        /// Builds a SkillSlot from the current UI state.
        /// Returns null if no actions are assigned.
        /// </summary>
        public SkillSlot ToSkillSlot()
        {
            var slot = new SkillSlot(_slotIndex);
            foreach (var actionSlotUI in _actionSlots)
            {
                if (actionSlotUI.Action != null)
                    slot.AddAction(actionSlotUI.Action);
            }
            return slot.actionSequence.Count > 0 ? slot : null;
        }

        /// <summary>
        /// Assigns an action to the first empty sub-slot.
        /// Called by HeroConfigManager after the player picks from the pool.
        /// </summary>
        public bool TryAssignAction(ActionDefinition action)
        {
            for (int i = 0; i < MaxActions; i++)
            {
                if (_actionSlots[i].Action == null)
                {
                    _actionSlots[i].SetAction(action);
                    DeselectAll();
                    UpdatePreview();
                    _manager.OnSkillChanged();
                    return true;
                }
            }
            return false; // full
        }

        public void OnActionSlotClicked(ActionSlotUI clicked)
        {
            // If it's empty, select it so the pool can fill it
            if (clicked.Action == null)
            {
                DeselectAll();
                clicked.SetSelected(true);
                _manager.OnSlotSelected(this, clicked);
            }
            else
            {
                // Clicking a filled slot selects this row for pool assignment
                DeselectAll();
                clicked.SetSelected(true);
                _manager.OnSlotSelected(this, clicked);
            }
        }

        public void OnActionSlotRightClicked(ActionSlotUI clicked)
        {
            if (clicked.Action == null) return;

            int removedIndex = clicked.Index;
            clicked.ClearAction();

            // Shift remaining actions left to fill the gap
            for (int i = removedIndex; i < MaxActions - 1; i++)
            {
                _actionSlots[i].SetAction(_actionSlots[i + 1].Action);
            }
            _actionSlots[MaxActions - 1].ClearAction();

            DeselectAll();
            UpdatePreview();
            _manager.OnSkillChanged();
        }

        public void DeselectAll()
        {
            foreach (var slot in _actionSlots)
                slot.SetSelected(false);
        }

        public void ClearAll()
        {
            foreach (var slot in _actionSlots)
                slot.ClearAction();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (previewLabel == null) return;

            var skillSlot = ToSkillSlot();
            if (skillSlot == null)
            {
                previewLabel.text = "(empty)";
                previewLabel.color = Color.grey;
                return;
            }

            // Use a dummy caster for preview (just need technique name/type)
            var dummyCaster = new UnitRuntime { currentStats = StatBlock.Default };
            var resolved = _skillSystem.ResolveSkill(skillSlot, dummyCaster);

            previewLabel.text = $"\u2192 \"{resolved.techniqueName}\" ({resolved.type}, Pwr: {resolved.power})";
            previewLabel.color = Color.white;
        }
    }
}
