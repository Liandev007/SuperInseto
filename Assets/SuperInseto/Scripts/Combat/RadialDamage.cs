using System.Collections.Generic;
using UnityEngine;

namespace SuperInseto
{
    // One query per activation, not per frame. Each instance owns its receiver set.
    public sealed class RadialDamage
    {
        readonly HashSet<IDamageable> struck = new HashSet<IDamageable>();

        public int Apply(Vector3 origin, float radius, float damage, GameObject owner,
            int targetMask, int obstructionMask)
        {
            struck.Clear();
            if (!owner || radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius) || damage <= 0f) return 0;
            // Rays starting inside solids miss their exit face. Reject such an origin explicitly.
            foreach (var solid in Physics.OverlapSphere(origin, 0.01f, obstructionMask, QueryTriggerInteraction.Ignore))
                if (!IgnoreSolid(solid, owner.transform)
                    && (solid.ClosestPoint(origin) - origin).sqrMagnitude < 0.000001f) return 0;

            int damaged = 0;
            var ownerReceiver = owner.GetComponentInParent<IDamageable>();
            foreach (var collider in Physics.OverlapSphere(origin, radius, targetMask, QueryTriggerInteraction.Ignore))
            {
                if (!collider || collider.transform.IsChildOf(owner.transform)) continue;
                var receiver = collider.GetComponentInParent<IDamageable>();
                if (receiver == null || receiver == ownerReceiver || struck.Contains(receiver)) continue;
                Vector3 point = collider.ClosestPoint(origin);
                // A true sphere: neither enemy type nor altitude extends the damage volume.
                if ((point - origin).sqrMagnitude > radius * radius
                    || !Visible(origin, point, receiver, owner.transform, obstructionMask)) continue;
                // Add before calling user code, including invulnerable or multi-collider receivers.
                struck.Add(receiver);
                if (receiver.TakeDamage(new DamageInfo(damage, owner, point))) damaged++;
            }
            return damaged;
        }

        static bool Visible(Vector3 origin, Vector3 point, IDamageable receiver, Transform owner, int mask)
        {
            Vector3 delta = point - origin;
            if (delta.sqrMagnitude < 0.000001f) return true;
            foreach (var hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude, mask, QueryTriggerInteraction.Ignore))
            {
                if (IgnoreSolid(hit.collider, owner)
                    || hit.collider.GetComponentInParent<IDamageable>() == receiver) continue;
                return false;
            }
            return true;
        }

        static bool IgnoreSolid(Collider collider, Transform owner)
        {
            if (collider.transform.IsChildOf(owner)) return true;
            // Match the existing Enemy hierarchy convention, including Default-layer child colliders.
            for (Transform parent = collider.transform; parent; parent = parent.parent)
                if (parent.gameObject.layer == 8) return true;
            return false;
        }
    }
}
