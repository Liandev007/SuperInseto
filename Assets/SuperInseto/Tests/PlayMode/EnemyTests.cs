using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class EnemyTests
    {
        GameObject root;
        static readonly Vector3 Origin = new Vector3(4000f, 0f, 4000f);
        [SetUp] public void Setup() { root = new GameObject("Enemy tests"); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
        GameObject At(string name, Vector3 offset)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            go.transform.position = Origin + offset; return go;
        }
        static void Set(object target, string name, object value)
        { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        Health Target(Vector3 offset)
        {
            var go = At("Player target", offset); go.layer = 2;
            go.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
            return go.AddComponent<Health>();
        }
        EnemyAttack Attacker()
        {
            var go = At("Enemy", Vector3.up);
            var hitbox = go.AddComponent<MeleeHitbox>();
            Set(hitbox, "attackOrigin", go.transform);
            Set(hitbox, "targetMask", (LayerMask)4);
            Set(hitbox, "obstructionMask", (LayerMask)~0);
            var attack = go.AddComponent<EnemyAttack>();
            Set(attack, "attackWindup", 0.15f);
            Set(attack, "damageWindow", 0.1f);
            Set(attack, "attackRecovery", 0.1f);
            Set(attack, "attackCooldown", 1f);
            return attack;
        }
        [Test]
        public void Perception_RequiresRangeFovAndUnobstructedSight_IncludingPlayerLayer()
        {
            var eye = At("Observer", Vector3.zero).AddComponent<EnemyPerception>();
            var target = Target(new Vector3(0f, 1.5f, 4f));
            var body = target.GetComponent<Collider>();
            Physics.SyncTransforms(); Assert.That(eye.CanSee(target, body), Is.True);
            var wall = At("Cover", new Vector3(0f, 1.5f, 2f)).AddComponent<BoxCollider>();
            wall.size = new Vector3(2f, 3f, 0.25f);
            Physics.SyncTransforms(); Assert.That(eye.CanSee(target, body), Is.False);
            wall.enabled = false;
            target.transform.position = Origin + new Vector3(0f, 1.5f, -4f);
            Physics.SyncTransforms(); Assert.That(eye.CanSee(target, body), Is.False);
            target.transform.position = Origin + new Vector3(0f, 1.5f, 20f);
            Physics.SyncTransforms(); Assert.That(eye.CanSee(target, body), Is.False);
        }
        [UnityTest]
        public IEnumerator Attack_HasWindup_SingleDamage_RecoveryAndCooldown()
        {
            var attack = Attacker(); var target = Target(new Vector3(0f, 1f, 1.3f));
            Physics.SyncTransforms();
            Assert.That(attack.TryStart(), Is.True);
            Assert.That(attack.TryStart(), Is.False);
            Assert.That(target.CurrentHealth, Is.EqualTo(100f));
            yield return new WaitForSeconds(0.6f);
            Assert.That(target.CurrentHealth, Is.EqualTo(85f));
            Assert.That(attack.Active, Is.False);
            Assert.That(attack.TryStart(), Is.False);
            yield return new WaitForSeconds(0.85f);
            Assert.That(attack.TryStart(), Is.True);
        }
        [UnityTest]
        public IEnumerator Attack_RespectsInvulnerability_AndDeathCancelsPendingDamage()
        {
            var attack = Attacker(); var target = Target(new Vector3(0f, 1f, 1.3f));
            Physics.SyncTransforms(); target.Invulnerable = true;
            attack.TryStart(); yield return new WaitForSeconds(0.6f);
            Assert.That(target.CurrentHealth, Is.EqualTo(100f));
            target.Invulnerable = false;
            yield return new WaitForSeconds(0.85f);
            Assert.That(attack.TryStart(), Is.True);
            attack.GetComponent<Health>().TakeDamage(1000f);
            Assert.That(attack.Active, Is.False);
            Assert.That(attack.TryStart(), Is.False);
            yield return new WaitForSeconds(0.4f);
            Assert.That(target.CurrentHealth, Is.EqualTo(100f));
        }
        [Test]
        public void Brain_DeathStopsActionsAndDisablesCollision()
        {
            var go = At("Agent", Vector3.zero); go.SetActive(false);
            go.AddComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            var brain = go.AddComponent<EnemyBrain>();
            go.SetActive(true);
            go.GetComponent<Health>().TakeDamage(1000f);
            Assert.That(brain.State, Is.EqualTo(EnemyState.Dead));
            Assert.That(go.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled, Is.False);
            Assert.That(go.GetComponent<EnemyAttack>().TryStart(), Is.False);
            Assert.That(go.GetComponent<CapsuleCollider>().enabled, Is.False);
        }
    }
}
