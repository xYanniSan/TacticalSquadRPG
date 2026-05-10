using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalRPG.ThirdPerson
{
    /// <summary>
    /// WASD movement for the H2H training scene. Routes through
    /// `H2HMovementController.SetMoveIntent` instead of calling
    /// `CharacterController.Move` directly so the locomotion driver,
    /// turn-in-place clips, start/stop foot-plant clips, and the
    /// movement controller's accel/decel curves all engage correctly.
    ///
    /// Hold Shift to run; otherwise walks. Hold Ctrl to creep at
    /// half-walk speed. The character rotates to face the movement
    /// direction via `Movement.FaceTowards`. Cursor lock toggles
    /// with Escape so the on-screen UI buttons remain clickable.
    ///
    /// Disabling the unit's AI (`H2HUnit.AIEnabled = false`) is the
    /// recommended way to take manual control — the brain's
    /// SetMoveIntent calls would otherwise overwrite ours every frame.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TrainingPlayerController : MonoBehaviour
    {
        [Header("Movement speeds (m/s)")]
        [SerializeField] private float _creepSpeed  = 1.0f;
        [SerializeField] private float _walkSpeed   = 2.5f;
        [SerializeField] private float _runSpeed    = 6.0f;
        [SerializeField] private float _sprintSpeed = 8.0f;

        [Header("Camera reference (auto-found if blank)")]
        [SerializeField] private Transform _cameraTransform;

        [Header("H2H phase clamp")]
        [Tooltip("If true and a KuboldLocomotionDriver is present, clamp WASD speed to the unit's H2H phase max (combat / disengage / traversal).")]
        [SerializeField] private bool _clampToH2HPhase = true;

        [Header("Auto-disable AI when player input is detected")]
        [Tooltip("On the first WASD/Shift/Ctrl press, switch the unit's AI off so the brain stops fighting input.")]
        [SerializeField] private bool _autoDisableAI = true;

        private CharacterController     _cc;
        private KuboldLocomotionDriver  _loco;
        private H2HMovementController   _movement;
        private H2HUnit                 _h2h;
        private bool                    _autoDisableFired;
        private bool                    _wasInputHeld;

        private void Start()
        {
            _cc       = GetComponent<CharacterController>();
            _loco     = GetComponent<KuboldLocomotionDriver>();
            _movement = GetComponent<H2HMovementController>();
            _h2h      = GetComponent<H2HUnit>();
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
            // Lock the cursor on entry so ThirdPersonCamera's mouse-orbit
            // works immediately. Press Escape to free the cursor for the
            // on-screen UI buttons; press Escape again to re-lock and
            // resume orbiting.
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            HandleCursorToggle();
            HandleMovement();
        }

        private void HandleCursorToggle()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame)
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
        }

        private void HandleMovement()
        {
            if (_cc == null || !_cc.enabled) return;
            if (_movement == null) return;          // no controller → no manual control

            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            var kb = Keyboard.current;
            Vector2 input = Vector2.zero;
            bool sprintHeld = false, runHeld = false, creepHeld = false;
            if (kb != null)
            {
                if (kb.wKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed) input.y -= 1f;
                if (kb.aKey.isPressed) input.x -= 1f;
                if (kb.dKey.isPressed) input.x += 1f;
                runHeld    = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                sprintHeld = kb.spaceKey.isPressed;
                creepHeld  = kb.leftCtrlKey.isPressed  || kb.rightCtrlKey.isPressed;
            }
            input = Vector2.ClampMagnitude(input, 1f);

            bool anyInput = input.sqrMagnitude > 0.01f || sprintHeld || runHeld || creepHeld;

            // First time we detect input, optionally disable AI so the
            // brain stops calling SetMoveIntent on top of ours, and clear
            // suppression so the controller / loco driver respond to our
            // intent immediately (otherwise lingering Exchange suppression
            // makes the first ~5 seconds of WASD do nothing visible).
            if (_autoDisableAI && !_autoDisableFired && anyInput)
            {
                _autoDisableFired = true;
                if (_h2h != null) _h2h.AIEnabled = false;
                _movement.ClearSuppression();
                if (_loco != null) _loco.ClearSuppression();
            }

            // Yield to a one-shot only if we have nothing better to do —
            // any active input clears suppression above, so this only
            // returns early during a strike clip with no input.
            if (_movement.IsSuppressed) return;

            // CRITICAL: when no input is held this frame, do NOT call
            // SetMoveIntent. Doing so every frame would clobber whatever
            // is legitimately driving the unit (the brain when AI is on,
            // a Manual Move button click, an external system). We only
            // touch intent in two cases:
            //   1. Input is held: drive intent (overrides everything)
            //   2. Input was held last frame but released this frame:
            //      issue a single Stop() so the unit decelerates from
            //      the WASD motion. After that, sit silent.
            if (input.sqrMagnitude < 0.01f)
            {
                if (_wasInputHeld)
                {
                    _wasInputHeld = false;
                    _movement.Stop();
                }
                return;
            }

            _wasInputHeld = true;

            // Speed band selection. Sprint > Run > Creep > Walk (default).
            float speed = sprintHeld ? _sprintSpeed
                        : runHeld    ? _runSpeed
                        : creepHeld  ? _creepSpeed
                        :              _walkSpeed;

            // Clamp to the H2H phase max if requested AND the brain still
            // owns the unit. Once the user takes manual control we let
            // them drive the full speed range, otherwise WASD in Engaged
            // can't push above 1.5 m/s and feels broken for testing.
            if (_clampToH2HPhase && _loco != null
                && _h2h != null && _h2h.AIEnabled)
            {
                float phaseMax = _loco.ResolvePhaseMaxSpeed();
                if (phaseMax > 0f && speed > phaseMax) speed = phaseMax;
            }

            // Compose move direction in world space, relative to camera
            // when one is available so WASD is screen-relative.
            Vector3 moveDir;
            if (_cameraTransform != null)
            {
                Vector3 fwd = _cameraTransform.forward; fwd.y = 0f; fwd.Normalize();
                Vector3 rgt = _cameraTransform.right;   rgt.y = 0f; rgt.Normalize();
                moveDir = fwd * input.y + rgt * input.x;
            }
            else
            {
                moveDir = new Vector3(input.x, 0f, input.y);
            }
            if (moveDir.sqrMagnitude < 0.0001f) return;

            // Hand intent + facing target to the controller. The controller
            // smoothly accelerates current velocity toward (moveDir * speed)
            // and lerps rotation to face moveDir. The locomotion driver
            // picks the matching standing/run/sprint loop based on the
            // resulting velocity.
            _movement.SetMoveIntent(moveDir, speed);
            _movement.FaceTowards(transform.position + moveDir.normalized * 5f);
        }
    }
}
