using UnityEngine;

namespace SuperInseto
{
    // Sphere collider on the logical root; visual rotation/geometry never drives collision or motion.
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class DroneFlight : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float patrolSpeed = 1.4f;
        [SerializeField, Min(0.1f)] float chaseSpeed = 2.1f;
        [SerializeField, Min(0.1f)] float acceleration = 7f;
        [SerializeField, Min(1f)] float rotationSpeed = 180f;
        [Header("Center height above a fixed room floor")]
        [SerializeField] float floorHeight;
        [SerializeField, Min(0.1f)] float preferredHeight = 1.45f;
        [SerializeField, Min(0.1f)] float minHeight = 1.1f;
        [SerializeField, Min(0.1f)] float maxHeight = 3.2f;
        [Header("Flight volume and collision")]
        [SerializeField] Vector3 boundsCenter = new Vector3(0f, 2.5f, 0f);
        [SerializeField] Vector3 boundsSize = new Vector3(12f, 5f, 12f);
        [SerializeField, Min(0.01f)] float safetyMargin = 0.04f;
        [SerializeField, Min(0.1f)] float obstacleLookAhead = 1f;
        [SerializeField] LayerMask collisionMask = ~0; // Includes player and other enemies.
        SphereCollider body;
        readonly RaycastHit[] hits = new RaycastHit[32];
        readonly Collider[] overlaps = new Collider[32];
        Vector3 goal, velocity, avoidance;
        float speed, avoidanceUntil;
        bool moving, stopped;
        public bool Blocked { get; private set; }
        public float Speed => velocity.magnitude;
        public float CombatHeight => Mathf.Clamp(floorHeight + preferredHeight, LowY, HighY);
        float Radius => body ? body.radius * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z)) : 0.45f;
        Bounds Volume => new Bounds(boundsCenter, boundsSize);
        float LowY => Mathf.Max(floorHeight + Mathf.Max(minHeight, Radius + safetyMargin), Volume.min.y + Radius + safetyMargin);
        float HighY => Mathf.Max(LowY, Mathf.Min(floorHeight + Mathf.Max(minHeight, maxHeight), Volume.max.y - Radius - safetyMargin));
        void Awake() { body = GetComponent<SphereCollider>(); }
        public void ConfigureVolume(Vector3 center, Vector3 size, float floor)
        { boundsCenter = center; boundsSize = size; floorHeight = floor; }
        public Vector3 Constrain(Vector3 point)
        {
            Bounds b = Volume; float pad = Radius + safetyMargin;
            point.x = Mathf.Clamp(point.x, b.min.x + pad, Mathf.Max(b.min.x + pad, b.max.x - pad));
            point.z = Mathf.Clamp(point.z, b.min.z + pad, Mathf.Max(b.min.z + pad, b.max.z - pad));
            point.y = Mathf.Clamp(point.y, LowY, HighY);
            return point;
        }
        public Vector3 CombatPoint(Vector3 point) { point.y = CombatHeight; return Constrain(point); }
        public bool TryPlace(Vector3 point)
        {
            if ((Constrain(point) - point).sqrMagnitude > 0.001f || !ClearAt(point)) return false;
            transform.position = point; stopped = false; Stop(); return true;
        }
        bool ClearAt(Vector3 point)
        {
            int count = Physics.OverlapSphereNonAlloc(point, Radius, overlaps, collisionMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++) if (!overlaps[i].transform.IsChildOf(transform)) return false;
            return true;
        }
        float FreeDistance(Vector3 origin, Vector3 direction, float distance, out Vector3 normal)
        {
            normal = Vector3.zero;
            if (distance <= 0f) return 0f;
            int count = Physics.SphereCastNonAlloc(origin, Radius, direction, hits, distance + safetyMargin,
                collisionMask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return 0f;
            float free = distance;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                float candidate = Mathf.Max(0f, hits[i].distance - safetyMargin);
                if (candidate < free) { free = candidate; normal = hits[i].normal; }
            }
            return free;
        }
        public bool ClearRoute(Vector3 point)
        {
            if ((Constrain(point) - point).sqrMagnitude > 0.001f || !ClearAt(point)) return false;
            Vector3 delta = point - transform.position;
            return FreeDistance(transform.position, delta.normalized, delta.magnitude, out _) >= delta.magnitude - 0.01f;
        }
        public void MoveTo(Vector3 point, bool chasing)
        { if (stopped) return; goal = Constrain(point); speed = chasing ? chaseSpeed : patrolSpeed; moving = true; }
        public void Stop() { moving = false; velocity = Vector3.zero; avoidanceUntil = 0f; Blocked = false; }
        public void Shutdown() { stopped = true; Stop(); }
        public void Face(Vector3 point)
        {
            if (stopped) return;
            Vector3 forward = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
            if (forward.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(forward), rotationSpeed * Time.deltaTime);
        }
        Vector3 Steer(Vector3 direction)
        {
            float probe = Mathf.Min(obstacleLookAhead, Vector3.Distance(goal, transform.position));
            float direct = FreeDistance(transform.position, direction, probe, out Vector3 normal);
            if (direct >= probe - 0.01f) return direction;
            if (Time.time < avoidanceUntil && FreeDistance(transform.position, avoidance, probe, out _) > probe * 0.8f
                && (Constrain(transform.position + avoidance * probe) - (transform.position + avoidance * probe)).sqrMagnitude < 0.001f)
                return avoidance;
            Vector3 best = Vector3.zero; float bestScore = 0.1f;
            bool vertical = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.9f;
            // A few local alternatives, not 3D pathfinding. Stable choice is held briefly.
            for (int i = 0; i < 5; i++)
            {
                Vector3 candidate = i == 0 ? Vector3.ProjectOnPlane(direction, normal).normalized
                    : i == 1 ? Quaternion.AngleAxis(60f, Vector3.up) * direction
                    : i == 2 ? Quaternion.AngleAxis(-60f, Vector3.up) * direction
                    : (direction + Vector3.up * (i == 3 ? 1f : -1f)).normalized;
                // A blocked descent above cover must be able to slide off its edge before descending.
                if (vertical && i > 0)
                    candidate = i == 1 ? transform.right : i == 2 ? -transform.right : i == 3 ? transform.forward : -transform.forward;
                if (candidate.sqrMagnitude < 0.5f) continue;
                Vector3 end = transform.position + candidate * probe;
                if ((Constrain(end) - end).sqrMagnitude > 0.001f) continue;
                float free = FreeDistance(transform.position, candidate, probe, out _);
                float alignment = vertical && i > 0 ? 0.2f : Vector3.Dot(candidate, direction);
                float score = alignment * (free / Mathf.Max(0.01f, probe));
                if (free >= probe * 0.8f && score > bestScore) { best = candidate; bestScore = score; }
            }
            avoidance = best; avoidanceUntil = Time.time + 0.45f;
            return best;
        }
        void Update() { Simulate(Time.deltaTime); }
        public void Simulate(float deltaTime)
        {
            if (stopped || !moving || deltaTime <= 0f) return;
            float dt = Mathf.Min(deltaTime, 0.1f);
            Vector3 delta = goal - transform.position;
            if (delta.magnitude <= 0.04f) { Stop(); return; }
            if (!ClearAt(transform.position)) { velocity = Vector3.zero; Blocked = true; return; }
            Vector3 direction = Steer(delta.normalized);
            velocity = Vector3.MoveTowards(velocity, direction * Mathf.Min(speed, delta.magnitude / dt), acceleration * dt);
            Vector3 step = Constrain(transform.position + velocity * dt) - transform.position;
            float length = step.magnitude;
            if (length < 0.0001f) { velocity = Vector3.zero; Blocked = true; return; }
            float travel = FreeDistance(transform.position, step / length, length, out _);
            Vector3 next = transform.position + step / length * travel;
            // Endpoint check also catches moving obstacles that already overlap the destination.
            if (!ClearAt(next)) travel = 0f;
            transform.position += step / length * travel;
            Blocked = travel < length - 0.001f || direction == Vector3.zero;
            if (Blocked) velocity = Vector3.zero;
        }
        void OnDisable() { Shutdown(); }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan; Gizmos.DrawWireCube(boundsCenter, boundsSize);
            Vector3 at = transform.position; at.y = CombatHeight;
            Gizmos.DrawWireSphere(at, Radius); Gizmos.DrawLine(transform.position, at);
        }
    }
}
