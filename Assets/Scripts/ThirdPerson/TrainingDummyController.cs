using Animancer;
using TacticalRPG.Systems.Combat;
using UnityEngine;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Buttons-only test driver for the TrainingDummy scene. Plays clips from
    /// a `BattleAnimancerClipLibrary` on demand — no AI, no engine, no input.
    /// The 8 public Play* methods are bound to UI buttons in
    /// `TrainingDummy.unity`; everything else just reports state for the
    /// debug overlay.
    /// </summary>
    [RequireComponent(typeof(AnimancerComponent))]
    public class TrainingDummyController : MonoBehaviour
    {
        [SerializeField] private BattleAnimancerClipLibrary _library;
        [SerializeField] private bool _autoPlayIdleOnStart = true;

        /// <summary>Public read-access used by H2HUnit's runtime library fallback
        /// when the Dummy's H2HUnit was authored without an explicit library
        /// reference (the Dummy GameObject originally used this controller's
        /// library and the H2H setup never copied it over).</summary>
        public BattleAnimancerClipLibrary Library => _library;

        private AnimancerComponent _animancer;

        /// <summary>Logical id of the last button pressed (or "idle" on autoplay).</summary>
        public string CurrentAction { get; private set; } = "(none)";

        /// <summary>Underlying clip name on Animancer's current state.</summary>
        public string CurrentAnimation
        {
            get
            {
                if (_animancer == null) return "(none)";
                var state = _animancer.States.Current;
                if (state == null) return "(none)";
                return state.Clip != null ? state.Clip.name : "(none)";
            }
        }

        private void Awake()
        {
            _animancer = GetComponent<AnimancerComponent>();

            // HeroPrefab ships with an Animator Controller for the combat
            // path. On a Humanoid rig, Animancer can't blend over a native
            // controller — the controller wins and Animancer's clips never
            // visibly drive the rig. Clear it in the test scene so the
            // playable graph owns the output.
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = null;
                // Also disable root motion. With it on, the Animator drives
                // the character's translation from the active clip's root
                // curves — which would override / fight the player
                // controller's CharacterController.Move calls and pin the
                // body in idle's near-zero motion regardless of input.
                animator.applyRootMotion = false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Animancer.OptionalWarning.NativeControllerHumanoid.Disable();
#endif
        }

        private void Start()
        {
            if (_autoPlayIdleOnStart) PlayClip("idle");
        }

        // Each public method matches one UI button. Names kept terse so the
        // wiring in `TrainingDummyUI` reads cleanly.
        public void PlayIdle()     => PlayClip("idle");
        public void PlayWalk()     => PlayClip("walk_forward");
        public void PlayRun()      => PlayClip("run_forward");
        public void PlayPunch()    => PlayClip("punch");
        public void PlayKick()     => PlayClip("kick");
        public void PlayBlock()    => PlayClip("block");
        public void PlayDodge()    => PlayClip("dodge");
        public void PlayHitReact() => PlayClip("hit_react");

        // ── Ragdoll test hooks (wired to extra UI buttons) ──────────

        [Header("Ragdoll test (optional)")]
        [Tooltip("Chest-aimed backward impulse strength when 'Punch Chest' is pressed.")]
        [SerializeField] private float _impulseForce = 180f;

        public void RagdollImpulseChest()
        {
            var d = GetComponent<ActiveRagdollDriver>();
            if (d == null) return;
            // Skill-style "knockdown punch": activate ragdoll first (so the
            // body goes physical), then apply impulse + a small upward kick
            // so it doesn't just slide along the ground.
            d.ActivateRagdoll();
            Vector3 force = -transform.forward * _impulseForce + Vector3.up * (_impulseForce * 0.3f);
            d.ApplyImpulse("Chest", force);
            CurrentAction = "ragdoll_punch_chest";
        }

        public void RagdollGoLimp()
        {
            var d = GetComponent<ActiveRagdollDriver>();
            if (d == null) return;
            d.GoLimp();
            CurrentAction = "ragdoll_limp";
        }

        public void RagdollRecover()
        {
            var d = GetComponent<ActiveRagdollDriver>();
            if (d == null) return;
            d.Recover();
            CurrentAction = "ragdoll_recover";
        }

        public void PlayClip(string id)
        {
            if (_animancer == null || _library == null) return;
            if (!_library.TryGet(id, out var transition) || transition == null)
            {
                Debug.LogWarning($"[TrainingDummyController] Library has no '{id}'.");
                return;
            }
            var state = _animancer.Play(transition);
            CurrentAction = id;

            // If a `KuboldLocomotionDriver` is on the same unit, tell it to
            // stand down for the clip's length so this one-shot isn't
            // immediately overridden by idle/walk/run on the next frame.
            var loco = GetComponent<KuboldLocomotionDriver>();
            if (loco != null && state != null && state.Clip != null)
                loco.SuppressFor(state.Clip.length);
        }
    }
}
