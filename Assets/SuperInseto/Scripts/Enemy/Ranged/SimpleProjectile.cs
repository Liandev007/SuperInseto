using UnityEngine;

namespace SuperInseto
{
    [DefaultExecutionOrder(100)] // PlayerCombat updates dodge invulnerability first.
    public sealed class SimpleProjectile : MonoBehaviour
    {
        [SerializeField] LayerMask collisionMask = ~ProjectileCollision.EnemyMask;
        [SerializeField] LayerMask damageMask = 4; // Existing player layer, including Ignore Raycast.
        readonly ProjectileCollision collision = new ProjectileCollision();
        Transform owner;
        GameObject source;
        Vector3 direction;
        float speed, remaining, radius, damage;
        public bool Flying { get; private set; }
        public void Launch(GameObject shooter, Vector3 heading, float amount, float velocity, float lifetime, float size)
        {
            source = shooter; owner = shooter ? shooter.transform : null;
            direction = heading.normalized; damage = amount;
            speed = Mathf.Max(0.1f, velocity); remaining = Mathf.Max(0.01f, lifetime); radius = Mathf.Max(0.01f, size);
            Flying = direction.sqrMagnitude > 0.5f;
            if (!Flying) Destroy(gameObject);
        }
        void Update() { Advance(Time.deltaTime); }
        // One swept segment per tick: also usable by focused physics tests.
        public void Advance(float deltaTime)
        {
            if (!Flying || deltaTime <= 0f) return;
            float step = Mathf.Min(deltaTime, remaining);
            if (collision.Trace(transform.position, direction, speed * step, radius, owner, collisionMask,
                out var hit, out var point))
            {
                Flying = false; // Set before calling external damage handlers.
                transform.position = point;
                if (hit && (damageMask.value & (1 << hit.gameObject.layer)) != 0)
                    hit.GetComponentInParent<IDamageable>()?.TakeDamage(new DamageInfo(damage, source, point));
                Destroy(gameObject);
                return;
            }
            transform.position += direction * speed * step;
            remaining -= step;
            if (remaining <= 0f) { Flying = false; Destroy(gameObject); }
        }
        void OnDisable() { Flying = false; }
    }
}
