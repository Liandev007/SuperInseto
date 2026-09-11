using UnityEngine;

namespace SuperInseto
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerInputReader input;
        [SerializeField, Min(0.01f)] float sensitivity = 0.12f;
        [SerializeField, Min(0.5f)] float distance = 4.2f;
        [SerializeField, Min(0.1f)] float targetHeight = 1.45f;
        [SerializeField, Range(-85f, 0f)] float minPitch = -35f;
        [SerializeField, Range(0f, 85f)] float maxPitch = 70f;
        [SerializeField, Min(0.1f)] float collisionRadius = 0.25f;
        [SerializeField, Min(0.01f)] float collisionPadding = 0.05f;
        [SerializeField, Min(0.1f)] float recoverySpeed = 5f;
        [SerializeField] LayerMask obstacleMask = ~4;
        CharacterController body;
        float yaw, pitch = 15f, currentDistance;

        void Awake()
        {
            if (!target || !input)
            {
                Debug.LogError("ThirdPersonCamera requires target and input.", this);
                enabled = false;
                return;
            }
            body = target.GetComponent<CharacterController>();
            yaw = target.eulerAngles.y;
            currentDistance = distance;
            GetComponent<Camera>().nearClipPlane = 0.05f;
        }

        void Update()
        {
            // Mouse delta is already a per-frame displacement; no deltaTime multiplier.
            yaw += input.Look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - input.Look.y * sensitivity, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        void LateUpdate()
        {
            Vector3 pivot = target.position + Vector3.up * (body ? Mathf.Min(targetHeight, body.height * 0.8f) : targetHeight);
            Vector3 backward = -(Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward);
            float allowed = distance;
            if (Physics.SphereCast(pivot, collisionRadius, backward, out var hit, distance,
                    obstacleMask, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(0f, hit.distance - collisionPadding);
            // If the pivot itself is inside geometry, collapse instead of trusting a missed cast.
            if (Physics.CheckSphere(pivot, collisionRadius, obstacleMask, QueryTriggerInteraction.Ignore)) allowed = 0f;
            currentDistance = allowed < currentDistance ? allowed : Mathf.MoveTowards(currentDistance, allowed, recoverySpeed * Time.deltaTime);
            transform.SetPositionAndRotation(pivot + backward * currentDistance, Quaternion.Euler(pitch, yaw, 0f));
        }
    }
}
