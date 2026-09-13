using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class RespawnTests
    {
        static readonly Vector3 Origin = new Vector3(14000f, 0f, 14000f);
        GameObject root, player;
        PlayerRespawn respawn;
        Checkpoint first, second;
        Health health;
        ChitinEnergy energy;
        CursorLockMode oldLock;
        bool oldVisible;
        [SetUp] public void Setup()
        {
            root = new GameObject("M13 tests");
            oldLock = Cursor.lockState; oldVisible = Cursor.visible;
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(root);
            Cursor.lockState = oldLock; Cursor.visible = oldVisible;
        }
        GameObject At(string name, Vector3 offset)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            go.transform.position = Origin + offset; return go;
        }
        static void Set(object target, string field, object value)
        { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        Checkpoint Point(string name, Vector3 offset, int index)
        {
            var go = At(name, offset); go.SetActive(false);
            var point = go.AddComponent<Checkpoint>(); Set(point, "progressIndex", index);
            go.SetActive(true); return point;
        }
        void Build(bool assignInitial = true)
        {
            var floor = At("Floor", Vector3.down * 0.1f).AddComponent<BoxCollider>();
            floor.size = new Vector3(30f, 0.2f, 30f);
            first = Point("Initial", Vector3.zero, 0);
            second = Point("Later", Vector3.right * 4f, 1);
            second.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var viewObject = At("Camera", new Vector3(0f, 1.2f, -4f)); viewObject.SetActive(false);
            var view = viewObject.AddComponent<Camera>();
            player = At("Player", Vector3.up * 0.05f); player.layer = 2; player.SetActive(false);
            var motor = player.AddComponent<PlayerMotor>(); Set(motor, "cameraTransform", view.transform);
            var body = player.GetComponent<CharacterController>();
            body.height = 1.8f; body.radius = 0.35f; body.center = Vector3.up * 0.9f; body.skinWidth = 0.02f;
            player.AddComponent<ChitinImpact>(); var stinger = player.AddComponent<BioelectricStinger>();
            var socket = new GameObject("Socket").transform; socket.SetParent(player.transform, false);
            socket.localPosition = new Vector3(0.3f, 1.2f, 0.6f);
            Set(stinger, "firePoint", socket); Set(stinger, "aimCamera", view);
            Set(stinger, "projectilePrefab", At("Projectile template", Vector3.left * 40f).AddComponent<BioelectricProjectile>());
            stinger.Fired += p => p.transform.SetParent(root.transform);
            health = player.GetComponent<Health>(); energy = player.GetComponent<ChitinEnergy>();
            Set(energy, "regenerationRate", 0f);
            var camera = viewObject.AddComponent<ThirdPersonCamera>();
            Set(camera, "target", player.transform); Set(camera, "input", player.GetComponent<PlayerInputReader>());
            respawn = player.AddComponent<PlayerRespawn>();
            if (assignInitial) Set(respawn, "initialCheckpoint", first);
            Set(respawn, "followCamera", camera); Set(respawn, "respawnDelay", 0.15f);
            viewObject.SetActive(true); player.SetActive(true);
            Physics.SyncTransforms(); body.Move(Vector3.down * 0.2f);
        }
        [UnityTest]
        public IEnumerator Death_IsSingle_BlocksActions_RestoresResourcesAndCooldowns_AndRepeats()
        {
            Build(); yield return null;
            int died = 0, returned = 0;
            respawn.DeathStarted += () => died++; respawn.Respawned += () => returned++;
            Assert.That(respawn.CurrentCheckpoint, Is.SameAs(first));
            var impact = player.GetComponent<ChitinImpact>(); var stinger = player.GetComponent<BioelectricStinger>();
            var combat = player.GetComponent<PlayerCombat>(); var motor = player.GetComponent<PlayerMotor>();
            var input = player.GetComponent<PlayerInputReader>();
            Assert.That(impact.TryActivate(), Is.True);
            Set(stinger, "readyAt", Time.time + 100f);
            Assert.That(health.TakeDamage(1000f), Is.True);
            Vector3 deathPosition = player.transform.position;
            for (int i = 0; i < 5; i++) Assert.That(health.TakeDamage(1000f), Is.False);
            Assert.That(died, Is.EqualTo(1)); Assert.That(respawn.State, Is.EqualTo(RespawnState.Dead));
            Assert.That(input.GameplayBlocked, Is.True); Assert.That(input.Captured, Is.False);
            Assert.That(input.Move, Is.EqualTo(Vector2.zero)); Assert.That(input.InteractPressed, Is.False);
            Assert.That(motor.enabled, Is.False); Assert.That(combat.Action, Is.EqualTo(CombatAction.Dead));
            Assert.That(impact.Active, Is.False); Assert.That(impact.TryActivate(), Is.False);
            Assert.That(stinger.TryActivate(), Is.False);
            Assert.That(player.GetComponent<WallClimber>().Attached, Is.False);
            yield return null;
            Assert.That(player.transform.position, Is.EqualTo(deathPosition));
            yield return new WaitForSeconds(0.22f); yield return null;
            Assert.That(returned, Is.EqualTo(1)); Assert.That(respawn.State, Is.EqualTo(RespawnState.Alive));
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(energy.CurrentEnergy, Is.EqualTo(energy.MaxEnergy)); Assert.That(energy.DelayRemaining, Is.Zero);
            Assert.That(health.Invulnerable, Is.False); Assert.That(input.GameplayBlocked, Is.False);
            Assert.That(motor.enabled, Is.True); Assert.That(motor.ActionLocked, Is.False);
            Assert.That(combat.Action, Is.EqualTo(CombatAction.Idle)); Assert.That(combat.ComboStep, Is.Zero);
            Assert.That(impact.CooldownRemaining, Is.Zero); Assert.That(stinger.CooldownRemaining, Is.Zero);
            Assert.That(combat.CanStartAbility, Is.True);
            Assert.That(stinger.TryActivate(), Is.True); stinger.Cancel();
            health.TakeDamage(1000f);
            yield return new WaitForSeconds(0.22f); yield return null;
            Assert.That(died, Is.EqualTo(2)); Assert.That(returned, Is.EqualTo(2));
            Assert.That(impact.TryActivate(), Is.True); impact.Cancel();
        }
        [UnityTest]
        public IEnumerator Checkpoint_ProgressDoesNotRegress_UsesRotation_AndPreservesWorldState()
        {
            Build(); yield return null;
            int activations = 0; respawn.CheckpointActivated += _ => activations++;
            // Exercise the actual trigger handler with the Player collider.
            second.SendMessage("OnTriggerEnter", player.GetComponent<CharacterController>());
            Assert.That(respawn.CurrentCheckpoint, Is.SameAs(second));
            Assert.That(respawn.TryActivateCheckpoint(second), Is.False);
            Assert.That(respawn.TryActivateCheckpoint(first), Is.False);
            Assert.That(activations, Is.EqualTo(1)); Assert.That(first.IsActive, Is.False);
            var broken = At("Broken", Vector3.left * 8f).AddComponent<DestructibleObject>();
            var node = At("Powered", Vector3.left * 9f).AddComponent<BioelectricReceiver>();
            yield return null;
            broken.GetComponent<Health>().TakeDamage(1000f); node.ReceiveBioelectricImpact();
            health.TakeDamage(1000f); yield return new WaitForSeconds(0.22f); yield return null;
            Assert.That(Vector3.Distance(Vector3.ProjectOnPlane(player.transform.position - second.Position, Vector3.up), Vector3.zero), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(player.transform.rotation, second.Rotation), Is.LessThan(0.1f));
            Assert.That(broken.IsDestroyed, Is.True); Assert.That(node.IsPowered, Is.True);
            var camera = root.GetComponentInChildren<ThirdPersonCamera>();
            Assert.That(Vector3.Distance(camera.transform.position, player.transform.position), Is.LessThan(6f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.y, 90f)), Is.LessThan(1f));
        }
        [UnityTest]
        public IEnumerator MissingInitialReference_StillReturnsToAuthoredPlayerStart()
        {
            Build(false); yield return null;
            // Initial trigger may activate first, so remove it before death and verify the stored fallback.
            Object.DestroyImmediate(first.gameObject);
            Vector3 start = Origin + Vector3.up * 0.05f;
            var body = player.GetComponent<CharacterController>(); body.enabled = false;
            player.transform.position += Vector3.forward * 5f; body.enabled = true;
            health.TakeDamage(1000f); yield return new WaitForSeconds(0.22f); yield return null;
            Assert.That(Vector3.Distance(player.transform.position, start), Is.LessThan(0.2f));
            Assert.That(health.IsDead, Is.False);
        }
        [UnityTest]
        public IEnumerator OccupiedCheckpoint_SelectsClearNearbyPose_AndNeverRevivesInsideSolid()
        {
            Build(); yield return null; respawn.TryActivateCheckpoint(second);
            var blocker = At("Solid at checkpoint", new Vector3(4f, 1f, 0f)).AddComponent<BoxCollider>();
            blocker.size = new Vector3(1f, 2f, 1f);
            Physics.SyncTransforms(); health.TakeDamage(1000f);
            yield return new WaitForSeconds(0.22f); yield return null;
            Assert.That(health.IsDead, Is.False);
            Assert.That(Vector3.Distance(player.transform.position, second.Position), Is.GreaterThan(0.7f));
            Assert.That(Vector3.Distance(player.transform.position, second.Position), Is.LessThan(2.2f));
            // Block both the checkpoint and the fallback: stay dead until there is a safe destination.
            blocker.transform.position = Origin + Vector3.up * 2f; blocker.size = new Vector3(30f, 4f, 30f);
            Physics.SyncTransforms(); health.TakeDamage(1000f);
            yield return new WaitForSeconds(0.22f);
            Assert.That(respawn.State, Is.EqualTo(RespawnState.Dead)); Assert.That(health.IsDead, Is.True);
            Object.DestroyImmediate(blocker.gameObject); Physics.SyncTransforms();
            yield return new WaitForSeconds(0.6f); yield return null;
            Assert.That(respawn.State, Is.EqualTo(RespawnState.Alive));
        }
    }
}
