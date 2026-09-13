using System;
using UnityEngine;

namespace SuperInseto
{
    // Normal combat resolves simultaneous mouse/Q input first; this runs before interaction and locomotion.
    [DefaultExecutionOrder(-70)]
    [RequireComponent(typeof(PlayerCombat), typeof(ChitinEnergy))]
    public sealed class ChitinImpact : MonoBehaviour
    {
        [SerializeField] Transform impactOrigin;
        [SerializeField, Min(0f)] float energyCost = 35f;
        [SerializeField, Min(1f)] float impactDamage = 35f;
        [SerializeField, Min(0.1f)] float impactRadius = 3f;
        [Tooltip("Seconds from activation. Cancellation does not refund the cooldown.")]
        [SerializeField, Min(0.1f)] float impactCooldown = 5f;
        [SerializeField, Min(0.05f)] float impactWindup = 0.5f;
        [SerializeField, Min(0.05f)] float impactRecovery = 0.5f;
        [SerializeField] LayerMask targetMask = ~4;
        [Tooltip("Scene solids block the wave; Player (2) and Enemy (8) bodies do not shield nearby enemies.")]
        [SerializeField] LayerMask obstructionMask = ~((1 << 2) | (1 << 8));
        [Tooltip("Use CombatAnimationBridge.EmitChitinImpact. Without an event, windup + recovery is the safety timeout.")]
        [SerializeField] bool useAnimationEvents;
        readonly RadialDamage damageQuery = new RadialDamage();
        PlayerCombat combat;
        PlayerMotor motor;
        PlayerInputReader input;
        WallClimber climber;
        CharacterController body;
        Health health;
        ChitinEnergy energy;
        float startedAt, impactedAt, readyAt;
        bool emitted, ownsMotion;
        public bool Active { get; private set; }
        public bool WindingUp => Active && !emitted;
        public float Radius => Mathf.Max(0.1f, impactRadius);
        public Vector3 Origin => impactOrigin ? impactOrigin.position : body ? body.bounds.center : transform.position;
        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
        public float WindupProgress => WindingUp ? Mathf.Clamp01((Time.time - startedAt) / Windup) : 0f;
        public float RecoveryProgress => Active && emitted ? Mathf.Clamp01((Time.time - impactedAt) / Recovery) : 0f;
        public event Action PreparationStarted;
        public event Action<Vector3, float, int> Impacted;
        public event Action Finished;
        float Windup => Mathf.Max(0.05f, impactWindup);
        float Recovery => Mathf.Max(0.05f, impactRecovery);

        void Awake()
        {
            combat = GetComponent<PlayerCombat>(); motor = GetComponent<PlayerMotor>();
            input = GetComponent<PlayerInputReader>(); climber = GetComponent<WallClimber>();
            body = GetComponent<CharacterController>(); health = GetComponent<Health>();
            energy = GetComponent<ChitinEnergy>();
        }
        void OnEnable() { health.Died += Cancel; }
        void OnDisable() { health.Died -= Cancel; Cancel(); }

        public bool TryActivate()
        {
            if (!isActiveAndEnabled || Active || CooldownRemaining > 0f || !combat.CanStartAbility
                || climber.Mantling) return false;
            if (!energy.TrySpend(energyCost)) return false;
            Active = true; emitted = false; startedAt = Time.time;
            readyAt = startedAt + Mathf.Max(0.1f, impactCooldown);
            ownsMotion = true; motor.SetActionMotion(Vector3.zero);
            PreparationStarted?.Invoke();
            return true;
        }
        bool CanContinue => !health.IsDead && combat.isActiveAndEnabled && combat.Action == CombatAction.Idle
            && motor.isActiveAndEnabled && body.enabled && input.Captured && !climber.Attached && !climber.Mantling;
        void Update()
        {
            if (input.ImpactPressed) TryActivate();
            if (!Active) return;
            if (!CanContinue) { Cancel(); return; }
            if (!useAnimationEvents && !emitted && Time.time - startedAt >= Windup) Emit();
            if ((emitted && Time.time - impactedAt >= Recovery)
                || (!emitted && Time.time - startedAt >= Windup + Recovery)) Cancel();
        }
        void Emit()
        {
            if (!Active || emitted || !CanContinue) return;
            emitted = true; impactedAt = Time.time; // Latch before invoking receiver/presentation callbacks.
            Vector3 origin = Origin;
            int hits = damageQuery.Apply(origin, Radius, impactDamage, gameObject, targetMask, obstructionMask);
            Impacted?.Invoke(origin, Radius, hits);
        }
        public void AnimationImpact()
        {
            if (useAnimationEvents && Time.time - startedAt < Windup + Recovery) Emit();
        }
        public void Cancel()
        {
            bool active = Active; Active = false;
            // Death now owns the motor lock. Never clear it or grant invulnerability here.
            if (ownsMotion && !health.IsDead && combat.Action == CombatAction.Idle) motor.ClearActionMotion();
            ownsMotion = false;
            if (active) Finished?.Invoke();
        }
        public void ResetForRespawn()
        {
            Cancel(); readyAt = 0f;
        }
        void OnDrawGizmosSelected()
        { Gizmos.color = Color.green; Gizmos.DrawWireSphere(Origin, Radius); }
    }
}
