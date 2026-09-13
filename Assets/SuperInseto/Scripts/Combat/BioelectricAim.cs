using UnityEngine;

namespace SuperInseto
{
    // Camera acquisition and muzzle clearance share the same solid/owner filtering as flight.
    public sealed class BioelectricAim
    {
        readonly ProjectileCollision collision = new ProjectileCollision(false);
        public bool TryGetPoint(Camera view, Transform owner, float distance, int mask, out Vector3 point)
        {
            point = Vector3.zero;
            if (!view) return false;
            Ray ray = view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            // Include the segment before the near plane, so near-camera walls also block aiming.
            Vector3 origin = ray.origin - ray.direction * view.nearClipPlane;
            if (collision.Trace(origin, ray.direction, distance, 0.001f, owner, mask, out var hit, out point))
                return hit && Vector3.Dot(point - origin, ray.direction) > 0.001f;
            point = origin + ray.direction * distance;
            return true;
        }
        public bool MuzzleClear(Vector3 bodyCenter, Vector3 muzzle, float radius, Transform owner, int mask)
        {
            Vector3 offset = muzzle - bodyCenter;
            return !collision.Trace(bodyCenter, offset, offset.magnitude, radius, owner, mask, out _, out _);
        }
    }
}
