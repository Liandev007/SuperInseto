using UnityEngine;

namespace SuperInseto
{
    // After camera input (-100), before locomotion (0): exactly one consumer of E.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(PlayerInputReader), typeof(AccessCredentials))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField, Min(0.5f)] float reach = 2.4f;
        [SerializeField, Range(1f, 45f)] float aimHalfAngle = 18f;
        [SerializeField, Min(0.5f)] float messageDuration = 3f;
        [SerializeField] LayerMask geometryMask = ~4;
        readonly InteractionProbe probe = new InteractionProbe();
        PlayerInputReader input;
        AccessCredentials access;
        WallClimber climber;
        CharacterController body;
        PlayerMotor motor;
        Interactable target;
        string message;
        float messageUntil;
        GUIStyle style;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            access = GetComponent<AccessCredentials>();
            climber = GetComponent<WallClimber>();
            body = GetComponent<CharacterController>();
            motor = GetComponent<PlayerMotor>();
            if (!view) view = Camera.main;
            if (!view) { Debug.LogError("PlayerInteractor requires a camera.", this); enabled = false; }
        }

        void Update()
        {
            target = null;
            if (!input.Captured || (climber && climber.Attached) || (motor && motor.ActionLocked)) return;
            Vector3 origin = transform.position + Vector3.up * (body ? body.height * 0.6f : 1f);
            target = probe.Find(origin, view.transform, reach, aimHalfAngle, geometryMask);
            if (!target || !input.InteractPressed) return;
            input.ConsumeInteraction();
            message = target.Interact(access);
            messageUntil = Time.unscaledTime + messageDuration;
        }

        void OnGUI()
        {
            if (!input || !input.Captured) return;
            if (style == null) style = new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            GUI.Label(new Rect(Screen.width * 0.5f - 5f, Screen.height * 0.5f - 10f, 20f, 20f), "+");
            float width = Mathf.Min(620f, Screen.width - 24f);
            if (target) GUI.Box(new Rect((Screen.width - width) / 2f, Screen.height - 115f, width, 38f), target.Prompt, style);
            if (Time.unscaledTime < messageUntil)
                GUI.Box(new Rect((Screen.width - width) / 2f, Screen.height - 70f, width, 48f), message, style);
            GUI.Box(new Rect(12f, 12f, 310f, 64f), "PROTÓTIPO M2\nMire nos painéis coloridos e pressione E.", style);
        }
    }
}
