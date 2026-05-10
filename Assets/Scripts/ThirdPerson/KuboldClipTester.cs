using Animancer;
using TacticalRPG.Systems.Combat;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// Standalone bring-up tester for the 8 Kubold clips wired by
    /// `TacticalRPG/Kubold/Setup Test Clip Library`. Drop on a humanoid prefab
    /// (HeroPrefab) that already has an AnimancerComponent, point at the
    /// generated library, and press number keys 1–8 in Play mode.
    ///
    /// Independent of the move-engine / brains — its job is to prove the
    /// retargeting works on the Mixamo skeleton before we wire any of it
    /// into the combat pipeline.
    /// </summary>
    [RequireComponent(typeof(AnimancerComponent))]
    public class KuboldClipTester : MonoBehaviour
    {
        [Tooltip("Library produced by TacticalRPG/Kubold/Setup Test Clip Library.")]
        [SerializeField] private BattleAnimancerClipLibrary _library;

        [Tooltip("Auto-play the idle clip on Start so the prefab doesn't sit in T-pose.")]
        [SerializeField] private bool _autoPlayIdleOnStart = true;

        [Tooltip("Show the on-screen legend + 'now playing' label.")]
        [SerializeField] private bool _showOverlay = true;

        // Order matters — index = (number key − 1).
        private static readonly string[] HotkeyIds = new[]
        {
            "idle",
            "walk_forward",
            "run_forward",
            "punch",
            "kick",
            "block",
            "dodge",
            "hit_react",
        };

        private AnimancerComponent _animancer;
        private string _lastPlayed;

        private void Awake()
        {
            _animancer = GetComponent<AnimancerComponent>();

            // Same reason as TrainingDummyController.Awake — HeroPrefab's
            // Animator Controller would block Animancer's output on a
            // Humanoid rig. Clear it so the test bench's clips actually
            // drive the rig.
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Animancer.OptionalWarning.NativeControllerHumanoid.Disable();
#endif
        }

        private void Start()
        {
            if (_autoPlayIdleOnStart) Play("idle");
        }

        private void Update()
        {
            int idx = ReadDigit();
            if (idx >= 1 && idx <= HotkeyIds.Length) Play(HotkeyIds[idx - 1]);
        }

        public void Play(string id)
        {
            if (_animancer == null || _library == null) return;
            if (!_library.TryGet(id, out var transition) || transition == null)
            {
                Debug.LogWarning($"[KuboldClipTester] Library has no entry for '{id}'.");
                return;
            }

            _animancer.Play(transition);
            // Looping is controlled by the underlying clip's import settings
            // (KB_Idle_1 / KB_WalkFwd1 / RunFwdLoop are imported as loops).
            // One-shots (punch/kick/block/dodge/hit) play once and hold the
            // last frame — press 1 to return to idle.
            _lastPlayed = id;
        }

        private static int ReadDigit()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return 0;
            if (kb.digit1Key.wasPressedThisFrame) return 1;
            if (kb.digit2Key.wasPressedThisFrame) return 2;
            if (kb.digit3Key.wasPressedThisFrame) return 3;
            if (kb.digit4Key.wasPressedThisFrame) return 4;
            if (kb.digit5Key.wasPressedThisFrame) return 5;
            if (kb.digit6Key.wasPressedThisFrame) return 6;
            if (kb.digit7Key.wasPressedThisFrame) return 7;
            if (kb.digit8Key.wasPressedThisFrame) return 8;
            if (kb.numpad1Key.wasPressedThisFrame) return 1;
            if (kb.numpad2Key.wasPressedThisFrame) return 2;
            if (kb.numpad3Key.wasPressedThisFrame) return 3;
            if (kb.numpad4Key.wasPressedThisFrame) return 4;
            if (kb.numpad5Key.wasPressedThisFrame) return 5;
            if (kb.numpad6Key.wasPressedThisFrame) return 6;
            if (kb.numpad7Key.wasPressedThisFrame) return 7;
            if (kb.numpad8Key.wasPressedThisFrame) return 8;
            return 0;
#else
            for (int k = 1; k <= 8; k++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + k) || Input.GetKeyDown(KeyCode.Keypad0 + k))
                    return k;
            return 0;
#endif
        }

        private void OnGUI()
        {
            if (!_showOverlay) return;
            const int W = 260, H = 210;
            GUI.Box(new Rect(10, 10, W, H), "Kubold Clip Tester");
            int y = 32;
            for (int i = 0; i < HotkeyIds.Length; i++)
            {
                bool playing = HotkeyIds[i] == _lastPlayed;
                string mark = playing ? "▶" : " ";
                GUI.Label(new Rect(20, y, W - 20, 20), $"{mark} {i + 1} — {HotkeyIds[i]}");
                y += 20;
            }
            if (_library == null)
                GUI.Label(new Rect(20, y, W - 20, 20), "(no library assigned)");
        }
    }
}
