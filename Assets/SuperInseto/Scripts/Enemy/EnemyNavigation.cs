using UnityEngine;
using UnityEngine.AI;

namespace SuperInseto
{
    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public sealed class EnemyNavigation : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float patrolSpeed = 1.7f;
        [SerializeField, Min(0.1f)] float chaseSpeed = 3.6f;
        [SerializeField, Min(1f)] float rotationSpeed = 360f;
        [SerializeField, Min(0.05f)] float repathInterval = 0.2f;
        [SerializeField, Min(0.01f)] float collisionMargin = 0.04f;
        [SerializeField] LayerMask collisionMask = ~0;
        NavMeshAgent agent;
        CapsuleCollider body;
        readonly RaycastHit[] hits = new RaycastHit[32];
        float nextPathAt;
        public bool Ready => agent && agent.enabled && agent.isOnNavMesh;
        public float Speed => Ready ? agent.velocity.magnitude : 0f;
        public bool Arrived => Ready && !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= 0.25f);

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>(); body = GetComponent<CapsuleCollider>();
            agent.enabled = false; // The gym builds navigation before placement.
            agent.updatePosition = false; agent.updateRotation = false;
        }
        public bool Place(Vector3 position)
        {
            if (!NavMesh.SamplePosition(position, out var hit, 1f, NavMesh.AllAreas)) return false;
            transform.position = hit.position;
            agent.enabled = true;
            return agent.Warp(hit.position) && agent.isOnNavMesh;
        }
        public bool GoTo(Vector3 destination, bool chasing)
        {
            if (!Ready) return false;
            agent.speed = chasing ? chaseSpeed : patrolSpeed;
            agent.isStopped = false;
            if (Time.time < nextPathAt) return true;
            nextPathAt = Time.time + repathInterval;
            // Do not project targets on upper platforms down onto a distant floor.
            if (!NavMesh.SamplePosition(destination, out var hit, 0.8f, agent.areaMask))
            { Stop(); return false; }
            return agent.SetDestination(hit.position);
        }
        public void Stop()
        {
            if (!Ready) return;
            agent.isStopped = true; agent.ResetPath(); agent.velocity = Vector3.zero;
            agent.nextPosition = transform.position;
            nextPathAt = 0f;
        }
        public void Face(Vector3 position)
        {
            Vector3 direction = Vector3.ProjectOnPlane(position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
        }
        void LateUpdate()
        {
            if (!Ready || agent.isStopped) return;
            Vector3 delta = agent.nextPosition - transform.position;
            float length = delta.magnitude;
            if (length > 0.0001f)
            {
                // Physical sweep covers carving delay, closing doors, and the player.
                Vector3 center = transform.position + body.center;
                float radius = body.radius;
                float half = Mathf.Max(0f, body.height * 0.5f - radius);
                int count = Physics.CapsuleCastNonAlloc(center + Vector3.up * half,
                    center - Vector3.up * half, radius, delta / length, hits,
                    length + collisionMargin, collisionMask, QueryTriggerInteraction.Ignore);
                float travel = count == hits.Length ? 0f : length;
                for (int i = 0; i < count; i++)
                {
                    if (hits[i].collider.transform.IsChildOf(transform)) continue;
                    // Walkable ground is handled by NavMesh, not as a forward obstacle.
                    if (hits[i].normal.y > 0.7f) continue;
                    travel = Mathf.Min(travel, Mathf.Max(0f, hits[i].distance - collisionMargin));
                }
                transform.position += delta / length * travel;
                agent.nextPosition = transform.position;
            }
            if (agent.desiredVelocity.sqrMagnitude > 0.02f) Face(transform.position + agent.desiredVelocity);
        }
        public void Shutdown()
        {
            Stop();
            if (agent) agent.enabled = false;
        }
        void OnDisable() { Shutdown(); }
    }
}
