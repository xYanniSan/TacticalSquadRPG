using System;
using System.Collections;
using System.Collections.Generic;
using Animancer;
using TacticalRPG.DataModels;
using TacticalRPG.Systems.Combat;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Per-unit H2H combat MonoBehaviour. Owns:
    ///   - phase membership (BattleH2HPhaseSystem)
    ///   - exchange agency  (BattleH2HOrchestrator via IH2HExchangeAgent)
    ///   - resource model   (HP / Speed / Energy with per-phase ticks)
    ///   - damage + death   (ApplyDamage, IsDead, OnDeath)
    ///   - per-impact FX    (hit-stop, camera shake, particle burst)
    ///   - clip playback    (BattleAnimancerClipLibrary)
    ///   - reactive AI      (H2HUnitBrain — see neighboring file)
    ///
    /// Attach alongside an `AnimancerComponent`, a `CharacterController`,
    /// a `KuboldLocomotionDriver`, and (optionally) a `TrainingDummyController`.
    /// In the training scene, both player-controlled subject and dummy carry
    /// an instance. The orchestrator and phase system are spawned once on
    /// `H2HTrainingDirector`.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AnimancerComponent))]
    public class H2HUnit : MonoBehaviour, IH2HExchangeAgent, IH2HConfigured
    {
        // ── Combo data ──────────────────────────────────────────────

        [Serializable]
        public struct ComboHit
        {
            [Tooltip("Library clip id played for this hit.")]
            public string attackId;
            public AttackArchetype archetype;
            [Tooltip("Normalized impact time within the clip (0-1). 0.45 is a sensible default.")]
            [Range(0.05f, 0.95f)] public float impactNormalized;
            [Tooltip("Damage on a clean (non-blocked, non-dodged) hit.")]
            public int damage;
            [Tooltip("Speed cost debited from the attacker on commit.")]
            public float speedCost;
        }

        [Serializable]
        public class Combo
        {
            public string name = "BasicCombo";
            public List<ComboHit> hits = new List<ComboHit>();
            [Tooltip("Min current speed (resource) the unit must have to pick this combo. Used to lock big combos behind Sharp / Primed bands.")]
            public float minSpeed = 0f;
            [Tooltip("Min current energy the unit must have. 0 = no gate.")]
            public float minEnergy = 0f;
            [Tooltip("Desired distance from defender at the moment of the FIRST impact. Pre-positioning closes the gap to this.")]
            public float desiredImpactDistance = 1.0f;
            [Tooltip("Seconds spent smoothstepping into impact distance before the first strike. Long-range kicks use ~0.5s so the back-skip animation is readable.")]
            [Range(0f, 1f)] public float positionAdjustDuration = 0.15f;
            [Tooltip("Time-gap (seconds) between consecutive hits' impact frames. The orchestrator overlaps clip playback up to this gap.")]
            public float interHitGap = 0.3f;
            [Tooltip("Optional clip played at exchange start as a visual overlay during the pre-position window. Used by long-range kick combos to back-step into kick range before the strike clip plays. Empty = play the first strike clip immediately.")]
            public string preStrikeClipId = "";
            [Tooltip("If true, the first strike clip plays with Animator.applyRootMotion = true so the kick's baked forward motion (+0.9m to +1.5m for KB_m_* kicks) carries the unit into the defender during the strike. Used by long-range kick combos.")]
            public bool applyRootMotionOnFirstStrike = false;
        }

        // ── Inspector ───────────────────────────────────────────────

        [Header("Identity")]
        [SerializeField] private string _displayName = "Unit";
        [SerializeField] private UnitDefinition _definition;
        [SerializeField] private StanceDefinition _stance;
        [SerializeField] private UnitTeam _team = UnitTeam.Player;

        [Header("Hostility")]
        [SerializeField] private List<H2HUnit> _explicitHostiles = new List<H2HUnit>();
        [SerializeField] private List<UnitTeam> _hostileTeams = new List<UnitTeam>();

        [Header("Resources")]
        [SerializeField, Range(0f, 100f)] private float _currentSpeed = 30f;
        [Tooltip("Soft cap on speed — gain × 0.4 above this band.")]
        [SerializeField, Range(0f, 100f)] private float _softCapSpeed = 70f;
        [SerializeField, Range(0f, 100f)] private float _currentEnergy = 50f;
        [SerializeField, Range(0f, 200f)] private float _maxEnergy = 100f;
        [SerializeField, Range(0f, 200f)] private float _maxHp = 100f;
        [SerializeField, Range(0f, 200f)] private float _currentHp = 100f;

        [Header("AI")]
        [Tooltip("If false, the brain stops making decisions.")]
        [SerializeField] private bool _aiEnabled = true;

        [Header("Animation library")]
        [SerializeField] private BattleAnimancerClipLibrary _library;

        [Header("Upper-body layering")]
        [Tooltip("AvatarMask used by Animancer Layer 1 for upper-body overlays " +
                 "(guard pose over sprint, punch over walk, hand-sign over idle). " +
                 "Wired by H2HTrainingSceneSetup; usually `Assets/Animation/Masks/UpperBody.mask`.")]
        [SerializeField] private AvatarMask _upperBodyMask;

        [Header("Phase clips")]
        [SerializeField] private string _alertIdleId = "combat_idle";
        [SerializeField] private string _combatIdleId = "combat_idle";
        [SerializeField] private string _separationClipId = "combat_skip_bwd";
        [SerializeField] private string _deathClipId = "death_collapse";

        [Header("Combos (multi-hit attacks)")]
        [Tooltip("Combos this unit may commit to. The brain picks one per exchange based on resources / stance.")]
        [SerializeField] private List<Combo> _combos = new List<Combo>();

        [Header("Reaction clips")]
        [SerializeField] private string _hitReactClipId = "hit_react";
        [SerializeField] private string _blockClipId = "block";
        [SerializeField] private string _dodgeClipId = "dodge";

        [Header("Counter")]
        [Range(0f, 1f)] public float CounterChance = 0f;

        [Header("Impact FX")]
        [Tooltip("Per-unit Animancer pause length on landed impacts (seconds, real time).")]
        [SerializeField, Range(0f, 0.5f)] private float _hitStopSeconds = 0.06f;
        [Tooltip("Camera shake intensity on landed impacts (peak local-position offset).")]
        [SerializeField, Range(0f, 1f)] private float _cameraShakeAmplitude = 0.12f;
        [Tooltip("Camera shake duration on landed impacts (seconds, real time).")]
        [SerializeField, Range(0f, 0.6f)] private float _cameraShakeDuration = 0.18f;
        [Tooltip("Particle burst — uses primitive sphere if no prefab is assigned.")]
        [SerializeField] private GameObject _impactBurstPrefab;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        // ── Subsystem refs ──────────────────────────────────────────

        private BattleH2HPhaseSystem    _phases;
        private BattleH2HOrchestrator   _orch;
        private CharacterController     _cc;
        private AnimancerComponent      _animancer;
        private KuboldLocomotionDriver  _loco;
        private H2HMovementController   _movement;
        private H2HUnitBrain            _brain;
        private TrainingDummyController _trainingDummy;

        private bool _justDefendedLastExchange;
        private bool _isDead;

        public event Action<H2HUnit> OnDeath;

        // ── Public API ──────────────────────────────────────────────

        public string                DisplayName => _displayName;
        public UnitTeam              Team        => _team;
        public UnitDefinition        Definition  => _definition;
        public StanceDefinition      Stance      => _stance != null ? _stance : (_definition != null ? _definition.defaultStance : null);
        public BattleH2HPhaseSystem  Phases      => _phases;
        public BattleH2HOrchestrator Orchestrator=> _orch;
        public KuboldLocomotionDriver Locomotion => _loco;
        public H2HMovementController  Movement   => _movement;
        public BattleAnimancerClipLibrary Library => _library;
        public CharacterController   CC          => _cc;
        public AnimancerComponent    Animancer   => _animancer;
        public bool                  AIEnabled
        {
            get => _aiEnabled && !_isDead;
            set
            {
                bool wasEnabled = _aiEnabled;
                _aiEnabled = value;
                // AI just turned off — stop motion so the unit doesn't keep
                // running on whatever intent the brain set last frame
                // ("dummy runs off the map" bug). Manual control paths can
                // still call SetMoveIntent immediately after toggling AI
                // off; that new intent will override this Stop because
                // setter runs first.
                if (wasEnabled && !value && _movement != null)
                    _movement.Stop();
            }
        }
        public bool   IsDead       => _isDead;
        public float  CurrentSpeed { get => _currentSpeed; set => _currentSpeed = Mathf.Clamp(value, 0f, 100f); }
        public float  SoftCapSpeed => _softCapSpeed;
        public float  CurrentEnergy{ get => _currentEnergy; set => _currentEnergy = Mathf.Clamp(value, 0f, _maxEnergy); }
        public float  MaxEnergy    => _maxEnergy;
        public float  MaxHp        { get => _maxHp;        set => _maxHp = value; }
        public float  CurrentHp    { get => _currentHp;    set => _currentHp = Mathf.Clamp(value, 0f, _maxHp); }
        public float  HpFraction   => _maxHp <= 0f ? 0f : Mathf.Clamp01(_currentHp / _maxHp);
        public bool   JustDefendedLastExchange => _justDefendedLastExchange;

        // Stats for the disengage-trigger system. Snapshotted at Exchange
        // entry and updated as hits land; the brain reads them to decide
        // whether to bail after a heavy beating (HP loss ≥ threshold or
        // hits absorbed ≥ threshold).
        public int   HitsAbsorbedLastExchange => _hitsAbsorbedInExchange;
        public float HpFractionLostLastExchange =>
            _maxHp <= 0f ? 0f : Mathf.Clamp01((_hpAtExchangeStart - _currentHp) / _maxHp);
        private float _hpAtExchangeStart;
        private int   _hitsAbsorbedInExchange;

        /// <summary>
        /// Brain calls this after it has read the exchange stats and used
        /// them (e.g., armed a disengage boost). Resets the counters so a
        /// subsequent Engaged-entry from a Separating bounce-back doesn't
        /// see the same old numbers and re-arm the boost on every cycle —
        /// that produced the "constantly disengaging" loop where the unit
        /// would Disengage → Separate → re-engage → Disengage forever from
        /// a single 6-hit beating. New exchanges reset the counters in
        /// HandlePhaseEnter (case Exchange), so this only clears stale
        /// post-resolved data.
        /// </summary>
        public void ConsumeLastExchangeStats()
        {
            _hitsAbsorbedInExchange = 0;
            _hpAtExchangeStart      = _currentHp;
        }
        public List<Combo> Combos => _combos;

        public IReadOnlyList<H2HUnit> ExplicitHostiles => _explicitHostiles;
        public IReadOnlyList<UnitTeam> HostileTeams => _hostileTeams;

        // ── Lifecycle ───────────────────────────────────────────────

        public void Configure(BattleH2HPhaseSystem phases, BattleH2HOrchestrator orch)
        {
            _phases = phases;
            _orch   = orch;
            if (_phases != null && !_phases.IsRegistered(this))
                _phases.Register(this);
        }

        private void Awake()
        {
            _cc        = GetComponent<CharacterController>();
            _animancer = GetComponent<AnimancerComponent>();
            _loco      = GetComponent<KuboldLocomotionDriver>();
            // Runtime fallback for `_library`: if the scene authoring left
            // the H2HUnit's library reference null (the Dummy was never
            // touched by the H2H setup menu), hunt for one in this order:
            //   1. The TrainingDummyController on the same GameObject
            //   2. Any other H2HUnit in the scene with a library wired
            //   3. Any KuboldLocomotionDriver in the scene with one wired
            // Without this, every PlayLibraryClip call no-ops and the unit
            // never plays an animation in the entire scene.
            if (_library == null)
            {
                var dummyCtrl = GetComponent<TrainingDummyController>();
                if (dummyCtrl != null && dummyCtrl.Library != null) _library = dummyCtrl.Library;
            }
            if (_library == null)
            {
                var others = FindObjectsByType<H2HUnit>(FindObjectsSortMode.None);
                foreach (var u in others)
                {
                    if (u != null && u != this && u._library != null)
                    {
                        _library = u._library;
                        break;
                    }
                }
            }
            if (_library == null)
            {
                var drivers = FindObjectsByType<KuboldLocomotionDriver>(FindObjectsSortMode.None);
                foreach (var d in drivers)
                {
                    if (d == null) continue;
                    var lib = d.Library; if (lib != null) { _library = lib; break; }
                }
            }
            // Runtime safety net: if the scene was authored without a
            // locomotion driver on this unit (e.g. the bare TrainingDummy
            // GameObject), add one and wire its library. Without the
            // driver, the per-frame ResolveClipId path never fires and
            // the unit gets stuck on whatever clip Animancer last played
            // — the symptom the user saw as "Dummy doesn't play any
            // animations."
            if (_loco == null)
            {
                _loco = gameObject.AddComponent<KuboldLocomotionDriver>();
                if (_library != null) _loco.EnsureLibrary(_library);
            }
            else if (_library != null)
            {
                _loco.EnsureLibrary(_library);
            }
            _movement  = GetComponent<H2HMovementController>();
            if (_movement == null) _movement = gameObject.AddComponent<H2HMovementController>();
            _brain     = GetComponent<H2HUnitBrain>();
            if (_brain == null) _brain = gameObject.AddComponent<H2HUnitBrain>();
            _trainingDummy = GetComponent<TrainingDummyController>();

            if (_combos.Count == 0) PopulateDefaultCombos();

            // Set up Animancer Layer 1 for upper-body overlays. The layer is
            // started at zero weight so it's invisible until something calls
            // `BattleAnimancerDriver.PlayUpperBody` / `SetUpperBodyWeight`.
            // Mask is required: without it, layer 1 would replace the full
            // body, defeating the layering. If the mask wasn't wired (older
            // scene predating UpperBodyMaskCreator), we leave layer 1 alone
            // and the layering API will simply be a no-op for this unit.
            if (_animancer != null && _upperBodyMask != null)
            {
                var upper = _animancer.Layers[1];
                upper.Mask   = _upperBodyMask;
                upper.Weight = 0f;
            }
        }

        private void Start()
        {
            if (_phases == null) _phases = FindAnyObjectByType<BattleH2HPhaseSystem>();
            if (_orch   == null) _orch   = FindAnyObjectByType<BattleH2HOrchestrator>();
            if (_phases != null && !_phases.IsRegistered(this)) _phases.Register(this);

            if (_phases != null)
            {
                _phases.OnPhaseEnter += HandlePhaseEnter;
                _phases.OnPhaseExit  += HandlePhaseExit;
            }
        }

        private void OnDestroy()
        {
            if (_phases != null)
            {
                _phases.OnPhaseEnter -= HandlePhaseEnter;
                _phases.OnPhaseExit  -= HandlePhaseExit;
                _phases.Unregister(this);
            }
        }

        // ── Per-phase resource tick ─────────────────────────────────

        private void Update()
        {
            if (_isDead) return;
            if (_phases == null) return;
            float dt = Time.deltaTime;
            TickResources(_phases.GetPhase(this), dt);
        }

        private void TickResources(H2HPhase phase, float dt)
        {
            // Speed gain rate by phase. Mirrors COMBAT_DESIGN.md §
            // "Speed pool" + HAND_TO_HAND_COMBAT.md §4.2.
            float speedRate = 0f;
            switch (phase)
            {
                case H2HPhase.Approaching:
                    speedRate = +8f;
                    break;
                case H2HPhase.Engaged:
                    // Moving rewards more than holding still — read CC velocity.
                    float v = _cc != null ? new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).magnitude : 0f;
                    speedRate = v > 0.4f ? +6f : -5f;
                    break;
                case H2HPhase.Separating:
                    speedRate = +4f;
                    break;
                case H2HPhase.Exchange:
                    speedRate = 0f; // costs are debited per-strike, not per-tick
                    break;
                default:
                    speedRate = -2f; // slow drain when idle / NotEngaged / Spotting
                    break;
            }
            // Honor the orchestrator's drain toggle. When OFF, negative
            // rates are clamped to 0 so the Speed pool never goes down on
            // its own — units only lose Speed when they explicitly spend
            // it on attacks (and that path is also gated by SkillCostsEnabled).
            bool drainAllowed = _orch == null || _orch.ResourceDrainEnabled;
            if (!drainAllowed && speedRate < 0f) speedRate = 0f;
            // Above the soft cap, gain rate × 0.4 (the "primed" band is expensive to hold).
            if (speedRate > 0f && _currentSpeed > _softCapSpeed) speedRate *= 0.4f;
            _currentSpeed = Mathf.Clamp(_currentSpeed + speedRate * dt, 0f, 100f);

            // Energy regen. Modest passive; +5 on block is added in the
            // defender response path.
            if (phase != H2HPhase.NotEngaged)
                _currentEnergy = Mathf.Clamp(_currentEnergy + 2f * dt, 0f, _maxEnergy);
        }

        // ── Phase-entry visuals ─────────────────────────────────────

        private void HandlePhaseEnter(MonoBehaviour unit, H2HPhase phase, string reason)
        {
            if (unit != this) return;
            if (_debugLog) Debug.Log($"[H2HUnit {_displayName}] enter {phase} ({reason})");

            switch (phase)
            {
                case H2HPhase.Spotting:    PlayLibraryClip(_alertIdleId, fallback: "idle"); break;
                case H2HPhase.Engaged:     PlayLibraryClip(_combatIdleId, fallback: "idle"); break;
                // Separation kicks off the back-skip clip as a visual overlay;
                // the controller continues to drive the unit backward via the
                // brain's intent so actual displacement matches the animation.
                case H2HPhase.Separating:  PlayLibraryClipOneShot(_separationClipId, fallback: "combat_skip_bwd", lockMovement: false); break;
                case H2HPhase.NotEngaged:  PlayLibraryClip("idle", fallback: null); break;
                case H2HPhase.Exchange:
                    // Snapshot pre-exchange HP and reset hit counter so the
                    // brain can detect "heavy exchange" on exit and trigger
                    // a forced disengage (per ENGAGEMENT_ANIMATIONS.md
                    // separation-after-beating pattern).
                    _hpAtExchangeStart       = _currentHp;
                    _hitsAbsorbedInExchange  = 0;
                    break;
            }

            // Lock facing during Exchange so the orchestrator's snap-face
            // (right before the strike clip plays) isn't fought by the
            // movement controller's smooth rotation lerp. Also stop and
            // suppress brain-driven motion so the orchestrator's
            // pre-position smoothstep and strike-clip playback don't
            // compete with leftover velocity from the brain. Unlock /
            // unsuppress when we leave Exchange.
            if (_movement != null)
            {
                if (phase == H2HPhase.Exchange)
                {
                    _movement.LockFacing();
                    _movement.Stop();
                    // Hand cc.Move ownership to the orchestrator's
                    // pre-position coroutine + the strike-clip suppression
                    // started by PlayLibraryClipOneShot. 5s is a safe upper
                    // bound for any single combo length; the next phase
                    // exit clears it.
                    _movement.SuppressFor(5f);
                }
                else
                {
                    _movement.UnlockFacing();
                    _movement.ClearSuppression();
                }
            }
        }

        private void HandlePhaseExit(MonoBehaviour unit, H2HPhase phase, string reason) { /* hook for future */ }

        // ── Hostility ───────────────────────────────────────────────

        public bool ConsidersHostile(H2HUnit other)
        {
            if (other == null || other == this) return false;
            if (other.IsDead) return false;
            if (_explicitHostiles.Contains(other)) return true;
            if (_hostileTeams.Contains(other.Team)) return true;
            return false;
        }

        public H2HUnit FindClosestHostile(IEnumerable<H2HUnit> candidates)
        {
            float best = float.MaxValue;
            H2HUnit found = null;
            foreach (var u in candidates)
            {
                if (u == null || u == this || u.IsDead) continue;
                if (!ConsidersHostile(u)) continue;
                float d = Vector3.Distance(transform.position, u.transform.position);
                if (d < best) { best = d; found = u; }
            }
            return found;
        }

        // ── IH2HExchangeAgent ───────────────────────────────────────

        public float GetTotalLengthSeconds(string attackId, float fallback)
        {
            if (_library != null && _library.TryGet(attackId, out var t) && t != null)
            {
                var ct = t.Transition as ClipTransition;
                if (ct != null && ct.Clip != null && ct.Clip.length > 0f)
                    return ct.Clip.length + 0.05f;
            }
            return fallback;
        }

        // Pool of close-range (in-place root-motion) hit options for random
        // combo generation. Each entry: (libraryId, archetype, damage,
        // speedCost, impactNormalized). All clips here have ~0m forward
        // motion per the motion probe, so they fire cleanly at strike
        // range without sliding through the defender. Long-range kicks
        // (KB_AxeKick, KB_m_HighKickRound_*, KB_m_RoundhouseKickRight,
        // KB_m_KneeLeft/Right, etc. — see ANIMATION_MOTION_PROBE.md) are
        // deferred to a later "kick-at-distance" feature that pre-positions
        // backward before committing.
        private struct HitTemplate
        {
            public string id; public AttackArchetype arch;
            public int dmg; public float speedCost; public float impactNorm;
        }
        private static readonly HitTemplate[] _closeRangeHitPool = new HitTemplate[]
        {
            // Punches (all in-place per probe)
            new HitTemplate { id = "attack_punch_jab",      arch = AttackArchetype.Light, dmg =  6, speedCost = 4f, impactNorm = 0.40f },
            new HitTemplate { id = "punch_jab_L",           arch = AttackArchetype.Light, dmg =  6, speedCost = 4f, impactNorm = 0.40f },
            new HitTemplate { id = "attack_punch_hook",     arch = AttackArchetype.Light, dmg =  7, speedCost = 5f, impactNorm = 0.45f },
            new HitTemplate { id = "punch_hook_L",          arch = AttackArchetype.Light, dmg =  7, speedCost = 5f, impactNorm = 0.45f },
            new HitTemplate { id = "attack_punch_uppercut", arch = AttackArchetype.Heavy, dmg = 10, speedCost = 8f, impactNorm = 0.55f },
            new HitTemplate { id = "punch_uppercut_L",      arch = AttackArchetype.Heavy, dmg = 10, speedCost = 8f, impactNorm = 0.55f },
            new HitTemplate { id = "punch_elbow_R",         arch = AttackArchetype.Light, dmg =  7, speedCost = 5f, impactNorm = 0.45f },
            new HitTemplate { id = "punch_elbow_L",         arch = AttackArchetype.Light, dmg =  7, speedCost = 5f, impactNorm = 0.45f },
            // Close-range kicks (in-place per probe — `_p_` family)
            new HitTemplate { id = "kick_low_L",            arch = AttackArchetype.Light, dmg =  6, speedCost = 5f, impactNorm = 0.45f },
            new HitTemplate { id = "kick_mid_L",            arch = AttackArchetype.Light, dmg =  7, speedCost = 6f, impactNorm = 0.50f },
            new HitTemplate { id = "kick_mid_R",            arch = AttackArchetype.Light, dmg =  7, speedCost = 6f, impactNorm = 0.50f },
            new HitTemplate { id = "kick_high_straight",    arch = AttackArchetype.Heavy, dmg = 10, speedCost = 8f, impactNorm = 0.50f },
            new HitTemplate { id = "kick_mid_straight",     arch = AttackArchetype.Heavy, dmg = 10, speedCost = 8f, impactNorm = 0.50f },
            new HitTemplate { id = "kick_mid_front_L",      arch = AttackArchetype.Light, dmg =  7, speedCost = 6f, impactNorm = 0.50f },
            new HitTemplate { id = "kick_mid_front_R",      arch = AttackArchetype.Light, dmg =  7, speedCost = 6f, impactNorm = 0.50f },
        };

        // Long-range "lunge" kicks. Every entry has meaningful baked
        // forward root motion (+0.6m–+1.5m per the motion probe) — when
        // played with applyRootMotion=true the kick carries the unit into
        // the defender at the strike frame. Each long-range kick combo is
        // a single-hit committed swing, fired by `GenerateLongRangeKickCombo`
        // with a `combat_skip_bwd` pre-strike clip that buys 0.5s of back-
        // step before the lunge.
        private static readonly HitTemplate[] _longRangeKickPool = new HitTemplate[]
        {
            new HitTemplate { id = "kick_axe",          arch = AttackArchetype.Heavy, dmg = 14, speedCost = 12f, impactNorm = 0.55f }, // KB_AxeKick +1.31m
            new HitTemplate { id = "kick_knee_R",       arch = AttackArchetype.Heavy, dmg = 12, speedCost = 10f, impactNorm = 0.50f }, // +1.53m
            new HitTemplate { id = "kick_knee_L",       arch = AttackArchetype.Heavy, dmg = 12, speedCost = 10f, impactNorm = 0.50f }, // +1.19m
            new HitTemplate { id = "kick_high_round_R", arch = AttackArchetype.Heavy, dmg = 13, speedCost = 11f, impactNorm = 0.50f }, // +0.58m
            new HitTemplate { id = "kick_high_round_L", arch = AttackArchetype.Heavy, dmg = 13, speedCost = 11f, impactNorm = 0.50f }, // +1.29m
            new HitTemplate { id = "kick_uppercut_R",   arch = AttackArchetype.Heavy, dmg = 13, speedCost = 11f, impactNorm = 0.50f }, // +1.24m
            new HitTemplate { id = "kick_side_L",       arch = AttackArchetype.Heavy, dmg = 12, speedCost = 10f, impactNorm = 0.50f }, // +1.05m
            new HitTemplate { id = "kick_back_R",       arch = AttackArchetype.Heavy, dmg = 13, speedCost = 11f, impactNorm = 0.50f }, // +0.89m
            new HitTemplate { id = "kick_roundhouse_R", arch = AttackArchetype.Heavy, dmg = 13, speedCost = 11f, impactNorm = 0.50f }, // +0.87m
        };

        [Header("Combo generation")]
        [Tooltip("Minimum number of hits in a randomly-generated combo.")]
        [SerializeField, Range(1, 12)] private int _comboMinHits = 3;
        [Tooltip("Maximum number of hits in a randomly-generated combo.")]
        [SerializeField, Range(1, 12)] private int _comboMaxHits = 7;
        [Tooltip("Time-gap between consecutive hits' impact frames in randomly-generated combos. Longer = clips play more fully.")]
        [SerializeField, Range(0.2f, 1.5f)] private float _comboInterHitGap = 0.7f;
        [Tooltip("Use the static `_combos` list (legacy) instead of generating random combos at PickCombo time. Off by default — random combos are the new normal.")]
        [SerializeField] private bool _useStaticCombos = false;
        [Tooltip("Per-commit chance the unit picks a long-range kick combo instead of the close-range mixed punch/kick combo. Long-range kicks back-step ~1m before lunging in — visually distinct from a close-range exchange.")]
        [SerializeField, Range(0f, 1f)] private float _longRangeKickChance = 0.25f;
        [Tooltip("Speed-pool minimum for the unit to pick a long-range kick. Below this, only close-range combos fire.")]
        [SerializeField, Range(0f, 100f)] private float _longRangeKickMinSpeed = 25f;

        public Combo PickCombo()
        {
            if (_useStaticCombos) return PickStaticCombo();
            // Roll for long-range kick pick: stance-aware (Aggressive +25%
            // bias, Defensive doesn't lunge in). Above the speed gate, the
            // unit can spend the kick's higher speed cost.
            BehaviorType bias = Stance != null ? Stance.behaviorBias : BehaviorType.Balanced;
            float chance = _longRangeKickChance;
            if (bias == BehaviorType.Aggressive) chance += 0.25f;
            else if (bias == BehaviorType.Defensive) chance = 0f;
            if (_currentSpeed >= _longRangeKickMinSpeed
                && UnityEngine.Random.value < chance)
            {
                return GenerateLongRangeKickCombo();
            }
            return GenerateRandomCombo();
        }

        /// <summary>
        /// Builds a single-hit long-range kick combo. The combo's
        /// `preStrikeClipId` plays a back-skip overlay during the
        /// pre-position window (~0.5s, orchestrator smoothsteps the
        /// attacker physically back ~0.8m). Then the kick clip plays
        /// with applyRootMotion enabled so its baked +0.9m–+1.5m forward
        /// motion lunges the unit back into the defender at the strike
        /// frame. desiredImpactDistance is set above current strike range
        /// (1.8m) so the smoothstep moves backward instead of forward.
        /// </summary>
        private Combo GenerateLongRangeKickCombo()
        {
            var t = _longRangeKickPool[UnityEngine.Random.Range(0, _longRangeKickPool.Length)];
            return new Combo
            {
                name = $"LungeKick({t.id})",
                desiredImpactDistance        = 1.8f,
                positionAdjustDuration       = 0.5f,
                interHitGap                  = _comboInterHitGap,
                preStrikeClipId              = "combat_skip_bwd",
                applyRootMotionOnFirstStrike = true,
                hits = new List<ComboHit>
                {
                    new ComboHit
                    {
                        attackId         = t.id,
                        archetype        = t.arch,
                        damage           = t.dmg,
                        speedCost        = t.speedCost,
                        impactNormalized = t.impactNorm,
                    }
                }
            };
        }

        /// <summary>
        /// Builds a fresh combo with a random number of hits (clamped to
        /// `[_comboMinHits, _comboMaxHits]`) drawn uniformly from the
        /// close-range hit pool. Each call returns a NEW Combo instance —
        /// the orchestrator stores it on the ExchangeHandle for the
        /// duration of the exchange. interHitGap is a single tunable so
        /// every hit has the same recovery window before the next.
        /// </summary>
        private Combo GenerateRandomCombo()
        {
            int minH = Mathf.Max(1, _comboMinHits);
            int maxH = Mathf.Max(minH, _comboMaxHits);
            int hitCount = UnityEngine.Random.Range(minH, maxH + 1); // inclusive
            var combo = new Combo
            {
                name                   = $"RandomCombo({hitCount})",
                desiredImpactDistance  = 1.0f,
                positionAdjustDuration = 0.15f,
                interHitGap            = _comboInterHitGap,
                hits                   = new List<ComboHit>(hitCount),
            };
            for (int i = 0; i < hitCount; i++)
            {
                var t = _closeRangeHitPool[UnityEngine.Random.Range(0, _closeRangeHitPool.Length)];
                combo.hits.Add(new ComboHit
                {
                    attackId         = t.id,
                    archetype        = t.arch,
                    damage           = t.dmg,
                    speedCost        = t.speedCost,
                    impactNormalized = t.impactNorm,
                });
            }
            return combo;
        }

        private Combo PickStaticCombo()
        {
            // Legacy path: pick from the inspector-edited `_combos` list,
            // weighted by stance / speed pool / energy. Available via the
            // `_useStaticCombos` toggle for cases where designed combos
            // matter (boss patterns, signature moves) more than variety.
            float bigCombo = Stance != null ? Stance.speedThresholdBigCombo : 65f;
            Combo best = null;
            foreach (var c in _combos)
            {
                if (c == null || c.hits == null || c.hits.Count == 0) continue;
                if (_currentSpeed < c.minSpeed) continue;
                if (_currentEnergy < c.minEnergy) continue;
                if (best == null || c.minSpeed > best.minSpeed) best = c;
            }
            if (best != null && best.hits.Count == 1 && _currentSpeed >= bigCombo)
            {
                foreach (var c in _combos)
                {
                    if (c.hits.Count > 1
                        && _currentSpeed >= c.minSpeed && _currentEnergy >= c.minEnergy)
                    {
                        best = c; break;
                    }
                }
            }
            return best;
        }

        public void OnAssignedAttacker(BattleH2HOrchestrator.ExchangeHandle h)
        {
            _justDefendedLastExchange = false;
            var def = h.defender as Component;
            if (def != null) FaceTarget(def.transform.position);
            if (h.combo == null || h.combo.hits.Count == 0) return;

            var first = h.combo.hits[0];
            if (!string.IsNullOrEmpty(h.combo.preStrikeClipId))
            {
                // Long-range kick path: play a back-skip overlay during the
                // pre-position window (orchestrator's smoothstep moves the
                // unit physically back), then crossfade into the strike
                // clip at t=positionAdjustDuration. The orchestrator
                // already schedules first-impact at `now + posAdj +
                // impactNorm × clipLen`, so the clip's impact frame lines
                // up with the scheduled impact callback.
                PlayLibraryClipOneShot(h.combo.preStrikeClipId,
                    fallback: "combat_skip_bwd",
                    lockMovement: false);
                StartCoroutine(PlayDelayedStrikeClip(
                    h.combo.positionAdjustDuration,
                    first.attackId,
                    h.combo.applyRootMotionOnFirstStrike));
            }
            else
            {
                PlayLibraryClipOneShot(first.attackId, fallback: "punch",
                    applyRootMotion: h.combo.applyRootMotionOnFirstStrike);
            }
            if (_orch == null || _orch.SkillCostsEnabled)
                _currentSpeed = Mathf.Max(0f, _currentSpeed - first.speedCost);
        }

        private IEnumerator PlayDelayedStrikeClip(float delaySec, string strikeId, bool applyRootMotion)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delaySec));
            if (this == null) yield break;
            PlayLibraryClipOneShot(strikeId, fallback: "punch",
                applyRootMotion: applyRootMotion);
        }

        public void OnAssignedDefender(BattleH2HOrchestrator.ExchangeHandle h)
        {
            var atk = h.attacker as Component;
            if (atk != null) FaceTarget(atk.transform.position);
        }

        public void OnExchangeImpactAttacker(BattleH2HOrchestrator.ExchangeHandle h, int hitIndex)
        {
            if (h.combo == null || hitIndex >= h.combo.hits.Count) return;
            // For chained hits beyond the first, queue the next clip just
            // before its impact frame so its strike pose lines up.
            if (hitIndex > 0)
            {
                var hit = h.combo.hits[hitIndex];
                PlayLibraryClipOneShot(hit.attackId, fallback: "punch");
                if (_orch == null || _orch.SkillCostsEnabled)
                    _currentSpeed = Mathf.Max(0f, _currentSpeed - hit.speedCost);
            }
            if (_debugLog) Debug.Log($"[H2HUnit {_displayName}] ATK strike #{hitIndex} '{h.combo.hits[hitIndex].attackId}'");
        }

        public bool OnExchangeImpactDefender(BattleH2HOrchestrator.ExchangeHandle h, int hitIndex)
        {
            if (h.combo == null || hitIndex >= h.combo.hits.Count) return false;
            var hit = h.combo.hits[hitIndex];

            // Counter check first (only on first hit — counters interrupt the chain).
            if (hitIndex == 0 && CounterChance > 0f && UnityEngine.Random.value < CounterChance)
            {
                if (H2HLogger.Instance != null)
                    H2HLogger.Instance.Log("DEFENSE", _displayName,
                        $"#{hitIndex} pick=Counter attack='{hit.attackId}' archetype={hit.archetype} dmg=0 hp={_currentHp:F0}/{_maxHp:F0}");
                return true;
            }

            var pick = PickDefenseResponse(hit.archetype);
            int incoming = hit.damage;
            int dmg = incoming;
            switch (pick)
            {
                case DefenderResponse.Block:
                    PlayLibraryClipOneShot(_blockClipId, fallback: "block");
                    dmg = Mathf.Max(0, Mathf.RoundToInt(dmg * 0.5f));
                    _currentEnergy = Mathf.Min(_maxEnergy, _currentEnergy + 5f);
                    break;
                case DefenderResponse.Dodge:
                {
                    // 4-way dodge pool: L / R / back-L / back-R. Pure L/R
                    // play KB_Dodge_L (-1.63m lat) / KB_Dodge_R (+2.15m lat)
                    // with root motion enabled so the defender physically
                    // slips out of the strike path. The back-L / back-R
                    // variants pre-rotate the unit ~30° so the same side-
                    // dodge clip's local-X root motion translates as a
                    // back-diagonal step in world space, then a coroutine
                    // restores facing toward the attacker once the clip
                    // ends. Result: visible variety — sometimes a clean
                    // side-step, sometimes a retreating diagonal.
                    int dodgeRoll = UnityEngine.Random.Range(0, 4); // 0=L 1=R 2=BL 3=BR
                    bool useLeft = (dodgeRoll == 0 || dodgeRoll == 2);
                    bool diagonalBack = (dodgeRoll == 2 || dodgeRoll == 3);
                    string dodgeId = useLeft ? "combat_dodge_l" : "combat_dodge_r";
                    if (diagonalBack)
                    {
                        // Rotate body away from the dodge direction:
                        //   left dodge  → turn body -30° (CCW) → -X is back-left
                        //   right dodge → turn body +30° (CW)  → +X is back-right
                        float deg = useLeft ? -30f : +30f;
                        transform.rotation *= Quaternion.Euler(0f, deg, 0f);
                        var atkComp = h.attacker as Component;
                        StartCoroutine(RestoreFacingAfter(0.55f, atkComp));
                    }
                    PlayLibraryClipOneShot(dodgeId, fallback: _dodgeClipId, applyRootMotion: true);
                    dmg = 0;
                    _currentSpeed = Mathf.Max(0f, _currentSpeed - 10f);
                    break;
                }
                default: // Eat
                {
                    // Pick a hit-react clip whose height + severity matches
                    // the incoming attack instead of always playing the
                    // generic `hit_react`. Uppercuts/high punches → high
                    // reacts; mid punches/elbows/mid kicks → mid reacts;
                    // low kicks → low reacts. Heavy archetype escalates
                    // weak → med, with a stagger chance on top. The library
                    // has 30+ KB_Hits variants wired by KuboldClipLibrarySetup
                    // — most of them never played until now.
                    string reactId = PickHitReactId(hit);
                    PlayLibraryClipOneShot(reactId, fallback: _hitReactClipId);
                    break;
                }
            }
            ApplyDamage(dmg);
            _justDefendedLastExchange = true;

            // Track hits absorbed for the disengage-trigger system. Dodge
            // doesn't count — the defender slipped out, no damage, no
            // "I just got beaten on" signal.
            if (pick != DefenderResponse.Dodge) _hitsAbsorbedInExchange++;

            // Hit-only impact FX (skip on dodge).
            if (pick != DefenderResponse.Dodge)
            {
                Vector3 hitPoint = transform.position + Vector3.up * 1.2f;
                FireImpactFX(hitPoint);
                var attacker = h.attacker as H2HUnit;
                if (attacker != null) attacker.ApplyHitStop(_hitStopSeconds);
                ApplyHitStop(_hitStopSeconds);
            }

            if (H2HLogger.Instance != null)
            {
                H2HLogger.Instance.Log("DEFENSE", _displayName,
                    $"#{hitIndex} pick={pick} attack='{hit.attackId}' archetype={hit.archetype} dmg={dmg}/{incoming} hp={_currentHp:F0}/{_maxHp:F0}");
            }
            return false;
        }

        public void OnExchangeResolved(BattleH2HOrchestrator.ExchangeHandle h, bool asAttacker, bool separating)
        {
            if (_debugLog)
                Debug.Log($"[H2HUnit {_displayName}] exchange resolved (asAttacker={asAttacker} separating={separating})");
        }

        /// <summary>
        /// Maps an incoming attack to one of the wired KB_Hits library ids.
        /// Height comes from the attack id (low_/mid_/high_/uppercut → high);
        /// direction is randomized between front/left/right (always relative
        /// to the defender's facing — defender is already FaceTarget'd to the
        /// attacker, so "front" reads as a forward hit). Severity escalates
        /// weak → med → stagger by archetype + a random roll.
        /// </summary>
        private string PickHitReactId(ComboHit hit)
        {
            string id = hit.attackId ?? "";
            string lower = id.ToLowerInvariant();

            // Height bucket.
            string height;
            if (lower.Contains("uppercut") || lower.Contains("high")) height = "high";
            else if (lower.Contains("low"))                            height = "low";
            else                                                       height = "mid";

            // Direction bucket — randomize for variety. The library only has
            // left/right variants for some heights; fall back to front.
            float dirRoll = UnityEngine.Random.value;
            string dir = dirRoll < 0.34f ? "front"
                       : dirRoll < 0.67f ? "left"
                       :                   "right";
            if (height == "low" && dir == "front") dir = "left"; // no low_front

            // Severity. Heavy archetype → med, with 25% chance of stagger.
            // Light → weak, with 10% chance of med if mid/high (no low staggers).
            bool heavy = hit.archetype == AttackArchetype.Heavy;
            float sevRoll = UnityEngine.Random.value;
            string sev;
            if (heavy)
            {
                sev = (sevRoll < 0.25f && height != "low") ? "stagger" : "med";
            }
            else
            {
                sev = (sevRoll < 0.10f && height != "low") ? "med" : "weak";
            }

            // Stagger only exists for high_front, high_back, mid_front,
            // mid_left, mid_right.
            if (sev == "stagger")
            {
                if (height == "high" && dir != "front" && dir != "back") dir = "front";
                if (height == "mid" && dir == "back") dir = "front";
            }

            // Compose: hit_{height}_{dir}_{sev}. Library ids match this pattern
            // (see KuboldClipLibrarySetup spec list). Falls back to
            // `_hitReactClipId` if the composed id isn't registered.
            return $"hit_{height}_{dir}_{sev}";
        }

        private DefenderResponse PickDefenseResponse(AttackArchetype archetype)
        {
            BehaviorType bias = Stance != null ? Stance.behaviorBias : BehaviorType.Balanced;
            float r = UnityEngine.Random.value;
            bool dodgeAllowed = _orch == null || _orch.DodgeEnabled;
            float dodgeReady = (dodgeAllowed && _currentEnergy >= 10f && _currentSpeed >= 15f) ? 1f : 0f;
            switch (bias)
            {
                case BehaviorType.Defensive:
                    // 65% Block, 12% Dodge, 23% Eat.
                    if (r < 0.65f) return DefenderResponse.Block;
                    if (r < 0.77f && dodgeReady > 0f) return DefenderResponse.Dodge;
                    return DefenderResponse.Eat;
                case BehaviorType.Aggressive:
                    // 18% Dodge, 82% Eat — dodging is unaggressive, trim it
                    // so Aggressive stance reads as "eats hits to keep
                    // pressure on" instead of dancing out of every swing.
                    if (r < 0.18f && dodgeReady > 0f) return DefenderResponse.Dodge;
                    return DefenderResponse.Eat;
                default:
                    // 50% Block, 15% Dodge, 35% Eat. Down from 30% Dodge —
                    // the 7-hit combos felt like the defender was slipping
                    // half the chain instead of trading.
                    if (r < 0.50f) return DefenderResponse.Block;
                    if (r < 0.65f && dodgeReady > 0f) return DefenderResponse.Dodge;
                    return DefenderResponse.Eat;
            }
        }

        // ── Damage + death ──────────────────────────────────────────

        public void ApplyDamage(int damage)
        {
            if (_isDead || damage <= 0) return;
            _currentHp = Mathf.Max(0f, _currentHp - damage);
            if (_currentHp <= 0f) HandleDeath();
        }

        private void HandleDeath()
        {
            if (_isDead) return;
            _isDead = true;
            _aiEnabled = false;

            // Cancel any active exchange this unit is in.
            var active = _orch != null ? _orch.GetActiveExchange(this) : null;
            if (active != null) _orch.CancelExchange(active, "death");

            // Pop to NotEngaged — we don't have a Death phase enum value, but
            // NotEngaged + IsDead is the same effective behavior (no AI, no
            // movement). H2HLogger logs the transition as the death marker.
            if (_phases != null) _phases.TransitionPhase(this, H2HPhase.NotEngaged, "death");

            // Knock the locomotion driver and movement controller off —
            // no more idle/walk decisions, no more cc.Move calls.
            if (_loco != null) _loco.SuppressFor(99f);
            if (_movement != null) { _movement.Stop(); _movement.SuppressFor(99f); }
            PlayLibraryClipOneShot(_deathClipId, fallback: "hit_react");

            OnDeath?.Invoke(this);
            if (H2HLogger.Instance != null)
                H2HLogger.Instance.Log("DEATH  ", _displayName, "HP=0 — unit down");
        }

        /// <summary>Resets the unit to a fightable state — clears IsDead,
        /// refills resources, re-enables AI, lets the locomotion driver
        /// resume, and pops the unit back to NotEngaged so it'll re-spot.
        /// Called by the training UI's Revive button and auto-fired by
        /// the HP slider when the user drags it above 0 on a dead unit.</summary>
        public void Revive()
        {
            _isDead = false;
            _aiEnabled = true;
            _currentHp = _maxHp;
            _currentEnergy = _maxEnergy * 0.5f;
            _currentSpeed = 30f;
            if (_loco != null) _loco.ClearSuppression();
            if (_movement != null) _movement.ClearSuppression();
            if (_phases != null) _phases.TransitionPhase(this, H2HPhase.NotEngaged, "revive");
            PlayLibraryClip("idle", null);
            if (H2HLogger.Instance != null)
                H2HLogger.Instance.Log("REVIVE ", _displayName, $"HP={_currentHp:F0} energy={_currentEnergy:F0} speed={_currentSpeed:F0}");
        }

        // ── Impact FX ───────────────────────────────────────────────

        public void ApplyHitStop(float seconds)
        {
            if (_animancer == null || _animancer.Graph == null) return;
            if (seconds <= 0f) return;
            StartCoroutine(HitStopCoroutine(seconds));
        }

        private IEnumerator HitStopCoroutine(float seconds)
        {
            float prev = _animancer.Graph.Speed;
            _animancer.Graph.Speed = 0f;
            yield return new WaitForSecondsRealtime(seconds);
            if (_animancer != null && _animancer.Graph != null)
                _animancer.Graph.Speed = prev;
        }

        public void FireImpactFX(Vector3 worldPos)
        {
            ShakeCamera();
            SpawnBurst(worldPos);
        }

        private void ShakeCamera()
        {
            if (_cameraShakeAmplitude <= 0f || _cameraShakeDuration <= 0f) return;
            var cam = Camera.main;
            if (cam == null) return;
            var shake = cam.GetComponent<H2HCameraShake>();
            if (shake == null) shake = cam.gameObject.AddComponent<H2HCameraShake>();
            shake.Kick(_cameraShakeAmplitude, _cameraShakeDuration);
        }

        private void SpawnBurst(Vector3 worldPos)
        {
            GameObject burst;
            if (_impactBurstPrefab != null)
            {
                burst = Instantiate(_impactBurstPrefab, worldPos, Quaternion.identity);
            }
            else
            {
                // Programmatic fallback: a brief glowing sphere (no Resources
                // dependency, works in any scene).
                burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                burst.transform.position = worldPos;
                burst.transform.localScale = Vector3.one * 0.4f;
                var col = burst.GetComponent<Collider>(); if (col != null) Destroy(col);
                var mr  = burst.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var m = new Material(Shader.Find("Sprites/Default"));
                    m.color = new Color(1f, 0.85f, 0.3f, 1f);
                    mr.sharedMaterial = m;
                }
            }
            Destroy(burst, 0.18f);
        }

        // ── Helpers ─────────────────────────────────────────────────

        public void FaceTarget(Vector3 worldPosition)
        {
            Vector3 dir = worldPosition - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        public void PlayLibraryClip(string id, string fallback)
        {
            if (_animancer == null || _library == null) return;
            if (string.IsNullOrEmpty(id)) id = fallback;
            if (string.IsNullOrEmpty(id)) return;
            if (_library.TryGet(id, out var t) && t != null) _animancer.Play(t);
            else if (!string.IsNullOrEmpty(fallback) && _library.TryGet(fallback, out var fb) && fb != null)
                _animancer.Play(fb);
        }

        /// <summary>
        /// Plays a library clip as a one-shot, with optional locomotion +
        /// movement suppression for `clip.length`. Strikes / hit-reacts /
        /// death use `lockMovement: true` so the unit doesn't drift through
        /// the defender. Decoration overlays (separation skip, post-exchange
        /// taunts) use `lockMovement: false` so the brain's intent-driven
        /// movement keeps providing physical displacement underneath the
        /// animation.
        ///
        /// When `applyRootMotion: true`, the clip's baked root motion is
        /// applied to the transform for the clip's duration — used for
        /// dodge clips so the defender physically slips out of the strike
        /// path. Resets back to false when the clip ends.
        /// </summary>
        public void PlayLibraryClipOneShot(string id, string fallback, bool lockMovement = true, bool applyRootMotion = false)
        {
            if (_animancer == null || _library == null) return;
            if (string.IsNullOrEmpty(id)) id = fallback;
            if (string.IsNullOrEmpty(id)) return;
            TransitionAsset t = null;
            if (!_library.TryGet(id, out t) || t == null)
            {
                if (string.IsNullOrEmpty(fallback) || !_library.TryGet(fallback, out t)) return;
            }
            var state = _animancer.Play(t);
            if (state?.Clip != null && lockMovement)
            {
                float len = state.Clip.length;
                if (_loco != null) _loco.SuppressFor(len);
                // Halt brain-driven movement during the strike so the unit
                // doesn't slide through the defender on root motion (`_m_`)
                // clips or drift forward on in-place strikes.
                if (_movement != null)
                {
                    _movement.Stop();
                    _movement.SuppressFor(len);
                }
            }
            else if (state?.Clip != null)
            {
                // Overlay path: still suppress the locomotion driver so it
                // doesn't fight the playing clip with a loop, but DO NOT
                // suppress the movement controller — physical motion comes
                // from the brain's intent, animation comes from this clip.
                if (_loco != null) _loco.SuppressFor(state.Clip.length);
            }

            // Optional root-motion application: turn it on now, schedule
            // a reset after the clip's nominal length. The locomotion
            // driver's `ApplyTurnInPlaceRootMotion` is suppressed during
            // this window (because we just suppressed `_loco`), so no
            // race with the per-frame flag toggle.
            if (applyRootMotion && state?.Clip != null && _animancer.Animator != null)
            {
                _animancer.Animator.applyRootMotion = true;
                StartCoroutine(ResetRootMotionAfter(state.Clip.length));
            }
        }

        private IEnumerator ResetRootMotionAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds));
            if (_animancer != null && _animancer.Animator != null)
                _animancer.Animator.applyRootMotion = false;
        }

        private IEnumerator RestoreFacingAfter(float seconds, Component target)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds));
            if (target != null && this != null) FaceTarget(target.transform.position);
        }

        // Default combo set if Inspector left empty — gives the system
        // something to swing with out of the box. Uses the Kubold library
        // ids wired by `KuboldClipLibrarySetup`. Each hit plays a DIFFERENT
        // clip so a 3-hit combo reads as jab → hook → uppercut visually
        // (rather than the same punch animation thrice). PlayLibraryClipOneShot
        // falls back to "punch" / "kick" if a specific id is missing.
        private void PopulateDefaultCombos()
        {
            _combos.Add(new Combo
            {
                name = "BasicJab",
                minSpeed = 0f,
                desiredImpactDistance = 1.0f,
                positionAdjustDuration = 0.12f,
                interHitGap = 0.3f,
                hits = new List<ComboHit>
                {
                    new ComboHit { attackId = "attack_punch_jab", archetype = AttackArchetype.Light, impactNormalized = 0.45f, damage = 8, speedCost = 5f },
                }
            });
            _combos.Add(new Combo
            {
                name = "OneTwoUppercut",
                minSpeed = 35f,
                minEnergy = 10f,
                desiredImpactDistance = 1.0f,
                positionAdjustDuration = 0.15f,
                // interHitGap timed to roughly the clip's recovery so each
                // distinct swing visually resolves before the next begins.
                interHitGap = 0.45f,
                hits = new List<ComboHit>
                {
                    new ComboHit { attackId = "attack_punch_jab",      archetype = AttackArchetype.Light, impactNormalized = 0.40f, damage = 6, speedCost = 5f },
                    new ComboHit { attackId = "attack_punch_hook",     archetype = AttackArchetype.Light, impactNormalized = 0.45f, damage = 6, speedCost = 5f },
                    new ComboHit { attackId = "attack_punch_uppercut", archetype = AttackArchetype.Heavy, impactNormalized = 0.55f, damage = 10, speedCost = 12f },
                }
            });
            _combos.Add(new Combo
            {
                name = "AxeKick",
                minSpeed = 25f,
                desiredImpactDistance = 1.3f,
                positionAdjustDuration = 0.18f,
                interHitGap = 0.3f,
                hits = new List<ComboHit>
                {
                    new ComboHit { attackId = "attack_kick_crescent",  archetype = AttackArchetype.Heavy, impactNormalized = 0.55f, damage = 14, speedCost = 12f },
                }
            });
            _combos.Add(new Combo
            {
                name = "DoubleKickCombo",
                minSpeed = 50f,
                minEnergy = 15f,
                desiredImpactDistance = 1.3f,
                positionAdjustDuration = 0.18f,
                interHitGap = 0.5f,
                hits = new List<ComboHit>
                {
                    new ComboHit { attackId = "kick_low_L",           archetype = AttackArchetype.Light, impactNormalized = 0.45f, damage = 6, speedCost = 6f },
                    new ComboHit { attackId = "kick_high_round_R",    archetype = AttackArchetype.Heavy, impactNormalized = 0.50f, damage = 14, speedCost = 14f },
                }
            });
        }

        private void OnValidate()
        {
            if (_currentHp > _maxHp) _currentHp = _maxHp;
            if (_currentEnergy > _maxEnergy) _currentEnergy = _maxEnergy;
        }
    }
}
