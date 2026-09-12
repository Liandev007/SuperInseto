using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperInseto
{
    public sealed class MeleeHitbox : MonoBehaviour
    {
        [Tooltip("Temporary socket on the logical root. Can later point to an animated hand/arm socket.")]
        [SerializeField] Transform attackOrigin;
        [SerializeField, Min(0.1f)] float attackRange = 1.8f;
        [SerializeField, Min(0.05f)] float attackRadius = 0.5f;
        [SerializeField] LayerMask targetMask = ~4;
        [SerializeField] LayerMask obstructionMask = ~4;
        readonly Collider[] results = new Collider[64];
        readonly HashSet<IDamageable> struck = new HashSet<IDamageable>();
        GameObject owner;
        float damage;
        public bool WindowOpen { get; private set; }
        public event Action<float> Hit;

        public void BeginSwing(float amount, GameObject source)
        { damage = amount; owner = source; struck.Clear(); WindowOpen = false; }
        public void SetWindow(bool open) { WindowOpen = open; }
        public void EndSwing() { WindowOpen = false; struck.Clear(); }

        public void Sample()
        {
            if (!WindowOpen || !attackOrigin || !owner) return;
            Vector3 origin = attackOrigin.position;
            Vector3 forward = attackOrigin.forward;
            float radius = Mathf.Min(attackRadius, attackRange * 0.5f);
            int count = Physics.OverlapCapsuleNonAlloc(origin + forward * radius,
                origin + forward * (attackRange - radius), radius, results, targetMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var collider = results[i];
                if (collider.transform.IsChildOf(owner.transform)) continue;
                var receiver = collider.GetComponentInParent<IDamageable>();
                if (receiver == null || struck.Contains(receiver)) continue;
                Vector3 point = collider.ClosestPoint(origin);
                Vector3 delta = point - origin;
                if (delta.magnitude > attackRange || Vector3.Dot(delta, forward) <= 0f) continue;
                if (Physics.Raycast(origin, delta.normalized, out var wall, delta.magnitude,
                        obstructionMask, QueryTriggerInteraction.Ignore)
                    && wall.collider.GetComponentInParent<IDamageable>() != receiver) continue;
                // One attempt per receiver per swing, including multi-collider targets.
                struck.Add(receiver);
                if (receiver.TakeDamage(new DamageInfo(damage, owner, point))) Hit?.Invoke(damage);
            }
        }
        void OnDisable() { EndSwing(); }
        void OnDrawGizmosSelected()
        {
            if (!attackOrigin) return;
            Gizmos.color = Color.yellow;
            float radius = Mathf.Min(attackRadius, attackRange * 0.5f);
            Gizmos.DrawWireSphere(attackOrigin.position + attackOrigin.forward * radius, radius);
            Gizmos.DrawWireSphere(attackOrigin.position + attackOrigin.forward * (attackRange - radius), radius);
        }
    }
}
