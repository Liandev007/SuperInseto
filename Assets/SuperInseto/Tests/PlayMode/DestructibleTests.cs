using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace SuperInseto.Tests
{
    public sealed class DestructibleTests
    {
        static readonly Vector3 Origin = new Vector3(11000f, 0f, 11000f);
        GameObject root;
        [SetUp] public void Setup() { root = new GameObject("M11 tests"); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
        GameObject At(string name, Vector3 offset)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            go.transform.position = Origin + offset; return go;
        }
        static void Set(object target, string field, object value)
        { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        DestructibleObject Target(Vector3 offset, float hp = 50f, bool clearCollision = true)
        {
            var go = At("Breakable", offset); go.SetActive(false);
            var health = go.AddComponent<Health>(); Set(health, "maxHealth", hp);
            go.AddComponent<BoxCollider>().size = Vector3.one * 0.8f;
            go.AddComponent<NavMeshObstacle>().carving = true;
            var visual = new GameObject("Intact"); visual.transform.SetParent(go.transform, false);
            visual.AddComponent<BoxCollider>().size = Vector3.one * 0.7f; // Same Health through multiple colliders.
            var broken = new GameObject("Broken"); broken.transform.SetParent(go.transform, false); broken.SetActive(false);
            var target = go.AddComponent<DestructibleObject>();
            Set(target, "intactVisual", visual); Set(target, "brokenVisual", broken);
            Set(target, "disableColliderOnDestroy", clearCollision);
            Set(target, "disableVisualOnDestroy", clearCollision);
            go.SetActive(true); return target;
        }
        GameObject Owner()
        { var owner = At("Player", Vector3.up); owner.layer = 2; owner.AddComponent<Health>(); return owner; }

        [Test]
        public void ZeroHealth_BreaksOnce_PreservesRoot_AndClearsAllCollisionAndNavigation()
        {
            var target = Target(Vector3.up); var health = target.GetComponent<Health>();
            int broken = 0; target.Destroyed += () => broken++;
            health.TakeDamage(12f); Assert.That(health.CurrentHealth, Is.EqualTo(38f)); Assert.That(target.IsDestroyed, Is.False);
            health.TakeDamage(100f); health.TakeDamage(100f); health.Heal(100f);
            Assert.That(health.CurrentHealth, Is.Zero); Assert.That(target.IsDestroyed, Is.True); Assert.That(broken, Is.EqualTo(1));
            Assert.That(target.gameObject.activeSelf, Is.True);
            foreach (var c in target.GetComponentsInChildren<Collider>(true)) Assert.That(c.enabled, Is.False);
            Assert.That(target.GetComponent<NavMeshObstacle>().enabled, Is.False);
            Assert.That(target.transform.Find("Intact").gameObject.activeSelf, Is.False);
            Assert.That(target.transform.Find("Broken").gameObject.activeSelf, Is.True);
            target.enabled = false; target.enabled = true; Assert.That(broken, Is.EqualTo(1));
            var fixedObject = Target(Vector3.right * 5f, 80f, false); fixedObject.GetComponent<Health>().TakeDamage(1000f);
            Assert.That(fixedObject.IsDestroyed, Is.True); Assert.That(fixedObject.GetComponent<Collider>().enabled, Is.True);
            Assert.That(fixedObject.transform.Find("Intact").gameObject.activeSelf, Is.True);
        }

        [Test]
        public void ExistingMeleeLightAndHeavy_UseNormalDamage_OncePerSwing()
        {
            var owner = Owner(); var hitbox = owner.AddComponent<MeleeHitbox>(); Set(hitbox, "attackOrigin", owner.transform);
            var target = Target(new Vector3(0f, 1f, 1.2f), 120f); var health = target.GetComponent<Health>();
            Physics.SyncTransforms();
            hitbox.BeginSwing(12f, owner); hitbox.SetWindow(true); hitbox.Sample(); hitbox.Sample();
            Assert.That(health.CurrentHealth, Is.EqualTo(108f)); hitbox.EndSwing();
            hitbox.BeginSwing(30f, owner); hitbox.SetWindow(true); hitbox.Sample(); hitbox.Sample();
            Assert.That(health.CurrentHealth, Is.EqualTo(78f)); Assert.That(target.IsDestroyed, Is.False);
        }

        [Test]
        public void ExistingImpact_HitsTwoMultiColliderObjectsOnceEach_AndRespectsWallsAndRange()
        {
            var owner = Owner(); var first = Target(new Vector3(-0.9f, 1f, 1.5f));
            var second = Target(new Vector3(0.9f, 1f, 1.5f));
            var protectedObject = Target(new Vector3(0f, 1f, 2.6f)); var far = Target(new Vector3(0f, 1f, 5f));
            var wall = At("Solid wall", new Vector3(0f, 1f, 1.7f)).AddComponent<BoxCollider>(); wall.size = new Vector3(0.3f, 2f, 0.1f);
            Physics.SyncTransforms(); var wave = new RadialDamage();
            Assert.That(wave.Apply(owner.transform.position, 3f, 35f, owner, ~4, ~260), Is.EqualTo(2));
            Assert.That(first.GetComponent<Health>().CurrentHealth, Is.EqualTo(15f));
            Assert.That(second.GetComponent<Health>().CurrentHealth, Is.EqualTo(15f));
            Assert.That(protectedObject.GetComponent<Health>().CurrentHealth, Is.EqualTo(50f));
            Assert.That(far.GetComponent<Health>().CurrentHealth, Is.EqualTo(50f));
        }

        BioelectricProjectile Shot(GameObject owner)
        {
            var shot = At("Stinger", Vector3.up).AddComponent<BioelectricProjectile>();
            shot.Launch(owner, Vector3.forward, 25f, 18f, 3f, 0.1f); return shot;
        }
        [Test]
        public void ExistingStinger_StopsAtIntactObject_EvenOnKillingShot_ThenPassesAfterDestruction()
        {
            var owner = Owner(); var crate = Target(new Vector3(0f, 1f, 1.5f));
            var panel = Target(new Vector3(0f, 1f, 3.5f), 80f); Physics.SyncTransforms();
            var first = Shot(owner); first.Advance(0.5f); first.Advance(0.5f);
            Assert.That(first.Flying, Is.False); Assert.That(crate.GetComponent<Health>().CurrentHealth, Is.EqualTo(25f));
            Assert.That(panel.GetComponent<Health>().CurrentHealth, Is.EqualTo(80f));
            var second = Shot(owner); second.Advance(0.5f); second.Advance(0.5f);
            Assert.That(crate.IsDestroyed, Is.True); Assert.That(panel.GetComponent<Health>().CurrentHealth, Is.EqualTo(80f));
            Physics.SyncTransforms(); var third = Shot(owner); third.Advance(0.5f);
            Assert.That(panel.GetComponent<Health>().CurrentHealth, Is.EqualTo(55f));
            Assert.That(owner.GetComponent<Health>().CurrentHealth, Is.EqualTo(100f));
        }
    }
}
