using UnityEngine;
using UnityEngine.InputSystem;

namespace SuperInseto
{
    // Gameplay consumes actions, never Keyboard.current: bindings can be extended later.
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        InputActionMap map;
        InputAction move, look, sprint, walk, crouch, jump, climb, releaseCursor, captureCursor;
        public Vector2 Move => Captured ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => Captured ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool Sprint => Captured && sprint.IsPressed();
        public bool Walk => Captured && walk.IsPressed();
        public bool Crouch => Captured && crouch.IsPressed();
        public bool JumpPressed => Captured && jump.WasPressedThisFrame();
        public bool ClimbPressed => Captured && climb.WasPressedThisFrame();
        public bool Captured => Cursor.lockState == CursorLockMode.Locked;

        void Awake()
        {
            map = new InputActionMap("SuperInseto");
            move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            look = map.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            sprint = map.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            walk = map.AddAction("Walk", InputActionType.Button, "<Keyboard>/leftAlt");
            crouch = map.AddAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
            crouch.AddBinding("<Keyboard>/c");
            jump = map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            climb = map.AddAction("Climb", InputActionType.Button, "<Keyboard>/e");
            releaseCursor = map.AddAction("ReleaseCursor", InputActionType.Button, "<Keyboard>/escape");
            captureCursor = map.AddAction("CaptureCursor", InputActionType.Button, "<Mouse>/leftButton");
        }

        void OnEnable() { map.Enable(); SetCapture(true); }
        void OnDisable() { map.Disable(); SetCapture(false); }
        void OnDestroy() { map.Dispose(); }
        void Update()
        {
            if (releaseCursor.WasPressedThisFrame()) SetCapture(false);
            else if (captureCursor.WasPressedThisFrame()) SetCapture(true);
        }
        void OnApplicationFocus(bool focused) { if (!focused) SetCapture(false); }
        static void SetCapture(bool capture)
        {
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }
    }
}
