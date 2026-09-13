using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class PlayerAnimationBridgeTests
    {
        static readonly Vector3 Origin = new Vector3(15000f, 0f, 15000f);
        GameObject root;
        CursorLockMode previousLock;
        bool previousVisible;
        [SetUp] public void Setup()
        {
            root = new GameObject("M14.5 presentation tests"); root.transform.position = Origin;
            previousLock = Cursor.lockState; previousVisible = Cursor.visible;
        }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(root); Cursor.lockState = previousLock; Cursor.visible = previousVisible; }
        static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        GameObject Child(string name, Transform parent = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent ? parent : root.transform, false); return go;
        }

        [Test]
        public void CastOriginUsesCurrentHandsAndFacing_AndKeepsMuzzleObstruction()
        {
            var player = Child("Player"); player.layer = 2;
            var animator = player.AddComponent<Animator>();
            var left = Child("LeftHand", player.transform).transform;
            var right = Child("RightHand", player.transform).transform;
            left.localPosition = new Vector3(-0.1f, 1.2f, 0.7f);
            right.localPosition = new Vector3(0.1f, 1.2f, 0.7f);
            var origin = player.AddComponent<BioelectricCastOrigin>();
            // Math/socket test only; real Humanoid validation is the Editor builder's gate.
            Set(origin, "sourceAnimator", animator); Set(origin, "player", player.transform);
            Set(origin, "leftHand", left); Set(origin, "rightHand", right); Set(origin, "forwardOffset", 0.12f);
            Assert.That(origin.TryGetPosition(out var first), Is.True);
            Assert.That(Vector3.Distance(first, Origin + new Vector3(0f, 1.2f, 0.82f)), Is.LessThan(0.005f));
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            left.localPosition += Vector3.up * 0.2f; right.localPosition += Vector3.up * 0.2f;
            Assert.That(origin.TryGetPosition(out var current), Is.True);
            Assert.That(Vector3.Distance(current, (left.position + right.position) * 0.5f + player.transform.forward * 0.12f), Is.LessThan(0.005f));
            Assert.That(Vector3.Distance(first, current), Is.GreaterThan(0.5f));
            var wall = Child("Wall").AddComponent<BoxCollider>();
            wall.transform.position = Origin + new Vector3(0.4f, 1.4f, 0f);
            wall.size = new Vector3(0.05f, 2f, 2f);
            Physics.SyncTransforms();
            Assert.That(new BioelectricAim().MuzzleClear(Origin + Vector3.up * 0.9f, current, 0.1f, player.transform, ~4), Is.False);
        }

        [Test]
        public void MissingHandsRequestsFallbackOnce_WithoutThrowing()
        {
            var origin = Child("Missing hands").AddComponent<BioelectricCastOrigin>();
            LogAssert.Expect(LogType.Warning, "M14.5: hand bones unavailable; using the existing M9 FirePoint.");
            for (int i = 0; i < 5; i++) Assert.That(origin.TryGetPosition(out _), Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        GameObject Player()
        {
            var floor = Child("Floor").AddComponent<BoxCollider>();
            floor.transform.localPosition = Vector3.down * 0.1f; floor.size = new Vector3(20f, 0.2f, 20f);
            var camera = Child("Camera").AddComponent<Camera>();
            camera.transform.localPosition = new Vector3(0f, 2f, -4f);
            var player = Child("Player"); player.SetActive(false); player.layer = 2;
            player.transform.localPosition = Vector3.up * 0.05f;
            var motor = player.AddComponent<PlayerMotor>(); Set(motor, "cameraTransform", camera.transform);
            var body = player.GetComponent<CharacterController>();
            body.height = 1.8f; body.radius = 0.35f; body.center = Vector3.up * 0.9f;
            player.AddComponent<PlayerCombat>();
            var respawn = player.AddComponent<PlayerRespawn>(); Set(respawn, "respawnDelay", 0.2f);
            var visual = Child("VisualRoot", player.transform);
            var installer = player.AddComponent<PlayerVisualInstaller>(); Set(installer, "visualRoot", visual.transform);
            player.SetActive(true); Physics.SyncTransforms(); body.Move(Vector3.down * 0.2f);
            return player;
        }

        [UnityTest]
        public IEnumerator RealPreview_DeathRespawnClearsAnimatorAndOffsets_WithoutAnotherPlayer()
        {
            var definition = Resources.Load<PlayerAnimationDefinition>(PlayerAnimationDefinition.ResourcePath);
            if (!definition || !definition.IsUsable)
                Assert.Ignore("Build the supplied M14.5 Humanoid preview in Unity before running this lifecycle integration test.");
            var player = Player(); yield return null;
            var bridge = player.GetComponentInChildren<PlayerAnimationBridge>();
            Assert.That(bridge, Is.Not.Null);
            var animator = bridge.GetComponent<Animator>();
            var visualOffset = player.transform.Find("VisualRoot/PlayerVisualRoot");
            Assert.That(player.GetComponentsInChildren<CharacterController>().Length, Is.EqualTo(1));
            Assert.That(player.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(player.GetComponent<Health>().TakeDamage(1000f), Is.True);
            yield return null;
            Assert.That(bridge.CurrentVisualState, Is.EqualTo(PlayerVisualState.Death));
            visualOffset.localPosition = new Vector3(8f, 9f, 10f); // Simulate an interrupted visual offset.
            float deadline = Time.time + 1.5f;
            while (player.GetComponent<Health>().IsDead && Time.time < deadline) yield return null;
            Assert.That(player.GetComponent<Health>().IsDead, Is.False);
            Assert.That(animator.GetBool(PlayerAnimationState.IsDead), Is.False);
            Assert.That(bridge.CurrentVisualState, Is.EqualTo(PlayerVisualState.Locomotion));
            Assert.That(Vector3.Distance(visualOffset.localPosition, definition.baseVisualOffset), Is.LessThan(0.001f));
            Assert.That(player.GetComponent<PlayerMotor>().ActionLocked, Is.False);
            Assert.That(player.GetComponent<PlayerCombat>().Action, Is.EqualTo(CombatAction.Idle));
            Assert.That(player.GetComponent<ChitinEnergy>().CurrentEnergy, Is.EqualTo(100f));
            Assert.That(Vector3.Distance(player.transform.position, Origin), Is.LessThan(0.2f));
        }
    }
}
