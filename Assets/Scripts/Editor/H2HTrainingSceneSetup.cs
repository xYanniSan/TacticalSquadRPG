using System.Collections.Generic;
using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat;
using TacticalRPG.ThirdPerson;
using TacticalRPG.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalRPG.EditorTools
{
    /// <summary>
    /// Editor menu that adds the hand-to-hand combat layer on top of the
    /// existing TrainingDummy scene:
    ///   - Adds an H2HTrainingDirector GameObject (which spawns the
    ///     phase system and orchestrator subsystems).
    ///   - Adds H2HUnit + H2HUnitBrain components to TestSubject and
    ///     Dummy if they are missing.
    ///   - Builds an H2H control Canvas with position presets, phase
    ///     forcing buttons, sliders, and the eight verification scenarios.
    ///
    /// Idempotent — running it twice rebuilds the canvas in place but
    /// keeps the existing units and director.
    ///
    /// See `Docs/Design/HAND_TO_HAND_COMBAT.md` §11 for the verification
    /// scenarios and §14 for the testing setup spec.
    /// </summary>
    public static class H2HTrainingSceneSetup
    {
        private const string CanvasName = "H2H_Canvas";
        private const string DirectorName = "H2H_Director";

        [MenuItem("TacticalRPG/H2H/Build Training Scene")]
        public static void BuildTrainingScene()
        {
            var subject = GameObject.Find("TestSubject");
            var dummy   = GameObject.Find("Dummy");

            if (subject == null || dummy == null)
            {
                EditorUtility.DisplayDialog(
                    "H2H setup",
                    "Open the TrainingDummy.unity scene first — this tool extends it. Looking for objects named 'TestSubject' and 'Dummy'.",
                    "OK");
                return;
            }

            EnsureH2HOnUnit(subject, displayName: "Subject", team: UnitTeam.Player,
                            hostiles: new[] { UnitTeam.Enemy });
            EnsureH2HOnUnit(dummy, displayName: "Dummy", team: UnitTeam.Enemy,
                            hostiles: new[] { UnitTeam.Player });

            var subjectH2H = subject.GetComponent<H2HUnit>();
            var dummyH2H   = dummy.GetComponent<H2HUnit>();

            // Both units' AI OFF by default — manual testing is the
            // primary use case for this scene now (Manual Move panel,
            // Showcase sequences, etc.). Use the "Toggle AI" UI buttons
            // (Subject Phase / Dummy Phase panels) or the Manual Move
            // "Resume AI" button to flip them on for combat scenarios.
            if (subjectH2H != null) subjectH2H.AIEnabled = false;
            if (dummyH2H   != null) dummyH2H.AIEnabled   = false;

            var director = EnsureDirector(subjectH2H, dummyH2H);
            BuildCanvas(director, subjectH2H, dummyH2H);

            EditorSceneSave();
            Debug.Log("[H2H setup] Training scene rebuilt. Press Play and use the H2H_Canvas controls.");
        }

        // ── Per-unit setup ──────────────────────────────────────────

        private static void EnsureH2HOnUnit(GameObject unit, string displayName, UnitTeam team,
                                             UnitTeam[] hostiles)
        {
            var h2h = unit.GetComponent<H2HUnit>();
            if (h2h == null) h2h = unit.AddComponent<H2HUnit>();

            // Force-set serialized fields via SerializedObject so values
            // persist into the prefab/scene.
            var so = new SerializedObject(h2h);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_team").enumValueIndex = (int)team;

            // Hostile teams.
            var hostileList = so.FindProperty("_hostileTeams");
            hostileList.arraySize = hostiles.Length;
            for (int i = 0; i < hostiles.Length; i++)
                hostileList.GetArrayElementAtIndex(i).enumValueIndex = (int)hostiles[i];

            // Library reference. Try the locomotion driver first, then the
            // training-dummy controller (in case this unit was the bare
            // Dummy with no driver yet), then fall back to loading the
            // shared asset by path. Without this, a Dummy that lacked a
            // KuboldLocomotionDriver ended up with `_library = null` on
            // its H2HUnit, and every PlayLibraryClip call no-opped — the
            // unit never played a single animation in the whole scene.
            var lib = TryFindClipLibrary(unit);
            if (lib == null)
            {
                var dummyCtrl = unit.GetComponent<TrainingDummyController>();
                if (dummyCtrl != null)
                {
                    var dso = new SerializedObject(dummyCtrl);
                    var dlib = dso.FindProperty("_library");
                    if (dlib != null) lib = dlib.objectReferenceValue as BattleAnimancerClipLibrary;
                }
            }
            if (lib == null)
            {
                lib = AssetDatabase.LoadAssetAtPath<BattleAnimancerClipLibrary>(
                    "Assets/Data/AnimancerClips/Kubold/Kubold_TestClipLibrary.asset");
            }
            if (lib != null)
                so.FindProperty("_library").objectReferenceValue = lib;
            else
                Debug.LogWarning($"[H2H setup] No BattleAnimancerClipLibrary found for {displayName}. Run 'TacticalRPG/Kubold/Setup Test Clip Library' first.");

            // Make sure the locomotion driver exists with the same library
            // wired — without it, the unit gets stuck in whatever clip was
            // last played and never picks new locomotion clips per frame.
            // This was the "Dummy doesn't play any animations" bug.
            var loco = unit.GetComponent<KuboldLocomotionDriver>();
            if (loco == null) loco = unit.AddComponent<KuboldLocomotionDriver>();
            if (lib != null)
            {
                var lso = new SerializedObject(loco);
                var lprop = lso.FindProperty("_library");
                if (lprop != null && lprop.objectReferenceValue == null)
                {
                    lprop.objectReferenceValue = lib;
                    lso.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Upper-body AvatarMask for Animancer Layer 1 (engagement
            // animations §4.1). Created by `UpperBodyMaskCreator`; if the
            // asset doesn't exist yet, the layering API simply no-ops on
            // this unit until the menu is run.
            var maskProp = so.FindProperty("_upperBodyMask");
            if (maskProp != null && maskProp.objectReferenceValue == null)
            {
                var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(
                    "Assets/Animation/Masks/UpperBody.mask");
                if (mask != null) maskProp.objectReferenceValue = mask;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            // Combo set is auto-populated in H2HUnit.Awake if empty;
            // user can override per-unit in the Inspector if they want
            // bespoke move sets.

            // The brain is auto-added in H2HUnit.Awake, but ensure component
            // exists in editor so the Inspector shows it.
            var brain = unit.GetComponent<H2HUnitBrain>();
            if (brain == null) brain = unit.AddComponent<H2HUnitBrain>();

            // Force-apply the latest tuning defaults via SerializedObject
            // so re-running the menu propagates code-level default changes
            // to existing scene components. Without this, Unity preserves
            // the originally-saved defaults from when the brain was first
            // instantiated, and code-level default changes never take
            // effect on the live unit.
            var bso = new SerializedObject(brain);
            void SetFloatIfPresent(string n, float v)
            {
                var p = bso.FindProperty(n);
                if (p != null) p.floatValue = v;
            }
            SetFloatIfPresent("_disengageChanceAggressive", 0.01f);
            SetFloatIfPresent("_disengageChanceBalanced",  0.02f);
            SetFloatIfPresent("_disengageChanceDefensive", 0.04f);
            SetFloatIfPresent("_disengageBoost",            0.20f);
            SetFloatIfPresent("_engagementCooldownSeconds", 1.5f);
            bso.ApplyModifiedPropertiesWithoutUndo();

            // Ensure the movement controller exists at scene-build time so
            // the Inspector shows it. H2HUnit.Awake also auto-adds it as a
            // safety net at runtime.
            if (unit.GetComponent<H2HMovementController>() == null)
                unit.AddComponent<H2HMovementController>();

            // Foot IK on the Animator child. Animator's IK Pass must be
            // enabled in the controller; for the cleared-controller test
            // scene, the Animator drives the avatar via Animancer's
            // playable graph, which doesn't honor IK weights — so the
            // component sits idle until a real Animator Controller
            // is reintroduced. Adding it now avoids a second pass.
            var animator = unit.GetComponentInChildren<Animator>();
            if (animator != null && animator.GetComponent<H2HFootIK>() == null)
                animator.gameObject.AddComponent<H2HFootIK>();
        }

        private static BattleAnimancerClipLibrary TryFindClipLibrary(GameObject unit)
        {
            var loco = unit.GetComponent<KuboldLocomotionDriver>();
            if (loco == null) return null;
            var so = new SerializedObject(loco);
            var prop = so.FindProperty("_library");
            return prop != null ? prop.objectReferenceValue as BattleAnimancerClipLibrary : null;
        }

        // ── Director ────────────────────────────────────────────────

        private static H2HTrainingDirector EnsureDirector(H2HUnit subject, H2HUnit dummy)
        {
            var existing = GameObject.Find(DirectorName);
            if (existing == null) existing = new GameObject(DirectorName);

            var director = existing.GetComponent<H2HTrainingDirector>();
            if (director == null) director = existing.AddComponent<H2HTrainingDirector>();

            // Logger lives on the same GameObject so it's easy to find and
            // its events fire as soon as units register on Awake.
            if (existing.GetComponent<H2HLogger>() == null)
                existing.AddComponent<H2HLogger>();

            var so = new SerializedObject(director);
            var unitsList = so.FindProperty("_units");
            var newUnits = new List<H2HUnit>();
            if (subject != null) newUnits.Add(subject);
            if (dummy   != null) newUnits.Add(dummy);
            unitsList.arraySize = newUnits.Count;
            for (int i = 0; i < newUnits.Count; i++)
                unitsList.GetArrayElementAtIndex(i).objectReferenceValue = newUnits[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return director;
        }

        // ── Canvas ──────────────────────────────────────────────────

        private static void BuildCanvas(H2HTrainingDirector director, H2HUnit subject, H2HUnit dummy)
        {
            var existing = GameObject.Find(CanvasName);
            if (existing != null) Object.DestroyImmediate(existing);

            var canvasGO = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // EventSystem
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            var ui = canvasGO.AddComponent<H2HTrainingUI>();
            ui.Bind(director, subject, dummy);

            // ── Top toolbar: master + per-panel toggle buttons ──
            // Always visible at the top of the canvas. Master ☰ hides
            // everything (including the old animation-test Canvas);
            // smaller buttons toggle individual sub-panels.
            var toolbar = new GameObject("Toolbar", typeof(RectTransform));
            toolbar.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var trt = toolbar.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot     = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0, -10);
            trt.sizeDelta = new Vector2(1100, 36);

            // Layout buttons left-to-right inside the toolbar.
            float x = 0f; float gap = 6f;
            void AddTb(string label, float w, UnityEngine.Events.UnityAction act)
            {
                var b = MakeButton(toolbar.transform, label, new Vector2(x, 0), new Vector2(w, 32), act);
                var brt = b.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(0, 0.5f);
                brt.pivot     = new Vector2(0, 0.5f);
                brt.anchoredPosition = new Vector2(x, 0);
                brt.sizeDelta = new Vector2(w, 32);
                x += w + gap;
            }

            AddTb("☰ Hide All",      90f, ui.ToggleUIVisibility);
            AddTb("⚔ Enable AI",     95f, ui.EnableBothAI);
            AddTb("Status",          70f, ui.TogglePanelStatus);
            AddTb("Presets",         70f, ui.TogglePanelPresets);
            AddTb("Toggles",         70f, ui.TogglePanelToggles);
            AddTb("Subj Phase",      80f, ui.TogglePanelSubjectPhase);
            AddTb("Dummy Phase",     90f, ui.TogglePanelDummyPhase);
            AddTb("Scenarios",       80f, ui.TogglePanelScenarios);
            AddTb("Sliders",         70f, ui.TogglePanelSliders);
            AddTb("TimeScale",       80f, ui.TogglePanelTimeScale);
            AddTb("Layering",        75f, ui.TogglePanelLayering);
            AddTb("Manual Move",     90f, ui.TogglePanelManualMove);
            AddTb("Showcase",        80f, ui.TogglePanelShowcase);
            AddTb("Old Menu",        80f, ui.TogglePanelOldMenu);

            // Center the toolbar by setting its width to fit content.
            trt.sizeDelta = new Vector2(x, 36);

            // ── Status panel (top-left) ──
            var statusPanel = MakePanel(canvasGO.transform, "StatusPanel",
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1),
                pivot: new Vector2(0, 1),
                pos: new Vector2(20, -20),
                size: new Vector2(380, 320),
                bgAlpha: 0.7f);

            var subjectLabel  = MakeLabel(statusPanel.transform, "SubjectStatus",
                pos: new Vector2(10, -10), size: new Vector2(360, 140), text: "Subject\n(no data)");
            var dummyLabel    = MakeLabel(statusPanel.transform, "DummyStatus",
                pos: new Vector2(10, -160), size: new Vector2(360, 140), text: "Dummy\n(no data)");
            var scenarioLabel = MakeLabel(canvasGO.transform, "ScenarioLabel",
                pos: new Vector2(420, -20), size: new Vector2(700, 60), text: "Scenario: (none)",
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1), pivot: new Vector2(0, 1));

            ui.BindLabels(subjectLabel, dummyLabel, scenarioLabel);
            ui.RegisterCollapsiblePanel(statusPanel);
            ui.RegisterCollapsiblePanel(scenarioLabel.gameObject);

            // ── Position presets (top-right) ──
            var presetsPanel = MakePanel(canvasGO.transform, "PresetsPanel",
                anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1),
                pos: new Vector2(-20, -50),
                size: new Vector2(220, 235),
                bgAlpha: 0.7f);
            MakeButton(presetsPanel.transform, "Adjacent (1m)",   new Vector2(10, -10),  new Vector2(200, 32), ui.PresetAdjacent);
            MakeButton(presetsPanel.transform, "Mid-range (3m)",  new Vector2(10, -47),  new Vector2(200, 32), ui.PresetMidRange);
            MakeButton(presetsPanel.transform, "Long-range (6m)", new Vector2(10, -84),  new Vector2(200, 32), ui.PresetLongRange);
            MakeButton(presetsPanel.transform, "Revive Both",     new Vector2(10, -121), new Vector2(200, 30), ui.ReviveBoth);
            MakeButton(presetsPanel.transform, "Reset tuning",    new Vector2(10, -156), new Vector2(200, 30), ui.ResetTuning);
            MakeButton(presetsPanel.transform, "Dump log (F10)",  new Vector2(10, -190), new Vector2(200, 30), ui.DumpLog);
            ui.RegisterCollapsiblePanel(presetsPanel);

            // ── Behavior toggles (just below presets) ──
            var togglesPanel = MakePanel(canvasGO.transform, "Toggles",
                anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1),
                pos: new Vector2(-20, -300),
                size: new Vector2(220, 100),
                bgAlpha: 0.7f);
            var sepLabel  = MakeLabel(togglesPanel.transform, "SepLabel",
                pos: new Vector2(10, -5), size: new Vector2(200, 25), text: "Separation: ON");
            MakeButton(togglesPanel.transform, "Toggle Separation",
                new Vector2(10, -28), new Vector2(200, 22), ui.ToggleSeparation);
            var dodgeLabel = MakeLabel(togglesPanel.transform, "DodgeLabel",
                pos: new Vector2(10, -52), size: new Vector2(200, 22), text: "Dodge: ON");
            MakeButton(togglesPanel.transform, "Toggle Dodge",
                new Vector2(10, -75), new Vector2(200, 22), ui.ToggleDodge);
            ui.BindBehaviorLabels(sepLabel, dodgeLabel);
            ui.RegisterCollapsiblePanel(togglesPanel);

            // ── Phase forcing for Subject (right column) ──
            var subjPhase = MakePanel(canvasGO.transform, "SubjectPhase",
                anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1),
                pos: new Vector2(-20, -415),
                size: new Vector2(220, 280),
                bgAlpha: 0.7f);
            MakeLabel(subjPhase.transform, "Header", new Vector2(10, -5), new Vector2(200, 25), "Force SUBJECT phase");
            MakeButton(subjPhase.transform, "NotEngaged",  new Vector2(10, -35),  new Vector2(200, 36), ui.ForceSubjectNotEngaged);
            MakeButton(subjPhase.transform, "Spotting",    new Vector2(10, -75),  new Vector2(200, 36), ui.ForceSubjectSpotting);
            MakeButton(subjPhase.transform, "Approaching", new Vector2(10, -115), new Vector2(200, 36), ui.ForceSubjectApproaching);
            MakeButton(subjPhase.transform, "Engaged",     new Vector2(10, -155), new Vector2(200, 36), ui.ForceSubjectEngaged);
            MakeButton(subjPhase.transform, "Separating",  new Vector2(10, -195), new Vector2(200, 36), ui.ForceSubjectSeparating);
            MakeButton(subjPhase.transform, "Toggle AI",   new Vector2(10, -235), new Vector2(200, 36), ui.ToggleSubjectAI);
            ui.RegisterCollapsiblePanel(subjPhase);

            // ── Phase forcing for Dummy ──
            var dummyPhase = MakePanel(canvasGO.transform, "DummyPhase",
                anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1),
                pos: new Vector2(-20, -710),
                size: new Vector2(220, 280),
                bgAlpha: 0.7f);
            MakeLabel(dummyPhase.transform, "Header", new Vector2(10, -5), new Vector2(200, 25), "Force DUMMY phase");
            MakeButton(dummyPhase.transform, "NotEngaged",  new Vector2(10, -35),  new Vector2(200, 36), ui.ForceDummyNotEngaged);
            MakeButton(dummyPhase.transform, "Spotting",    new Vector2(10, -75),  new Vector2(200, 36), ui.ForceDummySpotting);
            MakeButton(dummyPhase.transform, "Approaching", new Vector2(10, -115), new Vector2(200, 36), ui.ForceDummyApproaching);
            MakeButton(dummyPhase.transform, "Engaged",     new Vector2(10, -155), new Vector2(200, 36), ui.ForceDummyEngaged);
            MakeButton(dummyPhase.transform, "Separating",  new Vector2(10, -195), new Vector2(200, 36), ui.ForceDummySeparating);
            MakeButton(dummyPhase.transform, "Toggle AI",   new Vector2(10, -235), new Vector2(200, 36), ui.ToggleDummyAI);
            ui.RegisterCollapsiblePanel(dummyPhase);

            // ── Scenarios (left column under status) ──
            var scn = MakePanel(canvasGO.transform, "Scenarios",
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1),
                pivot: new Vector2(0, 1),
                pos: new Vector2(20, -360),
                size: new Vector2(380, 380),
                bgAlpha: 0.7f);
            MakeLabel(scn.transform, "ScnHdr", new Vector2(10, -5), new Vector2(360, 25), "Verification scenarios");
            MakeButton(scn.transform, "1 — Basic spotting & approach",         new Vector2(10, -35),  new Vector2(360, 36), ui.Scenario1_BasicSpotting);
            MakeButton(scn.transform, "2 — Engagement at distance",            new Vector2(10, -75),  new Vector2(360, 36), ui.Scenario2_EngagementAtDistance);
            MakeButton(scn.transform, "3 — Single exchange",                   new Vector2(10, -115), new Vector2(360, 36), ui.Scenario3_SingleExchange);
            MakeButton(scn.transform, "4 — Exchange → Separation (forced)",    new Vector2(10, -155), new Vector2(360, 36), ui.Scenario4_ExchangeIntoSeparation);
            MakeButton(scn.transform, "5 — Mutual separation",                 new Vector2(10, -195), new Vector2(360, 36), ui.Scenario5_MutualSeparation);
            MakeButton(scn.transform, "6 — Counter & role swap",               new Vector2(10, -235), new Vector2(360, 36), ui.Scenario6_CounterRoleSwap);
            MakeButton(scn.transform, "7 — Decision lag = 1.5s",               new Vector2(10, -275), new Vector2(360, 36), ui.Scenario7_DecisionLag);
            MakeButton(scn.transform, "8 — Combat speed differentiation",      new Vector2(10, -315), new Vector2(360, 36), ui.Scenario8_CombatSpeedDifferentiation);
            ui.RegisterCollapsiblePanel(scn);

            // ── Sliders (bottom-left) ──
            var sliders = MakePanel(canvasGO.transform, "Sliders",
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0),
                pivot: new Vector2(0, 0),
                pos: new Vector2(20, 20),
                size: new Vector2(420, 305),
                bgAlpha: 0.7f);
            MakeLabel(sliders.transform, "SldrHdr", new Vector2(10, -5), new Vector2(400, 25), "Tuning sliders (Subject)");

            var spotLbl = MakeLabel(sliders.transform, "SpotLbl", new Vector2(10, -35), new Vector2(400, 22), "Spotting: 0.50s");
            var spot = MakeSlider(sliders.transform, "SpotSlider", new Vector2(10, -60), new Vector2(400, 22),
                min: 0f, max: 2f, val: 0.5f, onChange: ui.OnSpottingSliderChanged);

            var lagLbl = MakeLabel(sliders.transform, "LagLbl", new Vector2(10, -90), new Vector2(400, 22), "Decision lag: 0.30s");
            var lag = MakeSlider(sliders.transform, "LagSlider", new Vector2(10, -115), new Vector2(400, 22),
                min: 0f, max: 2f, val: 0.3f, onChange: ui.OnDecisionLagSliderChanged);

            MakeLabel(sliders.transform, "HpHdr",   new Vector2(10, -145), new Vector2(400, 22), "HP");
            var hp = MakeSlider(sliders.transform, "HpSlider", new Vector2(10, -170), new Vector2(400, 22),
                min: 0f, max: 100f, val: 100f, onChange: ui.OnSubjectHpSliderChanged);

            MakeLabel(sliders.transform, "SpdHdr",  new Vector2(10, -200), new Vector2(400, 22), "Speed pool (kinetic)");
            var spd = MakeSlider(sliders.transform, "SpdSlider", new Vector2(10, -225), new Vector2(400, 22),
                min: 0f, max: 100f, val: 30f, onChange: ui.OnSubjectSpeedSliderChanged);

            MakeLabel(sliders.transform, "EnHdr",   new Vector2(10, -255), new Vector2(400, 22), "Energy");
            var en = MakeSlider(sliders.transform, "EnSlider", new Vector2(10, -280), new Vector2(400, 22),
                min: 0f, max: 100f, val: 50f, onChange: ui.OnSubjectEnergySliderChanged);

            ui.BindSliders(spot, spotLbl, lag, lagLbl, hp, spd);
            ui.RegisterCollapsiblePanel(sliders);

            // ── Time-scale panel (bottom-right) ──
            var tsPanel = MakePanel(canvasGO.transform, "TimeScale",
                anchorMin: new Vector2(1, 0), anchorMax: new Vector2(1, 0),
                pivot: new Vector2(1, 0),
                pos: new Vector2(-20, 20),
                size: new Vector2(280, 220),
                bgAlpha: 0.7f);
            var tsLabel = MakeLabel(tsPanel.transform, "TsLabel", new Vector2(10, -5),
                new Vector2(260, 25), "Time: 100%");
            MakeButton(tsPanel.transform, "5%",   new Vector2(10,  -35),  new Vector2(48, 30), ui.SetTimeScale5pct);
            MakeButton(tsPanel.transform, "10%",  new Vector2(63,  -35),  new Vector2(48, 30), ui.SetTimeScale10pct);
            MakeButton(tsPanel.transform, "25%",  new Vector2(116, -35),  new Vector2(48, 30), ui.SetTimeScale25pct);
            MakeButton(tsPanel.transform, "50%",  new Vector2(169, -35),  new Vector2(48, 30), ui.SetTimeScale50pct);
            MakeButton(tsPanel.transform, "100%", new Vector2(222, -35),  new Vector2(48, 30), ui.SetTimeScale100pct);
            var tsSlider = MakeSlider(tsPanel.transform, "TsSlider", new Vector2(10, -75),
                new Vector2(260, 28), min: 0.05f, max: 2f, val: 1f, onChange: ui.OnTimeScaleSliderChanged);
            MakeLabel(tsPanel.transform, "TsHint", new Vector2(10, -110),
                new Vector2(260, 100),
                "Slow combat down to inspect animations.\n5%–10% = single-frame review.\n50% = readable cinematic pace.\n100% = real time.");
            ui.BindTimeScale(tsSlider, tsLabel);
            ui.RegisterCollapsiblePanel(tsPanel);

            // ── Layering tests panel (centre-bottom) ──
            // Five buttons that exercise BattleAnimancerDriver's upper-body
            // layering API (§4.1 of `Docs/Design/ENGAGEMENT_ANIMATIONS.md`)
            // plus a slider to vary the upper layer's weight in real time.
            // Targets the Subject. Pressing any button suspends the
            // locomotion driver + brain so the test playback isn't fought.
            var layering = MakePanel(canvasGO.transform, "LayeringTests",
                anchorMin: new Vector2(0.5f, 0), anchorMax: new Vector2(0.5f, 0),
                pivot: new Vector2(0.5f, 0),
                pos: new Vector2(0, 20),
                size: new Vector2(360, 285),
                bgAlpha: 0.7f);
            MakeLabel(layering.transform, "LayHdr", new Vector2(10, -5),
                new Vector2(340, 25), "Layering tests (Subject)");
            MakeButton(layering.transform, "Sprint + No upper (baseline)",
                new Vector2(10, -35),  new Vector2(340, 30), ui.LayeringSprintNoUpper);
            MakeButton(layering.transform, "Sprint + Guard upper @ 100%",
                new Vector2(10, -70),  new Vector2(340, 30), ui.LayeringSprintGuard100);
            MakeButton(layering.transform, "Sprint + Guard upper @ 70%",
                new Vector2(10, -105), new Vector2(340, 30), ui.LayeringSprintGuard70);
            MakeButton(layering.transform, "Run + Punch upper",
                new Vector2(10, -140), new Vector2(340, 30), ui.LayeringRunPunch);
            MakeButton(layering.transform, "Idle + Hand Sign A on upper",
                new Vector2(10, -175), new Vector2(340, 30), ui.LayeringIdleHandSign);
            var lwLabel  = MakeLabel(layering.transform, "LayWeightLbl",
                new Vector2(10, -212), new Vector2(340, 22), "Upper-body weight: 0.00");
            var lwSlider = MakeSlider(layering.transform, "LayWeightSlider",
                new Vector2(10, -240), new Vector2(340, 22),
                min: 0f, max: 1f, val: 0f, onChange: ui.OnLayerWeightSliderChanged);
            ui.BindLayerWeightControls(lwSlider, lwLabel);
            ui.RegisterCollapsiblePanel(layering);

            // ── Manual Move panel (right column, below dummy phase) ──
            // 8 directions + 4 speed modes + Stop + Turn 90/180 L/R.
            // Disables AI on first click. Buttons drive
            // H2HMovementController.SetMoveIntent so all the smoothing,
            // turn-in-place, start-stop, and pivot-start logic engages.
            var manual = MakePanel(canvasGO.transform, "ManualMove",
                anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1),
                pos: new Vector2(-260, -50),
                size: new Vector2(220, 540),
                bgAlpha: 0.7f);
            MakeLabel(manual.transform, "MovHdr", new Vector2(10, -5),
                new Vector2(200, 22), "Manual move (Subject)");

            // Speed mode row (Creep / Walk / Run / Sprint).
            MakeButton(manual.transform, "Creep",  new Vector2(10,  -32), new Vector2(48, 28), ui.ManualSpeedCreep);
            MakeButton(manual.transform, "Walk",   new Vector2(63,  -32), new Vector2(48, 28), ui.ManualSpeedWalk);
            MakeButton(manual.transform, "Run",    new Vector2(116, -32), new Vector2(48, 28), ui.ManualSpeedRun);
            MakeButton(manual.transform, "Sprint", new Vector2(169, -32), new Vector2(40, 28), ui.ManualSpeedSprint);
            var spLbl = MakeLabel(manual.transform, "MovSpdLbl",
                new Vector2(10, -64), new Vector2(200, 20), "Speed: Walk (2.5 m/s)");

            // 8-direction grid (3x3 with center omitted).
            float gx = 10, gy = -90;
            float btnW = 60, btnH = 30, gapX = 70, gapY = 36;
            // Row 1: FL  Fwd  FR
            MakeButton(manual.transform, "↖",    new Vector2(gx + 0*gapX, gy + 0*gapY), new Vector2(btnW, btnH), ui.ManualMoveFL);
            MakeButton(manual.transform, "↑Fwd", new Vector2(gx + 1*gapX, gy + 0*gapY), new Vector2(btnW, btnH), ui.ManualMoveFwd);
            MakeButton(manual.transform, "↗",    new Vector2(gx + 2*gapX, gy + 0*gapY), new Vector2(btnW, btnH), ui.ManualMoveFR);
            // Row 2: Lt   Stop   Rt
            MakeButton(manual.transform, "←Lt",  new Vector2(gx + 0*gapX, gy - 1*gapY), new Vector2(btnW, btnH), ui.ManualMoveLt);
            MakeButton(manual.transform, "Stop", new Vector2(gx + 1*gapX, gy - 1*gapY), new Vector2(btnW, btnH), ui.ManualStop);
            MakeButton(manual.transform, "Rt→",  new Vector2(gx + 2*gapX, gy - 1*gapY), new Vector2(btnW, btnH), ui.ManualMoveRt);
            // Row 3: BL  Bwd  BR
            MakeButton(manual.transform, "↙",    new Vector2(gx + 0*gapX, gy - 2*gapY), new Vector2(btnW, btnH), ui.ManualMoveBL);
            MakeButton(manual.transform, "↓Bwd", new Vector2(gx + 1*gapX, gy - 2*gapY), new Vector2(btnW, btnH), ui.ManualMoveBwd);
            MakeButton(manual.transform, "↘",    new Vector2(gx + 2*gapX, gy - 2*gapY), new Vector2(btnW, btnH), ui.ManualMoveBR);

            // Turn-in-place row (90° L/R, 180° L/R).
            float turnY = gy - 3*gapY - 6;
            MakeButton(manual.transform, "↺ 90°",  new Vector2(10,  turnY), new Vector2(48, 28), ui.ManualTurnL90);
            MakeButton(manual.transform, "↻ 90°",  new Vector2(63,  turnY), new Vector2(48, 28), ui.ManualTurnR90);
            MakeButton(manual.transform, "↺ 180°", new Vector2(116, turnY), new Vector2(48, 28), ui.ManualTurnL180);
            MakeButton(manual.transform, "↻ 180°", new Vector2(169, turnY), new Vector2(40, 28), ui.ManualTurnR180);

            // Status + resume.
            var dirLbl = MakeLabel(manual.transform, "MovDirLbl",
                new Vector2(10, turnY - 32), new Vector2(200, 20), "Last: (none)");
            MakeButton(manual.transform, "Resume AI", new Vector2(10, turnY - 60), new Vector2(200, 30), ui.ManualResume);

            MakeLabel(manual.transform, "MovHint",
                new Vector2(10, turnY - 100), new Vector2(200, 60),
                "WASD also drives the unit\nvia TrainingPlayerController.\nShift = run, Space = sprint,\nCtrl = creep.");

            ui.BindManualMovementLabels(spLbl, dirLbl);
            ui.RegisterCollapsiblePanel(manual);

            // ── Movement Showcase panel (centre-top, below toolbar) ──
            // Sequence-based test rig: each button below runs a SCRIPTED
            // chain of clips (idle → start → loop → stop → idle, etc.) so
            // every transition is exercised in context. "Run All" chains
            // every sequence end-to-end (~5 minutes total).
            int seqCount = ui.ShowcaseSequenceCount;
            float panelHeight = 90 + 28 * 5 + 22 + 28 + 28 * seqCount + 20;
            var showcase = MakePanel(canvasGO.transform, "Showcase",
                anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
                pivot: new Vector2(0.5f, 1f),
                pos: new Vector2(0, -55),
                size: new Vector2(440, panelHeight),
                bgAlpha: 0.78f);
            MakeLabel(showcase.transform, "ScHdr", new Vector2(10, -5),
                new Vector2(420, 22),
                $"Movement Showcase — {seqCount} scripted sequences");

            var scStatus = MakeLabel(showcase.transform, "ScStatus",
                new Vector2(10, -32), new Vector2(420, 56),
                "Showcase: stopped\nPick a sequence below or click ▶▶ Run All.");
            var scSection = MakeLabel(showcase.transform, "ScSection",
                new Vector2(10, -88), new Vector2(420, 22),
                "—");

            // Transport row.
            MakeButton(showcase.transform, "▶▶ Run All",  new Vector2(10,  -114), new Vector2(95, 28), ui.StartRunAllSequences);
            MakeButton(showcase.transform, "⏸ Pause",     new Vector2(110, -114), new Vector2(75, 28), ui.PauseShowcase);
            MakeButton(showcase.transform, "◀ Prev",      new Vector2(190, -114), new Vector2(70, 28), ui.PrevShowcaseClip);
            MakeButton(showcase.transform, "▶ Skip",      new Vector2(265, -114), new Vector2(70, 28), ui.NextShowcaseClip);
            MakeButton(showcase.transform, "■ Stop",      new Vector2(340, -114), new Vector2(80, 28), ui.StopShowcase);

            // Speed slider for slow-mo inspection.
            MakeLabel(showcase.transform, "ScSpdHdr", new Vector2(10, -148),
                new Vector2(420, 20), "Animancer speed (0.05× – 2×):");
            var scSpeed = MakeSlider(showcase.transform, "ScSpdSlider",
                new Vector2(10, -170), new Vector2(420, 22),
                min: 0.05f, max: 2f, val: 1f, onChange: ui.OnShowcaseSpeedChanged);

            // One button per sequence. Persistent listeners need method
            // references (not lambdas) so we use the StartSeq01..16
            // entry-points on the UI. Indexed array of references makes
            // the loop clean.
            UnityEngine.Events.UnityAction[] seqActions = new UnityEngine.Events.UnityAction[]
            {
                ui.StartSeq01, ui.StartSeq02, ui.StartSeq03, ui.StartSeq04,
                ui.StartSeq05, ui.StartSeq06, ui.StartSeq07, ui.StartSeq08,
                ui.StartSeq09, ui.StartSeq10, ui.StartSeq11, ui.StartSeq12,
                ui.StartSeq13, ui.StartSeq14, ui.StartSeq15, ui.StartSeq16,
            };
            float listTop = -200f;
            int wireCount = Mathf.Min(seqCount, seqActions.Length);
            for (int i = 0; i < wireCount; i++)
            {
                string label = $"{i + 1}. {ui.GetShowcaseSequenceName(i)}";
                MakeButton(showcase.transform, label,
                    new Vector2(10, listTop - 28 * i),
                    new Vector2(420, 26),
                    seqActions[i]);
            }

            ui.BindShowcaseControls(scStatus, scSection, scSpeed);
            ui.RegisterCollapsiblePanel(showcase);

            // Bind per-panel refs so the toolbar buttons can flip each
            // panel individually. Also pull in the legacy TrainingDummy
            // canvas so the master and Old-Menu toggles can hide it.
            var oldCanvas = GameObject.Find("Canvas");
            ui.BindPanelRefs(statusPanel, presetsPanel, togglesPanel,
                subjPhase, dummyPhase, scn, sliders, tsPanel, oldCanvas, layering, manual, showcase);
            if (oldCanvas != null) ui.RegisterCollapsiblePanel(oldCanvas);
        }

        // ── UI primitive helpers ───────────────────────────────────

        private static GameObject MakePanel(Transform parent, string name,
                                             Vector2 anchorMin, Vector2 anchorMax,
                                             Vector2 pivot, Vector2 pos, Vector2 size, float bgAlpha)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.07f, bgAlpha);
            return go;
        }

        private static Text MakeLabel(Transform parent, string name, Vector2 pos, Vector2 size, string text,
                                       Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin ?? new Vector2(0, 1);
            rt.anchorMax = anchorMax ?? new Vector2(0, 1);
            rt.pivot     = pivot     ?? new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 14;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
                                          UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.32f, 1f);

            var txtGO = new GameObject("Text", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, worldPositionStays: false);
            var txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
            var txt = txtGO.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            var btn = go.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, onClick);
            return btn;
        }

        private static Slider MakeSlider(Transform parent, string name, Vector2 pos, Vector2 size,
                                          float min, float max, float val,
                                          UnityEngine.Events.UnityAction<float> onChange)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            var slider = go.AddComponent<Slider>();

            // Background
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.25f); bgRT.anchorMax = new Vector2(1, 0.75f);
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 1f);

            // Fill area / fill
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRT = fillArea.GetComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0, 0.25f); faRT.anchorMax = new Vector2(1, 0.75f);
            faRT.offsetMin = new Vector2(5, 0); faRT.offsetMax = new Vector2(-15, 0);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fRT = fill.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.4f, 0.6f, 0.9f, 1f);

            // Handle
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var haRT = handleArea.GetComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
            haRT.offsetMin = new Vector2(10, 0); haRT.offsetMax = new Vector2(-10, 0);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var hRT = handle.GetComponent<RectTransform>();
            hRT.sizeDelta = new Vector2(20, 0);
            handle.GetComponent<Image>().color = Color.white;

            slider.fillRect = fRT;
            slider.handleRect = hRT;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = val;

            UnityEditor.Events.UnityEventTools.AddPersistentListener(slider.onValueChanged, onChange);

            return slider;
        }

        private static void EditorSceneSave()
        {
            var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(active);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(active);
        }
    }
}
