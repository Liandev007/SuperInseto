using UnityEngine;

namespace SuperInseto
{
    // Managed query buffers shared by muzzle checks and projectile flight; no physics callbacks or native construction.
    public sealed class ProjectileCollision
    {
        public const int EnemyMask = 1 << 8;
        readonly RaycastHit[] hits = new RaycastHit[64];
        readonly Collider[] overlaps = new Collider[64];
        readonly bool ignoreEnemies;
        // Enemy weapons keep their original filtering; player projectiles opt in to hitting Enemy.
        public ProjectileCollision(bool ignoreEnemies = true) { this.ignoreEnemies = ignoreEnemies; }
        bool Ignore(Collider collider, Transform owner)
        {
            if (owner && collider.transform.IsChildOf(owner)) return true;
            if (ignoreEnemies)
                for (Transform t = collider.transform; t; t = t.parent)
                    if (t.gameObject.layer == 8) return true;
            return false;
        }
        public bool Trace(Vector3 origin, Vector3 direction, float distance, float radius,
            Transform owner, int mask, out Collider collider, out Vector3 point)
        {
            collider = null; point = origin;
            if (ignoreEnemies) mask &= ~EnemyMask;
            // SphereCast alone cannot detect a muzzle/projectile already overlapping a wall.
            int count = Physics.OverlapSphereNonAlloc(origin, radius, overlaps, mask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return true; // Fail closed, without damage to an uncertain target.
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (Ignore(overlaps[i], owner)) continue;
                Vector3 candidate = overlaps[i].ClosestPoint(origin);
                float sqr = (candidate - origin).sqrMagnitude;
                if (sqr < nearest) { nearest = sqr; collider = overlaps[i]; point = candidate; }
            }
            if (collider) return true;
            if (distance <= 0f) return false;
            count = Physics.SphereCastNonAlloc(origin, radius, direction.normalized, hits, distance,
                mask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return true;
            nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (Ignore(hits[i].collider, owner)) continue;
                if (hits[i].distance < nearest)
                { nearest = hits[i].distance; collider = hits[i].collider; point = hits[i].point; }
            }
            return collider;
        }
    }
}
