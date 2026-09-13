using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SuperInseto.Tests
{
    public sealed class SaveLoadTests
    {
        static readonly Vector3 Origin = new Vector3(16000f, 0f, 16000f);
        string directory;
        SaveFileStore store;
        GameObject root, player, barrier;
        SaveManager manager;
        PlayerRespawn respawn;
        Checkpoint first, second;
        DestructibleObject crate, panel;
        BioelectricReceiver a, b;
        SlidingDoor door;
        EnvironmentalPuzzle sabotage, circuit;
        AccessCard card;
        CursorLockMode oldLock;
        bool oldVisible;
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "SuperInseto_M14_" + Guid.NewGuid().ToString("N"));
            store = new SaveFileStore(Path.Combine(directory, SaveManager.FileName));
            root = new GameObject("M14 tests"); oldLock = Cursor.lockState; oldVisible = Cursor.visible;
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(root);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Cursor.lockState = oldLock; Cursor.visible = oldVisible;
        }
        static SaveGameData Data(string checkpoint = "checkpoint_02") => new SaveGameData {
            saveVersion = 1, checkpointId = checkpoint, hasAccessCard = true,
            destroyedObjectIds = new[] { "crate", "panel" }, poweredReceiverIds = new[] { "node_a" } };
        [Test]
        public void Store_RoundTrips_ReplacesAtomically_RejectsInvalidData_AndClears()
        {
            Assert.That(store.Read(out _, out _), Is.EqualTo(SaveReadResult.Missing));
            Assert.That(store.Write(Data(), out var error), Is.True, error);
            Assert.That(store.Read(out var loaded, out _), Is.EqualTo(SaveReadResult.Loaded));
            Assert.That(loaded.checkpointId, Is.EqualTo("checkpoint_02"));
            Assert.That(loaded.destroyedObjectIds, Is.EquivalentTo(new[] { "crate", "panel" }));
            Assert.That(loaded.poweredReceiverIds, Is.EquivalentTo(new[] { "node_a" }));
            var both = Data(); both.poweredReceiverIds = new[] { "node_a", "node_b" };
            Assert.That(store.Write(both, out error), Is.True, error);
            Assert.That(File.Exists(store.FilePath + ".tmp"), Is.False);
            string intact = File.ReadAllText(store.FilePath);
            both.saveVersion = 2;
            Assert.That(store.Write(both, out _), Is.False);
            Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(intact));
            Assert.That(store.Clear(out _), Is.True);
            Assert.That(store.Read(out _, out _), Is.EqualTo(SaveReadResult.Missing));
        }
        [Test]
        public void InvalidJson_MissingFields_WrongVersion_AndDuplicateIds_AreRejected()
        {
            Directory.CreateDirectory(directory);
            foreach (string text in new[] { "", " ", "{broken", "{}", "null", "[]",
                "{\"saveVersion\":1,\"checkpointId\":\"checkpoint_start\"}",
                JsonUtility.ToJson(new SaveGameData { saveVersion = 2, checkpointId = "checkpoint_start", destroyedObjectIds = new string[0], poweredReceiverIds = new string[0] }) })
            {
                File.WriteAllText(store.FilePath, text);
                Assert.That(store.Read(out var data, out _), Is.EqualTo(SaveReadResult.Invalid));
                Assert.That(data, Is.Null);
            }
            var duplicate = Data(); duplicate.destroyedObjectIds = new[] { "crate", "crate" };
            Assert.That(SaveGameData.TryParse(JsonUtility.ToJson(duplicate), out _), Is.False);
        }
        GameObject At(string name, Vector3 offset)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            go.transform.position = Origin + offset; return go;
        }
        static void Set(object target, string field, object value)
        { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        Checkpoint Point(string id, int index, Vector3 offset)
        {
            var point = At(id, offset).AddComponent<Checkpoint>();
            Set(point, "persistentId", id); Set(point, "progressIndex", index); return point;
        }
        DestructibleObject Breakable(string id, Vector3 offset)
        {
            var go = At(id, offset); var collider = go.AddComponent<BoxCollider>(); collider.center = Vector3.up * 0.5f;
            var item = go.AddComponent<DestructibleObject>(); PersistentId.Assign(go, id); return item;
        }
        EnvironmentalPuzzle Puzzle(string name, DestructibleObject[] destroyed, BioelectricReceiver[] powered, UnityEngine.Events.UnityAction output)
        {
            var go = At(name, Vector3.zero); go.SetActive(false);
            var puzzle = go.AddComponent<EnvironmentalPuzzle>(); puzzle.Configure(destroyed, powered, output);
            go.SetActive(true); return puzzle;
        }
        void Build(bool duplicate = false)
        {
            var floor = At("Floor", Vector3.down * 0.1f).AddComponent<BoxCollider>(); floor.size = new Vector3(30f, 0.2f, 30f);
            first = Point("checkpoint_start", 0, Vector3.zero); second = Point("checkpoint_02", 1, Vector3.right * 4f);
            second.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var view = At("Camera", new Vector3(0f, 1f, -4f)).AddComponent<Camera>();
            player = At("Player", Vector3.up * 0.05f); player.layer = 2; player.SetActive(false);
            var motor = player.AddComponent<PlayerMotor>(); Set(motor, "cameraTransform", view.transform);
            var body = player.GetComponent<CharacterController>(); body.height = 1.8f; body.radius = 0.35f;
            body.center = Vector3.up * 0.9f; body.skinWidth = 0.02f;
            player.AddComponent<ChitinImpact>(); player.AddComponent<BioelectricStinger>(); player.AddComponent<AccessCredentials>();
            respawn = player.AddComponent<PlayerRespawn>(); Set(respawn, "initialCheckpoint", first); Set(respawn, "respawnDelay", 0.1f);
            player.SetActive(true); Physics.SyncTransforms(); body.Move(Vector3.down * 0.2f);
            player.GetComponent<Health>().TakeDamage(60f); player.GetComponent<ChitinEnergy>().TrySpend(80f);
            Set(player.GetComponent<ChitinImpact>(), "readyAt", Time.time + 99f);
            Set(player.GetComponent<BioelectricStinger>(), "readyAt", Time.time + 99f);
            crate = Breakable("crate", Vector3.left * 5f); panel = Breakable("panel", Vector3.left * 7f);
            a = At("A", Vector3.forward * 6f).AddComponent<BioelectricReceiver>(); PersistentId.Assign(a.gameObject, "node_a");
            b = At("B", Vector3.forward * 8f).AddComponent<BioelectricReceiver>(); PersistentId.Assign(b.gameObject, duplicate ? "node_a" : "node_b");
            barrier = At("Barrier", Vector3.forward * 10f); barrier.AddComponent<BoxCollider>();
            sabotage = Puzzle("Sabotage", new[] { panel }, null, () => barrier.SetActive(false));
            var doorRoot = At("Door", Vector3.right * 10f); doorRoot.SetActive(false);
            var leaf = new GameObject("Leaf"); leaf.transform.SetParent(doorRoot.transform, false);
            var leafCollider = leaf.AddComponent<BoxCollider>();
            door = doorRoot.AddComponent<SlidingDoor>(); Set(door, "leaf", leaf.transform);
            Set(door, "leafCollider", leafCollider); Set(door, "mode", DoorMode.Remote); doorRoot.SetActive(true);
            circuit = Puzzle("Circuit", null, new[] { a, b }, door.OpenFromControl);
            card = At("Access card", Vector3.back * 6f).AddComponent<AccessCard>();
            var go = At("Save manager", Vector3.zero); go.SetActive(false);
            manager = go.AddComponent<SaveManager>(); Set(manager, "player", respawn); Set(manager, "scope", root.transform);
            Set(manager, "store", store); go.SetActive(true);
        }
        void Flush() { manager.SendMessage("OnApplicationPause", true); }
        void Restart() { Object.DestroyImmediate(root); root = new GameObject("M14 reopened scene"); Build(); }
        [UnityTest]
        public IEnumerator NewGame_AutosavesProgress_ReloadsPartialPuzzle_AndRespawnsAtLoadedCheckpoint()
        {
            Build(); yield return null;
            Assert.That(manager.Ready, Is.True); Assert.That(File.Exists(store.FilePath), Is.False);
            Assert.That(respawn.CurrentCheckpoint, Is.SameAs(first));
            respawn.TryActivateCheckpoint(second); crate.RestoreDestroyed(); panel.RestoreDestroyed(); a.ReceiveBioelectricImpact();
            player.GetComponent<AccessCredentials>().Grant("SecurityLevel01"); Flush();
            Assert.That(store.Read(out var saved, out _), Is.EqualTo(SaveReadResult.Loaded));
            Assert.That(saved.checkpointId, Is.EqualTo("checkpoint_02"));
            Assert.That(saved.destroyedObjectIds, Is.EquivalentTo(new[] { "crate", "panel" }));
            string bytes = File.ReadAllText(store.FilePath);
            Restart(); yield return null; yield return null;
            Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(bytes), "Load must not autosave defaults/restoration callbacks.");
            Assert.That(respawn.CurrentCheckpoint, Is.SameAs(second));
            Assert.That(Vector3.Distance(player.transform.position, second.Position), Is.LessThan(0.2f));
            Assert.That(crate.IsDestroyed && panel.IsDestroyed, Is.True);
            Assert.That(crate.GetComponent<BoxCollider>().enabled, Is.False);
            Assert.That(sabotage.IsSolved, Is.True); Assert.That(barrier.activeSelf, Is.False);
            Assert.That(a.IsPowered, Is.True); Assert.That(b.IsPowered, Is.False);
            Assert.That(circuit.ConditionsMet, Is.EqualTo(1)); Assert.That(circuit.IsSolved, Is.False); Assert.That(door.WantsOpen, Is.False);
            Assert.That(player.GetComponent<AccessCredentials>().Has("SecurityLevel01"), Is.True); Assert.That(card.gameObject.activeSelf, Is.False);
            Assert.That(player.GetComponent<Health>().CurrentHealth, Is.EqualTo(100f));
            Assert.That(player.GetComponent<ChitinEnergy>().CurrentEnergy, Is.EqualTo(100f));
            Assert.That(player.GetComponent<ChitinImpact>().CooldownRemaining, Is.Zero);
            Assert.That(player.GetComponent<BioelectricStinger>().CooldownRemaining, Is.Zero);
            player.GetComponent<Health>().TakeDamage(1000f); yield return new WaitForSeconds(0.2f); yield return null;
            Assert.That(respawn.State, Is.EqualTo(RespawnState.Alive)); Assert.That(respawn.CurrentCheckpoint, Is.SameAs(second));
            Assert.That(Vector3.Distance(player.transform.position, second.Position), Is.LessThan(0.2f));
        }
        [UnityTest]
        public IEnumerator BothNodes_RestoreDoorThroughExistingPuzzle_AndClearDoesNotResetWorld()
        {
            var data = Data(); data.poweredReceiverIds = new[] { "node_a", "node_b" };
            Assert.That(store.Write(data, out _), Is.True); Build(); yield return null;
            Assert.That(circuit.IsSolved, Is.True); Assert.That(door.WantsOpen, Is.True);
            yield return new WaitForSeconds(1.5f); Assert.That(door.IsOpen, Is.True);
            manager.ClearSave(); Flush();
            Assert.That(File.Exists(store.FilePath), Is.False); Assert.That(circuit.IsSolved, Is.True);
        }
        [UnityTest]
        public IEnumerator UnknownCheckpointAndObject_UseInitialPointWithoutDiscardingKnownProgress()
        {
            var data = Data("removed_checkpoint"); data.destroyedObjectIds = new[] { "crate", "removed_object" };
            Assert.That(store.Write(data, out _), Is.True);
            LogAssert.Expect(LogType.Warning, new Regex("M14: checkpoint salvo ausente"));
            Build(); yield return null;
            Assert.That(respawn.CurrentCheckpoint, Is.SameAs(first)); Assert.That(crate.IsDestroyed, Is.True);
            Assert.That(player.GetComponent<Health>().IsDead, Is.False);
        }
        [UnityTest]
        public IEnumerator CorruptFile_UsesDefaults_AndDuplicateSceneIdsNeverOverwriteExistingSave()
        {
            Directory.CreateDirectory(directory); File.WriteAllText(store.FilePath, "{broken");
            LogAssert.Expect(LogType.Warning, new Regex("M14: Save vazio, inválido"));
            Build(); yield return null;
            Assert.That(respawn.CurrentCheckpoint, Is.SameAs(first)); Assert.That(crate.IsDestroyed, Is.False);
            Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo("{broken"));
            Object.DestroyImmediate(root); root = new GameObject("Duplicates");
            Assert.That(store.Write(Data(), out _), Is.True); string bytes = File.ReadAllText(store.FilePath);
            LogAssert.Expect(LogType.Warning, new Regex("M14: ID persistível vazio, inválido ou duplicado"));
            LogAssert.Expect(LogType.Warning, new Regex("M14: corrija os IDs"));
            Build(true); yield return null;
            Assert.That(manager.Ready, Is.False); a.ReceiveBioelectricImpact(); Flush();
            Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(bytes));
        }
    }
}
