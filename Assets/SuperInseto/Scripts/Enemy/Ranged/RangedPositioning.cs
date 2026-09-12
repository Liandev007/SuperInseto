using UnityEngine;
using UnityEngine.AI;

namespace SuperInseto
{
    [RequireComponent(typeof(EnemyNavigation))]
    public sealed class RangedPositioning : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] float retreatDistance = 3f;
        [SerializeField, Min(0.1f)] float sampleRadius = 0.7f;
        NavMeshPath path;
        NavMeshAgent agent;
        readonly Collider[] overlaps = new Collider[32];
        static readonly float[] Angles = { 0f, -40f, 40f, -75f, 75f };
        void Awake() { path = new NavMeshPath(); agent = GetComponent<NavMeshAgent>(); }
        public bool TryFindRetreat(Vector3 player, out Vector3 destination)
        {
            destination = transform.position;
            if (!agent.enabled || !agent.isOnNavMesh) return false;
            Vector3 away = Vector3.ProjectOnPlane(transform.position - player, Vector3.up).normalized;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            float currentDistance = Vector3.Distance(transform.position, player);
            foreach (float angle in Angles)
            {
                Vector3 candidate = transform.position + Quaternion.AngleAxis(angle, Vector3.up) * away * retreatDistance;
                if (!NavMesh.SamplePosition(candidate, out var hit, sampleRadius, agent.areaMask)
                    || Mathf.Abs(hit.position.y - transform.position.y) > 0.5f
                    || Vector3.Distance(hit.position, player) < currentDistance + 0.75f) continue;
                if (!NavMesh.CalculatePath(transform.position, hit.position, agent.areaMask, path)
                    || path.status != NavMeshPathStatus.PathComplete) continue;
                var corners = path.corners;
                float length = 0f;
                for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i-1], corners[i]);
                if (length > retreatDistance * 2f) continue;
                int count = Physics.OverlapCapsuleNonAlloc(hit.position + Vector3.up * 0.46f,
                    hit.position + Vector3.up * 1.4f, 0.4f, overlaps, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = count == overlaps.Length;
                for (int i = 0; i < count; i++) if (!overlaps[i].transform.IsChildOf(transform)) blocked = true;
                if (blocked) continue;
                destination = hit.position;
                return true;
            }
            return false;
        }
    }
}
