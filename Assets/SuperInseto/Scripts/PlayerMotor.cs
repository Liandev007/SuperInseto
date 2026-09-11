using UnityEngine;

namespace SuperInseto
{
    public enum MovementState { Grounded, Airborne, Crouched, WallAttached, WallClimbing, Mantling }

    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader), typeof(WallClimber))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraTransform;
        [Tooltip("Visual child only; replace its contents with the final model. Keep the controller root unscaled.")]
        [SerializeField] Transform visualRoot;
        [Header("Locomotion (metres / second)")]
        [SerializeField, Min(0f)] float walkSpeed = 2f;
        [SerializeField, Min(0f)] float runSpeed = 4.2f;
        [SerializeField, Min(0f)] float sprintSpeed = 6.5f;
        [SerializeField, Min(0f)] float crouchSpeed = 1.6f;
        [SerializeField, Min(0.1f)] float acceleration = 22f;
        [SerializeField, Min(0.1f)] float deceleration = 28f;
        [SerializeField, Range(0f, 1f)] float airControl = 0.65f;
        [SerializeField, Min(1f)] float rotationSpeed = 720f;
        [Header("Jump and gravity")]
        [SerializeField, Min(0.1f)] float jumpHeight = 1.15f;
        [SerializeField, Min(0.1f)] float gravity = 22f;
        [SerializeField, Min(1f)] float terminalSpeed = 35f;
        [Header("Crouch")]
        [SerializeField, Min(0.8f)] float crouchedHeight = 1.1f;
        [SerializeField] LayerMask obstructionMask = ~4;
        [SerializeField] MovementState state;
        public MovementState State => state;
        CharacterController body;
        PlayerInputReader input;
        WallClimber climber;
        Vector3 planarVelocity;
        float verticalVelocity, standingHeight, standingStep;
        bool crouched;
        readonly Collider[] overlaps = new Collider[16];

        void Awake()
        {
            body = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            climber = GetComponent<WallClimber>();
            standingHeight = body.height;
            standingStep = body.stepOffset;
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
            if (!cameraTransform)
            {
                Debug.LogError("PlayerMotor needs a camera reference.", this);
                enabled = false;
            }
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;
            if (climber.Attached)
            {
                body.stepOffset = 0f;
                climber.Tick(input.Move, input.ClimbPressed || input.JumpPressed || input.Crouch, dt);
                planarVelocity = Vector3.zero;
                verticalVelocity = 0f;
                if (climber.Attached)
                {
                    state = climber.Mantling ? MovementState.Mantling
                        : climber.Moving ? MovementState.WallClimbing : MovementState.WallAttached;
                    return;
                }
            }

            bool grounded = body.isGrounded;
            UpdateCrouch(grounded);
            if (!crouched && input.ClimbPressed && climber.TryAttach())
            {
                planarVelocity = Vector3.zero;
                verticalVelocity = 0f;
                state = MovementState.WallAttached;
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 direction = Vector3.ClampMagnitude(forward * input.Move.y + right * input.Move.x, 1f);
            float speed = crouched ? crouchSpeed : input.Walk ? walkSpeed : input.Sprint ? sprintSpeed : runSpeed;
            float rate = direction.sqrMagnitude > 0.001f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, direction * speed, rate * (grounded ? 1f : airControl) * dt);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * dt);

            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (grounded && !crouched && input.JumpPressed)
                verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            verticalVelocity = Mathf.Max(verticalVelocity - gravity * dt, -terminalSpeed);
            body.stepOffset = grounded && verticalVelocity <= 0f ? standingStep : 0f;
            CollisionFlags flags = body.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f) verticalVelocity = -2f;
            state = body.isGrounded ? (crouched ? MovementState.Crouched : MovementState.Grounded) : MovementState.Airborne;
        }

        void UpdateCrouch(bool grounded)
        {
            bool wanted = input.Crouch && (grounded || crouched);
            if (!wanted && crouched && !CanStand()) wanted = true;
            if (wanted == crouched) return;
            crouched = wanted;
            body.height = crouched ? Mathf.Clamp(crouchedHeight, body.radius * 2f, standingHeight) : standingHeight;
            body.center = Vector3.up * (body.height * 0.5f); // Feet remain fixed.
            if (visualRoot) visualRoot.localScale = new Vector3(1f, body.height / standingHeight, 1f);
        }

        bool CanStand()
        {
            float radius = Mathf.Max(0.05f, body.radius - body.skinWidth);
            Vector3 bottom = transform.position + Vector3.up * (body.radius + body.skinWidth);
            Vector3 top = transform.position + Vector3.up * (standingHeight - body.radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, obstructionMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (!overlaps[i].transform.IsChildOf(transform)) return false;
            return true;
        }
    }
}
