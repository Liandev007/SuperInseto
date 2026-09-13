using System;
using UnityEngine;

namespace SuperInseto
{
    // Normal combat (-75) and Impact (-70) resolve first. The shared motor lock excludes concurrent actions.
    [DefaultExecutionOrder(-65)]
    [RequireComponent(typeof(PlayerCombat), typeof(ChitinEnergy))]
    public sealed class BioelectricStinger : MonoBehaviour
    {
        [SerializeField] Transform firePoint;
        [SerializeField, Min(0f)] float energyCost = 20f;
        [SerializeField] Camera aimCamera;
        [SerializeField] BioelectricProjectile projectilePrefab;
        [SerializeField, Min(1f)] float stingerDamage = 25f;
        [SerializeField, Min(0.1f)] float projectileSpeed = 18f;
        [SerializeField, Min(0.01f)] float projectileLifetime = 3f;
        [SerializeField, Min(0.01f)] float projectileRadius = 0.1f;
        [Tooltip("Seconds from the shot. A blocked muzzle or cancelled preparation also consumes cooldown.")]
        [SerializeField, Min(0.1f)] float abilityCooldown = 2.5f;
        [SerializeField, Min(0.05f)] float windup = 0.25f;
        [SerializeField, Min(0.05f)] float recovery = 0.3f;
        [SerializeField, Min(1f)] float aimDistance = 45f;
        [SerializeField, Min(0f)] float turnSpeed = 540f;
        [SerializeField] LayerMask aimMask = ~4;
        [Tooltip("CombatAnimationBridge.FireBioelectricStinger requests the shot. A missing event times out safely.")]
        [SerializeField] bool useAnimationEvents;
        readonly BioelectricAim aim = new BioelectricAim();
        PlayerCombat combat;
        PlayerMotor motor;
        PlayerInputReader input;
        WallClimber climber;
        CharacterController body;
        Health health;
        ChitinEnergy energy;
        float startedAt, shotAt, readyAt;
        bool resolved, fireRequested, ownsMotion;
        public bool Active { get; private set; }
        public bool WindingUp => Active && !resolved;
        public Transform FirePoint => firePoint;
        public Vector3 AimPoint { get; private set; }
        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
        public float WindupProgress => WindingUp ? Mathf.Clamp01((Time.time - startedAt) / Windup) : 0f;
        public event Action PreparationStarted;
        public event Action<BioelectricProjectile> Fired;
        public event Action<Vector3> Blocked;
        public event Action Finished;
        float Windup => Mathf.Max(0.05f, windup);
        float Recovery => Mathf.Max(0.05f, recovery);

        void Awake()
        {
            combat = GetComponent<PlayerCombat>(); motor = GetComponent<PlayerMotor>();
            input = GetComponent<PlayerInputReader>(); climber = GetComponent<WallClimber>();
            body = GetComponent<CharacterController>(); health = GetComponent<Health>();
            energy = GetComponent<ChitinEnergy>();
            if (!aimCamera) aimCamera = Camera.main;
        }
        void OnEnable() { health.Died += Cancel; }
        void OnDisable() { health.Died -= Cancel; Cancel(); }

        public bool TryActivate()
        {
            if (!isActiveAndEnabled || Active || CooldownRemaining > 0f || !combat.CanStartAbility
                || climber.Mantling || !firePoint || !aimCamera || !projectilePrefab) return false;
            if (!energy.TrySpend(energyCost)) return false;
            Active = true; resolved = false; fireRequested = false; startedAt = Time.time;
            ownsMotion = true; motor.SetActionMotion(Vector3.zero);
            PreparationStarted?.Invoke();
            return true;
        }
        bool CanContinue => !health.IsDead && combat.isActiveAndEnabled && combat.Action == CombatAction.Idle
            && motor.isActiveAndEnabled && body.enabled && input.Captured && !climber.Attached && !climber.Mantling
            && firePoint && aimCamera && projectilePrefab;
        void Update()
        {
            if (input.StingerPressed) TryActivate();
            if (!Active) return;
            if (!CanContinue) { Cancel(); return; }
            if (resolved && Time.time - shotAt >= Recovery) Cancel();
            else if (!resolved && useAnimationEvents && !fireRequested
                && Time.time - startedAt >= Windup + Recovery) Cancel();
        }
        void LateUpdate()
        {
            // ThirdPersonCamera (-100) has now positioned the camera for this frame.
            if (!WindingUp) return;
            if (!CanContinue) { Cancel(); return; }
            bool validAim = aim.TryGetPoint(aimCamera, transform, Mathf.Max(1f, aimDistance), aimMask, out var point);
            AimPoint = point;
            if (validAim)
            {
                Vector3 facing = point - transform.position; facing.y = 0f;
                if (facing.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing),
                        Mathf.Max(0f, turnSpeed) * Time.deltaTime);
            }
            if (fireRequested || (!useAnimationEvents && Time.time - startedAt >= Windup)) Fire(validAim);
        }
        void Fire(bool validAim)
        {
            if (!WindingUp || !CanContinue) return;
            resolved = true; fireRequested = false; shotAt = Time.time;
            readyAt = shotAt + Mathf.Max(0.1f, abilityCooldown);
            Vector3 origin = firePoint.position;
            Vector3 heading = AimPoint - origin;
            // Check from inside the body to the socket: never spawn beyond a wall crossed by the arm.
            if (!validAim || heading.sqrMagnitude < 0.0001f
                || !aim.MuzzleClear(body.bounds.center, origin, Mathf.Max(0.01f, projectileRadius), transform, aimMask))
            { Blocked?.Invoke(origin); return; }
            var projectile = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(heading));
            projectile.Launch(gameObject, heading, stingerDamage, projectileSpeed, projectileLifetime, projectileRadius);
            Fired?.Invoke(projectile);
        }
        public void AnimationFire()
        {
            // Queue for LateUpdate so aiming uses the current camera. Repeated/late events cannot create extra shots.
            if (useAnimationEvents && WindingUp && CanContinue && Time.time - startedAt < Windup + Recovery)
                fireRequested = true;
        }
        public void Cancel()
        {
            bool active = Active;
            if (active && !resolved) readyAt = Time.time + Mathf.Max(0.1f, abilityCooldown);
            Active = false; fireRequested = false;
            if (ownsMotion && !health.IsDead && combat.Action == CombatAction.Idle) motor.ClearActionMotion();
            ownsMotion = false;
            if (active) Finished?.Invoke();
        }
        public void ResetForRespawn()
        {
            Cancel(); readyAt = 0f;
        }
        void OnDrawGizmosSelected()
        {
            if (!firePoint) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(firePoint.position, Mathf.Max(0.01f, projectileRadius));
            Gizmos.DrawLine(firePoint.position, Application.isPlaying && WindingUp ? AimPoint : firePoint.position + firePoint.forward * 2f);
            if (aimCamera) Gizmos.DrawRay(aimCamera.transform.position, aimCamera.transform.forward * Mathf.Max(1f, aimDistance));
        }
    }
}
