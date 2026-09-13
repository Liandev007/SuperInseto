using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class EnvironmentalPuzzleTests
    {
        static readonly Vector3 Origin = new Vector3(12000f, 0f, 12000f);
        GameObject root;
        [SetUp] public void Setup() { root = new GameObject("M12 tests"); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
        GameObject At(string name, Vector3 offset)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            go.transform.position = Origin + offset; return go;
        }
        static void Set(object target, string field, object value)
        { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        BioelectricReceiver Node(Vector3 offset)
        {
            var go = At("Circuit", offset); go.AddComponent<BoxCollider>().size = Vector3.one * 0.7f;
            var child = new GameObject("Extra collider"); child.transform.SetParent(go.transform, false);
            child.AddComponent<BoxCollider>().size = Vector3.one * 0.6f;
            return go.AddComponent<BioelectricReceiver>();
        }
        BioelectricProjectile Fire(Vector3 point)
        {
            var shot = At("Stinger", Vector3.up).AddComponent<BioelectricProjectile>();
            shot.Launch(null, point - shot.transform.position, 25f, 18f, 3f, 0.1f);
            shot.Advance(0.5f); shot.Advance(0.5f); return shot;
        }
        EnvironmentalPuzzle Puzzle(DestructibleObject[] objects, BioelectricReceiver[] nodes, UnityEngine.Events.UnityAction consequence)
        {
            var go = At("Puzzle", Vector3.zero); go.SetActive(false);
            var puzzle = go.AddComponent<EnvironmentalPuzzle>(); puzzle.Configure(objects, nodes, consequence);
            go.SetActive(true); return puzzle;
        }
        DestructibleObject Panel(Vector3 offset)
        {
            var go = At("Panel", offset); go.AddComponent<BoxCollider>(); return go.AddComponent<DestructibleObject>();
        }
        SlidingDoor Door()
        {
            var go = At("Remote door", Vector3.forward * 6f); go.SetActive(false);
            var leaf = new GameObject("Leaf"); leaf.transform.SetParent(go.transform, false);
            leaf.transform.localPosition = Vector3.up * 1.5f;
            var collider = leaf.AddComponent<BoxCollider>(); collider.size = new Vector3(2f, 3f, 0.2f);
            var door = go.AddComponent<SlidingDoor>(); Set(door, "leaf", leaf.transform); Set(door, "leafCollider", collider);
            Set(door, "mode", DoorMode.Remote); go.SetActive(true); return door;
        }

        [Test]
        public void OnlyStingerPowersNode_NormalAttacksAndEnemyShotDoNot_AndSignalsOnce()
        {
            var node = Node(new Vector3(0f, 1f, 1.3f)); int powered = 0; node.Powered += () => powered++;
            Assert.That(node.IsPowered, Is.False); Assert.That(node.GetComponent<IDamageable>(), Is.Null);
            var owner = At("Player", Vector3.up); owner.layer = 2;
            var melee = owner.AddComponent<MeleeHitbox>(); Set(melee, "attackOrigin", owner.transform);
            Physics.SyncTransforms();
            foreach (float damage in new[] { 12f, 30f })
            { melee.BeginSwing(damage, owner); melee.SetWindow(true); melee.Sample(); melee.EndSwing(); }
            new RadialDamage().Apply(owner.transform.position, 3f, 35f, owner, ~4, ~260);
            var enemyShot = At("Enemy shot", Vector3.up).AddComponent<SimpleProjectile>();
            enemyShot.Launch(null, Vector3.forward, 12f, 18f, 3f, 0.1f); enemyShot.Advance(0.5f);
            Assert.That(enemyShot.Flying, Is.False); Assert.That(node.IsPowered, Is.False); Assert.That(powered, Is.Zero);
            var first = Fire(node.transform.position); Fire(node.transform.position);
            Assert.That(first.Flying, Is.False); Assert.That(node.IsPowered, Is.True); Assert.That(powered, Is.EqualTo(1));
        }

        [Test]
        public void SolidWallBlocksElectricalSignal_IncludingInitialOverlap()
        {
            var node = Node(new Vector3(0f, 1f, 4f));
            var wall = At("Wall", new Vector3(0f, 1f, 2f)).AddComponent<BoxCollider>(); wall.size = new Vector3(2f, 3f, 0.05f);
            Physics.SyncTransforms(); Fire(node.transform.position); Assert.That(node.IsPowered, Is.False);
            var overlap = At("Shot inside wall", wall.transform.position - Origin).AddComponent<BioelectricProjectile>();
            overlap.Launch(null, Vector3.forward, 25f, 18f, 3f, 0.1f); overlap.Advance(0.5f);
            Assert.That(overlap.Flying, Is.False); Assert.That(node.IsPowered, Is.False);
        }

        [UnityTest] public IEnumerator Circuit_AThenB() { return Circuit(false); }
        [UnityTest] public IEnumerator Circuit_BThenA() { return Circuit(true); }
        IEnumerator Circuit(bool reverse)
        {
            var a = Node(new Vector3(-1.5f, 1f, 3f)); var b = Node(new Vector3(1.5f, 1f, 3f));
            var door = Door(); int outputs = 0;
            var puzzle = Puzzle(null, new[] { a, b }, () => { outputs++; door.OpenFromControl(); });
            yield return null; Physics.SyncTransforms();
            Assert.That(puzzle.IsSolved, Is.False); Assert.That(puzzle.ConditionsMet, Is.Zero); Assert.That(door.WantsOpen, Is.False);
            door.Interact(null); Assert.That(door.WantsOpen, Is.False);
            Fire((reverse ? b : a).transform.position);
            Assert.That(puzzle.ConditionsMet, Is.EqualTo(1)); Assert.That(puzzle.IsSolved, Is.False); Assert.That(door.WantsOpen, Is.False);
            Fire((reverse ? a : b).transform.position);
            Assert.That(puzzle.ConditionsMet, Is.EqualTo(2)); Assert.That(puzzle.IsSolved, Is.True); Assert.That(door.WantsOpen, Is.True);
            Fire(a.transform.position); puzzle.enabled = false; puzzle.enabled = true;
            Assert.That(outputs, Is.EqualTo(1)); Assert.That(a.IsPowered && b.IsPowered, Is.True);
            yield return new WaitForSeconds(1.5f);
            Assert.That(door.IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator Sabotage_UsesSpecificDestructible_CatchesMissedEvent_AndDisablesWholeBarrierOnce()
        {
            var panel = Panel(Vector3.left * 3f); var other = Panel(Vector3.right * 3f);
            var barrier = At("Barrier", Vector3.forward * 3f); var collider = barrier.AddComponent<BoxCollider>();
            int outputs = 0;
            var puzzle = Puzzle(new[] { panel }, null, () => { outputs++; barrier.SetActive(false); });
            yield return null;
            other.GetComponent<Health>().TakeDamage(1000f);
            Assert.That(puzzle.IsSolved, Is.False); Assert.That(barrier.activeSelf, Is.True);
            puzzle.enabled = false; panel.GetComponent<Health>().TakeDamage(1000f);
            Assert.That(outputs, Is.Zero); puzzle.enabled = true;
            Assert.That(panel.IsDestroyed, Is.True); Assert.That(puzzle.IsSolved, Is.True);
            Assert.That(barrier.activeSelf, Is.False); Assert.That(collider.gameObject.activeInHierarchy, Is.False);
            panel.GetComponent<Health>().TakeDamage(1000f); puzzle.enabled = false; puzzle.enabled = true;
            Assert.That(outputs, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EmptyMissingOrDuplicateConditionsNeverAutoSolve()
        {
            var node = Node(Vector3.up); int outputs = 0;
            var empty = Puzzle(null, null, () => outputs++);
            var missing = Puzzle(null, new BioelectricReceiver[] { null }, () => outputs++);
            var duplicate = Puzzle(null, new[] { node, node }, () => outputs++);
            yield return null; node.ReceiveBioelectricImpact();
            Assert.That(empty.IsSolved || missing.IsSolved || duplicate.IsSolved, Is.False);
            Assert.That(outputs, Is.Zero);
        }
    }
}
