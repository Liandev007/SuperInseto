using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class CombatIntegrationTests
    {
        GameObject root;
        static readonly Vector3 Origin = new Vector3(7000f, 0f, 7000f);
        [SetUp] public void Setup() { root = new GameObject("M7 coexistence tests"); }
        [TearDown] public void Cleanup()
        {
            foreach (var projectile in Object.FindObjectsByType<SimpleProjectile>(FindObjectsSortMode.None))
                if (Vector3.Distance(projectile.transform.position, Origin) < 100f)
                    Object.DestroyImmediate(projectile.gameObject);
            Object.DestroyImmediate(root);
        }
        GameObject At(string name, Vector3 offset)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            go.transform.position = Origin + offset; return go;
        }
        static void Set(object target, string name, object value)
        { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        Health Body(string name, Vector3 offset, int layer)
        {
            var go = At(name, offset); go.layer = layer;
            go.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
            return go.AddComponent<Health>();
        }
        SimpleProjectile Shot(Health source, Health target, float damage)
        {
            var go = At("Independent projectile", source.transform.position - Origin);
            var shot = go.AddComponent<SimpleProjectile>();
            shot.Launch(source.gameObject, target.transform.position - go.transform.position, damage, 100f, 1f, 0.09f);
            return shot;
        }

        [TestCase(false, 100, 63, 3, 0)]
        [TestCase(true, 100, 100, 0, 0)]
        [TestCase(false, 15, 0, 2, 1)]
        public void SameTick_MeleeAndTwoProjectiles_DeduplicateRespectInvulnerabilityAndDeath(
            bool invulnerable, int initialHealth, int expectedHealth, int expectedHits, int expectedDeaths)
        {
            var player = Body("Player", new Vector3(0f, 1.05f, 1.3f), 2);
            player.TakeDamage(100 - initialHealth); player.Invulnerable = invulnerable;
            var melee = Body("Melee", new Vector3(0f, 1.05f, 0f), 8);
            var ranged = Body("Ranged", new Vector3(-3f, 1.3f, -3f), 8);
            var drone = Body("Drone", new Vector3(3f, 1.8f, -3f), 8);
            // An ally is also directly in a shot's path, including a child on the Default layer.
            var ally = Body("Enemy obstruction", new Vector3(-1.5f, 1.175f, -0.85f), 8);
            var child = new GameObject("Default-layer ally collider"); child.transform.SetParent(ally.transform, false);
            child.AddComponent<BoxCollider>().size = Vector3.one * 0.6f;
            var hitbox = melee.gameObject.AddComponent<MeleeHitbox>();
            Set(hitbox, "attackOrigin", melee.transform); Set(hitbox, "targetMask", (LayerMask)4);
            Set(hitbox, "obstructionMask", (LayerMask)~0);
            hitbox.BeginSwing(15f, melee.gameObject); hitbox.SetWindow(true);
            int hits = 0, deaths = 0;
            player.Damaged += _ => hits++; player.Died += () => deaths++;
            Physics.SyncTransforms();
            var first = Shot(ranged, player, 12f); var second = Shot(drone, player, 10f);
            first.Advance(0.1f); hitbox.Sample(); second.Advance(0.1f);
            first.Advance(0.1f); hitbox.Sample(); second.Advance(0.1f);
            Assert.That(player.CurrentHealth, Is.EqualTo(expectedHealth));
            Assert.That(hits, Is.EqualTo(expectedHits)); Assert.That(deaths, Is.EqualTo(expectedDeaths));
            Assert.That(first.Flying || second.Flying, Is.False);
            foreach (var enemy in new[] { melee, ranged, drone, ally })
                Assert.That(enemy.CurrentHealth, Is.EqualTo(100f));
        }

        void PrepareWeapon(RangedWeapon weapon, Health player)
        {
            var go = weapon.gameObject; go.layer = 8;
            go.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(player.transform.position - go.transform.position, Vector3.up));
            var muzzle = new GameObject("Test FirePoint").transform; muzzle.SetParent(go.transform, false);
            muzzle.localPosition = new Vector3(0f, 1f, 0.6f);
            Set(weapon, "firePoint", muzzle); Set(go.GetComponent<EnemyPerception>(), "eye", muzzle);
            Set(weapon, "projectilePrefab", At("Projectile template", Vector3.left * 30f).AddComponent<SimpleProjectile>());
            Set(weapon, "attackWindup", 0.15f); Set(weapon, "attackRecovery", 0.1f); Set(weapon, "attackCooldown", 1f);
        }

        [TestCase(0, 1, 2)] [TestCase(0, 2, 1)] [TestCase(1, 0, 2)]
        [TestCase(1, 2, 0)] [TestCase(2, 0, 1)] [TestCase(2, 1, 0)]
        public void AnyDeathOrder_CancelsOnlyTheDyingActorsActions(int first, int second, int third)
        {
            var player = Body("Player", new Vector3(0f, 1f, 6f), 2);
            var meleeObject = At("Melee brain", new Vector3(-3f, 0f, 0f)); meleeObject.SetActive(false);
            meleeObject.AddComponent<NavMeshAgent>().enabled = false;
            var melee = meleeObject.AddComponent<EnemyBrain>(); meleeObject.SetActive(true);
            melee.Configure(player, new Transform[0]);
            var rangedObject = At("Ranged brain", new Vector3(3f, 0f, 0f)); rangedObject.SetActive(false);
            rangedObject.AddComponent<NavMeshAgent>().enabled = false;
            var ranged = rangedObject.AddComponent<RangedBrain>(); rangedObject.SetActive(true);
            ranged.Configure(player, new Transform[0]);
            var drone = At("Drone brain", new Vector3(0f, 1.45f, 0f)).AddComponent<DroneBrain>();
            drone.Configure(player, new Transform[0]);
            var meleeAttack = melee.GetComponent<EnemyAttack>();
            var rangedWeapon = ranged.GetComponent<RangedWeapon>(); var droneWeapon = drone.GetComponent<RangedWeapon>();
            PrepareWeapon(rangedWeapon, player); PrepareWeapon(droneWeapon, player);
            Physics.SyncTransforms();
            Assert.That(meleeAttack.TryStart(), Is.True);
            Assert.That(rangedWeapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            Assert.That(droneWeapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            var health = new[] { melee.GetComponent<Health>(), ranged.GetComponent<Health>(), drone.GetComponent<Health>() };
            var dead = new bool[3];
            foreach (int index in new[] { first, second, third })
            {
                health[index].TakeDamage(1000f); dead[index] = true;
                Assert.That(melee.State == EnemyState.Dead, Is.EqualTo(dead[0]));
                Assert.That(ranged.State == RangedState.Dead, Is.EqualTo(dead[1]));
                Assert.That(drone.State == DroneState.Dead, Is.EqualTo(dead[2]));
                Assert.That(meleeAttack.Active, Is.EqualTo(!dead[0]));
                Assert.That(rangedWeapon.Active, Is.EqualTo(!dead[1]));
                Assert.That(droneWeapon.Active, Is.EqualTo(!dead[2]));
                for (int i = 0; i < health.Length; i++)
                {
                    Assert.That(health[i].CurrentHealth, Is.EqualTo(dead[i] ? 0f : 100f));
                    Assert.That(health[i].GetComponent<Collider>().enabled, Is.EqualTo(!dead[i]));
                }
            }
            Assert.That(meleeAttack.TryStart(), Is.False);
            Assert.That(rangedWeapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.False);
            Assert.That(droneWeapon.TryStart(player, player.GetComponent<Collider>(), 15f), Is.False);
        }

        [UnityTest]
        public IEnumerator TwoWeapons_RecheckCover_KeepIndependentCooldowns_AndCancelOnPlayerDeath()
        {
            var player = Body("Player", new Vector3(0f, 1f, 5f), 2);
            var ranged = At("Ranged weapon", new Vector3(-2f, 0f, 0f)).AddComponent<RangedWeapon>();
            var drone = At("Drone weapon", new Vector3(2f, 0.45f, 0f)).AddComponent<RangedWeapon>();
            PrepareWeapon(ranged, player); PrepareWeapon(drone, player);
            int rangedShots = 0, droneShots = 0;
            ranged.Fired += () => rangedShots++; drone.Fired += () => droneShots++;
            Physics.SyncTransforms();
            Assert.That(ranged.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            Assert.That(drone.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            var cover = At("Shared cover", new Vector3(0f, 1.5f, 2.5f)).AddComponent<BoxCollider>();
            cover.size = new Vector3(8f, 3f, 0.2f); Physics.SyncTransforms();
            yield return new WaitForSeconds(0.4f);
            Assert.That(rangedShots + droneShots, Is.Zero);
            Assert.That(ranged.Active || drone.Active, Is.False);
            cover.enabled = false; Physics.SyncTransforms();
            yield return new WaitForSeconds(1f);
            Assert.That(ranged.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            Assert.That(drone.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            // Killing one owner during windup cannot cancel the other's shot or reset its cooldown.
            ranged.GetComponent<Health>().TakeDamage(1000f);
            yield return new WaitForSeconds(0.4f);
            Assert.That(rangedShots, Is.Zero); Assert.That(droneShots, Is.EqualTo(1));
            Assert.That(drone.TryStart(player, player.GetComponent<Collider>(), 15f), Is.False);
            yield return new WaitForSeconds(1f);
            Assert.That(drone.TryStart(player, player.GetComponent<Collider>(), 15f), Is.True);
            player.TakeDamage(1000f);
            yield return new WaitForSeconds(0.3f);
            Assert.That(drone.Active, Is.False); Assert.That(droneShots, Is.EqualTo(1));
        }
    }
}
