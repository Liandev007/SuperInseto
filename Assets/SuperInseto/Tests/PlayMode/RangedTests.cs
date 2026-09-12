using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class RangedTests
    {
        GameObject root;
        static readonly Vector3 Origin = new Vector3(5000f, 0f, 5000f);
        [SetUp] public void Setup() { root = new GameObject("Ranged tests"); }
        [TearDown] public void Cleanup()
        {
            foreach (var p in Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None))
                if (Vector3.Distance(p.transform.position, Origin) < 100f) Object.DestroyImmediate(p.gameObject);
            Object.DestroyImmediate(root);
        }
        GameObject At(string name, Vector3 offset)
        { var go = new GameObject(name); go.transform.SetParent(root.transform); go.transform.position = Origin + offset; return go; }
        static void Set(object target, string name, object value)
        { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        Health Target(Vector3 offset, int layer = 2)
        {
            var go = At("Target", offset); go.layer = layer;
            go.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
            return go.AddComponent<Health>();
        }
        SimpleProjectile Shot(GameObject owner, float lifetime = 1f)
        {
            var shot = At("Projectile", Vector3.up * 1.5f).AddComponent<SimpleProjectile>();
            shot.Launch(owner, Vector3.forward, 12f, 100f, lifetime, 0.09f);
            return shot;
        }
        [Test]
        public void Projectile_IgnoresShooterAndEnemyHierarchy_DealsDamageOnce()
        {
            var owner = Target(new Vector3(0f, 1.5f, 0.1f), 8);
            var friendly = Target(new Vector3(0f, 1.5f, 1f), 8);
            var child = new GameObject("Enemy child collider"); child.transform.SetParent(friendly.transform, false);
            child.AddComponent<BoxCollider>().size = Vector3.one * 0.6f; // Default layer child must also be ignored.
            var player = Target(new Vector3(0f, 1.5f, 4f));
            int events = 0; player.Damaged += _ => events++;
            Physics.SyncTransforms();
            var shot = Shot(owner.gameObject); shot.Advance(0.1f); shot.Advance(0.1f);
            Assert.That(events, Is.EqualTo(1)); Assert.That(player.CurrentHealth, Is.EqualTo(88f));
            Assert.That(owner.CurrentHealth, Is.EqualTo(100f)); Assert.That(friendly.CurrentHealth, Is.EqualTo(100f));
            Assert.That(shot.Flying, Is.False);
        }
        [Test]
        public void Projectile_SweepsThinWall_HandlesInitialOverlap_Expires()
        {
            var player = Target(new Vector3(0f, 1.5f, 4f));
            var wall = At("Thin wall", new Vector3(0f, 1.5f, 2f)).AddComponent<BoxCollider>();
            wall.size = new Vector3(2f, 3f, 0.02f); Physics.SyncTransforms();
            var shot = Shot(null); shot.Advance(0.1f);
            Assert.That(player.CurrentHealth, Is.EqualTo(100f)); Assert.That(shot.Flying, Is.False);
            var overlap = Shot(null); overlap.transform.position = wall.transform.position; overlap.Advance(0.1f);
            Assert.That(overlap.Flying, Is.False); Assert.That(player.CurrentHealth, Is.EqualTo(100f));
            var expired = Shot(null, 0.01f); expired.transform.position += Vector3.right * 10f;
            expired.Advance(1f); Assert.That(expired.Flying, Is.False);
        }
        [Test]
        public void Projectile_InvulnerabilityAndMovingOutOfTrajectoryAvoidDamage()
        {
            var player = Target(new Vector3(0f, 1.5f, 4f)); player.Invulnerable = true; Physics.SyncTransforms();
            var first = Shot(null); first.Advance(0.1f);
            Assert.That(player.CurrentHealth, Is.EqualTo(100f)); Assert.That(first.Flying, Is.False);
            player.Invulnerable = false;
            var second = Shot(null);
            player.transform.position += Vector3.right * 2f; Physics.SyncTransforms(); second.Advance(0.1f);
            Assert.That(player.CurrentHealth, Is.EqualTo(100f)); Assert.That(second.Flying, Is.True);
        }
        RangedWeapon Weapon()
        {
            var go = At("Ranged", Vector3.zero); go.layer = 8;
            var weapon = go.AddComponent<RangedWeapon>();
            var muzzle = At("Muzzle", new Vector3(0f, 1.5f, 0.6f)).transform; muzzle.SetParent(go.transform);
            Set(weapon, "firePoint", muzzle);
            Set(go.GetComponent<EnemyPerception>(), "eye", muzzle);
            Set(weapon, "projectilePrefab", At("Projectile template", Vector3.left * 20f).AddComponent<SimpleProjectile>());
            Set(weapon, "attackWindup", 0.2f); Set(weapon, "attackRecovery", 0.1f); Set(weapon, "attackCooldown", 1f);
            return weapon;
        }
        [UnityTest]
        public IEnumerator Weapon_WindupSingleShotAndCooldown()
        {
            var weapon = Weapon(); var player = Target(new Vector3(0f, 1.5f, 4f)); Physics.SyncTransforms();
            int shots = 0; weapon.Fired += () => shots++;
            Assert.That(weapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            Assert.That(shots, Is.Zero); Assert.That(weapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.False);
            yield return new WaitForSeconds(0.5f);
            Assert.That(shots, Is.EqualTo(1)); Assert.That(weapon.Active, Is.False);
            Assert.That(weapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.False);
            yield return new WaitForSeconds(0.9f);
            Assert.That(weapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
        }
        [UnityTest]
        public IEnumerator Weapon_CancelsShotWhenSightIsBlocked_OrOwnerDies()
        {
            var weapon = Weapon(); var player = Target(new Vector3(0f, 1.5f, 4f)); Physics.SyncTransforms();
            int shots = 0; weapon.Fired += () => shots++;
            Assert.That(weapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            var wall = At("Closing cover", new Vector3(0f, 1.5f, 2f)).AddComponent<BoxCollider>();
            wall.size = new Vector3(2f, 3f, 0.1f); Physics.SyncTransforms();
            yield return new WaitForSeconds(0.4f); Assert.That(shots, Is.Zero); Assert.That(weapon.Active, Is.False);
            wall.enabled = false; Physics.SyncTransforms(); yield return new WaitForSeconds(1f);
            Assert.That(weapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            weapon.GetComponent<Health>().TakeDamage(1000f);
            yield return new WaitForSeconds(0.4f);
            Assert.That(shots, Is.Zero); Assert.That(weapon.Active, Is.False);
            Assert.That(weapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.False);
        }
        [Test]
        public void Brain_DistanceBands_DeathStopsNavigationAndCollision()
        {
            var go = At("Ranged brain", Vector3.zero); go.SetActive(false);
            go.AddComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            var brain = go.AddComponent<RangedBrain>(); go.SetActive(true);
            Assert.That(brain.ClassifyDistance(1f), Is.EqualTo(RangedDistance.TooClose));
            Assert.That(brain.ClassifyDistance(3f), Is.EqualTo(RangedDistance.Close));
            Assert.That(brain.ClassifyDistance(6f), Is.EqualTo(RangedDistance.Preferred));
            Assert.That(brain.ClassifyDistance(10f), Is.EqualTo(RangedDistance.Far));
            Assert.That(brain.ClassifyDistance(20f), Is.EqualTo(RangedDistance.OutOfCombat));
            go.GetComponent<Health>().TakeDamage(1000f);
            Assert.That(brain.State, Is.EqualTo(RangedState.Dead));
            Assert.That(go.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled, Is.False);
            Assert.That(go.GetComponent<Collider>().enabled, Is.False);
            Assert.That(go.GetComponent<RangedWeapon>().Active, Is.False);
        }
    }
}
