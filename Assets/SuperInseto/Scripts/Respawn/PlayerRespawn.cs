using System;
using UnityEngine;

namespace SuperInseto
{
    public enum RespawnState { Alive, Dead, Respawning }

    // Subscribe after combat/abilities. Update after gameplay so a revival cannot reuse old input edges.
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(Health), typeof(PlayerMotor), typeof(ChitinEnergy))]
    public sealed class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] Checkpoint initialCheckpoint;
        [SerializeField] ThirdPersonCamera followCamera;
        [SerializeField, Min(0.1f)] float respawnDelay = 2f;
        [SerializeField, Min(0f)] float clearance = 0.08f;
        [SerializeField, Min(0.1f)] float safeSearchStep = 1f;
        [SerializeField] LayerMask solidMask = ~4;
        [SerializeField] RespawnState state;
        [SerializeField] Checkpoint currentCheckpoint;
        Health health;
        ChitinEnergy energy;
        PlayerInputReader input;
        PlayerMotor motor;
        PlayerCombat combat;
        ChitinImpact impact;
        BioelectricStinger stinger;
        WallClimber climber;
        CharacterController body;
        Vector3 initialPosition;
        Quaternion initialRotation;
        float standingHeight, radius, respawnAt;
        bool motorWasEnabled;
        readonly Collider[] overlaps = new Collider[32];
        public RespawnState State => state;
        public float Delay => Mathf.Max(0.1f, respawnDelay);
        public Checkpoint CurrentCheckpoint => currentCheckpoint;
        public Vector3 CheckpointPosition => currentCheckpoint ? currentCheckpoint.Position : initialPosition;
        public event Action DeathStarted;
        public event Action Respawned;
        public event Action<Checkpoint> CheckpointActivated;

        void Awake()
        {
            health = GetComponent<Health>(); energy = GetComponent<ChitinEnergy>();
            input = GetComponent<PlayerInputReader>(); motor = GetComponent<PlayerMotor>();
            combat = GetComponent<PlayerCombat>(); climber = GetComponent<WallClimber>();
            impact = GetComponent<ChitinImpact>(); stinger = GetComponent<BioelectricStinger>();
            body = GetComponent<CharacterController>();
            initialPosition = transform.position; initialRotation = transform.rotation;
            standingHeight = body.height; radius = body.radius;
            state = RespawnState.Alive;
        }
        void OnEnable() { health.Died += BeginDeath; }
        void OnDisable() { health.Died -= BeginDeath; }
        void Start()
        {
            if (initialCheckpoint && !currentCheckpoint) TryActivateCheckpoint(initialCheckpoint);
            if (health.IsDead) BeginDeath();
        }
        public bool TryActivateCheckpoint(Checkpoint checkpoint)
        {
            if (!checkpoint || !checkpoint.isActiveAndEnabled || state != RespawnState.Alive || health.IsDead
                || checkpoint == currentCheckpoint
                || (currentCheckpoint && checkpoint.ProgressIndex < currentCheckpoint.ProgressIndex)) return false;
            if (currentCheckpoint) currentCheckpoint.SetActiveCheckpoint(false);
            currentCheckpoint = checkpoint;
            checkpoint.SetActiveCheckpoint(true);
            CheckpointActivated?.Invoke(checkpoint);
            return true;
        }
        void BeginDeath()
        {
            if (state != RespawnState.Alive) return;
            state = RespawnState.Dead; // Latch before any callbacks.
            input.BlockGameplay(true);
            if (impact) impact.Cancel();
            if (stinger) stinger.Cancel();
            climber.Detach();
            motorWasEnabled = motor.enabled;
            motor.SetActionMotion(Vector3.zero); motor.enabled = false;
            respawnAt = Time.time + Mathf.Max(0.1f, respawnDelay);
            DeathStarted?.Invoke();
        }
        void Update()
        {
            if (state == RespawnState.Alive)
            {
                if (health.IsDead) BeginDeath(); // Also catches a death while this component was disabled.
                return;
            }
            if (Time.time >= respawnAt) TryRespawn();
        }
        // Startup load uses the same safe placement and cleanup as death, without a death event/delay.
        public void RestoreCheckpointForLoad(Checkpoint checkpoint)
        {
            if (state != RespawnState.Alive) return;
            if (currentCheckpoint) currentCheckpoint.SetActiveCheckpoint(false);
            currentCheckpoint = checkpoint ? checkpoint : initialCheckpoint;
            if (currentCheckpoint) currentCheckpoint.SetActiveCheckpoint(true);
            state = RespawnState.Respawning;
            input.BlockGameplay(true);
            motorWasEnabled = motor.enabled;
            motor.SetActionMotion(Vector3.zero); motor.enabled = false;
            climber.Detach();
            if (impact) impact.Cancel();
            if (stinger) stinger.Cancel();
            TryRespawn();
        }
        void TryRespawn()
        {
            Physics.SyncTransforms();
            Vector3 preferred = CheckpointPosition;
            Quaternion rotation = currentCheckpoint ? currentCheckpoint.Rotation : initialRotation;
            if (!TrySafePosition(preferred, out var position))
            {
                if (!TrySafePosition(initialPosition, out position))
                {
                    respawnAt = Time.time + 0.5f; // Never revive inside solids; bounded retry, no extra coroutine.
                    return;
                }
                rotation = initialRotation;
            }
            state = RespawnState.Respawning;
            body.enabled = false;
            if (impact) impact.ResetForRespawn();
            if (stinger) stinger.ResetForRespawn();
            if (combat) combat.ResetForRespawn();
            motor.ResetForRespawn();
            transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();
            body.enabled = true;
            body.Move(Vector3.down * 0.08f);
            energy.RestoreForRespawn();
            health.RestoreForRespawn();
            motor.enabled = motorWasEnabled;
            if (followCamera) followCamera.ResetForRespawn();
            state = RespawnState.Alive;
            input.BlockGameplay(false);
            Respawned?.Invoke();
        }
        bool TrySafePosition(Vector3 preferred, out Vector3 result)
        {
            // Authored point first, then at most sixteen nearby candidates (two small rings).
            if (ClearGround(preferred, out result)) return true;
            for (int ring = 1; ring <= 2; ring++)
                for (int direction = 0; direction < 8; direction++)
                {
                    float angle = direction * Mathf.PI * 0.25f;
                    var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                        * Mathf.Max(0.1f, safeSearchStep) * ring;
                    if (ClearGround(preferred + offset, out result)) return true;
                }
            result = preferred; return false;
        }
        bool ClearGround(Vector3 candidate, out Vector3 result)
        {
            result = candidate;
            if (!Physics.Raycast(candidate + Vector3.up * 0.5f, Vector3.down, out var ground, 1.5f,
                    solidMask, QueryTriggerInteraction.Ignore)
                || ground.normal.y < Mathf.Cos(body.slopeLimit * Mathf.Deg2Rad)
                || ground.collider.GetComponentInParent<Health>()) return false;
            result = ground.point + Vector3.up * 0.04f;
            float r = radius + Mathf.Max(0f, clearance);
            Vector3 bottom = result + Vector3.up * (r + 0.01f);
            Vector3 top = result + Vector3.up * Mathf.Max(r, standingHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, r, overlaps, solidMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (!overlaps[i].transform.IsChildOf(transform)) return false;
            return true;
        }
    }
}
