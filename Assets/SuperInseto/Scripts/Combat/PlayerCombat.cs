using System;
using UnityEngine;

namespace SuperInseto
{
    public enum CombatAction { Idle, Light, Heavy, Dodge, Dead }

    [DefaultExecutionOrder(-75)]
    [RequireComponent(typeof(PlayerMotor), typeof(Health), typeof(MeleeHitbox))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [Header("Attacks (seconds include recovery)")]
        [SerializeField, Min(1f)] float lightAttackDamage = 12f;
        [SerializeField, Min(1f)] float heavyAttackDamage = 30f;
        [SerializeField, Min(0.1f)] float lightAttackCooldown = 0.38f;
        [SerializeField, Min(0.1f)] float heavyAttackCooldown = 0.8f;
        [SerializeField, Min(0.05f)] float comboWindow = 0.28f;
        [SerializeField] Vector2 lightDamageWindow = new Vector2(0.1f, 0.22f);
        [SerializeField] Vector2 heavyDamageWindow = new Vector2(0.28f, 0.43f);
        [Tooltip("Enable only after clips/Animation Events are wired through CombatAnimationBridge. Recovery still has a timeout.")]
        [SerializeField] bool useAnimationEvents;
        [Header("Dodge")]
        [SerializeField, Min(0.1f)] float dodgeDistance = 2.5f;
        [SerializeField, Min(0.1f)] float dodgeDuration = 0.25f;
        [SerializeField, Min(0f)] float dodgeCooldown = 0.55f;
        [SerializeField, Min(0f)] float invulnerabilityStart = 0.04f;
        [SerializeField, Min(0f)] float invulnerabilityDuration = 0.12f;
        public CombatAction Action { get; private set; }
        public int ComboStep { get; private set; }
        public float ActionProgress => actionDuration > 0f ? Mathf.Clamp01(elapsed / actionDuration) : 0f;
        public event System.Action<CombatAction, int> ActionStarted;
        public event System.Action ActionEnded;
        readonly LightCombo combo = new LightCombo();
        PlayerMotor motor;
        PlayerInputReader input;
        WallClimber climber;
        CharacterController body;
        Health health;
        MeleeHitbox hitbox;
        float elapsed, actionDuration, canDodgeAt, combatClock;
        Vector3 dodgeDirection;
        bool queuedLight;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>(); input = GetComponent<PlayerInputReader>();
            climber = GetComponent<WallClimber>(); body = GetComponent<CharacterController>();
            health = GetComponent<Health>(); hitbox = GetComponent<MeleeHitbox>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        }
        void OnEnable() { health.Died += Die; }
        void OnDisable()
        {
            health.Died -= Die;
            EndAction();
        }

        bool CanStart => motor.enabled && body.enabled && body.isGrounded && !motor.IsCrouched
            && !input.Crouch && !climber.Attached && !health.IsDead && input.Captured
            && !input.JumpPressed && !input.InteractPressed;

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;
            combatClock += dt;
            if (health.IsDead) { motor.SetActionMotion(Vector3.zero); return; }
            if (Action != CombatAction.Idle && (!input.Captured || !motor.enabled || climber.Attached)) EndAction();
            if (Action != CombatAction.Idle && elapsed >= actionDuration)
            {
                bool chain = queuedLight && Action == CombatAction.Light && CanStart;
                EndAction();
                if (chain) StartAttack(false);
            }
            if (Action == CombatAction.Idle && CanStart)
            {
                if (input.DodgePressed && combatClock >= canDodgeAt) StartDodge();
                else if (input.HeavyAttackPressed) StartAttack(true);
                else if (input.LightAttackPressed) StartAttack(false);
            }
            if (Action == CombatAction.Idle) return;
            float previous = elapsed;
            float step = Mathf.Min(dt, actionDuration - elapsed);
            elapsed += step;
            if (Action == CombatAction.Dodge)
            {
                motor.SetActionMotion(dodgeDirection * (dodgeDistance / actionDuration) * (step / dt));
                health.Invulnerable = elapsed >= invulnerabilityStart
                    && elapsed < Mathf.Min(actionDuration, invulnerabilityStart + invulnerabilityDuration);
                return;
            }
            motor.SetActionMotion(Vector3.zero);
            if (Action == CombatAction.Light && ComboStep < 3 && input.LightAttackPressed
                && previous >= Mathf.Max(lightDamageWindow.x, actionDuration - comboWindow)) queuedLight = true;
            if (!useAnimationEvents)
            {
                Vector2 window = Action == CombatAction.Heavy ? heavyDamageWindow : lightDamageWindow;
                // Overlap the time interval so a short window is not skipped at low frame rate.
                hitbox.SetWindow(elapsed >= window.x && previous < Mathf.Min(window.y, actionDuration));
            }
            hitbox.Sample();
        }

        void StartAttack(bool heavy)
        {
            Action = heavy ? CombatAction.Heavy : CombatAction.Light;
            actionDuration = Mathf.Max(0.1f, heavy ? heavyAttackCooldown : lightAttackCooldown);
            elapsed = 0f; queuedLight = false;
            if (heavy) { combo.Reset(); ComboStep = 0; }
            else ComboStep = combo.Next(combatClock, actionDuration + comboWindow);
            hitbox.BeginSwing(heavy ? heavyAttackDamage : lightAttackDamage, gameObject);
            ActionStarted?.Invoke(Action, ComboStep);
        }

        void StartDodge()
        {
            Action = CombatAction.Dodge; combo.Reset(); ComboStep = 0;
            elapsed = 0f; queuedLight = false; actionDuration = Mathf.Max(0.1f, dodgeDuration);
            Vector3 forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : transform.forward;
            dodgeDirection = forward * input.Move.y + Vector3.Cross(Vector3.up, forward) * input.Move.x;
            dodgeDirection = dodgeDirection.sqrMagnitude > 0.01f ? dodgeDirection.normalized : transform.forward;
            canDodgeAt = combatClock + actionDuration + dodgeCooldown;
            ActionStarted?.Invoke(Action, 0);
        }

        void EndAction()
        {
            hitbox.EndSwing(); health.Invulnerable = false;
            motor.ClearActionMotion(); queuedLight = false;
            Action = CombatAction.Idle;
            ActionEnded?.Invoke();
        }
        void Die()
        {
            EndAction(); climber.Detach(); combo.Reset();
            Action = CombatAction.Dead; motor.SetActionMotion(Vector3.zero);
            ActionStarted?.Invoke(Action, 0);
        }
        public void AnimationOpenDamageWindow()
        {
            if (useAnimationEvents && (Action == CombatAction.Light || Action == CombatAction.Heavy)
                && elapsed < actionDuration) hitbox.SetWindow(true);
        }
        public void AnimationCloseDamageWindow() { hitbox.SetWindow(false); }
    }
}
