using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class ChitinImpactTests
    {
        static readonly Vector3 Origin = new Vector3(8000f, 0f, 8000f);
        GameObject root;
        CursorLockMode previousLock;
        bool previousVisible;
        [SetUp] public void Setup()
        { root = new GameObject("M8 tests"); previousLock = Cursor.lockState; previousVisible = Cursor.visible; }
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
        Health Body(string name, Vector3 offset, int layer = 8)
        {
            var go = At(name, offset); go.layer = layer;
            go.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
            return go.AddComponent<Health>();
        }
        Health GroundEnemy(Vector3 offset, bool ranged)
        {
            var go = At(ranged ? "Ranged" : "Melee", offset); go.layer = 8; go.SetActive(false);
            go.AddComponent<NavMeshAgent>().enabled = false;
            if (ranged) go.AddComponent<RangedBrain>(); else go.AddComponent<EnemyBrain>();
            var body = go.GetComponent<CapsuleCollider>(); body.height = 1.8f; body.radius = 0.4f; body.center = Vector3.up * 0.9f;
            go.SetActive(true); return go.GetComponent<Health>();
        }

        [Test]
        public void Sphere_HitsMeleeRangedDroneAndDummy_OnceEach_WithoutSelfDamage()
        {
            var owner = Body("Player", Vector3.up * 0.9f, 2);
            var ownChild = new GameObject("Player Default collider"); ownChild.transform.SetParent(owner.transform, false);
            ownChild.AddComponent<BoxCollider>();
            var melee = GroundEnemy(Vector3.forward, false);
            var ranged = GroundEnemy(Vector3.forward * 2.5f, true);
            var drone = At("Drone", new Vector3(-2f, 1.45f, 0f)); drone.layer = 8; drone.AddComponent<DroneBrain>();
            var dummy = Body("Dummy", new Vector3(1.8f, 0.9f, -1f), 0); dummy.gameObject.AddComponent<TargetDummy>();
            // This child also lies in front of ranged: an Enemy body must not act as a wall for the wave.
            var extra = new GameObject("Enemy Default collider"); extra.transform.SetParent(melee.transform, false);
            extra.transform.localPosition = Vector3.up * 0.9f;
            extra.AddComponent<BoxCollider>().size = new Vector3(0.6f, 1.5f, 0.6f);
            int hits = 0;
            var targets = new[] { melee, ranged, drone.GetComponent<Health>(), dummy };
            foreach (var target in targets) target.Damaged += _ => hits++;
            Physics.SyncTransforms();
            var query = new RadialDamage();
            Assert.That(query.Apply(owner.transform.position, 3f, 35f, owner.gameObject, ~0, ~260), Is.EqualTo(4));
            Assert.That(hits, Is.EqualTo(4)); Assert.That(owner.CurrentHealth, Is.EqualTo(100f));
            foreach (var target in targets) Assert.That(target.CurrentHealth, Is.EqualTo(65f));
        }

        [Test]
        public void Sphere_UsesActualVolumeAndSolidObstruction_IncludingOriginInsideWall()
        {
            var owner = Body("Player", Vector3.up * 0.9f, 2);
            var blocked = Body("Behind thick wall", new Vector3(0f, 0.9f, 2f));
            var far = Body("Far", new Vector3(-4f, 0.9f, 0f));
            var high = Body("Too high", new Vector3(0f, 4.5f, 0f));
            var edge = Body("Collider enters sphere", new Vector3(3.2f, 0.9f, 0f));
            var behind = Body("Behind player", new Vector3(0f, 0.9f, -2f));
            var wall = At("Thick wall", new Vector3(0f, 1.5f, 1f)).AddComponent<BoxCollider>();
            wall.size = new Vector3(2f, 3f, 0.8f); Physics.SyncTransforms();
            var query = new RadialDamage();
            Assert.That(query.Apply(owner.transform.position, 3f, 35f, owner.gameObject, ~4, ~260), Is.EqualTo(2));
            Assert.That(blocked.CurrentHealth, Is.EqualTo(100f)); Assert.That(far.CurrentHealth, Is.EqualTo(100f));
            Assert.That(high.CurrentHealth, Is.EqualTo(100f)); Assert.That(edge.CurrentHealth, Is.EqualTo(65f));
            Assert.That(behind.CurrentHealth, Is.EqualTo(65f));
            Assert.That(query.Apply(wall.transform.position, 3f, 35f, owner.gameObject, ~4, ~260), Is.Zero);
            Assert.That(blocked.CurrentHealth, Is.EqualTo(100f));
        }

        ChitinImpact Player()
        {
            var floor = At("Floor", new Vector3(0f, -0.1f, 0f)).AddComponent<BoxCollider>();
            floor.size = new Vector3(12f, 0.2f, 12f);
            var view = At("Test camera", new Vector3(0f, 2f, -4f)).AddComponent<Camera>();
            var go = At("Player", new Vector3(0f, 0.05f, 0f)); go.layer = 2; go.SetActive(false);
            var motor = go.AddComponent<PlayerMotor>(); Set(motor, "cameraTransform", view.transform);
            var body = go.GetComponent<CharacterController>();
            body.height = 1.8f; body.radius = 0.35f; body.center = Vector3.up * 0.9f; body.skinWidth = 0.02f;
            go.AddComponent<PlayerCombat>(); var ability = go.AddComponent<ChitinImpact>();
            Set(ability, "impactWindup", 0.12f); Set(ability, "impactRecovery", 0.1f); Set(ability, "impactCooldown", 2f);
            go.SetActive(true); Physics.SyncTransforms(); body.Move(Vector3.down * 0.2f);
            Cursor.lockState = CursorLockMode.Locked;
            Assert.That(go.GetComponent<PlayerCombat>().CanStartAbility, Is.True,
                "Run in PlayMode with cursor capture available; fixture must begin standing and idle.");
            return ability;
        }
        static IEnumerator WaitForEnd(ChitinImpact ability)
        {
            float timeout = Time.time + 1.5f;
            while (ability.Active && Time.time < timeout) yield return null;
            Assert.That(ability.Active, Is.False, "Ability did not release its action in time.");
        }

        [UnityTest]
        public IEnumerator Ability_WindupRecoveryCooldown_AndRepeatedAnimationEvents()
        {
            var ability = Player(); var motor = ability.GetComponent<PlayerMotor>();
            var health = ability.GetComponent<Health>(); var target = Body("Target", new Vector3(1f, 0.9f, 0f));
            Physics.SyncTransforms(); int impacts = 0, damageEvents = 0;
            ability.Impacted += (p, r, n) => impacts++; target.Damaged += _ => damageEvents++;
            Assert.That(ability.TryActivate(), Is.True); Assert.That(ability.WindingUp, Is.True);
            Assert.That(target.CurrentHealth, Is.EqualTo(100f)); Assert.That(motor.ActionLocked, Is.True);
            Assert.That(health.Invulnerable, Is.False); Assert.That(ability.TryActivate(), Is.False);
            yield return WaitForEnd(ability);
            Assert.That(impacts, Is.EqualTo(1)); Assert.That(damageEvents, Is.EqualTo(1));
            Assert.That(target.CurrentHealth, Is.EqualTo(65f)); Assert.That(motor.ActionLocked, Is.False);
            Assert.That(ability.TryActivate(), Is.False); // Recovery ended, cooldown has not.
            yield return new WaitForSeconds(ability.CooldownRemaining + 0.05f);
            Set(ability, "useAnimationEvents", true);
            Assert.That(ability.TryActivate(), Is.True);
            for (int i = 0; i < 10; i++) ability.AnimationImpact();
            Assert.That(impacts, Is.EqualTo(2)); Assert.That(damageEvents, Is.EqualTo(2));
            Assert.That(target.CurrentHealth, Is.EqualTo(30f)); Assert.That(ability.WindingUp, Is.False);
            ability.enabled = false;
            Assert.That(ability.Active, Is.False); Assert.That(motor.ActionLocked, Is.False);
            ability.enabled = true;
            Assert.That(ability.TryActivate(), Is.False, "Disabling does not refund cooldown.");
        }

        [UnityTest]
        public IEnumerator MissingAnimationEvent_TimesOutWithoutDamage_AndReleasesControl()
        {
            var ability = Player(); Set(ability, "useAnimationEvents", true);
            var target = Body("Target", new Vector3(1f, 0.9f, 0f)); Physics.SyncTransforms();
            Assert.That(ability.TryActivate(), Is.True);
            yield return WaitForEnd(ability);
            ability.AnimationImpact(); // A late event cannot create an impact after cancellation.
            Assert.That(target.CurrentHealth, Is.EqualTo(100f));
            Assert.That(ability.GetComponent<PlayerMotor>().ActionLocked, Is.False);
            Assert.That(ability.CooldownRemaining, Is.GreaterThan(0f));
        }

        [Test]
        public void IncompatibleStatesBlockActivation_DeathCancelsAndKeepsDeathLock()
        {
            var ability = Player(); var combat = ability.GetComponent<PlayerCombat>();
            var motor = ability.GetComponent<PlayerMotor>(); var climber = ability.GetComponent<WallClimber>();
            foreach (var action in new[] { CombatAction.Light, CombatAction.Heavy, CombatAction.Dodge })
            { SetAuto(combat, "Action", action); Assert.That(ability.TryActivate(), Is.False); }
            SetAuto(combat, "Action", CombatAction.Idle);
            motor.SetActionMotion(Vector3.zero); Assert.That(ability.TryActivate(), Is.False); motor.ClearActionMotion();
            SetAuto(climber, "Attached", true); Assert.That(ability.TryActivate(), Is.False); SetAuto(climber, "Attached", false);
            var mantle = ability.GetComponent<LedgeMantle>();
            SetAuto(mantle, "Active", true); Assert.That(ability.TryActivate(), Is.False); SetAuto(mantle, "Active", false);
            Set(motor, "crouched", true); Assert.That(ability.TryActivate(), Is.False); Set(motor, "crouched", false);
            Set(ability, "useAnimationEvents", true);
            var target = Body("Target", new Vector3(1f, 0.9f, 0f)); Physics.SyncTransforms();
            Assert.That(ability.TryActivate(), Is.True);
            ability.GetComponent<Health>().TakeDamage(1000f);
            ability.AnimationImpact();
            Assert.That(ability.Active, Is.False); Assert.That(ability.TryActivate(), Is.False);
            Assert.That(target.CurrentHealth, Is.EqualTo(100f));
            Assert.That(combat.Action, Is.EqualTo(CombatAction.Dead)); Assert.That(motor.ActionLocked, Is.True);
        }
    }
}
