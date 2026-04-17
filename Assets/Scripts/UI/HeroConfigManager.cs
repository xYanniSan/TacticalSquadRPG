using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TacticalRPG.DataModels;

namespace TacticalRPG.UI
{
    /// <summary>
    /// Main controller for the Hero Configuration scene.
    /// Wires up hero selector, skill slots, action pool, stats, and start button.
    /// </summary>
    public class HeroConfigManager : MonoBehaviour
    {
        [Header("Hero Definitions (assign in Inspector)")]
        [SerializeField] private List<UnitDefinition> availableHeroes;

        [Header("Action Definitions (assign in Inspector)")]
        [SerializeField] private List<ActionDefinition> availableActions;

        [Header("UI References")]
        [SerializeField] private Transform heroSelectorContainer;
        [SerializeField] private GameObject heroButtonPrefab;
        [SerializeField] private ActionPoolUI actionPoolUI;
        [SerializeField] private Transform skillSlotsContainer;
        [SerializeField] private GameObject skillSlotPrefab;
        [SerializeField] private Button startBattleButton;

        [Header("Stats Panel")]
        [SerializeField] private TMP_Text heroNameText;
        [SerializeField] private TMP_Text statsText;

        [Header("Hero Preview")]
        [SerializeField] private Transform heroPreviewPoint;

        [Header("Scene")]
        [SerializeField] private string battleSceneName = "BattleScene";

        private const int SkillSlotCount = 5;

        private int _currentHeroIndex;
        private List<SkillSlotUI> _skillSlotUIs = new List<SkillSlotUI>();
        private List<Button> _heroButtons = new List<Button>();

        // Currently selected action slot awaiting an action from the pool
        private SkillSlotUI _selectedSkillSlot;
        private ActionSlotUI _selectedActionSlot;

        private GameObject _previewInstance;

        // Per-hero loadout state kept in memory during config
        private Dictionary<string, List<SkillSlot>> _heroLoadouts = new Dictionary<string, List<SkillSlot>>();

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            BuildHeroSelector();
            BuildSkillSlots();
            BuildActionPool();

            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnStartBattle);
                startBattleButton.interactable = false;
            }

            if (availableHeroes.Count > 0)
                SelectHero(0);
        }

        // ── Hero Selector ────────────────────────────────────────────

        private void BuildHeroSelector()
        {
            if (heroSelectorContainer == null || heroButtonPrefab == null) return;

            foreach (Transform child in heroSelectorContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < availableHeroes.Count; i++)
            {
                var hero = availableHeroes[i];
                var go = Instantiate(heroButtonPrefab, heroSelectorContainer);

                var label = go.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = hero.displayName ?? hero.unitId;

                // Portrait if available
                var img = go.transform.Find("Portrait")?.GetComponent<Image>();
                if (img != null && hero.portrait != null)
                    img.sprite = hero.portrait;

                var btn = go.GetComponent<Button>();
                int index = i;
                btn.onClick.AddListener(() => SelectHero(index));
                _heroButtons.Add(btn);
            }
        }

        private void SelectHero(int index)
        {
            // Save current hero's loadout before switching
            SaveCurrentHeroLoadout();

            _currentHeroIndex = index;
            var hero = availableHeroes[index];

            // Update hero name and stats
            if (heroNameText != null)
                heroNameText.text = hero.displayName ?? hero.unitId;

            if (statsText != null)
            {
                var s = hero.baseStats;
                statsText.text = $"HP:  {s.maxHP}\nATK: {s.attack}\nDEF: {s.defense}\nSPD: {s.moveSpeed:F1}";
            }

            // Highlight active hero button
            for (int i = 0; i < _heroButtons.Count; i++)
            {
                var colors = _heroButtons[i].colors;
                colors.normalColor = i == index ? new Color(1f, 0.85f, 0.2f) : Color.white;
                _heroButtons[i].colors = colors;
            }

            // Load this hero's saved loadout into skill slot UI
            LoadHeroLoadout(hero);

            // Update 3D preview
            UpdateHeroPreview(hero);
        }

        // ── Skill Slots ──────────────────────────────────────────────

        private void BuildSkillSlots()
        {
            if (skillSlotsContainer == null || skillSlotPrefab == null) return;

            foreach (Transform child in skillSlotsContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < SkillSlotCount; i++)
            {
                var go = Instantiate(skillSlotPrefab, skillSlotsContainer);
                var slotUI = go.GetComponent<SkillSlotUI>();
                slotUI.Initialize(this, i);
                _skillSlotUIs.Add(slotUI);
            }
        }

        // ── Action Pool ──────────────────────────────────────────────

        private void BuildActionPool()
        {
            if (actionPoolUI == null) return;
            actionPoolUI.Initialize(this, availableActions.ToArray());
        }

        // ── Slot Selection (called by SkillSlotUI) ──────────────────

        public void OnSlotSelected(SkillSlotUI skillSlot, ActionSlotUI actionSlot)
        {
            // Deselect all other rows
            foreach (var row in _skillSlotUIs)
            {
                if (row != skillSlot)
                    row.DeselectAll();
            }

            _selectedSkillSlot = skillSlot;
            _selectedActionSlot = actionSlot;
        }

        /// <summary>
        /// Called when the player clicks an action in the pool.
        /// Assigns it to the currently selected slot.
        /// </summary>
        public void OnActionPoolClicked(ActionDefinition action)
        {
            if (_selectedSkillSlot == null) return;

            // If a specific empty slot is selected, fill it
            if (_selectedActionSlot != null && _selectedActionSlot.Action == null)
            {
                _selectedActionSlot.SetAction(action);
                _selectedActionSlot.SetSelected(false);
                _selectedActionSlot = null;

                // Auto-advance: find next empty in this row
                // (handled via SkillSlotUI.TryAssignAction for next click)
                OnSkillChanged();
                return;
            }

            // Otherwise fill the first empty in the selected row
            _selectedSkillSlot.TryAssignAction(action);
        }

        /// <summary>Called whenever any skill slot changes.</summary>
        public void OnSkillChanged()
        {
            // Update start button state
            if (startBattleButton != null)
                startBattleButton.interactable = HasAnySkillConfigured();
        }

        // ── Loadout Save / Load ──────────────────────────────────────

        private void SaveCurrentHeroLoadout()
        {
            if (availableHeroes.Count == 0) return;

            var hero = availableHeroes[_currentHeroIndex];
            var slots = new List<SkillSlot>();

            foreach (var slotUI in _skillSlotUIs)
            {
                var skill = slotUI.ToSkillSlot();
                if (skill != null)
                    slots.Add(skill);
            }

            _heroLoadouts[hero.unitId] = slots;
        }

        private void LoadHeroLoadout(UnitDefinition hero)
        {
            // Clear all UI first
            foreach (var slotUI in _skillSlotUIs)
                slotUI.ClearAll();

            if (_heroLoadouts.TryGetValue(hero.unitId, out var savedSlots))
            {
                for (int i = 0; i < savedSlots.Count && i < _skillSlotUIs.Count; i++)
                    _skillSlotUIs[i].LoadFromSkillSlot(savedSlots[i]);
            }
        }

        // ── Hero Preview ─────────────────────────────────────────────

        private void UpdateHeroPreview(UnitDefinition hero)
        {
            if (heroPreviewPoint == null) return;

            if (_previewInstance != null)
                Destroy(_previewInstance);

            if (hero.visualPrefab != null)
            {
                _previewInstance = Instantiate(hero.visualPrefab, heroPreviewPoint.position, Quaternion.identity, heroPreviewPoint);
            }
            else
            {
                // Default: colored capsule
                _previewInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _previewInstance.transform.SetParent(heroPreviewPoint);
                _previewInstance.transform.localPosition = Vector3.zero;
                _previewInstance.transform.localScale = Vector3.one;

                var renderer = _previewInstance.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = new Color(0.2f, 0.4f, 0.9f);

                // Remove collider (preview only)
                var col = _previewInstance.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
        }

        // ── Start Battle ─────────────────────────────────────────────

        private void OnStartBattle()
        {
            SaveCurrentHeroLoadout();

            // Push everything to the static bridge
            HeroLoadoutData.Clear();
            foreach (var hero in availableHeroes)
            {
                if (_heroLoadouts.TryGetValue(hero.unitId, out var slots) && slots.Count > 0)
                    HeroLoadoutData.SaveLoadout(hero, slots);
            }

            Debug.Log($"[HeroConfig] Saved {HeroLoadoutData.SelectedHeroes.Count} heroes → loading battle");
            SceneManager.LoadScene(battleSceneName);
        }

        private bool HasAnySkillConfigured()
        {
            foreach (var slotUI in _skillSlotUIs)
            {
                if (slotUI.ToSkillSlot() != null)
                    return true;
            }
            return false;
        }
    }
}
