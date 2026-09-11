using UnityEngine;

namespace SuperInseto
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class WallDetector : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] float detectionDistance = 0.55f;
        [SerializeField, Range(0.03f, 0.2f)] float probeRadius = 0.1f;
        [SerializeField, Range(0f, 0.3f)] float maxVerticalNormal = 0.15f;
        [SerializeField] LayerMask surfaceMask = ~4; // Ignore Raycast is reserved for the player.
        CharacterController body;
        void Awake() { body = GetComponent<CharacterController>(); }

        public bool Find(Vector3 direction, out RaycastHit wall, float extraDistance = 0f)
        {
            return FindAt(transform.position, direction, out wall, extraDistance);
        }

        public bool FindAt(Vector3 feet, Vector3 direction, out RaycastHit wall, float extraDistance = 0f)
        {
            wall = default;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.5f) return false;
            Vector3 origin = feet + Vector3.up * (body.height * 0.6f);
            float reach = body.radius + detectionDistance + extraDistance;
            // Broad probe plus a centre ray: never attach through another collider or
            // keep climbing on an edge detected only by the sphere's rounded rim.
            if (!Physics.SphereCast(origin, probeRadius, direction, out _, reach,
                    surfaceMask, QueryTriggerInteraction.Ignore)) return false;
            if (!Physics.Raycast(origin, direction, out wall, reach,
                    surfaceMask, QueryTriggerInteraction.Ignore)) return false;
            var surface = wall.collider.GetComponentInParent<ClimbableSurface>();
            return surface != null && surface.isActiveAndEnabled
                && Mathf.Abs(wall.normal.y) <= maxVerticalNormal
                && Vector3.Dot(-wall.normal, direction) > 0.8f;
        }
    }
}
