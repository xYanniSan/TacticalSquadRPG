using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat;
using TacticalRPG.ThirdPerson;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalRPG.UI
{
    /// <summary>
    /// Inspector-driven training UI for the H2H combat layer. The eight
    /// scenarios from `Docs/Design/HAND_TO_HAND_COMBAT.md` §11 each map
    /// to a scenario button; live status panels show every unit's phase
    /// and movement speed; sliders override spotting / decision lag in
    /// the phase system.
    ///
    /// Wire UI elements via inspector or via the editor menu
    /// `TacticalRPG → H2H → Build Training Scene`. All button handlers
    /// are `public` so `Button.onClick` can target them.
    /// </summary>
    public class H2HTrainingUI : MonoBehaviour
    {
        [Header("Director")]
        [SerializeField] private H2HTrainingDirector _director;

        [Header("Test units")]
        [Tooltip("Unit treated as 'Subject' (the one player typically controls).")]
        [SerializeField] private H2HUnit _subject;
        [Tooltip("Unit treated as 'Dummy' (the standing target).")]
        [SerializeField] private H2HUnit _dummy;

        [Header("Status labels (optional)")]
        [SerializeField] private Text _subjectStatusLabel;
        [SerializeField] private Text _dummyStatusLabel;
        [SerializeField] private Text _scenarioLabel;

        [Header("Reaction-timing sliders (optional)")]
        [SerializeField] private Slider _spottingSlider;
        [SerializeField] private Slider _decisionLagSlider;
        [SerializeField] private Text   _spottingSliderLabel;
        [SerializeField] private Text   _decisionLagSliderLabel;

        [Header("Resource sliders (optional, target = Subject)")]
        [SerializeField] private Slider _subjectHpSlider;
        [SerializeField] private Slider _subjectSpeedSlider;

        // ── Update labels ──────────────────────────────────────────

        private void Update()
        {
            if (_director == null) return;
            if (_subjectStatusLabel != null) _subjectStatusLabel.text = FormatStatus(_subject);
            if (_dummyStatusLabel   != null) _dummyStatusLabel.text   = FormatStatus(_dummy);

            if (_spottingSlider != null && _spottingSliderLabel != null)
                _spottingSliderLabel.text = $"Spotting: {_spottingSlider.value:F2}s";
            if (_decisionLagSlider != null && _decisionLagSliderLabel != null)
                _decisionLagSliderLabel.text = $"Decision lag: {_decisionLagSlider.value:F2}s";

            TickShowcase();
        }

        private string FormatStatus(H2HUnit u)
        {
            if (u == null) return "(no unit)";
            var phase = u.Phases != null ? u.Phases.GetPhase(u) : H2HPhase.NotEngaged;
            float maxSpeed = u.Locomotion != null ? u.Locomotion.ResolvePhaseMaxSpeed() : -1f;
            float vel = u.CC != null ? new Vector3(u.CC.velocity.x, 0f, u.CC.velocity.z).magnitude : 0f;
            string clamp = maxSpeed > 0f ? $"max {maxSpeed:F1}" : "no clamp";
            string stanceName = u.Stance != null ? u.Stance.id.ToString() : "(none)";
            string ai = u.AIEnabled ? "ON" : (u.IsDead ? "DEAD ☠" : "OFF ⚠");
            string deadTag = u.IsDead ? "  [DEAD — click Revive or drag HP slider up]" : "";
            return $"{u.DisplayName}{deadTag}\n  AI: {ai}    Phase: {phase}\n  Stance: {stanceName}\n  Move: {vel:F2} m/s ({clamp})\n  HP: {u.CurrentHp:F0}/{u.MaxHp:F0}    Energy: {u.CurrentEnergy:F0}/{u.MaxEnergy:F0}\n  Speed pool: {u.CurrentSpeed:F0}/100  (soft cap {u.SoftCapSpeed:F0})";
        }

        // ── Position presets ───────────────────────────────────────

        public void PresetAdjacent()  => PlaceUnits(1f);
        public void PresetMidRange()  => PlaceUnits(3f);
        public void PresetLongRange() => PlaceUnits(6f);

        public void PlaceUnits(float distance)
        {
            if (_subject == null || _dummy == null) return;
            Vector3 origin = (_subject.transform.position + _dummy.transform.position) * 0.5f;
            origin.y = _subject.transform.position.y;
            Vector3 dir = (_dummy.transform.position - _subject.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 sPos = origin - dir * (distance * 0.5f);
            Vector3 dPos = origin + dir * (distance * 0.5f);
            sPos.y = _subject.transform.position.y;
            dPos.y = _dummy.transform.position.y;

            // CharacterController.enabled toggle so the move applies.
            TeleportTo(_subject, sPos, lookAt: dPos);
            TeleportTo(_dummy,   dPos, lookAt: sPos);
            SetScenarioLabel($"Position preset: {distance:F1}m");
        }

        private void TeleportTo(H2HUnit unit, Vector3 pos, Vector3 lookAt)
        {
            var cc = unit.CC;
            bool wasEnabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            unit.transform.position = pos;
            Vector3 dir = lookAt - pos; dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) unit.transform.rotation = Quaternion.LookRotation(dir);
            if (cc != null && wasEnabled) cc.enabled = true;
        }

        // ── Phase forcing ───────────────────────────────────────────

        public void ForceSubjectNotEngaged()  => ForcePhase(_subject, H2HPhase.NotEngaged);
        public void ForceSubjectSpotting()    => ForcePhase(_subject, H2HPhase.Spotting);
        public void ForceSubjectApproaching() => ForcePhase(_subject, H2HPhase.Approaching);
        public void ForceSubjectEngaged()     => ForcePhase(_subject, H2HPhase.Engaged);
        public void ForceSubjectSeparating()  => ForcePhase(_subject, H2HPhase.Separating);

        public void ForceDummyNotEngaged()  => ForcePhase(_dummy, H2HPhase.NotEngaged);
        public void ForceDummySpotting()    => ForcePhase(_dummy, H2HPhase.Spotting);
        public void ForceDummyApproaching() => ForcePhase(_dummy, H2HPhase.Approaching);
        public void ForceDummyEngaged()     => ForcePhase(_dummy, H2HPhase.Engaged);
        public void ForceDummySeparating()  => ForcePhase(_dummy, H2HPhase.Separating);

        public void ForceBothEngaged() { ForcePhase(_subject, H2HPhase.Engaged); ForcePhase(_dummy, H2HPhase.Engaged); }
        public void ForceBothApproaching() { ForcePhase(_subject, H2HPhase.Approaching); ForcePhase(_dummy, H2HPhase.Approaching); }

        public void ForcePhase(H2HUnit unit, H2HPhase phase)
        {
            if (unit == null || unit.Phases == null) return;
            unit.Phases.TransitionPhase(unit, phase, $"ui-force:{phase}");
            SetScenarioLabel($"Forced {unit.DisplayName} → {phase}");
        }

        // ── AI toggles ─────────────────────────────────────────────

        public void ToggleSubjectAI() { ToggleOrRevive(_subject); }
        public void ToggleDummyAI()   { ToggleOrRevive(_dummy); }
        public void EnableBothAI()    { EnableOrRevive(_subject); EnableOrRevive(_dummy); }
        public void DisableBothAI()   { if (_subject != null) _subject.AIEnabled = false; if (_dummy != null) _dummy.AIEnabled = false; }

        private void ToggleOrRevive(H2HUnit u)
        {
            if (u == null) return;
            if (u.IsDead) { u.Revive(); return; }      // dead → revive on first click
            u.AIEnabled = !u.AIEnabled;
        }
        private void EnableOrRevive(H2HUnit u)
        {
            if (u == null) return;
            if (u.IsDead) u.Revive();
            else u.AIEnabled = true;
        }

        // ── Revive ───────────────────────────────────────────────

        public void ReviveSubject() { if (_subject != null) _subject.Revive(); }
        public void ReviveDummy()   { if (_dummy   != null) _dummy.Revive(); }
        public void ReviveBoth()    { ReviveSubject(); ReviveDummy(); }

        // ── Reaction timing sliders ────────────────────────────────

        public void OnSpottingSliderChanged(float v)
        {
            if (_director?.Phases == null) return;
            _director.Phases.OverrideSpottingTime = v;
        }

        public void OnDecisionLagSliderChanged(float v)
        {
            if (_director?.Phases == null) return;
            _director.Phases.OverrideDecisionLag = v;
        }

        public void OnSubjectHpSliderChanged(float v)
        {
            if (_subject == null) return;
            // Dragging HP above 0 on a dead unit revives it — saves the
            // user a separate click.
            if (_subject.IsDead && v > 0f) _subject.Revive();
            _subject.CurrentHp = Mathf.Clamp(v, 0f, _subject.MaxHp);
        }

        public void OnSubjectSpeedSliderChanged(float v)
        {
            if (_subject == null) return;
            _subject.CurrentSpeed = Mathf.Clamp(v, 0f, 100f);
        }

        public void OnSubjectEnergySliderChanged(float v)
        {
            if (_subject == null) return;
            _subject.CurrentEnergy = Mathf.Clamp(v, 0f, _subject.MaxEnergy);
        }

        // ── 8 verification scenarios ───────────────────────────────

        public void Scenario1_BasicSpotting()
        {
            // Long-range, no combat → enable subject AI, disable dummy AI.
            PlaceUnits(6f);
            DisableBothAI();
            if (_subject != null) _subject.AIEnabled = true;
            ForcePhase(_subject, H2HPhase.NotEngaged);
            ForcePhase(_dummy,   H2HPhase.NotEngaged);
            SetScenarioLabel("Scenario 1 — Basic spotting & approach. Watch Subject enter Spotting → Approaching → Engaged.");
        }

        public void Scenario2_EngagementAtDistance()
        {
            PlaceUnits(3f);
            DisableBothAI();
            if (_subject != null) _subject.AIEnabled = true;
            ForcePhase(_subject, H2HPhase.Approaching);
            SetScenarioLabel("Scenario 2 — Engagement at distance. Subject runs in, decelerates at engagement range.");
        }

        public void Scenario3_SingleExchange()
        {
            PlaceUnits(1f);
            ForcePhase(_subject, H2HPhase.Engaged);
            ForcePhase(_dummy,   H2HPhase.Engaged);
            DisableBothAI();
            // Manually fire the orchestrator: Subject attacks Dummy.
            if (_director?.Orchestrator != null && _subject != null && _dummy != null)
                _director.Orchestrator.RegisterPair(_subject, _dummy);
            SetScenarioLabel("Scenario 3 — Single exchange. Subject attacks; orchestrator picks attacker; Dummy reacts.");
        }

        public void Scenario4_ExchangeIntoSeparation()
        {
            PlaceUnits(1f);
            ForcePhase(_subject, H2HPhase.Engaged);
            ForcePhase(_dummy,   H2HPhase.Engaged);
            // Subject = Defensive stance, force separation.
            if (_director?.Orchestrator != null) _director.Orchestrator.baseSeparationChance = 1f;
            DisableBothAI();
            if (_director?.Orchestrator != null && _subject != null && _dummy != null)
                _director.Orchestrator.RegisterPair(_dummy, _subject);
            SetScenarioLabel("Scenario 4 — Exchange → forced Separation. Watch Subject backstep ~1.5m.");
        }

        public void Scenario5_MutualSeparation()
        {
            PlaceUnits(1f);
            ForcePhase(_subject, H2HPhase.Engaged);
            ForcePhase(_dummy,   H2HPhase.Engaged);
            if (_director?.Orchestrator != null) _director.Orchestrator.baseSeparationChance = 1f;
            EnableBothAI();
            if (_director?.Orchestrator != null && _subject != null && _dummy != null)
                _director.Orchestrator.RegisterPair(_subject, _dummy);
            SetScenarioLabel("Scenario 5 — Mutual separation & re-engagement (both AI on, sep chance 100%).");
        }

        public void Scenario6_CounterRoleSwap()
        {
            PlaceUnits(1f);
            ForcePhase(_subject, H2HPhase.Engaged);
            ForcePhase(_dummy,   H2HPhase.Engaged);
            if (_subject != null) _subject.CounterChance = 1f;
            DisableBothAI();
            if (_director?.Orchestrator != null && _dummy != null && _subject != null)
                _director.Orchestrator.RegisterPair(_dummy, _subject);
            SetScenarioLabel("Scenario 6 — Counter & role swap. Subject's counter chance = 100%.");
        }

        public void Scenario7_DecisionLag()
        {
            PlaceUnits(1f);
            ForcePhase(_subject, H2HPhase.Engaged);
            ForcePhase(_dummy,   H2HPhase.Engaged);
            EnableBothAI();
            if (_director?.Phases != null) _director.Phases.OverrideDecisionLag = 1.5f;
            SetScenarioLabel("Scenario 7 — Decision lag = 1.5s. Watch units pause between commits.");
        }

        public void Scenario8_CombatSpeedDifferentiation()
        {
            PlaceUnits(6f);
            DisableBothAI();
            ForcePhase(_subject, H2HPhase.Approaching);
            ForcePhase(_dummy,   H2HPhase.Approaching);
            SetScenarioLabel("Scenario 8 — Both Approaching → traversal speed. Then click 'Force Both Engaged' to see combat shuffle.");
        }

        // ── Behavior toggles ──────────────────────────────────────

        [Header("Behavior toggle labels (optional)")]
        [SerializeField] private Text _separationToggleLabel;
        [SerializeField] private Text _dodgeToggleLabel;

        public void ToggleSeparation()
        {
            if (_director?.Orchestrator == null) return;
            _director.Orchestrator.SeparationEnabled = !_director.Orchestrator.SeparationEnabled;
            UpdateBehaviorLabels();
            string state = _director.Orchestrator.SeparationEnabled ? "ON" : "OFF";
            SetScenarioLabel($"Separation: {state}");
            if (H2HLogger.Instance != null)
                H2HLogger.Instance.Log("TOGGLE ", "ui", $"SeparationEnabled = {state}");
        }

        public void ToggleDodge()
        {
            if (_director?.Orchestrator == null) return;
            _director.Orchestrator.DodgeEnabled = !_director.Orchestrator.DodgeEnabled;
            UpdateBehaviorLabels();
            string state = _director.Orchestrator.DodgeEnabled ? "ON" : "OFF";
            SetScenarioLabel($"Dodge: {state}");
            if (H2HLogger.Instance != null)
                H2HLogger.Instance.Log("TOGGLE ", "ui", $"DodgeEnabled = {state}");
        }

        public void BindBehaviorLabels(Text separationLabel, Text dodgeLabel)
        {
            _separationToggleLabel = separationLabel;
            _dodgeToggleLabel = dodgeLabel;
            UpdateBehaviorLabels();
        }

        private void UpdateBehaviorLabels()
        {
            if (_director?.Orchestrator == null) return;
            if (_separationToggleLabel != null)
                _separationToggleLabel.text = $"Separation: {(_director.Orchestrator.SeparationEnabled ? "ON" : "OFF")}";
            if (_dodgeToggleLabel != null)
                _dodgeToggleLabel.text = $"Dodge: {(_director.Orchestrator.DodgeEnabled ? "ON" : "OFF")}";
        }

        // ── Collapse / show panels ────────────────────────────────

        [Header("Panels root (managed by collapse button)")]
        [SerializeField] private List<GameObject> _collapsiblePanels = new List<GameObject>();
        // Snapshot of each panel's state when "Hide All" was last clicked,
        // so "Show All" restores the user's configuration rather than
        // forcing every panel back on at once.
        private List<bool> _restoreState = new List<bool>();
        private bool _panelsHidden;

        public void RegisterCollapsiblePanel(GameObject panel)
        {
            if (panel != null && !_collapsiblePanels.Contains(panel))
            {
                _collapsiblePanels.Add(panel);
                // Default-hide the panel so the scene starts clean — only
                // the toolbar (with per-panel toggle buttons) is visible.
                // User clicks individual buttons to bring up specific
                // panels; ☰ Hide All hides them all again in one click.
                panel.SetActive(false);
            }
        }

        public void ToggleUIVisibility()
        {
            _panelsHidden = !_panelsHidden;
            if (_panelsHidden)
            {
                _restoreState.Clear();
                foreach (var p in _collapsiblePanels)
                {
                    _restoreState.Add(p != null && p.activeSelf);
                    if (p != null) p.SetActive(false);
                }
                SetScenarioLabel("All UI hidden — click ☰ to restore.");
            }
            else
            {
                for (int i = 0; i < _collapsiblePanels.Count; i++)
                {
                    bool was = i < _restoreState.Count ? _restoreState[i] : true;
                    if (_collapsiblePanels[i] != null) _collapsiblePanels[i].SetActive(was);
                }
                SetScenarioLabel("UI restored.");
            }
        }

        /// <summary>Toggle a single panel on/off by reference. Used by
        /// the per-panel toolbar buttons.</summary>
        public void TogglePanel(GameObject panel)
        {
            if (panel == null) return;
            panel.SetActive(!panel.activeSelf);
        }

        // The editor setup wires these closures so individual buttons can
        // target specific panels. Each public method here is one named
        // panel; it just flips that panel's active state.
        [Header("Per-panel refs (for toolbar toggles)")]
        [SerializeField] private GameObject _panelStatus;
        [SerializeField] private GameObject _panelPresets;
        [SerializeField] private GameObject _panelToggles;
        [SerializeField] private GameObject _panelSubjectPhase;
        [SerializeField] private GameObject _panelDummyPhase;
        [SerializeField] private GameObject _panelScenarios;
        [SerializeField] private GameObject _panelSliders;
        [SerializeField] private GameObject _panelTimeScale;
        [SerializeField] private GameObject _panelOldMenu;
        [SerializeField] private GameObject _panelLayering;
        [SerializeField] private GameObject _panelManualMove;
        [SerializeField] private GameObject _panelShowcase;

        public void TogglePanelStatus()       => TogglePanel(_panelStatus);
        public void TogglePanelPresets()      => TogglePanel(_panelPresets);
        public void TogglePanelToggles()      => TogglePanel(_panelToggles);
        public void TogglePanelSubjectPhase() => TogglePanel(_panelSubjectPhase);
        public void TogglePanelDummyPhase()   => TogglePanel(_panelDummyPhase);
        public void TogglePanelScenarios()    => TogglePanel(_panelScenarios);
        public void TogglePanelSliders()      => TogglePanel(_panelSliders);
        public void TogglePanelTimeScale()    => TogglePanel(_panelTimeScale);
        public void TogglePanelOldMenu()      => TogglePanel(_panelOldMenu);
        public void TogglePanelLayering()     => TogglePanel(_panelLayering);
        public void TogglePanelManualMove()   => TogglePanel(_panelManualMove);
        public void TogglePanelShowcase()     => TogglePanel(_panelShowcase);

        public void BindPanelRefs(GameObject status, GameObject presets, GameObject toggles,
            GameObject subj, GameObject dum, GameObject scen, GameObject sliders, GameObject ts, GameObject oldMenu,
            GameObject layering = null, GameObject manualMove = null, GameObject showcase = null)
        {
            _panelStatus = status; _panelPresets = presets; _panelToggles = toggles;
            _panelSubjectPhase = subj; _panelDummyPhase = dum; _panelScenarios = scen;
            _panelSliders = sliders; _panelTimeScale = ts; _panelOldMenu = oldMenu;
            _panelLayering = layering;
            _panelManualMove = manualMove;
            _panelShowcase = showcase;
        }

        // ── Layering tests (upper-body Animancer layer over base locomotion) ─
        // These wire the H2HMovementController's locomotion driver out of the
        // way (so it can't override our explicit base-layer plays) and then
        // call into BattleAnimancerDriver's static layering helpers. The
        // SLIDER varies the upper-layer weight in real time so the user can
        // see the gradient between "no upper" and "full upper."
        //
        // Tests target the Subject. The base layer is the unit's locomotion
        // clip (sprint / run / idle); the upper layer is a guard / punch /
        // hand-sign overlay masked to spine + arms + head.

        private float _layeringFadeShort = 0.10f;
        private float _layeringFadeLong  = 0.25f;

        public void LayeringSprintNoUpper()
        {
            if (!TryGetSubjectAnimancer(out var a)) return;
            SuppressSubjectLocomotion();
            var sprintClip = ResolveClip("loco_sprint_loop", "loco_run_fwd_loop");
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.PlayBaseLayer(a, sprintClip, _layeringFadeLong);
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.ReleaseUpperBody(a, _layeringFadeShort);
            SetScenarioLabel("Layering: Sprint base, NO upper layer (baseline).");
        }

        public void LayeringSprintGuard100()
        {
            if (!TryGetSubjectAnimancer(out var a)) return;
            SuppressSubjectLocomotion();
            var sprintClip = ResolveClip("loco_sprint_loop", "loco_run_fwd_loop");
            var guardClip  = ResolveClip("block_loop", "combat_idle");
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.PlayBaseLayer(a, sprintClip, _layeringFadeLong);
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.PlayUpperBody(a, guardClip, _layeringFadeShort);
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.SetUpperBodyWeight(a, 1.0f, _layeringFadeShort);
            if (_layerWeightSlider != null) _layerWeightSlider.SetValueWithoutNotify(1.0f);
            UpdateLayerWeightLabel(1.0f);
            SetScenarioLabel("Layering: Sprint base + Guard upper @ 100% (legs sprint, arms hold guard).");
        }

        public void LayeringSprintGuard70()
        {
            if (!TryGetSubjectAnimancer(out var a)) return;
            SuppressSubjectLocomotion();
            var sprintClip = ResolveClip("loco_sprint_loop", "loco_run_fwd_loop");
            var guardClip  = ResolveClip("block_loop", "combat_idle");
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.PlayBaseLayer(a, sprintClip, _layeringFadeLong);
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.PlayUpperBody(a, guardClip, _layeringFadeShort);
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.SetUpperBodyWeight(a, 0.7f, _layeringFadeShort);
            if (_layerWeightSlider != null) _layerWeightSlider.SetValueWithoutNotify(0.7f);
            UpdateLayerWeightLabel(0.7f);
            SetScenarioLabel("Layering: Sprint base + Guard upper @ 70% (partial blend — natural arm swing partially preserved).");
        }

        public void LayeringRunPunch()
        {
            if (!TryGetSubjectAnimancer(out var a)) return;
            SuppressSubjectLocomotion();
            var runClip   = ResolveClip("loco_run_fwd_loop", "loco_walk_fwd_loop");
            var punchClip = ResolveClip("punch_one_two_three", "attack_punch_jab");
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.PlayBaseLayer(a, runClip, _layeringFadeLong);
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.PlayUpperBody(a, punchClip, _layeringFadeShort);
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.SetUpperBodyWeight(a, 1.0f, _layeringFadeShort);
            if (_layerWeightSlider != null) _layerWeightSlider.SetValueWithoutNotify(1.0f);
            UpdateLayerWeightLabel(1.0f);
            SetScenarioLabel("Layering: Run base + Punch upper (punch combo plays over running legs).");
        }

        public void LayeringIdleHandSign()
        {
            // No real "Hand Sign A" clip in the Kubold pack — substitute an
            // arm-raise gesture (`punch_uppercut_R`) as a stand-in. Re-bind
            // when a dedicated hand-sign clip ships.
            if (!TryGetSubjectAnimancer(out var a)) return;
            SuppressSubjectLocomotion();
            var idleClip = ResolveClip("combat_idle", "idle");
            var signClip = ResolveClip("punch_uppercut_R", "punch_uppercut_L");
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.PlayBaseLayer(a, idleClip, _layeringFadeLong);
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.PlayUpperBody(a, signClip, _layeringFadeShort);
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.SetUpperBodyWeight(a, 1.0f, _layeringFadeShort);
            if (_layerWeightSlider != null) _layerWeightSlider.SetValueWithoutNotify(1.0f);
            UpdateLayerWeightLabel(1.0f);
            SetScenarioLabel("Layering: Idle base + Hand-Sign upper (using punch_uppercut_R as a stand-in for Hand Sign A).");
        }

        public void OnLayerWeightSliderChanged(float v)
        {
            if (!TryGetSubjectAnimancer(out var a)) return;
            TacticalRPG.ThirdPerson.BattleAnimancerDriver.SetUpperBodyWeight(a, v, 0.05f);
            UpdateLayerWeightLabel(v);
        }

        // ── Layering helpers ──────────────────────────────────────────

        private UnityEngine.UI.Slider _layerWeightSlider;
        private Text                  _layerWeightLabel;

        public void BindLayerWeightControls(UnityEngine.UI.Slider slider, Text label)
        {
            _layerWeightSlider = slider;
            _layerWeightLabel  = label;
            UpdateLayerWeightLabel(slider != null ? slider.value : 0f);
        }

        private void UpdateLayerWeightLabel(float v)
        {
            if (_layerWeightLabel != null)
                _layerWeightLabel.text = $"Upper-body weight: {v:0.00}";
        }

        private bool TryGetSubjectAnimancer(out Animancer.AnimancerComponent animancer)
        {
            animancer = null;
            if (_subject == null) return false;
            animancer = _subject.Animancer;
            if (animancer == null)
            {
                Debug.LogWarning("[H2HTrainingUI] Subject has no AnimancerComponent.");
                return false;
            }
            return true;
        }

        private void SuppressSubjectLocomotion()
        {
            // The locomotion driver runs every frame and would override our
            // explicit base-layer Play. Suppress it for a few seconds; user
            // can re-trigger by exiting/entering Play mode or by calling a
            // standard preset which restores locomotion.
            if (_subject?.Locomotion != null) _subject.Locomotion.SuppressFor(120f);
            // Same for the movement controller — we don't want the brain to
            // interfere with the test by setting move intent.
            if (_subject?.Movement != null) _subject.Movement.SuppressFor(120f);
            // And turn off AI so the brain isn't fighting our manual playback.
            if (_subject != null) _subject.AIEnabled = false;
        }

        private AnimationClip ResolveClip(string id, string fallback)
        {
            if (_subject == null || _subject.Library == null) return null;
            if (_subject.Library.TryGet(id, out var ta) && ta?.Transition is Animancer.ClipTransition ct && ct.Clip != null)
                return ct.Clip;
            if (!string.IsNullOrEmpty(fallback)
                && _subject.Library.TryGet(fallback, out var fb)
                && fb?.Transition is Animancer.ClipTransition fct
                && fct.Clip != null)
                return fct.Clip;
            Debug.LogWarning($"[H2HTrainingUI] Library missing clip '{id}' (and fallback '{fallback}').");
            return null;
        }

        // ── Manual movement controls (Subject) ───────────────────────
        // Buttons that exercise the H2HMovementController's full intent
        // matrix without requiring keyboard input. Disable AI on first use
        // so the brain stops overwriting the manual intent. Each button
        // calls SetMoveIntent with a world-space direction and the chosen
        // speed; the controller smoothly accelerates and the locomotion
        // driver picks the matching loop.
        //
        // Direction nomenclature: relative to the unit's CURRENT facing.
        // - Fwd / Bwd: along transform.forward / -forward
        // - Lt / Rt:   strafe left / right
        // - FL / FR:   forward-left / forward-right diagonal
        // - BL / BR:   back-left / back-right diagonal
        //
        // Speed mode is applied by the next direction press; default is
        // Walk. "Sprint" goes above the standing-run boundary and triggers
        // the sprint loop. "Stop" sets intent to zero (test stop clips).
        // "Turn L/R 90°" rotates facing 90° in place — tests turn-in-place
        // clips (combat_turn_l90 / r90).

        public enum ManualSpeedMode { Creep, Walk, Run, Sprint }
        private ManualSpeedMode _manualSpeedMode = ManualSpeedMode.Walk;
        private Text _manualSpeedLabel;
        private Text _manualDirectionLabel;

        public void BindManualMovementLabels(Text speedLabel, Text directionLabel)
        {
            _manualSpeedLabel     = speedLabel;
            _manualDirectionLabel = directionLabel;
            UpdateManualSpeedLabel();
        }

        public void ManualSpeedCreep()  { _manualSpeedMode = ManualSpeedMode.Creep;  UpdateManualSpeedLabel(); }
        public void ManualSpeedWalk()   { _manualSpeedMode = ManualSpeedMode.Walk;   UpdateManualSpeedLabel(); }
        public void ManualSpeedRun()    { _manualSpeedMode = ManualSpeedMode.Run;    UpdateManualSpeedLabel(); }
        public void ManualSpeedSprint() { _manualSpeedMode = ManualSpeedMode.Sprint; UpdateManualSpeedLabel(); }

        public void ManualMoveFwd() => ManualMove(0f);     // 0° = forward
        public void ManualMoveFR()  => ManualMove(45f);    // forward-right
        public void ManualMoveRt()  => ManualMove(90f);    // pure right
        public void ManualMoveBR()  => ManualMove(135f);   // back-right
        public void ManualMoveBwd() => ManualMove(180f);   // backward
        public void ManualMoveBL()  => ManualMove(-135f);  // back-left
        public void ManualMoveLt()  => ManualMove(-90f);   // pure left
        public void ManualMoveFL()  => ManualMove(-45f);   // forward-left

        public void ManualStop()
        {
            DisableSubjectAI();
            if (_subject?.Movement != null) _subject.Movement.Stop();
            UpdateManualDirectionLabel("Stop");
        }

        public void ManualTurnL90() => ManualTurn(-90f);
        public void ManualTurnR90() => ManualTurn(+90f);
        public void ManualTurnL180() => ManualTurn(-180f);
        public void ManualTurnR180() => ManualTurn(+180f);

        public void ManualResume()
        {
            // Hand control back to the AI: clear suppression on the
            // locomotion driver and movement controller, re-enable brain.
            if (_subject?.Locomotion != null) _subject.Locomotion.ClearSuppression();
            if (_subject?.Movement   != null) { _subject.Movement.ClearSuppression(); _subject.Movement.Stop(); }
            if (_subject != null) _subject.AIEnabled = true;
            UpdateManualDirectionLabel("AI resumed");
        }

        private void ManualMove(float angleDegrees)
        {
            DisableSubjectAI();
            if (_subject?.Movement == null) return;

            // Convert relative angle to a world-space direction based on
            // the unit's CURRENT facing — angle 0 = transform.forward,
            // 90° = transform.right, 180° = -transform.forward.
            var unitT = _subject.transform;
            Vector3 fwd = unitT.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            else fwd.Normalize();
            Quaternion rot = Quaternion.AngleAxis(angleDegrees, Vector3.up);
            Vector3 worldDir = rot * fwd;

            float speed = ResolveManualSpeed();
            _subject.Movement.SetMoveIntent(worldDir, speed);

            // Also update facing — when moving fwd/back the unit keeps facing
            // forward; on lateral / diagonal moves we keep facing the original
            // forward (so the strafe loops play). Set FaceTowards to current
            // forward so the lerp doesn't drift.
            _subject.Movement.FaceTowards(unitT.position + fwd * 5f);

            UpdateManualDirectionLabel(DirectionLabel(angleDegrees));
        }

        private void ManualTurn(float angleDegrees)
        {
            DisableSubjectAI();
            if (_subject?.Movement == null) return;
            // Stop motion and request a face-target rotated by the angle —
            // the controller's smooth lerp + the driver's turn-in-place
            // pickup will play combat_turn_*90 / *180 as the rotation
            // happens.
            _subject.Movement.Stop();
            var unitT = _subject.transform;
            Vector3 fwd = unitT.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            else fwd.Normalize();
            Quaternion rot = Quaternion.AngleAxis(angleDegrees, Vector3.up);
            Vector3 newFwd = rot * fwd;
            _subject.Movement.FaceTowards(unitT.position + newFwd * 5f);
            UpdateManualDirectionLabel($"Turn {angleDegrees:+0;-0}°");
        }

        private float ResolveManualSpeed()
        {
            switch (_manualSpeedMode)
            {
                case ManualSpeedMode.Creep:  return 1.0f;
                case ManualSpeedMode.Walk:   return 2.5f;
                case ManualSpeedMode.Run:    return 5.0f;
                case ManualSpeedMode.Sprint: return 7.5f;
            }
            return 2.5f;
        }

        private void DisableSubjectAI()
        {
            if (_subject == null) return;
            _subject.AIEnabled = false;
            // Clear any leftover suppression from Exchange / Death / one-shot
            // strike clips, otherwise the controller and locomotion driver
            // ignore our SetMoveIntent for up to 5 seconds and the unit just
            // sits in the post-exchange combat_idle clip.
            if (_subject.Movement   != null) _subject.Movement.ClearSuppression();
            if (_subject.Locomotion != null) _subject.Locomotion.ClearSuppression();
        }

        private void UpdateManualSpeedLabel()
        {
            if (_manualSpeedLabel == null) return;
            _manualSpeedLabel.text = $"Speed: {_manualSpeedMode} ({ResolveManualSpeed():F1} m/s)";
        }

        private void UpdateManualDirectionLabel(string text)
        {
            if (_manualDirectionLabel == null) return;
            _manualDirectionLabel.text = $"Last: {text}";
        }

        private static string DirectionLabel(float angleDeg)
        {
            int a = Mathf.RoundToInt(angleDeg);
            switch (a)
            {
                case 0:    return "Forward";
                case 45:   return "Fwd-Right (45°)";
                case 90:   return "Strafe Right";
                case 135:  return "Back-Right (135°)";
                case 180:  return "Backward";
                case -135: return "Back-Left (-135°)";
                case -90:  return "Strafe Left";
                case -45:  return "Fwd-Left (-45°)";
            }
            return $"{a}°";
        }

        // ── Movement Showcase (sequence-based) ─────────────────────
        // Each sequence below is a SCRIPTED chain of clips that demonstrates
        // a complete movement story — e.g. "idle → walk-start → walk-loop
        // → walk-stop → idle". The user clicks one button per sequence,
        // optionally "Run All" to chain every sequence together (~5 min).
        // Pause/Skip/Prev step through individual clips; speed slider
        // applies Animancer.Graph.Speed for slow-mo inspection.
        //
        // Each step is `(clipId, dwellSeconds, label)`. `dwellSeconds = -1`
        // means "use the clip's actual length" (correct for non-loop start /
        // stop / turn / dodge clips). For loops we cap at a fixed dwell so
        // the test moves on. Animancer.Play handles cross-fades between
        // steps automatically (default 0.15s, per call).
        //
        // The unit is taken fully out of the gameplay loop while showcasing
        // (AI off, movement controller and locomotion driver suppressed);
        // "■ Stop" restores them.

        public struct ShowcaseStep
        {
            public string clipId;
            public float  dwellSeconds;     // -1 → use clip.length
            public string label;
            public bool   applyRootMotion;  // true → physically rotate / translate the transform
            public ShowcaseStep(string id, float dwell, string lbl, bool rootMotion = false)
            {
                clipId = id; dwellSeconds = dwell; label = lbl; applyRootMotion = rootMotion;
            }
        }

        public struct ShowcaseSequence
        {
            public string         name;
            public string         description;
            public ShowcaseStep[] steps;
        }

        // Helper builders. Loops + start/stop transitions stay in-place
        // (root motion off so the unit doesn't drift around the scene).
        // T() marks turn / pivot / mid-run-pivot clips that physically
        // rotate the transform — the clip's baked rotation is meaningless
        // without applyRootMotion=true.
        private static ShowcaseStep S(string id, float dwell, string lbl) => new ShowcaseStep(id, dwell, lbl, false);
        private static ShowcaseStep AsClipLen(string id, string lbl)      => new ShowcaseStep(id, -1f, lbl, false);
        private static ShowcaseStep T(string id, string lbl)              => new ShowcaseStep(id, -1f, lbl, true);

        private static readonly ShowcaseSequence[] _sequences = new ShowcaseSequence[]
        {
            // 1. Standing walk forward — full cycle
            new ShowcaseSequence {
                name        = "Walk fwd cycle",
                description = "Idle → walk start → walk loop → walk stop → idle",
                steps = new[] {
                    S("idle_relaxed",         1.5f, "1. Idle (relaxed)"),
                    AsClipLen("loco_walk_fwd_start",  "2. Walk start"),
                    S("loco_walk_fwd_loop",   3.0f, "3. Walk loop"),
                    AsClipLen("loco_walk_fwd_stop_l", "4. Walk stop (L foot plant)"),
                    S("idle_relaxed",         1.5f, "5. Idle (back)"),
                    AsClipLen("loco_walk_fwd_start",  "6. Walk start (again)"),
                    S("loco_walk_fwd_loop",   3.0f, "7. Walk loop"),
                    AsClipLen("loco_walk_fwd_stop_r", "8. Walk stop (R foot plant)"),
                    S("idle_relaxed",         1.5f, "9. Idle"),
                }
            },

            // 2. Standing walk backward
            new ShowcaseSequence {
                name        = "Walk bwd cycle",
                description = "Idle → bwd start → bwd loop → bwd stop → idle",
                steps = new[] {
                    S("idle_relaxed",         1.5f, "1. Idle"),
                    AsClipLen("loco_walk_bwd_start",  "2. Walk-bwd start"),
                    S("loco_walk_bwd_loop",   3.0f, "3. Walk-bwd loop"),
                    AsClipLen("loco_walk_bwd_stop_l", "4. Stop (L)"),
                    S("idle_relaxed",         1.0f, "5. Idle"),
                    AsClipLen("loco_walk_bwd_stop_r", "6. Stop (R)"),
                    S("idle_relaxed",         1.5f, "7. Idle"),
                }
            },

            // 3. Standing run forward
            new ShowcaseSequence {
                name        = "Run fwd cycle",
                description = "Idle → run start → run loop → run stop → idle",
                steps = new[] {
                    S("idle_relaxed",         1.5f, "1. Idle"),
                    AsClipLen("loco_run_fwd_start",   "2. Run start"),
                    S("loco_run_fwd_loop",    3.5f, "3. Run loop"),
                    AsClipLen("loco_run_fwd_stop_l",  "4. Run stop (L)"),
                    S("idle_relaxed",         1.0f, "5. Idle"),
                    AsClipLen("loco_run_fwd_start",   "6. Run start"),
                    S("loco_run_fwd_loop",    2.5f, "7. Run loop"),
                    AsClipLen("loco_run_fwd_stop_r",  "8. Run stop (R)"),
                    S("idle_relaxed",         1.5f, "9. Idle"),
                }
            },

            // 4. Sprint cycle
            new ShowcaseSequence {
                name        = "Sprint cycle",
                description = "Idle → run start → sprint loop → slide → idle",
                steps = new[] {
                    S("idle_relaxed",         1.5f, "1. Idle"),
                    AsClipLen("loco_run_fwd_start",   "2. Run start (lead-in)"),
                    S("loco_run_fwd_loop",    1.5f, "3. Run loop (build-up)"),
                    S("loco_sprint_loop",     3.5f, "4. Sprint loop"),
                    AsClipLen("loco_slide",           "5. Slide (sprint stop)"),
                    S("idle_relaxed",         1.5f, "6. Idle"),
                }
            },

            // 5. Strafe octagon (left/right transitions)
            new ShowcaseSequence {
                name        = "Strafe L↔R",
                description = "Strafe left start → loop → stop → right start → loop → stop",
                steps = new[] {
                    S("idle_relaxed",         1.0f, "1. Idle"),
                    AsClipLen("loco_strafe_l_start",  "2. Strafe-L start"),
                    S("loco_strafe_l_loop",   2.5f, "3. Strafe-L loop"),
                    AsClipLen("loco_strafe_l_stop_l", "4. Strafe-L stop (L)"),
                    S("idle_relaxed",         0.8f, "5. Idle"),
                    AsClipLen("loco_strafe_r_start",  "6. Strafe-R start"),
                    S("loco_strafe_r_loop",   2.5f, "7. Strafe-R loop"),
                    AsClipLen("loco_strafe_r_stop_r", "8. Strafe-R stop (R)"),
                    S("idle_relaxed",         1.0f, "9. Idle"),
                }
            },

            // 6. Standing 8-direction walk octagon
            new ShowcaseSequence {
                name        = "Standing octagon (walk)",
                description = "All 8 cardinal+diagonal walk loops in sequence",
                steps = new[] {
                    S("idle_relaxed",            1.0f, "1. Idle"),
                    S("loco_walk_fwd_loop",      2.0f, "2. Forward (0°)"),
                    S("loco_strafe_r45_loop",    2.0f, "3. Fwd-Right (45°)"),
                    S("loco_strafe_r_loop",      2.0f, "4. Right (90°)"),
                    S("loco_strafe_r135_loop",   2.0f, "5. Back-Right (135°)"),
                    S("loco_walk_bwd_loop",      2.0f, "6. Backward (180°)"),
                    S("loco_strafe_l135_loop",   2.0f, "7. Back-Left (-135°)"),
                    S("loco_strafe_l_loop",      2.0f, "8. Left (-90°)"),
                    S("loco_strafe_l45_loop",    2.0f, "9. Fwd-Left (-45°)"),
                    S("idle_relaxed",            1.0f, "10. Idle"),
                }
            },

            // 7. Standing 6-direction RUN octagon (no run-back-diagonals lateral)
            new ShowcaseSequence {
                name        = "Standing octagon (run)",
                description = "Run loops in 6 directions (cardinals + 45° diagonals + 135° diagonals)",
                steps = new[] {
                    S("idle_relaxed",                1.0f, "1. Idle"),
                    AsClipLen("loco_run_fwd_start",          "2. Run start"),
                    S("loco_run_fwd_loop",           1.8f, "3. Run forward"),
                    S("loco_run_strafe_r45_loop",    2.0f, "4. Run fwd-right 45°"),
                    S("loco_run_strafe_r_loop",      2.0f, "5. Run right (90°)"),
                    S("loco_run_strafe_r135_loop",   2.0f, "6. Run back-right 135°"),
                    S("loco_run_bwd_loop",           2.0f, "7. Run backward"),
                    S("loco_run_strafe_l135_loop",   2.0f, "8. Run back-left 135°"),
                    S("loco_run_strafe_l_loop",      2.0f, "9. Run left (-90°)"),
                    S("loco_run_strafe_l45_loop",    2.0f, "10. Run fwd-left 45°"),
                    AsClipLen("loco_run_fwd_stop_l",         "11. Run stop"),
                    S("idle_relaxed",                1.0f, "12. Idle"),
                }
            },

            // 8. Pivot starts — walk
            new ShowcaseSequence {
                name        = "Pivot starts (walk)",
                description = "Idle → pivot+walk start at 90°/135°/180° both sides (root motion ON for pivot steps)",
                steps = new[] {
                    S("idle_relaxed",                  1.0f, "1. Idle"),
                    T("loco_walk_fwd_start_l90",             "2. Walk start, pivot 90° L"),
                    S("loco_walk_fwd_loop",            1.0f, "3. Walk loop (settle)"),
                    AsClipLen("loco_walk_fwd_stop_l",          "4. Stop"),
                    S("idle_relaxed",                  0.8f, "5. Idle"),
                    T("loco_walk_fwd_start_r90",             "6. Walk start, pivot 90° R"),
                    S("loco_walk_fwd_loop",            1.0f, "7. Walk loop"),
                    AsClipLen("loco_walk_fwd_stop_r",          "8. Stop"),
                    S("idle_relaxed",                  0.8f, "9. Idle"),
                    T("loco_walk_fwd_start_l135",            "10. Pivot 135° L"),
                    S("loco_walk_fwd_loop",            1.0f, "11. Walk loop"),
                    AsClipLen("loco_walk_fwd_stop_l",          "12. Stop"),
                    S("idle_relaxed",                  0.8f, "13. Idle"),
                    T("loco_walk_fwd_start_r135",            "14. Pivot 135° R"),
                    S("loco_walk_fwd_loop",            1.0f, "15. Walk loop"),
                    AsClipLen("loco_walk_fwd_stop_r",          "16. Stop"),
                    S("idle_relaxed",                  0.8f, "17. Idle"),
                    T("loco_walk_fwd_start_l180",            "18. Pivot 180° L"),
                    S("loco_walk_fwd_loop",            1.0f, "19. Walk loop"),
                    AsClipLen("loco_walk_fwd_stop_l",          "20. Stop"),
                    S("idle_relaxed",                  0.8f, "21. Idle"),
                    T("loco_walk_fwd_start_r180",            "22. Pivot 180° R"),
                    S("loco_walk_fwd_loop",            1.0f, "23. Walk loop"),
                    AsClipLen("loco_walk_fwd_stop_r",          "24. Stop"),
                    S("idle_relaxed",                  1.0f, "25. Idle"),
                }
            },

            // 9. Pivot starts — run
            new ShowcaseSequence {
                name        = "Pivot starts (run)",
                description = "Idle → pivot+run start at 90°/135°/180° both sides (root motion ON for pivot steps)",
                steps = new[] {
                    S("idle_relaxed",                 1.0f, "1. Idle"),
                    T("loco_run_fwd_start_l90",             "2. Run start, pivot 90° L"),
                    S("loco_run_fwd_loop",            1.0f, "3. Run loop"),
                    AsClipLen("loco_run_fwd_stop_l",          "4. Stop"),
                    S("idle_relaxed",                 0.8f, "5. Idle"),
                    T("loco_run_fwd_start_r90",             "6. Pivot 90° R"),
                    S("loco_run_fwd_loop",            1.0f, "7. Run loop"),
                    AsClipLen("loco_run_fwd_stop_r",          "8. Stop"),
                    S("idle_relaxed",                 0.8f, "9. Idle"),
                    T("loco_run_fwd_start_l135",            "10. Pivot 135° L"),
                    S("loco_run_fwd_loop",            1.0f, "11. Run loop"),
                    AsClipLen("loco_run_fwd_stop_l",          "12. Stop"),
                    S("idle_relaxed",                 0.8f, "13. Idle"),
                    T("loco_run_fwd_start_r135",            "14. Pivot 135° R"),
                    S("loco_run_fwd_loop",            1.0f, "15. Run loop"),
                    AsClipLen("loco_run_fwd_stop_r",          "16. Stop"),
                    S("idle_relaxed",                 0.8f, "17. Idle"),
                    T("loco_run_fwd_start_l180",            "18. Pivot 180° L"),
                    S("loco_run_fwd_loop",            1.0f, "19. Run loop"),
                    AsClipLen("loco_run_fwd_stop_l",          "20. Stop"),
                    S("idle_relaxed",                 0.8f, "21. Idle"),
                    T("loco_run_fwd_start_r180",            "22. Pivot 180° R"),
                    S("loco_run_fwd_loop",            1.0f, "23. Run loop"),
                    AsClipLen("loco_run_fwd_stop_r",          "24. Stop"),
                    S("idle_relaxed",                 1.0f, "25. Idle"),
                }
            },

            // 10. Mid-run 180° turns
            new ShowcaseSequence {
                name        = "Mid-run 180° turns",
                description = "Run loop → mid-run 180° turn (LU/RU foot variants, root motion ON)",
                steps = new[] {
                    S("idle_relaxed",                 1.0f, "1. Idle"),
                    AsClipLen("loco_run_fwd_start",           "2. Run start"),
                    S("loco_run_fwd_loop",            2.0f, "3. Run forward"),
                    T("loco_run_fwd_turn_l180_l",           "4. Mid-run 180° L (L foot)"),
                    S("loco_run_fwd_loop",            1.5f, "5. Run forward (now reversed)"),
                    T("loco_run_fwd_turn_l180_r",           "6. Mid-run 180° L (R foot)"),
                    S("loco_run_fwd_loop",            1.5f, "7. Run"),
                    T("loco_run_fwd_turn_r180_l",           "8. Mid-run 180° R (L foot)"),
                    S("loco_run_fwd_loop",            1.5f, "9. Run"),
                    T("loco_run_fwd_turn_r180_r",           "10. Mid-run 180° R (R foot)"),
                    S("loco_run_fwd_loop",            1.5f, "11. Run"),
                    AsClipLen("loco_run_fwd_stop_l",          "12. Stop"),
                    S("idle_relaxed",                 1.0f, "13. Idle"),
                }
            },

            // 11. In-place turns (standing) — all turn steps use root motion
            new ShowcaseSequence {
                name        = "Turn-in-place (standing)",
                description = "Idle → 90°/180° turns left and right (root motion ON so the unit physically rotates)",
                steps = new[] {
                    S("idle_relaxed",   1.0f, "1. Idle"),
                    T("loco_turn_l90",        "2. Turn 90° L"),
                    S("idle_relaxed",   0.5f, "3. Idle (now -90°)"),
                    T("loco_turn_r90",        "4. Turn 90° R (back to 0°)"),
                    S("idle_relaxed",   0.5f, "5. Idle"),
                    T("loco_turn_l180",       "6. Turn 180° L"),
                    S("idle_relaxed",   0.5f, "7. Idle (now -180°)"),
                    T("loco_turn_r180",       "8. Turn 180° R (back to 0°)"),
                    S("idle_relaxed",   1.0f, "9. Idle"),
                }
            },

            // 12. Combat-stance walk octagon
            new ShowcaseSequence {
                name        = "Combat octagon",
                description = "All 8 directions of combat-stance walk (KB pack)",
                steps = new[] {
                    S("combat_idle",            1.0f, "1. Combat idle"),
                    S("combat_walk_fwd_loop",   2.0f, "2. Combat fwd (slow, KB_WalkFwd1)"),
                    S("combat_walk_fwd_fast",   2.0f, "3. Combat fwd (fast, KB_WalkFwd2)"),
                    S("combat_walk_r45_loop",   2.0f, "4. Combat fwd-right 45°"),
                    S("combat_sidestep_r",      1.5f, "5. Combat sidestep right"),
                    S("combat_walk_r135_loop",  2.0f, "6. Combat back-right 135°"),
                    S("combat_walk_bwd_loop",   2.0f, "7. Combat backward"),
                    S("combat_walk_l135_loop",  2.0f, "8. Combat back-left 135°"),
                    S("combat_sidestep_l",      1.5f, "9. Combat sidestep left"),
                    S("combat_walk_l45_loop",   2.0f, "10. Combat fwd-left 45°"),
                    S("combat_idle",            1.0f, "11. Combat idle"),
                }
            },

            // 13. Combat skips and dodges
            new ShowcaseSequence {
                name        = "Combat bursts",
                description = "Skip fwd / bwd (both variants) + dodge L / R",
                steps = new[] {
                    S("combat_idle",          0.8f, "1. Combat idle"),
                    AsClipLen("combat_skip_fwd",      "2. Skip forward (KB_SkipFwd_1)"),
                    S("combat_idle",          0.6f, "3. Idle"),
                    AsClipLen("skip_forward_2",       "4. Skip forward variant 2"),
                    S("combat_idle",          0.6f, "5. Idle"),
                    AsClipLen("combat_skip_bwd",      "6. Skip backward (KB_SkipBwd_1)"),
                    S("combat_idle",          0.6f, "7. Idle"),
                    AsClipLen("skip_back_2",          "8. Skip backward variant 2"),
                    S("combat_idle",          0.6f, "9. Idle"),
                    AsClipLen("combat_dodge_l",       "10. Dodge left"),
                    S("combat_idle",          0.6f, "11. Idle"),
                    AsClipLen("combat_dodge_r",       "12. Dodge right"),
                    S("combat_idle",          1.0f, "13. Idle"),
                }
            },

            // 14. Combat turn-in-place (root motion ON for all turn steps)
            new ShowcaseSequence {
                name        = "Combat turn-in-place",
                description = "90°/180° combat turns both sides (root motion ON so the unit physically rotates)",
                steps = new[] {
                    S("combat_idle",          1.0f, "1. Combat idle"),
                    T("combat_turn_l90",            "2. Turn 90° L"),
                    S("combat_idle",          0.5f, "3. Combat idle"),
                    T("combat_turn_r90",            "4. Turn 90° R"),
                    S("combat_idle",          0.5f, "5. Combat idle"),
                    T("combat_turn_l180",           "6. Turn 180° L"),
                    S("combat_idle",          0.5f, "7. Combat idle"),
                    T("combat_turn_r180",           "8. Turn 180° R"),
                    S("combat_idle",          1.0f, "9. Combat idle"),
                }
            },

            // 15. Lean / arch curving variants
            new ShowcaseSequence {
                name        = "Walk/run lean & arch",
                description = "Walk and run variants for curving paths",
                steps = new[] {
                    S("idle_relaxed",        1.0f, "1. Idle"),
                    S("walk_lean_L",         2.5f, "2. Walk leaning left"),
                    S("walk_lean_R",         2.5f, "3. Walk leaning right"),
                    S("walk_arch_L",         2.5f, "4. Walk arc left"),
                    S("walk_arch_R",         2.5f, "5. Walk arc right"),
                    S("run_lean_L",          2.5f, "6. Run leaning left"),
                    S("run_lean_R",          2.5f, "7. Run leaning right"),
                    S("run_arch_L",          2.5f, "8. Run arc left"),
                    S("run_arch_R",          2.5f, "9. Run arc right"),
                    S("idle_relaxed",        1.0f, "10. Idle"),
                }
            },

            // 16. Idle variants
            new ShowcaseSequence {
                name        = "Idle variants",
                description = "All standing + combat idle variants",
                steps = new[] {
                    S("idle_relaxed",   3.0f, "1. Idle relaxed (primary)"),
                    S("idle_relaxed_2", 3.0f, "2. Idle variant 2"),
                    S("idle_relaxed_3", 3.0f, "3. Idle variant 3"),
                    S("idle_relaxed_4", 3.0f, "4. Idle variant 4"),
                    S("idle_relaxed_5", 3.0f, "5. Idle variant 5"),
                    S("idle_relaxed_6", 3.0f, "6. Idle variant 6"),
                    S("combat_idle",    3.0f, "7. Combat idle (primary, KB_Idle_1)"),
                    S("idle_combat_2",  3.0f, "8. Combat idle 2"),
                    S("idle_combat_3",  3.0f, "9. Combat idle 3"),
                    S("idle_combat_4",  3.0f, "10. Combat idle 4"),
                    S("idle_combat_5",  3.0f, "11. Combat idle 5"),
                    S("idle_combat_6",  3.0f, "12. Combat idle 6"),
                }
            },
        };

        // Legacy flat list — kept for ShowcaseTotalClips compatibility but
        // no longer driven directly. Kept compact: just the unique ids that
        // the sequence steps reference.
        private static readonly (string id, string display, string section)[] _showcaseClipIds = new (string, string, string)[]
        {
            // Idles — relaxed
            ("idle_relaxed",        "Idle (relaxed, primary)",      "Idle / Standing"),
            ("idle_relaxed_2",      "Idle variant 2",                "Idle / Standing"),
            ("idle_relaxed_3",      "Idle variant 3",                "Idle / Standing"),
            ("idle_relaxed_4",      "Idle variant 4",                "Idle / Standing"),
            ("idle_relaxed_5",      "Idle variant 5",                "Idle / Standing"),
            ("idle_relaxed_6",      "Idle variant 6",                "Idle / Standing"),

            // Idles — combat
            ("combat_idle",         "Combat idle (KB_Idle_1)",       "Idle / Combat"),
            ("idle_combat_2",       "Combat idle variant 2",         "Idle / Combat"),
            ("idle_combat_3",       "Combat idle variant 3",         "Idle / Combat"),
            ("idle_combat_4",       "Combat idle variant 4",         "Idle / Combat"),
            ("idle_combat_5",       "Combat idle variant 5",         "Idle / Combat"),
            ("idle_combat_6",       "Combat idle variant 6",         "Idle / Combat"),

            // Standing walk loops + transitions
            ("loco_walk_fwd_loop",  "Walk forward (loop)",           "Standing walk"),
            ("loco_walk_fwd_start", "Walk forward (start)",          "Standing walk"),
            ("loco_walk_fwd_stop_l","Walk forward (stop, L foot)",   "Standing walk"),
            ("loco_walk_fwd_stop_r","Walk forward (stop, R foot)",   "Standing walk"),
            ("loco_walk_bwd_loop",  "Walk backward (loop)",          "Standing walk"),
            ("loco_walk_bwd_start", "Walk backward (start)",         "Standing walk"),
            ("loco_walk_bwd_stop_l","Walk backward (stop, L foot)",  "Standing walk"),
            ("loco_walk_bwd_stop_r","Walk backward (stop, R foot)",  "Standing walk"),

            // Standing strafe
            ("loco_strafe_l_loop",  "Strafe left (loop)",            "Standing strafe"),
            ("loco_strafe_l_start", "Strafe left (start)",           "Standing strafe"),
            ("loco_strafe_l_stop_l","Strafe left (stop, L foot)",    "Standing strafe"),
            ("loco_strafe_l_stop_r","Strafe left (stop, R foot)",    "Standing strafe"),
            ("loco_strafe_r_loop",  "Strafe right (loop)",           "Standing strafe"),
            ("loco_strafe_r_start", "Strafe right (start)",          "Standing strafe"),
            ("loco_strafe_r_stop_l","Strafe right (stop, L foot)",   "Standing strafe"),
            ("loco_strafe_r_stop_r","Strafe right (stop, R foot)",   "Standing strafe"),

            // Standing diagonal walks (8-direction)
            ("loco_strafe_l45_loop", "Walk fwd-left 45°",            "Standing diagonals"),
            ("loco_strafe_r45_loop", "Walk fwd-right 45°",           "Standing diagonals"),
            ("loco_strafe_l135_loop","Walk back-left 135°",          "Standing diagonals"),
            ("loco_strafe_r135_loop","Walk back-right 135°",         "Standing diagonals"),

            // Walk lean / arch (curving paths)
            ("walk_lean_L",         "Walk forward leaning left",     "Standing walk variants"),
            ("walk_lean_R",         "Walk forward leaning right",    "Standing walk variants"),
            ("walk_arch_L",         "Walk forward arc left",         "Standing walk variants"),
            ("walk_arch_R",         "Walk forward arc right",        "Standing walk variants"),

            // Standing run
            ("loco_run_fwd_loop",   "Run forward (loop)",            "Standing run"),
            ("loco_run_fwd_start",  "Run forward (start)",           "Standing run"),
            ("loco_run_fwd_stop_l", "Run forward (stop, L foot)",    "Standing run"),
            ("loco_run_fwd_stop_r", "Run forward (stop, R foot)",    "Standing run"),
            ("loco_run_bwd_loop",   "Run backward (loop)",           "Standing run"),
            ("loco_run_strafe_l_loop",  "Run strafe left",           "Standing run"),
            ("loco_run_strafe_r_loop",  "Run strafe right",          "Standing run"),
            ("loco_run_strafe_l45_loop","Run fwd-left 45°",          "Standing run"),
            ("loco_run_strafe_r45_loop","Run fwd-right 45°",         "Standing run"),
            ("loco_run_strafe_l135_loop","Run back-left 135°",       "Standing run"),
            ("loco_run_strafe_r135_loop","Run back-right 135°",      "Standing run"),

            // Run lean / arch (curving paths)
            ("run_lean_L",          "Run forward leaning left",      "Standing run variants"),
            ("run_lean_R",          "Run forward leaning right",     "Standing run variants"),
            ("run_arch_L",          "Run forward arc left",          "Standing run variants"),
            ("run_arch_R",          "Run forward arc right",         "Standing run variants"),

            // Pivot starts (idle → walk/run with rotation)
            ("loco_walk_fwd_start_l90",  "Walk start, pivot 90° L",  "Pivot starts (walk)"),
            ("loco_walk_fwd_start_r90",  "Walk start, pivot 90° R",  "Pivot starts (walk)"),
            ("loco_walk_fwd_start_l135", "Walk start, pivot 135° L", "Pivot starts (walk)"),
            ("loco_walk_fwd_start_r135", "Walk start, pivot 135° R", "Pivot starts (walk)"),
            ("loco_walk_fwd_start_l180", "Walk start, pivot 180° L", "Pivot starts (walk)"),
            ("loco_walk_fwd_start_r180", "Walk start, pivot 180° R", "Pivot starts (walk)"),
            ("loco_run_fwd_start_l90",   "Run start, pivot 90° L",   "Pivot starts (run)"),
            ("loco_run_fwd_start_r90",   "Run start, pivot 90° R",   "Pivot starts (run)"),
            ("loco_run_fwd_start_l135",  "Run start, pivot 135° L",  "Pivot starts (run)"),
            ("loco_run_fwd_start_r135",  "Run start, pivot 135° R",  "Pivot starts (run)"),
            ("loco_run_fwd_start_l180",  "Run start, pivot 180° L",  "Pivot starts (run)"),
            ("loco_run_fwd_start_r180",  "Run start, pivot 180° R",  "Pivot starts (run)"),

            // Mid-run 180° turns
            ("loco_run_fwd_turn_l180_l", "Run 180° turn left, L foot",  "Mid-run pivot"),
            ("loco_run_fwd_turn_l180_r", "Run 180° turn left, R foot",  "Mid-run pivot"),
            ("loco_run_fwd_turn_r180_l", "Run 180° turn right, L foot", "Mid-run pivot"),
            ("loco_run_fwd_turn_r180_r", "Run 180° turn right, R foot", "Mid-run pivot"),

            // Sprint
            ("loco_sprint_loop",    "Sprint forward (loop)",         "Sprint"),
            ("loco_slide",          "Slide (sprint stop / fast turn)","Sprint"),

            // Standing turn-in-place
            ("loco_turn_l90",       "Turn-in-place 90° left",        "Standing turn"),
            ("loco_turn_r90",       "Turn-in-place 90° right",       "Standing turn"),
            ("loco_turn_l180",      "Turn-in-place 180° left",       "Standing turn"),
            ("loco_turn_r180",      "Turn-in-place 180° right",      "Standing turn"),

            // Combat-stance walk + diagonals
            ("combat_walk_fwd_loop","Combat walk forward",           "Combat walk"),
            ("combat_walk_fwd_fast","Combat walk fwd (fast)",        "Combat walk"),
            ("combat_walk_bwd_loop","Combat walk backward",          "Combat walk"),
            ("combat_walk_l45_loop","Combat walk fwd-left 45°",      "Combat walk"),
            ("combat_walk_r45_loop","Combat walk fwd-right 45°",     "Combat walk"),
            ("combat_walk_l135_loop","Combat walk back-left 135°",   "Combat walk"),
            ("combat_walk_r135_loop","Combat walk back-right 135°",  "Combat walk"),

            // Combat sidesteps + skips + dodges
            ("combat_sidestep_l",   "Combat sidestep left",          "Combat bursts"),
            ("combat_sidestep_r",   "Combat sidestep right",         "Combat bursts"),
            ("combat_skip_fwd",     "Combat skip forward",           "Combat bursts"),
            ("skip_forward_2",      "Combat skip forward (alt)",     "Combat bursts"),
            ("combat_skip_bwd",     "Combat skip backward",          "Combat bursts"),
            ("skip_back_2",         "Combat skip backward (alt)",    "Combat bursts"),
            ("combat_dodge_l",      "Combat dodge left",             "Combat bursts"),
            ("combat_dodge_r",      "Combat dodge right",            "Combat bursts"),

            // Combat turn-in-place
            ("combat_turn_l90",     "Combat turn 90° left",          "Combat turn"),
            ("combat_turn_r90",     "Combat turn 90° right",         "Combat turn"),
            ("combat_turn_l180",    "Combat turn 180° left",         "Combat turn"),
            ("combat_turn_r180",    "Combat turn 180° right",        "Combat turn"),
        };

        private bool   _showcaseRunning;
        private bool   _showcasePaused;
        private int    _seqIndex;       // index into _sequences
        private int    _stepIndex;      // index into _sequences[_seqIndex].steps
        private bool   _runAllMode;     // when true, advance to next sequence after the last step
        private float  _stepAdvanceAt;
        private Text   _showcaseStatusLabel;
        private Text   _showcaseSectionLabel;
        private Slider _showcaseSpeedSlider;

        public int  ShowcaseTotalClips => _showcaseClipIds.Length; // legacy
        public int  ShowcaseSequenceCount => _sequences.Length;
        public string GetShowcaseSequenceName(int i) => i >= 0 && i < _sequences.Length ? _sequences[i].name : "";

        // Indexed entry-points for the per-sequence buttons. Need to be
        // method references (not lambdas) so `UnityEventTools.AddPersistentListener`
        // can wire them at scene-build time. Index is fixed by position in
        // `_sequences`; renumber both if the array order changes.
        public void StartSeq01() => StartSequence(0);
        public void StartSeq02() => StartSequence(1);
        public void StartSeq03() => StartSequence(2);
        public void StartSeq04() => StartSequence(3);
        public void StartSeq05() => StartSequence(4);
        public void StartSeq06() => StartSequence(5);
        public void StartSeq07() => StartSequence(6);
        public void StartSeq08() => StartSequence(7);
        public void StartSeq09() => StartSequence(8);
        public void StartSeq10() => StartSequence(9);
        public void StartSeq11() => StartSequence(10);
        public void StartSeq12() => StartSequence(11);
        public void StartSeq13() => StartSequence(12);
        public void StartSeq14() => StartSequence(13);
        public void StartSeq15() => StartSequence(14);
        public void StartSeq16() => StartSequence(15);

        public void BindShowcaseControls(Text status, Text section, Slider speedSlider)
        {
            _showcaseStatusLabel  = status;
            _showcaseSectionLabel = section;
            _showcaseSpeedSlider  = speedSlider;
        }

        /// <summary>Run a single sequence by index. Stops at the last step.</summary>
        public void StartSequence(int index)
        {
            if (_subject == null || _subject.Animancer == null) return;
            if (index < 0 || index >= _sequences.Length) return;
            BeginShowcase();
            _runAllMode = false;
            _seqIndex   = index;
            _stepIndex  = 0;
            PlayCurrentStep();
            UpdateShowcaseStatus();
        }

        /// <summary>Convenience: run sequence 0, 1, 2 ... in order, chaining
        /// through every defined sequence.</summary>
        public void StartRunAllSequences()
        {
            if (_subject == null || _subject.Animancer == null) return;
            BeginShowcase();
            _runAllMode = true;
            _seqIndex   = 0;
            _stepIndex  = 0;
            PlayCurrentStep();
            UpdateShowcaseStatus();
        }

        /// <summary>Legacy entry-point — starts "Run All".</summary>
        public void StartShowcase() => StartRunAllSequences();

        public void StopShowcase()
        {
            _showcaseRunning = false;
            _showcasePaused  = false;
            if (_subject?.Movement   != null) _subject.Movement.ClearSuppression();
            if (_subject?.Locomotion != null) _subject.Locomotion.ClearSuppression();
            // Reset root motion to off — gameplay flow doesn't want it
            // (the H2HMovementController owns position/rotation via cc.Move
            // and a smoothed rotation lerp).
            if (_subject?.Animancer?.Animator != null)
                _subject.Animancer.Animator.applyRootMotion = false;
            UpdateShowcaseStatus("Showcase: stopped");
        }

        public void PauseShowcase()
        {
            if (!_showcaseRunning) return;
            _showcasePaused = !_showcasePaused;
            UpdateShowcaseStatus();
        }

        public void NextShowcaseClip()
        {
            if (!_showcaseRunning) { StartRunAllSequences(); return; }
            AdvanceStep(forward: true);
            UpdateShowcaseStatus();
        }

        public void PrevShowcaseClip()
        {
            if (!_showcaseRunning) { StartRunAllSequences(); return; }
            AdvanceStep(forward: false);
            UpdateShowcaseStatus();
        }

        public void OnShowcaseSpeedChanged(float v)
        {
            if (_subject?.Animancer?.Graph != null)
                _subject.Animancer.Graph.Speed = Mathf.Clamp(v, 0.05f, 2f);
        }

        private void BeginShowcase()
        {
            _showcaseRunning = true;
            _showcasePaused  = false;
            _subject.AIEnabled = false;
            if (_subject.Movement   != null) { _subject.Movement.Stop();   _subject.Movement.SuppressFor(9999f); }
            if (_subject.Locomotion != null) _subject.Locomotion.SuppressFor(9999f);
        }

        private void TickShowcase()
        {
            if (!_showcaseRunning || _showcasePaused) return;
            if (Time.time < _stepAdvanceAt) return;
            AdvanceStep(forward: true);
            UpdateShowcaseStatus();
        }

        private void AdvanceStep(bool forward)
        {
            var seq = _sequences[_seqIndex];
            int next = _stepIndex + (forward ? 1 : -1);
            if (next < 0)
            {
                // Wrap to previous sequence's last step.
                _seqIndex = (_seqIndex - 1 + _sequences.Length) % _sequences.Length;
                _stepIndex = _sequences[_seqIndex].steps.Length - 1;
            }
            else if (next >= seq.steps.Length)
            {
                if (_runAllMode)
                {
                    _seqIndex = (_seqIndex + 1) % _sequences.Length;
                    _stepIndex = 0;
                    if (_seqIndex == 0)
                    {
                        // Wrapped past last sequence — stop.
                        StopShowcase();
                        return;
                    }
                }
                else
                {
                    // Single sequence — wrap back to start.
                    _stepIndex = 0;
                }
            }
            else
            {
                _stepIndex = next;
            }
            PlayCurrentStep();
        }

        private void PlayCurrentStep()
        {
            if (_subject == null || _subject.Animancer == null || _subject.Library == null) return;
            var step = _sequences[_seqIndex].steps[_stepIndex];

            // Toggle the Animator's root-motion flag based on what kind of
            // clip is playing. Turn / pivot / mid-run-pivot clips have
            // baked rotation that ONLY applies when root motion is on —
            // otherwise the bones twirl but the transform stays put.
            // Walk / run / strafe loops keep root motion off so the unit
            // doesn't drift around the scene during showcase playback.
            var animator = _subject.Animancer.Animator;
            if (animator != null) animator.applyRootMotion = step.applyRootMotion;

            if (_subject.Library.TryGet(step.clipId, out var t) && t != null)
            {
                _subject.Animancer.Play(t, fadeDuration: 0.15f);
                float dwell = step.dwellSeconds;
                if (dwell < 0f && t.Transition is Animancer.ClipTransition ct && ct.Clip != null)
                {
                    // -1 → use the clip's own length (correct for non-loop
                    // start / stop / turn / dodge clips so they finish).
                    dwell = ct.Clip.length * 1.05f;
                }
                if (dwell <= 0f) dwell = 1.0f; // safety fallback
                _stepAdvanceAt = Time.time + dwell;
            }
            else
            {
                Debug.LogWarning($"[H2HTrainingUI] Showcase clip '{step.clipId}' missing from library — skipping.");
                _stepAdvanceAt = Time.time + 0.1f;
            }
        }

        private void UpdateShowcaseStatus(string overrideText = null)
        {
            if (_showcaseStatusLabel == null) return;
            if (overrideText != null) { _showcaseStatusLabel.text = overrideText; return; }
            if (!_showcaseRunning)    { _showcaseStatusLabel.text = "Showcase: stopped"; return; }
            var seq  = _sequences[_seqIndex];
            var step = seq.steps[_stepIndex];
            string state = _showcasePaused ? " [PAUSED]" : "";
            string mode  = _runAllMode ? $" • Run All (seq {_seqIndex + 1}/{_sequences.Length})" : "";
            _showcaseStatusLabel.text =
                $"▶ {seq.name}{mode}{state}\n" +
                $"  Step {_stepIndex + 1}/{seq.steps.Length}: {step.label}\n" +
                $"  id='{step.clipId}'";
            if (_showcaseSectionLabel != null)
                _showcaseSectionLabel.text = seq.description;
        }

        // ── Logger controls ───────────────────────────────────────

        public void DumpLog()
        {
            if (H2HLogger.Instance != null) H2HLogger.Instance.Dump();
            else Debug.LogWarning("[H2HTrainingUI] No H2HLogger in scene.");
        }

        public void ClearLog()
        {
            if (H2HLogger.Instance != null) H2HLogger.Instance.Clear();
        }

        // ── Time scale dial ──────────────────────────────────────

        [Header("Time scale")]
        [SerializeField] private Slider _timeScaleSlider;
        [SerializeField] private Text _timeScaleLabel;

        public void SetTimeScale5pct()    => SetTimeScale(0.05f);
        public void SetTimeScale10pct()   => SetTimeScale(0.10f);
        public void SetTimeScale25pct()   => SetTimeScale(0.25f);
        public void SetTimeScale50pct()   => SetTimeScale(0.50f);
        public void SetTimeScale100pct()  => SetTimeScale(1.00f);

        public void OnTimeScaleSliderChanged(float v) => SetTimeScale(v);

        public void SetTimeScale(float v)
        {
            v = Mathf.Clamp(v, 0.01f, 2f);
            Time.timeScale = v;
            // Match physics step so deterministic ragdoll / CC behavior
            // doesn't break at very low time scales.
            Time.fixedDeltaTime = 0.02f * v;
            if (_timeScaleSlider != null && !Mathf.Approximately(_timeScaleSlider.value, v))
                _timeScaleSlider.value = v;
            SetScenarioLabel($"Time scale: {v * 100:F0}%");
        }

        public void BindTimeScale(Slider slider, Text label)
        {
            _timeScaleSlider = slider;
            _timeScaleLabel  = label;
        }

        private void OnDisable()
        {
            // Restore default time scale when the UI is disabled / scene unloads.
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        private void LateUpdate()
        {
            if (_timeScaleLabel != null)
                _timeScaleLabel.text = $"Time: {Time.timeScale * 100:F0}%";
        }

        public void ResetTuning()
        {
            if (_director?.Phases != null)
            {
                _director.Phases.OverrideSpottingTime = -1f;
                _director.Phases.OverrideDecisionLag = -1f;
            }
            if (_director?.Orchestrator != null)
                _director.Orchestrator.baseSeparationChance = 0.35f;
            if (_subject != null) _subject.CounterChance = 0f;
            if (_dummy   != null) _dummy.CounterChance = 0f;
            SetScenarioLabel("Tuning reset to defaults.");
        }

        // ── Wiring helpers (called from editor setup) ──────────────

        public void Bind(H2HTrainingDirector director, H2HUnit subject, H2HUnit dummy)
        {
            _director = director;
            _subject  = subject;
            _dummy    = dummy;
        }

        public void BindLabels(Text subjectStatus, Text dummyStatus, Text scenarioLabel)
        {
            _subjectStatusLabel = subjectStatus;
            _dummyStatusLabel   = dummyStatus;
            _scenarioLabel      = scenarioLabel;
        }

        public void BindSliders(Slider spotting, Text spottingLbl, Slider lag, Text lagLbl, Slider hp, Slider speed)
        {
            _spottingSlider = spotting;
            _spottingSliderLabel = spottingLbl;
            _decisionLagSlider = lag;
            _decisionLagSliderLabel = lagLbl;
            _subjectHpSlider = hp;
            _subjectSpeedSlider = speed;
        }

        private void SetScenarioLabel(string s)
        {
            if (_scenarioLabel != null) _scenarioLabel.text = s;
            else Debug.Log($"[H2HTrainingUI] {s}");
        }
    }
}
