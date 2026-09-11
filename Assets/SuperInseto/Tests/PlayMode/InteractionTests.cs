using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class InteractionTests
    {
        GameObject root;
        static readonly Vector3 Origin = new Vector3(2000f, 0f, 2000f);
        [SetUp] public void Setup() { root = new GameObject("Interaction tests"); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }

        GameObject ObjectAt(string name, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform);
            go.transform.position = Origin + local;
            return go;
        }
        static void Set(object component, string field, object value)
        {
            component.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(component, value);
        }
        SlidingDoor Door(DoorMode mode, string name, Vector3 position)
        {
            var panel = ObjectAt(name, position);
            panel.SetActive(false);
            panel.AddComponent<BoxCollider>().size = Vector3.one * 0.3f;
            var leaf = ObjectAt(name + " leaf", position + Vector3.right * 2f);
            var collider = leaf.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 3f, 0.3f);
            var door = panel.AddComponent<SlidingDoor>();
            Set(door, "leaf", leaf.transform);
            Set(door, "leafCollider", collider);
            Set(door, "mode", mode);
            Set(door, "speed", 100f);
            panel.SetActive(true);
            return door;
        }

        [Test]
        public void Probe_RejectsDistanceFacingAndOcclusion()
        {
            var target = ObjectAt("Card", new Vector3(0f, 1f, 2f));
            target.AddComponent<BoxCollider>().size = Vector3.one * 0.3f;
            var card = target.AddComponent<AccessCard>();
            var camera = ObjectAt("View", new Vector3(0f, 1f, -3f));
            var probe = new InteractionProbe();
            Vector3 player = Origin + Vector3.up;
            Physics.SyncTransforms();
            Assert.That(probe.Find(player, camera.transform, 2.4f, 18f, ~4), Is.SameAs(card));
            Assert.That(probe.Find(player, camera.transform, 1f, 18f, ~4), Is.Null);
            camera.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Assert.That(probe.Find(player, camera.transform, 2.4f, 18f, ~4), Is.Null);
            camera.transform.rotation = Quaternion.identity;
            ObjectAt("Opaque wall", new Vector3(0f, 1f, 1f)).AddComponent<BoxCollider>().size = new Vector3(3f, 3f, 0.2f);
            Physics.SyncTransforms();
            Assert.That(probe.Find(player, camera.transform, 2.4f, 18f, ~4), Is.Null);
        }

        [Test]
        public void Puzzle_RemoteControlsOnlyItsDoor_AndCardUnlocksSecurity()
        {
            var access = ObjectAt("Credentials", Vector3.zero).AddComponent<AccessCredentials>();
            var security = Door(DoorMode.AccessCard, "Security", new Vector3(0f, 1.5f, 0f));
            var controlled = Door(DoorMode.Remote, "Remote", new Vector3(10f, 1.5f, 0f));
            Assert.That(security.Interact(access), Does.Contain("cartão necessário"));
            Assert.That(security.WantsOpen, Is.False);
            security.OpenFromControl();
            Assert.That(security.WantsOpen, Is.False, "Remote signal must not bypass credentials.");
            Assert.That(controlled.Interact(access), Does.Contain("botão ou terminal"));
            var button = ObjectAt("Button", Vector3.zero).AddComponent<ControlPanel>();
            var action = new UnityEvent(); action.AddListener(controlled.OpenFromControl);
            Set(button, "onActivated", action);
            button.Interact(access);
            Assert.That(controlled.WantsOpen, Is.True);
            Assert.That(security.WantsOpen, Is.False);
            var terminalDoor = Door(DoorMode.Remote, "Terminal door", new Vector3(20f, 1.5f, 0f));
            var terminal = ObjectAt("Terminal", Vector3.zero).AddComponent<ControlPanel>();
            var terminalAction = new UnityEvent(); terminalAction.AddListener(terminalDoor.OpenFromControl);
            Set(terminal, "onActivated", terminalAction);
            terminal.Interact(access);
            Assert.That(terminalDoor.WantsOpen, Is.True);
            var card = ObjectAt("Card", Vector3.zero).AddComponent<AccessCard>();
            Assert.That(card.Interact(access), Does.Contain("Cartão adquirido"));
            Assert.That(access.Has("SecurityLevel01"), Is.True);
            Assert.That(card.Available, Is.False);
            Assert.That(card.Interact(access), Does.Contain("indisponível"));
            Assert.That(security.Interact(access), Does.Contain("Acesso liberado"));
            Assert.That(security.WantsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator ManualDoor_Opens_StopsClosingOnPlayer_ThenCloses()
        {
            var door = Door(DoorMode.Manual, "Manual", new Vector3(0f, 1.5f, 0f));
            door.Interact(null);
            for (int i = 0; i < 120 && !door.IsOpen; i++) yield return null;
            Assert.That(door.IsOpen, Is.True);
            var player = ObjectAt("Player blocker", new Vector3(2f, 0f, 0f));
            player.layer = 2;
            var body = player.AddComponent<CharacterController>();
            body.height = 1.8f; body.center = Vector3.up * 0.9f; body.radius = 0.35f;
            Physics.SyncTransforms();
            door.Interact(null);
            for (int i = 0; i < 120 && !door.SafetyBlocked; i++) yield return null;
            Assert.That(door.SafetyBlocked, Is.True);
            var leaf = root.transform.Find("Manual leaf");
            Assert.That(leaf.position.y, Is.GreaterThan(1.5f));
            player.transform.position += Vector3.right * 5f;
            Physics.SyncTransforms();
            for (int i = 0; i < 120 && Mathf.Abs(leaf.position.y - 1.5f) > 0.01f; i++) yield return null;
            Assert.That(leaf.position.y, Is.EqualTo(1.5f).Within(0.01f));
            Assert.That(door.WantsOpen, Is.False);
        }
    }
}
