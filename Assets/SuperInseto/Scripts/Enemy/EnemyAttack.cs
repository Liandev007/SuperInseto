using System;
using UnityEngine;

namespace SuperInseto
{
    [RequireComponent(typeof(Health), typeof(MeleeHitbox))]
    public sealed class EnemyAttack : MonoBehaviour
    {
        [SerializeField, Min(1f)] float enemyDamage = 15f;
        [Tooltip("Distance between actor roots required to START an attack. Hit range/radius are on MeleeHitbox.")]
        [SerializeField, Min(0.1f)] float attackRange = 1.45f;
        [SerializeField, Min(0.05f)] float attackWindup = 0.5f;
        [SerializeField, Min(0.02f)] float damageWindow = 0.12f;
        [SerializeField, Min(0.05f)] float attackRecovery = 0.55f;
        [SerializeField, Min(0f)] float attackCooldown = 0.55f;
        [SerializeField] bool useAnimationEvents;
        Health health;
        MeleeHitbox hitbox;
        float elapsed, readyAt;
        public bool Active { get; private set; }
        public bool WindingUp => Active && elapsed < attackWindup;
        public float Range => attackRange;
        public float Progress => Active ? Mathf.Clamp01(elapsed / TotalDuration) : 0f;
        float TotalDuration => Mathf.Max(0.05f, attackWindup) + Mathf.Max(0.02f, damageWindow) + Mathf.Max(0.05f, attackRecovery);
        public event Action Started;
        public event Action Finished;

        void Awake() { health = GetComponent<Health>(); hitbox = GetComponent<MeleeHitbox>(); }
        void OnEnable() { health.Died += Cancel; }
        void OnDisable() { health.Died -= Cancel; Cancel(); }
        public bool TryStart()
        {
            if (!isActiveAndEnabled || health.IsDead || Active || Time.time < readyAt) return false;
            elapsed = 0f; Active = true;
            readyAt = Time.time + TotalDuration + Mathf.Max(0f, attackCooldown);
            hitbox.BeginSwing(enemyDamage, gameObject);
            Started?.Invoke();
            return true;
        }
        void Update()
        {
            if (!Active) return;
            if (health.IsDead) { Cancel(); return; }
            float previous = elapsed;
            elapsed += Time.deltaTime;
            if (!useAnimationEvents)
                hitbox.SetWindow(elapsed >= attackWindup && previous < attackWindup + damageWindow);
            hitbox.Sample();
            if (elapsed >= TotalDuration) Cancel(); // Always recover even if an animation event is missing.
        }
        public void OpenDamageWindow()
        {
            if (useAnimationEvents && Active && !health.IsDead && elapsed < TotalDuration) hitbox.SetWindow(true);
        }
        public void CloseDamageWindow() { hitbox.SetWindow(false); }
        public void Cancel()
        {
            bool wasActive = Active; Active = false;
            if (hitbox) hitbox.EndSwing();
            if (wasActive) Finished?.Invoke();
        }
        void OnDrawGizmosSelected()
        { Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange); }
    }
}
