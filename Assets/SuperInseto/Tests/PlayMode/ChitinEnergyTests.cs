using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class ChitinEnergyTests
    {
        static readonly Vector3 Origin = new Vector3(10000f, 0f, 10000f);
        GameObject root;
        CursorLockMode previousLock;
        bool previousVisible;
        [SetUp] public void Setup()
        { root = new GameObject("M10 tests"); previousLock = Cursor.lockState; previousVisible = Cursor.visible; }
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(root); Cursor.lockState = previousLock; Cursor.visible = previousVisible; }
        GameObject At(string name, Vector3 offset)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform);
            go.transform.position = Origin + offset; return go;
        }
        static void Set(object target, string field, object value)
        { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        static void SetAuto(object target, string property, object value)
        { Set(target, "<" + property + ">k__BackingField", value); }

        [Test]
        public void Spend_IsAtomic_RejectsInvalidCosts_AndEmitsBoundaryEventsOnlyOnce()
        {
            var energy = At("Energy", Vector3.zero).AddComponent<ChitinEnergy>();
            int spent = 0, empty = 0, full = 0, denied = 0;
            energy.Spent += _ => spent++; energy.Depleted += () => empty++;
            energy.Full += () => full++; energy.Insufficient += _ => denied++;
            Assert.That(energy.CurrentEnergy, Is.EqualTo(100f));
            Assert.That(energy.TrySpend(35f), Is.True); Assert.That(energy.CurrentEnergy, Is.EqualTo(65f));
            Assert.That(energy.TrySpend(66f), Is.False); Assert.That(energy.CurrentEnergy, Is.EqualTo(65f));
            foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
                Assert.That(energy.TrySpend(invalid), Is.False);
            Assert.That(energy.TrySpend(65f), Is.True); Assert.That(energy.CurrentEnergy, Is.Zero);
            Assert.That(energy.TrySpend(20f), Is.False); Assert.That(energy.TrySpend(0f), Is.True);
            Assert.That(spent, Is.EqualTo(2)); Assert.That(empty, Is.EqualTo(1)); Assert.That(denied, Is.EqualTo(2));
            energy.SetCurrentEnergy(-100f); Assert.That(energy.CurrentEnergy, Is.Zero);
            energy.SetCurrentEnergy(200f); energy.SetCurrentEnergy(200f); energy.Advance(100f);
            Assert.That(energy.CurrentEnergy, Is.EqualTo(100f)); Assert.That(full, Is.EqualTo(1));
        }

        [Test]
        public void Regeneration_RespectsPartialDelay_ResetsOnSpend_AndStopsAtMaximumOrDeath()
        {
            var energy = At("Energy", Vector3.zero).AddComponent<ChitinEnergy>();
            energy.TrySpend(35f); energy.Advance(1f); Assert.That(energy.CurrentEnergy, Is.EqualTo(65f));
            energy.Advance(1f); Assert.That(energy.CurrentEnergy, Is.EqualTo(72.5f));
            energy.TrySpend(20f); energy.Advance(1f); Assert.That(energy.CurrentEnergy, Is.EqualTo(52.5f));
            float delay = energy.DelayRemaining;
            Assert.That(energy.TrySpend(100f), Is.False); Assert.That(energy.DelayRemaining, Is.EqualTo(delay));
            energy.Advance(0.5f); Assert.That(energy.CurrentEnergy, Is.EqualTo(52.5f));
            energy.Advance(1f); Assert.That(energy.CurrentEnergy, Is.EqualTo(67.5f));
            energy.Advance(100f); Assert.That(energy.CurrentEnergy, Is.EqualTo(100f));
            energy.TrySpend(20f); energy.enabled = false; energy.Advance(100f); energy.enabled = true;
            Assert.That(energy.CurrentEnergy, Is.EqualTo(80f)); Assert.That(energy.DelayRemaining, Is.EqualTo(1.5f));
            energy.GetComponent<Health>().TakeDamage(1000f); energy.Advance(100f);
            Assert.That(energy.CurrentEnergy, Is.EqualTo(80f)); Assert.That(energy.TrySpend(20f), Is.False);
        }

        GameObject Player()
        {
            var floor = At("Floor", new Vector3(0f, -0.1f, 0f)).AddComponent<BoxCollider>();
            floor.size = new Vector3(12f, 0.2f, 12f);
            var view = At("Camera", new Vector3(0f, 1.2f, -4f)).AddComponent<Camera>();
            var go = At("Player", new Vector3(0f, 0.05f, 0f)); go.layer = 2; go.SetActive(false);
            var motor = go.AddComponent<PlayerMotor>(); Set(motor, "cameraTransform", view.transform);
            var body = go.GetComponent<CharacterController>();
            body.height = 1.8f; body.radius = 0.35f; body.center = Vector3.up * 0.9f; body.skinWidth = 0.02f;
            go.AddComponent<ChitinImpact>(); var stinger = go.AddComponent<BioelectricStinger>();
            var socket = new GameObject("FirePoint").transform; socket.SetParent(go.transform, false);
            socket.localPosition = new Vector3(0.3f, 1.2f, 0.6f);
            Set(stinger, "firePoint", socket); Set(stinger, "aimCamera", view);
            Set(stinger, "projectilePrefab", At("Projectile template", Vector3.left * 30f).AddComponent<BioelectricProjectile>());
            stinger.Fired += p => p.transform.SetParent(root.transform);
            Set(go.GetComponent<ChitinEnergy>(), "regenerationRate", 0f);
            go.SetActive(true); Physics.SyncTransforms(); body.Move(Vector3.down * 0.2f);
            Cursor.lockState = CursorLockMode.Locked;
            Assert.That(go.GetComponent<PlayerCombat>().CanStartAbility, Is.True, "PlayMode requires cursor capture and grounded fixture.");
            return go;
        }

        [Test]
        public void Powers_ShareEnergy_UseDifferentCosts_AndDenialDoesNotStartActionOrCooldown()
        {
            var go = Player(); var energy = go.GetComponent<ChitinEnergy>();
            var impact = go.GetComponent<ChitinImpact>(); var stinger = go.GetComponent<BioelectricStinger>();
            int spent = 0; energy.Spent += _ => spent++;
            Assert.That(impact.TryActivate(), Is.True); Assert.That(energy.CurrentEnergy, Is.EqualTo(65f)); impact.Cancel();
            Assert.That(stinger.TryActivate(), Is.True); Assert.That(energy.CurrentEnergy, Is.EqualTo(45f)); stinger.Cancel();
            Set(stinger, "readyAt", Time.time);
            Assert.That(stinger.TryActivate(), Is.True); Assert.That(energy.CurrentEnergy, Is.EqualTo(25f)); stinger.Cancel();
            Set(impact, "readyAt", Time.time);
            Assert.That(impact.TryActivate(), Is.False); Assert.That(impact.Active, Is.False); Assert.That(impact.CooldownRemaining, Is.Zero);
            Set(stinger, "readyAt", Time.time);
            Assert.That(stinger.TryActivate(), Is.True); Assert.That(energy.CurrentEnergy, Is.EqualTo(5f)); stinger.Cancel();
            Set(stinger, "readyAt", Time.time);
            Assert.That(stinger.TryActivate(), Is.False); Assert.That(stinger.Active, Is.False); Assert.That(stinger.CooldownRemaining, Is.Zero);
            Assert.That(energy.CurrentEnergy, Is.EqualTo(5f)); Assert.That(spent, Is.EqualTo(4));
            Assert.That(go.GetComponent<PlayerMotor>().ActionLocked, Is.False);
            Set(energy, "regenerationRate", 15f); energy.Advance(2.5f);
            Assert.That(energy.CurrentEnergy, Is.EqualTo(20f));
            Assert.That(stinger.TryActivate(), Is.True); Assert.That(energy.CurrentEnergy, Is.Zero); stinger.Cancel();
        }

        [Test]
        public void InvalidStates_MutualExclusion_AndCooldownNeverConsume()
        {
            var go = Player(); var energy = go.GetComponent<ChitinEnergy>();
            var impact = go.GetComponent<ChitinImpact>(); var stinger = go.GetComponent<BioelectricStinger>();
            var combat = go.GetComponent<PlayerCombat>(); var climber = go.GetComponent<WallClimber>();
            foreach (var action in new[] { CombatAction.Light, CombatAction.Heavy, CombatAction.Dodge })
            {
                SetAuto(combat, "Action", action);
                Assert.That(impact.TryActivate(), Is.False); Assert.That(stinger.TryActivate(), Is.False);
            }
            SetAuto(combat, "Action", CombatAction.Idle);
            SetAuto(climber, "Attached", true);
            Assert.That(impact.TryActivate(), Is.False); Assert.That(stinger.TryActivate(), Is.False);
            SetAuto(climber, "Attached", false); SetAuto(go.GetComponent<LedgeMantle>(), "Active", true);
            Assert.That(impact.TryActivate(), Is.False); Assert.That(stinger.TryActivate(), Is.False);
            SetAuto(go.GetComponent<LedgeMantle>(), "Active", false);
            Assert.That(energy.CurrentEnergy, Is.EqualTo(100f));
            Assert.That(impact.TryActivate(), Is.True);
            Assert.That(stinger.TryActivate(), Is.False); Assert.That(impact.TryActivate(), Is.False);
            Assert.That(energy.CurrentEnergy, Is.EqualTo(65f)); impact.Cancel();
            Assert.That(impact.TryActivate(), Is.False); Assert.That(energy.CurrentEnergy, Is.EqualTo(65f));
            Set(impact, "readyAt", Time.time); Assert.That(stinger.TryActivate(), Is.True);
            Assert.That(impact.TryActivate(), Is.False); Assert.That(energy.CurrentEnergy, Is.EqualTo(45f)); stinger.Cancel();
            Assert.That(stinger.TryActivate(), Is.False); Assert.That(energy.CurrentEnergy, Is.EqualTo(45f));
            go.GetComponent<Health>().TakeDamage(1000f);
            Assert.That(impact.TryActivate(), Is.False); Assert.That(stinger.TryActivate(), Is.False);
            Assert.That(energy.CurrentEnergy, Is.EqualTo(45f));
        }

        [UnityTest]
        public IEnumerator AnimationEventsAndRecovery_DoNotSpendAgain_EmptyPowersProduceNoEffects()
        {
            var go = Player(); var energy = go.GetComponent<ChitinEnergy>();
            var impact = go.GetComponent<ChitinImpact>(); var stinger = go.GetComponent<BioelectricStinger>();
            int spent = 0, waves = 0, shots = 0;
            energy.Spent += _ => spent++; impact.Impacted += (p, r, n) => waves++; stinger.Fired += _ => shots++;
            Set(impact, "useAnimationEvents", true); Set(stinger, "useAnimationEvents", true);
            Assert.That(impact.TryActivate(), Is.True);
            for (int i = 0; i < 5; i++) impact.AnimationImpact();
            impact.Cancel(); Assert.That(waves, Is.EqualTo(1));
            Assert.That(stinger.TryActivate(), Is.True);
            for (int i = 0; i < 5; i++) stinger.AnimationFire();
            yield return new WaitForSeconds(0.5f);
            Assert.That(shots, Is.EqualTo(1)); Assert.That(spent, Is.EqualTo(2));
            Assert.That(energy.CurrentEnergy, Is.EqualTo(45f)); Assert.That(stinger.Active, Is.False);
            Assert.That(go.GetComponent<PlayerMotor>().ActionLocked, Is.False);
            energy.SetCurrentEnergy(0f); Set(impact, "readyAt", Time.time); Set(stinger, "readyAt", Time.time);
            Assert.That(impact.TryActivate(), Is.False); Assert.That(stinger.TryActivate(), Is.False);
            impact.AnimationImpact(); stinger.AnimationFire(); yield return null;
            Assert.That(waves, Is.EqualTo(1)); Assert.That(shots, Is.EqualTo(1)); Assert.That(spent, Is.EqualTo(2));
            Assert.That(impact.CooldownRemaining, Is.Zero); Assert.That(stinger.CooldownRemaining, Is.Zero);
        }
    }
}
