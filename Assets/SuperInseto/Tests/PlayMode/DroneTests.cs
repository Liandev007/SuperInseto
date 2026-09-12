using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SuperInseto.Tests
{
    public sealed class DroneTests
    {
        GameObject root;
        static readonly Vector3 Origin = new Vector3(6000f, 0f, 6000f);
        [SetUp] public void Setup() { root = new GameObject("Drone tests"); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
        GameObject At(string name, Vector3 offset, int layer = 0)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            go.transform.position = Origin + offset; go.layer = layer; return go;
        }
        static void Set(object target, string field, object value)
        { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        DroneFlight Flight(Vector3 offset)
        {
            var go = At("Drone flight", offset, 8); go.AddComponent<SphereCollider>().radius = 0.45f;
            var flight = go.AddComponent<DroneFlight>();
            flight.ConfigureVolume(Origin + new Vector3(0f, 2.5f, 0f), new Vector3(12f, 5f, 12f), 0f);
            Physics.SyncTransforms(); Assert.That(flight.TryPlace(go.transform.position), Is.True);
            return flight;
        }
        void Simulate(DroneFlight flight, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                Vector3 before = flight.transform.position;
                flight.Simulate(1f / 60f); Physics.SyncTransforms();
                Assert.That(Vector3.Distance(before, flight.transform.position), Is.LessThanOrEqualTo(2.1f / 60f + 0.001f));
                Assert.That(flight.transform.position.y, Is.InRange(1.099f, 3.201f));
            }
        }
        [Test]
        public void Flight_Reaches3DPointAndClampsHeight_WithoutTeleporting()
        {
            var flight = Flight(new Vector3(-3f, 1.45f, -3f));
            var destination = Origin + new Vector3(3f, 2.8f, 3f);
            flight.MoveTo(destination, false); Simulate(flight, 500);
            Assert.That(Vector3.Distance(flight.transform.position, destination), Is.LessThan(0.08f));
            flight.MoveTo(Origin + new Vector3(3f, 100f, 3f), true); Simulate(flight, 100);
            Assert.That(flight.transform.position.y, Is.InRange(3.15f, 3.201f));
            flight.MoveTo(Origin + new Vector3(3f, -100f, 3f), true); Simulate(flight, 200);
            Assert.That(flight.transform.position.y, Is.InRange(1.099f, 1.15f));
        }
        [Test]
        public void Flight_DoesNotCrossSolidWallOrLowCeiling_AndShutdownStopsMotion()
        {
            var flight = Flight(new Vector3(0f, 1.45f, -3f));
            var wall = At("Wall", new Vector3(0f, 2.5f, 0f)).AddComponent<BoxCollider>();
            wall.size = new Vector3(20f, 5f, 0.1f); Physics.SyncTransforms();
            flight.MoveTo(Origin + new Vector3(0f, 1.45f, 4f), true); Simulate(flight, 250);
            Assert.That(flight.transform.position.z, Is.LessThan(Origin.z - 0.49f));
            var ceiling = At("Low ceiling", new Vector3(0f, 2.6f, 0f)).AddComponent<BoxCollider>();
            ceiling.size = new Vector3(20f, 0.1f, 20f); Physics.SyncTransforms();
            flight.MoveTo(flight.transform.position + Vector3.up * 10f, true); Simulate(flight, 200);
            Assert.That(flight.transform.position.y, Is.LessThan(2.12f));
            flight.Shutdown(); Vector3 stopped = flight.transform.position;
            flight.MoveTo(Origin + new Vector3(4f, 1.45f, -4f), true); flight.Simulate(1f);
            Assert.That(flight.transform.position, Is.EqualTo(stopped));
        }
        [Test]
        public void Flight_CanSlideOffCoverBeforeDescendingToMeleeHeight()
        {
            var flight = Flight(new Vector3(0f, 2.4f, 0f));
            var cover = At("Cover", new Vector3(0f, 0.8f, 0f)).AddComponent<BoxCollider>();
            cover.size = new Vector3(2f, 1.6f, 2f); Physics.SyncTransforms();
            for (int i = 0; i < 300; i++)
            {
                flight.MoveTo(flight.CombatPoint(flight.transform.position), true);
                flight.Simulate(1f / 60f); Physics.SyncTransforms();
                Assert.That(Vector3.Distance(cover.ClosestPoint(flight.transform.position), flight.transform.position), Is.GreaterThanOrEqualTo(0.449f));
            }
            Assert.That(flight.transform.position.y, Is.InRange(1.4f, 1.5f));
        }
        [Test]
        public void GroundMelee_ReachesCombatHeight_HeavyDealsMore_DeathCancelsPendingShot()
        {
            var go = At("Drone", new Vector3(0f, 1.45f, 1.3f), 8); go.SetActive(false);
            go.AddComponent<SphereCollider>().radius = 0.45f;
            var brain = go.AddComponent<DroneBrain>(); var health = go.GetComponent<Health>();
            Set(health, "maxHealth", 60f); go.SetActive(true);
            var flight = go.GetComponent<DroneFlight>();
            flight.ConfigureVolume(Origin + new Vector3(0f, 2.5f, 0f), new Vector3(12f, 5f, 12f), 0f);
            var player = At("Attacker", Vector3.up, 2);
            var hitbox = player.AddComponent<MeleeHitbox>(); Set(hitbox, "attackOrigin", player.transform);
            Physics.SyncTransforms();
            hitbox.BeginSwing(12f, player); hitbox.SetWindow(true); hitbox.Sample(); hitbox.Sample();
            Assert.That(health.CurrentHealth, Is.EqualTo(48f));
            hitbox.BeginSwing(30f, player); hitbox.SetWindow(true); hitbox.Sample();
            Assert.That(health.CurrentHealth, Is.EqualTo(18f));
            var target = At("Shot target", new Vector3(0f, 1f, 5f), 2);
            target.AddComponent<BoxCollider>(); var targetHealth = target.AddComponent<Health>();
            var weapon = go.GetComponent<RangedWeapon>();
            var muzzle = new GameObject("FirePoint"); muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, -0.05f, 0.53f);
            Set(go.GetComponent<EnemyPerception>(), "eye", go.transform);
            Set(weapon, "firePoint", muzzle.transform);
            Set(weapon, "projectilePrefab", At("Projectile template", Vector3.left * 20f).AddComponent<SimpleProjectile>());
            Physics.SyncTransforms();
            Assert.That(weapon.TryStart(targetHealth, target.GetComponent<Collider>(), 14f), Is.True);
            flight.MoveTo(Origin + new Vector3(3f, 1.45f, 3f), true);
            hitbox.BeginSwing(30f, player); hitbox.SetWindow(true); hitbox.Sample();
            Assert.That(brain.State, Is.EqualTo(DroneState.Dead)); Assert.That(weapon.Active, Is.False);
            Assert.That(go.GetComponent<SphereCollider>().enabled, Is.False);
            Vector3 deathPosition = go.transform.position; flight.Simulate(1f);
            Assert.That(go.transform.position, Is.EqualTo(deathPosition));
            Assert.That(weapon.TryStart(targetHealth, target.GetComponent<Collider>(), 14f), Is.False);
            Assert.That(brain.ClassifyDistance(1f), Is.EqualTo(DroneDistance.TooClose));
            Assert.That(brain.ClassifyDistance(2.5f), Is.EqualTo(DroneDistance.Close));
            Assert.That(brain.ClassifyDistance(4f), Is.EqualTo(DroneDistance.Preferred));
            Assert.That(brain.ClassifyDistance(10f), Is.EqualTo(DroneDistance.Far));
        }
    }
}
