using NUnit.Framework;
using UnityEngine;

namespace SuperInseto.Tests
{
    // PlayMode physics regressions. No input emulation or scene dependency.
    public sealed class LedgeMantleTests
    {
        GameObject fixture, player;
        CharacterController body;
        WallClimber climber;
        static readonly Vector3 Origin = new Vector3(1000f, 0f, 1000f);

        void Build(bool blocked)
        {
            fixture = new GameObject("Mantle test fixture");
            Box("Floor", new Vector3(0f, -0.25f, 0f), new Vector3(8f, 0.5f, 8f));
            var wall = Box("Wall", new Vector3(0f, 1.5f, 0.3f), new Vector3(4f, 3f, 0.6f));
            wall.AddComponent<ClimbableSurface>();
            Box("Platform", new Vector3(0f, 2.8f, 1.3f), new Vector3(4f, 0.4f, 2f));
            if (blocked) Box("Low ceiling", new Vector3(0f, 4.15f, 0.8f), new Vector3(4f, 0.3f, 2f));
            player = new GameObject("Player");
            player.SetActive(false);
            player.layer = 2;
            player.transform.position = Origin + new Vector3(0f, 1.84f, -0.41f);
            body = player.AddComponent<CharacterController>();
            body.height = 1.8f;
            body.radius = 0.35f;
            body.center = Vector3.up * 0.9f;
            body.skinWidth = 0.035f;
            body.stepOffset = 0f;
            body.minMoveDistance = 0f;
            climber = player.AddComponent<WallClimber>();
            player.SetActive(true);
            Physics.SyncTransforms();
            Assert.That(climber.TryAttach(), Is.True, "Fixture must start attached to its wall.");
        }

        GameObject Box(string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(fixture.transform);
            go.transform.position = Origin + position;
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        void Step(Vector2 input, int count)
        {
            for (int i = 0; i < count && climber.Attached; i++)
                climber.Tick(input, false, 0.02f);
        }

        [Test]
        public void OpenLedge_MantlesContinuouslyAndLands()
        {
            Build(false);
            bool sawMantle = false;
            for (int i = 0; i < 150 && climber.Attached; i++)
            {
                Vector3 before = player.transform.position;
                climber.Tick(Vector2.up, false, 0.02f);
                sawMantle |= climber.Mantling;
                Assert.That(Vector3.Distance(before, player.transform.position), Is.LessThan(0.25f), "No teleport step.");
            }
            Assert.That(sawMantle, Is.True);
            Assert.That(climber.Attached, Is.False);
            Assert.That(body.isGrounded, Is.True);
            Assert.That(player.transform.position.y, Is.EqualTo(3f).Within(0.08f));
            Assert.That(player.transform.position.z - Origin.z, Is.GreaterThan(0.6f));
            Vector3 landed = player.transform.position;
            body.Move(new Vector3(0.15f, -0.05f, 0f));
            Assert.That(player.transform.position.x, Is.GreaterThan(landed.x + 0.1f));
        }

        [Test]
        public void BlockedLedge_HoldsWithoutPenetration_AndCanDescend()
        {
            Build(true);
            Step(Vector2.up, 150);
            Vector3 held = player.transform.position;
            Assert.That(climber.Attached, Is.True);
            Assert.That(climber.Mantling, Is.False);
            Assert.That(held.y + body.height, Is.LessThan(4f));
            Step(Vector2.up, 150);
            Assert.That(Vector3.Distance(held, player.transform.position), Is.LessThan(0.01f));
            Step(Vector2.down, 150);
            Assert.That(climber.Attached, Is.False);
            Assert.That(body.isGrounded, Is.True);
            Assert.That(player.transform.position.y, Is.EqualTo(0f).Within(0.08f));
        }

        [Test]
        public void MissingLandingPlatform_HoldsAtTopOfNarrowWall()
        {
            Build(false);
            Object.DestroyImmediate(fixture.transform.Find("Platform").gameObject);
            Physics.SyncTransforms();
            Step(Vector2.up, 150);
            Assert.That(climber.Attached, Is.True);
            Assert.That(climber.Mantling, Is.False);
            Assert.That(player.transform.position.y, Is.LessThan(2f));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Release_ExitsBlockedHoldOrActiveMantle(bool blocked)
        {
            Build(blocked);
            Step(Vector2.up, 8);
            Assert.That(climber.Attached, Is.True);
            Assert.That(climber.Mantling, Is.EqualTo(!blocked));
            climber.Tick(Vector2.zero, true, 0.02f);
            Assert.That(climber.Attached, Is.False);
            Assert.That(climber.Mantling, Is.False);
            float before = player.transform.position.y;
            body.Move(Vector3.down * 0.1f);
            Assert.That(player.transform.position.y, Is.LessThan(before));
        }

        [TearDown]
        public void Cleanup()
        {
            if (player) Object.DestroyImmediate(player);
            if (fixture) Object.DestroyImmediate(fixture);
        }
    }
}
