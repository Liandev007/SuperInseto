using UnityEngine;

namespace SuperInseto
{
    // After PlayerMotor.Update, before Unity's Animator evaluation. No controller.Move, locks, damage or energy.
    [DefaultExecutionOrder(10)]
    public sealed class PlayerAnimationBridge : MonoBehaviour
    {
        Animator animator;
        Transform offsetRoot;
        PlayerAnimationDefinition settings;
        PlayerMotor motor;
        PlayerInputReader input;
        PlayerCombat combat;
        Health health;
        WallClimber climber;
        LedgeMantle mantle;
        ChitinImpact impact;
        BioelectricStinger stinger;
        PlayerRespawn respawn;
        Vector3 offsetPosition, offsetScale, modelPosition, modelScale;
        Quaternion offsetRotation, modelRotation;
        bool initialized, subscribed, wasGrounded, takingOff, restartAction, nextHeavyMirrored, heavyMirrored;
        bool previousTraversal;
        float airElapsed, landingElapsed, landingDuration, hitElapsed, queuedHitUntil, deathAt, suppressLandingUntil;
        public PlayerVisualState CurrentVisualState { get; private set; }

        public bool Initialize(GameObject player, Transform visualOffset, Animator target, PlayerAnimationDefinition definition)
        {
            motor = player.GetComponent<PlayerMotor>(); input = player.GetComponent<PlayerInputReader>();
            combat = player.GetComponent<PlayerCombat>(); health = player.GetComponent<Health>();
            climber = player.GetComponent<WallClimber>(); mantle = player.GetComponent<LedgeMantle>();
            impact = player.GetComponent<ChitinImpact>(); stinger = player.GetComponent<BioelectricStinger>();
            respawn = player.GetComponent<PlayerRespawn>();
            if (!motor || !input || !combat || !health || !climber || !mantle || !visualOffset
                || !target || !target.avatar || !target.avatar.isValid || !target.avatar.isHuman || !target.runtimeAnimatorController)
                return false;
            animator = target; offsetRoot = visualOffset; settings = definition;
            offsetPosition = offsetRoot.localPosition; offsetRotation = offsetRoot.localRotation; offsetScale = offsetRoot.localScale;
            modelPosition = animator.transform.localPosition; modelRotation = animator.transform.localRotation;
            modelScale = animator.transform.localScale;
            initialized = true;
            return true;
        }

        void OnEnable()
        {
            if (!initialized) return;
            combat.ActionStarted += ActionStarted;
            health.Damaged += Damaged; health.Died += Died;
            if (impact) impact.PreparationStarted += AbilityStarted;
            if (stinger) stinger.PreparationStarted += AbilityStarted;
            if (respawn) respawn.Respawned += ResetPresentation;
            subscribed = true;
            ResetPresentation();
        }
        void OnDisable()
        {
            if (subscribed)
            {
                combat.ActionStarted -= ActionStarted;
                health.Damaged -= Damaged; health.Died -= Died;
                if (impact) impact.PreparationStarted -= AbilityStarted;
                if (stinger) stinger.PreparationStarted -= AbilityStarted;
                if (respawn) respawn.Respawned -= ResetPresentation;
                subscribed = false;
            }
            RestoreOffsets();
        }
        void ActionStarted(CombatAction action, int comboStep)
        {
            if (action == CombatAction.Dead) { Died(); return; }
            if (action == CombatAction.Heavy)
            { heavyMirrored = nextHeavyMirrored; nextHeavyMirrored = !nextHeavyMirrored; }
            restartAction = true;
            landingElapsed = settings.landingDuration;
        }
        void AbilityStarted() { restartAction = true; landingElapsed = settings.landingDuration; }
        void Damaged(DamageInfo damage)
        {
            if (health.IsDead) return;
            // Health raises this only for accepted damage; invulnerability produces no animation request.
            queuedHitUntil = Time.time + Mathf.Max(0f, settings.hitQueueLifetime);
            if (CurrentVisualState == PlayerVisualState.HitReaction) hitElapsed = 0f;
        }
        void Died()
        {
            deathAt = Time.time;
            queuedHitUntil = float.NegativeInfinity;
            hitElapsed = float.PositiveInfinity;
            restartAction = false;
            RestoreOffsets();
        }

        public void ResetPresentation()
        {
            if (!initialized || !animator) return;
            RestoreOffsets();
            animator.applyRootMotion = false;
            animator.speed = 1f;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            for (int i = 0; i < PlayerAnimationState.Names.Length; i++)
                if (PlayerAnimationState.HasMotionTime((PlayerVisualState)i)) animator.SetFloat(PlayerAnimationState.TimeHashes[i], 0f);
            animator.SetFloat(PlayerAnimationState.Speed, 0f);
            animator.SetFloat(PlayerAnimationState.LocomotionRate, 1f);
            animator.SetFloat(PlayerAnimationState.ClimbRate, 0f);
            animator.SetFloat(PlayerAnimationState.VerticalVelocity, 0f);
            animator.SetBool(PlayerAnimationState.IsDead, health.IsDead);
            animator.SetBool(PlayerAnimationState.IsDodging, false);
            animator.SetBool(PlayerAnimationState.IsMantling, false);
            animator.SetBool(PlayerAnimationState.IsClimbing, false);
            animator.SetBool(PlayerAnimationState.IsGrounded, motor.IsGrounded);
            animator.SetBool(PlayerAnimationState.IsSprinting, false);
            animator.SetInteger(PlayerAnimationState.AttackIndex, 0);
            CurrentVisualState = health.IsDead ? PlayerVisualState.Death : PlayerVisualState.Locomotion;
            animator.Play(PlayerAnimationState.Hashes[(int)CurrentVisualState], 0, 0f);
            animator.Update(0f);
            wasGrounded = motor.IsGrounded;
            previousTraversal = climber.Attached;
            airElapsed = 0f; takingOff = false; restartAction = false;
            heavyMirrored = nextHeavyMirrored = false;
            landingElapsed = hitElapsed = float.PositiveInfinity;
            queuedHitUntil = float.NegativeInfinity;
            deathAt = Time.time;
            suppressLandingUntil = Time.time + 0.15f;
        }

        void Update()
        {
            if (!initialized || !animator || !animator.isActiveAndEnabled || !animator.runtimeAnimatorController) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;
            bool grounded = motor.IsGrounded;
            bool traversal = climber.Attached || mantle.Active;
            float speed = motor.ActualPlanarVelocity.magnitude;

            if (!grounded && !traversal)
            {
                if (wasGrounded) { airElapsed = 0f; takingOff = motor.VerticalVelocity > 0.1f; }
                else airElapsed += dt;
            }
            else if (grounded && !wasGrounded && !previousTraversal && !traversal
                && airElapsed >= 0.04f && Time.time >= suppressLandingUntil)
            {
                landingElapsed = 0f;
                // Landing never locks motion. Keep a moving landing brief to return to the real run.
                landingDuration = speed > 0.2f ? Mathf.Min(0.12f, settings.landingDuration) : settings.landingDuration;
            }
            if (grounded || traversal) { airElapsed = 0f; takingOff = false; }
            wasGrounded = grounded; previousTraversal = traversal;

            bool committed = combat.Action != CombatAction.Idle || (impact && impact.Active) || (stinger && stinger.Active);
            if (!committed && !traversal && grounded && queuedHitUntil >= Time.time)
            { hitElapsed = 0f; queuedHitUntil = float.NegativeInfinity; }
            else hitElapsed += dt;

            var next = SelectState(grounded, traversal);
            SetTelemetry(grounded, traversal, speed, dt);
            if (PlayerAnimationState.HasMotionTime(next))
                animator.SetFloat(PlayerAnimationState.TimeHashes[(int)next], Mathf.Clamp01(PoseTime(next)));
            bool restart = restartAction && IsCommittedVisual(next);
            if (next != CurrentVisualState || restart)
            {
                float blend = next == PlayerVisualState.Locomotion ? settings.recoveryBlend
                    : IsCommittedVisual(next) || next == PlayerVisualState.HitReaction || next == PlayerVisualState.Death
                        ? settings.actionBlend : settings.traversalBlend;
                animator.CrossFadeInFixedTime(PlayerAnimationState.Hashes[(int)next], Mathf.Max(0f, blend), 0, 0f);
                CurrentVisualState = next;
            }
            restartAction = false;
            landingElapsed += dt;
            ApplyVisualOffsets();
        }

        PlayerVisualState SelectState(bool grounded, bool traversal)
        {
            if (health.IsDead || combat.Action == CombatAction.Dead) return PlayerVisualState.Death;
            if (mantle.Active) return PlayerVisualState.Mantle;
            if (traversal) return PlayerVisualState.Climb;
            switch (combat.Action)
            {
                case CombatAction.Dodge: return PlayerVisualState.Dodge;
                case CombatAction.Heavy: return heavyMirrored ? PlayerVisualState.HeavyLeft : PlayerVisualState.HeavyRight;
                case CombatAction.Light: return combat.ComboStep == 2 ? PlayerVisualState.Light2
                    : combat.ComboStep == 3 ? PlayerVisualState.Light3 : PlayerVisualState.Light1;
            }
            if (impact && impact.Active) return PlayerVisualState.ChitinImpact;
            if (stinger && stinger.Active) return PlayerVisualState.BioelectricStinger;
            if (!grounded) return takingOff && airElapsed < settings.takeoffDuration && motor.VerticalVelocity > 0f
                ? PlayerVisualState.Takeoff : PlayerVisualState.Floating;
            if (hitElapsed < settings.hitReactionDuration) return PlayerVisualState.HitReaction;
            if (landingElapsed < landingDuration) return PlayerVisualState.Landing;
            return PlayerVisualState.Locomotion;
        }

        float PoseTime(PlayerVisualState state)
        {
            switch (state)
            {
                case PlayerVisualState.Takeoff: return airElapsed / Mathf.Max(0.02f, settings.takeoffDuration);
                case PlayerVisualState.Landing: return landingElapsed / Mathf.Max(0.02f, landingDuration);
                case PlayerVisualState.Mantle: return mantle.Progress;
                case PlayerVisualState.Dodge: return combat.ActionProgress;
                case PlayerVisualState.HitReaction: return hitElapsed / Mathf.Max(0.02f, settings.hitReactionDuration);
                case PlayerVisualState.Death:
                    return (Time.time - deathAt) / Mathf.Max(0.04f, (respawn ? respawn.Delay : 2f) - settings.deathHoldDuration);
                case PlayerVisualState.Light1: return CombatPose(settings.light1Contact);
                case PlayerVisualState.Light2: return CombatPose(settings.light2Contact);
                case PlayerVisualState.Light3: return CombatPose(settings.light3Contact);
                case PlayerVisualState.HeavyRight:
                case PlayerVisualState.HeavyLeft: return CombatPose(settings.heavyContact);
                case PlayerVisualState.ChitinImpact:
                    return impact.WindingUp ? impact.WindupProgress * settings.impactPose
                        : Mathf.Lerp(settings.impactPose, 1f, impact.RecoveryProgress);
                case PlayerVisualState.BioelectricStinger:
                    return stinger.WindingUp ? stinger.WindupProgress * settings.stingerPose
                        : Mathf.Lerp(settings.stingerPose, 1f, stinger.RecoveryProgress);
                default: return 0f;
            }
        }
        float CombatPose(Vector2 contact) => PlayerAnimationState.ContactTime(combat.ActionElapsed,
            combat.ActionDuration, combat.DamageWindow, contact);
        static bool IsCommittedVisual(PlayerVisualState state) => state == PlayerVisualState.Light1
            || state == PlayerVisualState.Light2 || state == PlayerVisualState.Light3 || state == PlayerVisualState.HeavyRight
            || state == PlayerVisualState.HeavyLeft || state == PlayerVisualState.Dodge
            || state == PlayerVisualState.ChitinImpact || state == PlayerVisualState.BioelectricStinger;

        void SetTelemetry(bool grounded, bool traversal, float speed, float dt)
        {
            animator.SetFloat(PlayerAnimationState.Speed, speed < 0.06f ? 0f : speed, Mathf.Max(0.001f, settings.locomotionBlend), dt);
            float sprint = input.Sprint && !motor.IsCrouched
                ? Mathf.InverseLerp(motor.RunSpeed, Mathf.Max(motor.RunSpeed + 0.01f, motor.SprintSpeed), speed) : 0f;
            animator.SetFloat(PlayerAnimationState.LocomotionRate, Mathf.Lerp(1f, settings.sprintRate, sprint));
            // The controller's vertical motion belongs to climbing; input selects reverse playback on descent.
            float climbRate = climber.Moving ? motor.ActualVelocity.magnitude / Mathf.Max(0.1f, settings.climbReferenceSpeed)
                * (input.Move.y < -0.05f ? -1f : 1f) : 0f;
            animator.SetFloat(PlayerAnimationState.ClimbRate, climbRate);
            animator.SetFloat(PlayerAnimationState.VerticalVelocity, motor.VerticalVelocity);
            animator.SetBool(PlayerAnimationState.IsGrounded, grounded);
            animator.SetBool(PlayerAnimationState.IsSprinting, sprint > 0.01f);
            animator.SetBool(PlayerAnimationState.IsClimbing, traversal);
            animator.SetBool(PlayerAnimationState.IsMantling, mantle.Active);
            animator.SetBool(PlayerAnimationState.IsDodging, combat.Action == CombatAction.Dodge);
            animator.SetBool(PlayerAnimationState.IsDead, health.IsDead);
            animator.SetInteger(PlayerAnimationState.AttackIndex, combat.Action == CombatAction.Light ? combat.ComboStep : 0);
        }

        // Discard extracted motion even if a clip imports root curves. Only these VISUAL transforms are restored.
        // OnAnimatorMove runs before M9.LateUpdate, so socket positions include this frame's alignment.
        void OnAnimatorMove()
        {
            if (!initialized) return;
            RestoreModelTransform();
            ApplyVisualOffsets();
        }
        void ApplyVisualOffsets()
        {
            if (!offsetRoot) return;
            offsetRoot.localPosition = offsetPosition + settings.baseVisualOffset;
            offsetRoot.localRotation = offsetRotation;
            if (!health.IsDead && mantle.Active)
                offsetRoot.localPosition += PlayerAnimationState.MantleOffset(mantle.Progress,
                    settings.mantleStartOffset, settings.mantleMiddleOffset, settings.mantleEndOffset);
            if (!health.IsDead && combat.Action == CombatAction.Dodge && combat.DodgeDirection.sqrMagnitude > 0.001f)
                offsetRoot.rotation = Quaternion.LookRotation(combat.DodgeDirection, Vector3.up) * offsetRotation;
        }
        void RestoreModelTransform()
        {
            if (!animator) return;
            animator.transform.SetLocalPositionAndRotation(modelPosition, modelRotation);
            animator.transform.localScale = modelScale;
        }
        void RestoreOffsets()
        {
            if (!initialized || !offsetRoot) return;
            offsetRoot.SetLocalPositionAndRotation(offsetPosition + settings.baseVisualOffset, offsetRotation);
            offsetRoot.localScale = offsetScale;
            RestoreModelTransform();
        }
    }
}
