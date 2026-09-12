using UnityEngine;

namespace SuperInseto
{
    public sealed class EnemyPerception : MonoBehaviour
    {
        [SerializeField] Transform eye;
        [SerializeField, Min(0.1f)] float detectionRange = 8f;
        [SerializeField, Range(1f, 360f)] float fieldOfView = 110f;
        [SerializeField] LayerMask sightMask = ~0; // Includes the player's Ignore Raycast layer.
        readonly RaycastHit[] hits = new RaycastHit[32];
        public float DetectionRange => detectionRange;
        public Vector3 EyePosition => eye ? eye.position : transform.position + Vector3.up * 1.5f;

        public bool CanSee(Health target, Collider targetBody)
        {
            if (!target || target.IsDead || !target.isActiveAndEnabled || !targetBody) return false;
            Vector3 delta = targetBody.bounds.center - EyePosition;
            float distance = delta.magnitude;
            if (distance > detectionRange || Vector3.Angle(transform.forward, delta) > fieldOfView * 0.5f) return false;
            int count = Physics.RaycastNonAlloc(EyePosition, delta.normalized, hits, distance + 0.05f,
                sightMask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false; // Fail closed if the buffer fills.
            float nearest = float.PositiveInfinity;
            Collider first = null;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance < nearest) { nearest = hits[i].distance; first = hits[i].collider; }
            }
            return first && first.GetComponentInParent<Health>() == target;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(EyePosition, detectionRange);
            foreach (float angle in new[] { -fieldOfView * 0.5f, fieldOfView * 0.5f })
                Gizmos.DrawRay(EyePosition, Quaternion.AngleAxis(angle, Vector3.up) * transform.forward * detectionRange);
        }
    }
}
