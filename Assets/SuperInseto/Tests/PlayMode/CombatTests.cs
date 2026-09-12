using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SuperInseto.Tests
{
    public sealed class CombatTests
    {
        GameObject root;
        static readonly Vector3 Origin = new Vector3(3000f, 0f, 3000f);
        [SetUp] public void Setup() { root = new GameObject("Combat tests"); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
        GameObject At(string name, Vector3 offset)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            go.transform.position = Origin + offset; return go;
        }

        [Test]
        public void Health_ClampsHealingAndDamage_DeathOccursOnce()
        {
            var health = At("Health", Vector3.zero).AddComponent<Health>();
            int deaths = 0; health.Died += () => deaths++;
            health.Heal(1000f);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(health.TakeDamage(25f), Is.True);
            health.Heal(10f);
            Assert.That(health.CurrentHealth, Is.EqualTo(85f));
            health.Invulnerable = true;
            Assert.That(health.TakeDamage(1000f), Is.False);
            health.Invulnerable = false;
            Assert.That(health.TakeDamage(float.NaN), Is.False);
            Assert.That(health.TakeDamage(-10f), Is.False);
            Assert.That(health.TakeDamage(float.PositiveInfinity), Is.False);
            Assert.That(health.TakeDamage(1000f), Is.True);
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.TakeDamage(1f), Is.False);
            health.Heal(100f);
            Assert.That(health.IsDead, Is.True);
            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void Combo_HasThreeSteps_ThenResets_AndExpires()
        {
            var combo = new LightCombo();
            Assert.That(combo.Next(0f, 0.7f), Is.EqualTo(1));
            Assert.That(combo.Next(0.4f, 0.7f), Is.EqualTo(2));
            Assert.That(combo.Next(0.8f, 0.7f), Is.EqualTo(3));
            Assert.That(combo.Next(1.2f, 0.7f), Is.EqualTo(1));
            Assert.That(combo.Next(4f, 0.7f), Is.EqualTo(1));
            combo.Reset();
            Assert.That(combo.Next(4.1f, 0.7f), Is.EqualTo(1));
        }

        MeleeHitbox Hitbox(out GameObject source)
        {
            source = At("Source", Vector3.up); source.layer = 2;
            var hitbox = source.AddComponent<MeleeHitbox>();
            typeof(MeleeHitbox).GetField("attackOrigin", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(hitbox, source.transform);
            return hitbox;
        }
        Health Target(Vector3 offset)
        {
            var go = At("Target", offset);
            go.AddComponent<BoxCollider>().size = new Vector3(0.5f, 1f, 0.5f);
            return go.AddComponent<Health>();
        }

        [Test]
        public void Swing_DeduplicatesCollidersAndReopenedWindows_HeavyDealsMore()
        {
            var hitbox = Hitbox(out var source);
            var target = Target(new Vector3(0f, 1f, 1.3f));
            var extra = new GameObject("Extra collider"); extra.transform.SetParent(target.transform, false);
            extra.AddComponent<BoxCollider>().size = new Vector3(0.4f, 0.8f, 0.4f);
            Physics.SyncTransforms();
            hitbox.BeginSwing(12f, source); hitbox.SetWindow(true);
            for (int i = 0; i < 10; i++) hitbox.Sample();
            Assert.That(target.CurrentHealth, Is.EqualTo(88f));
            hitbox.SetWindow(false); hitbox.Sample(); hitbox.SetWindow(true); hitbox.Sample();
            Assert.That(target.CurrentHealth, Is.EqualTo(88f));
            hitbox.EndSwing(); hitbox.Sample();
            Assert.That(target.CurrentHealth, Is.EqualTo(88f));
            hitbox.BeginSwing(30f, source); hitbox.SetWindow(true); hitbox.Sample();
            Assert.That(target.CurrentHealth, Is.EqualTo(58f));
        }

        [Test]
        public void Swing_RejectsFarBehindAndOccludedTargets()
        {
            var hitbox = Hitbox(out var source);
            var far = Target(new Vector3(0f, 1f, 3f));
            var behind = Target(new Vector3(0f, 1f, -1f));
            var blocked = Target(new Vector3(0f, 1f, 1.3f));
            At("Wall", new Vector3(0f, 1f, 0.6f)).AddComponent<BoxCollider>().size = new Vector3(2f, 3f, 0.15f);
            Physics.SyncTransforms();
            hitbox.BeginSwing(30f, source); hitbox.SetWindow(true); hitbox.Sample();
            Assert.That(far.CurrentHealth, Is.EqualTo(100f));
            Assert.That(behind.CurrentHealth, Is.EqualTo(100f));
            Assert.That(blocked.CurrentHealth, Is.EqualTo(100f));
        }
    }
}
