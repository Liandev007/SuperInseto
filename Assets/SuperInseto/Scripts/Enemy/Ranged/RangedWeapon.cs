using System;
using UnityEngine;

namespace SuperInseto
{
    [DefaultExecutionOrder(50)] // Brain can cancel an aim before the firing tick.
    [RequireComponent(typeof(Health), typeof(EnemyPerception))]
    public sealed class RangedWeapon : MonoBehaviour
    {
        [SerializeField] Transform firePoint;
        [SerializeField] SimpleProjectile projectilePrefab;
        [SerializeField, Min(1f)] float rangedDamage = 12f;
        [SerializeField, Min(0.1f)] float projectileSpeed = 10f;
        [SerializeField, Min(0.1f)] float projectileLifetime = 3f;
        [SerializeField, Min(0.01f)] float projectileRadius = 0.09f;
        [SerializeField, Min(0.05f)] float attackWindup = 0.65f;
        [SerializeField, Min(0.05f)] float attackRecovery = 0.4f;
        [SerializeField, Min(0f)] float attackCooldown = 0.8f;
        [SerializeField] bool useAnimationEvents;
        Health health, target;
        Collider targetBody;
        EnemyPerception perception;
        readonly ProjectileCollision collision = new ProjectileCollision();
        float elapsed, readyAt, maxRange;
        bool fired;
        public bool Active { get; private set; }
        public bool Aiming => Active && !fired;
        public Vector3 AimPoint { get; private set; }
        public event Action AimStarted;
        public event Action Fired;
        public event Action Finished;
        float Duration => Mathf.Max(0.05f, attackWindup) + Mathf.Max(0.05f, attackRecovery);
        void Awake() { health = GetComponent<Health>(); perception = GetComponent<EnemyPerception>(); }
        void OnEnable() { health.Died += Cancel; }
        void OnDisable() { health.Died -= Cancel; Cancel(); }
        public bool TryStart(Health player, Collider body, float maximumRange)
        {
            if (!isActiveAndEnabled || health.IsDead || Active || Time.time < readyAt || !firePoint || !projectilePrefab
                || !perception.CanSee(player, body)) return false;
            if (Vector3.Distance(transform.position, player.transform.position) > maximumRange) return false;
            target = player; targetBody = body; maxRange = maximumRange;
            AimPoint = body.bounds.center; // Lock the telegraph; shots never home or follow the target after launch.
            elapsed = 0f; fired = false; Active = true;
            readyAt = Time.time + Duration + Mathf.Max(0f, attackCooldown);
            AimStarted?.Invoke();
            return true;
        }
        void Update()
        {
            if (!Active) return;
            if (health.IsDead || !target || target.IsDead) { Cancel(); return; }
            elapsed += Time.deltaTime;
            if (!useAnimationEvents && !fired && elapsed >= attackWindup) Fire();
            if (elapsed >= Duration) Cancel(); // Missing clip events cannot lock the actor forever.
        }
        bool ClearMuzzleAndSight()
        {
            if (!firePoint || !targetBody || !perception.CanSee(target, targetBody)
                || Vector3.Distance(transform.position, target.transform.position) > maxRange) return false;
            Vector3 eyeToMuzzle = firePoint.position - perception.EyePosition;
            if (collision.Trace(perception.EyePosition, eyeToMuzzle, eyeToMuzzle.magnitude, projectileRadius,
                    transform, ~0, out _, out _)) return false;
            Vector3 toTarget = targetBody.bounds.center - firePoint.position;
            bool hit = collision.Trace(firePoint.position, toTarget, toTarget.magnitude, projectileRadius,
                transform, ~0, out var first, out _);
            return hit && first && first.GetComponentInParent<Health>() == target;
        }
        void Fire()
        {
            if (!Active || fired || health.IsDead) return;
            fired = true;
            if (!ClearMuzzleAndSight()) { Cancel(); return; }
            Vector3 direction = AimPoint - firePoint.position;
            if (direction.sqrMagnitude < 0.001f) { Cancel(); return; }
            var projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
            projectile.Launch(gameObject, direction, rangedDamage, projectileSpeed, projectileLifetime, projectileRadius);
            Fired?.Invoke();
        }
        public void AnimationFire() { if (useAnimationEvents && elapsed < Duration) Fire(); }
        public void Cancel()
        {
            bool active = Active; Active = false;
            if (active) Finished?.Invoke();
        }
        void OnDrawGizmosSelected()
        {
            if (!firePoint) return;
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(firePoint.position, projectileRadius);
            Gizmos.DrawRay(firePoint.position, Aiming ? AimPoint - firePoint.position : firePoint.forward * 2f);
        }
    }
}
