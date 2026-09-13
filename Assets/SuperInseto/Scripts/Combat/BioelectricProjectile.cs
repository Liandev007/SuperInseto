using System;
using UnityEngine;

namespace SuperInseto
{
    [DefaultExecutionOrder(100)]
    public sealed class BioelectricProjectile : MonoBehaviour
    {
        [SerializeField] LayerMask collisionMask = ~4;
        [SerializeField] LayerMask damageMask = ~4;
        [Tooltip("Optional presentation only. Flight and damage never depend on its mesh.")]
        [SerializeField] Transform visual;
        [SerializeField, Min(0f)] float impactFlashDuration = 0.12f;
        readonly ProjectileCollision collision = new ProjectileCollision(false);
        Transform owner;
        GameObject source;
        IDamageable ownerReceiver;
        Vector3 direction;
        float speed, remaining, radius, damage;
        bool launched;
        public bool Flying { get; private set; }
        public event Action<Vector3, bool> Impacted;

        public void Launch(GameObject shooter, Vector3 heading, float amount, float velocity, float lifetime, float size)
        {
            if (launched) return; // Each instance has exactly one flight/impact lifecycle.
            launched = true; source = shooter; owner = shooter ? shooter.transform : null;
            ownerReceiver = shooter ? shooter.GetComponentInParent<IDamageable>() : null;
            direction = heading.normalized; damage = Mathf.Max(0f, amount);
            speed = Mathf.Max(0.1f, velocity); remaining = Mathf.Max(0.01f, lifetime); radius = Mathf.Max(0.01f, size);
            Flying = direction.sqrMagnitude > 0.5f;
            if (!Flying) Destroy(gameObject);
        }
        void Update() { Advance(Time.deltaTime); }
        public void Advance(float deltaTime)
        {
            if (!Flying || deltaTime <= 0f) return;
            float step = Mathf.Min(deltaTime, remaining);
            if (collision.Trace(transform.position, direction, speed * step, radius, owner, collisionMask,
                out var hit, out var point))
            {
                Flying = false; // Latch before callbacks, including death/disabling the target.
                transform.position = point;
                Destroy(gameObject, Mathf.Max(0f, impactFlashDuration));
                var receiver = hit && (damageMask.value & (1 << hit.gameObject.layer)) != 0
                    ? hit.GetComponentInParent<IDamageable>() : null;
                bool damaged = receiver != null && !ReferenceEquals(receiver, ownerReceiver)
                    && receiver.TakeDamage(new DamageInfo(damage, source, point));
                if (visual) visual.localScale *= 2f; // Brief stopped green flash, no extra object/collider.
                Impacted?.Invoke(point, damaged);
                return;
            }
            transform.position += direction * speed * step;
            remaining -= step;
            if (remaining <= 0f) { Flying = false; Destroy(gameObject); }
        }
        void OnDisable() { Flying = false; }
    }
}
