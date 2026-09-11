using UnityEngine;

namespace SuperInseto
{
    // Owns only the validated ledge trajectory. The controller stays enabled throughout.
    [RequireComponent(typeof(CharacterController))]
    public sealed class LedgeMantle : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float ledgeSearchHeight = 0.65f;
        [SerializeField, Min(0.4f)] float landingDepth = 0.75f;
        [SerializeField, Min(0.01f)] float safetySpace = 0.025f;
        [SerializeField, Min(0.05f)] float liftClearance = 0.1f;
        [SerializeField, Min(0.2f)] float duration = 0.85f;
        [SerializeField, Range(0f, 30f)] float maxTopSlope = 10f;
        [SerializeField] LayerMask geometryMask = ~4;
        CharacterController body;
        ClimbableSurface source;
        Vector3 start, raised, landing;
        float elapsed;
        public bool Active { get; private set; }

        void Awake() { body = GetComponent<CharacterController>(); }

        public bool TryBegin(RaycastHit wall)
        {
            Vector3 inward = Vector3.ProjectOnPlane(-wall.normal, Vector3.up).normalized;
            Vector3 probe = wall.point + inward * Mathf.Max(landingDepth, body.radius + safetySpace * 2f);
            probe.y = transform.position.y + body.height * 0.6f + ledgeSearchHeight;
            if (!Physics.Raycast(probe, Vector3.down, out var top, ledgeSearchHeight + 0.2f,
                    geometryMask, QueryTriggerInteraction.Ignore)
                || Vector3.Angle(top.normal, Vector3.up) > maxTopSlope) return false;

            start = transform.position;
            landing = new Vector3(top.point.x, top.point.y + liftClearance, top.point.z);
            raised = new Vector3(start.x, landing.y, start.z);
            if (raised.y <= start.y || !Supported(landing)
                || !ClearPose(raised) || !ClearPose(landing)
                || !ClearSegment(start, raised) || !ClearSegment(raised, landing)) return false;
            source = wall.collider.GetComponentInParent<ClimbableSurface>();
            elapsed = 0f;
            Active = true;
            return true;
        }

        // Returns true only once the physical controller actually lands.
        public bool Tick(float dt)
        {
            if (!Active) return false;
            if (!source || !source.isActiveAndEnabled || !Supported(landing))
            { Cancel(); return false; }
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / duration);
            // Lift outside the face first, then cross the edge; never cut through the wall.
            Vector3 next = t < 0.55f
                ? Vector3.Lerp(start, raised, Mathf.SmoothStep(0f, 1f, t / 0.55f))
                : Vector3.Lerp(raised, landing, Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.45f));
            if (!ClearPose(next) || !ClearSegment(transform.position, next))
            { Cancel(); return false; }
            body.Move(next - transform.position);
            if (Vector3.Distance(transform.position, next) > body.skinWidth + 0.02f)
            { Cancel(); return false; }
            if (t < 1f) return false;
            CollisionFlags flags = body.Move(Vector3.down * (liftClearance + body.skinWidth + 0.05f));
            Cancel();
            return (flags & CollisionFlags.Below) != 0;
        }

        public void Cancel() { Active = false; }
        void OnDisable() { Cancel(); }

        bool Supported(Vector3 feet)
        {
            // Centre and four footprint extremes reject narrow tops and unsupported landings.
            float extent = body.radius + safetySpace;
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = i == 0 ? Vector3.zero : i == 1 ? Vector3.right * extent
                    : i == 2 ? Vector3.left * extent : i == 3 ? Vector3.forward * extent : Vector3.back * extent;
                if (!Physics.Raycast(feet + offset + Vector3.up * safetySpace, Vector3.down, out var hit,
                        liftClearance + safetySpace * 2f, geometryMask, QueryTriggerInteraction.Ignore)
                    || Vector3.Angle(hit.normal, Vector3.up) > maxTopSlope) return false;
            }
            return true;
        }

        void Capsule(Vector3 feet, out Vector3 bottom, out Vector3 top, out float radius)
        {
            radius = body.radius + safetySpace;
            bottom = feet + Vector3.up * (body.radius + safetySpace);
            top = feet + Vector3.up * (body.height - body.radius);
        }

        bool ClearPose(Vector3 feet)
        {
            Capsule(feet, out var bottom, out var top, out float radius);
            return !Physics.CheckCapsule(bottom, top, radius, geometryMask, QueryTriggerInteraction.Ignore);
        }

        bool ClearSegment(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.00000001f) return true;
            Capsule(from, out var bottom, out var top, out float radius);
            return !Physics.CapsuleCast(bottom, top, radius, delta.normalized, delta.magnitude,
                geometryMask, QueryTriggerInteraction.Ignore);
        }
    }
}
