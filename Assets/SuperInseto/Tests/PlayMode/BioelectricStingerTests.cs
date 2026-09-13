using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace SuperInseto.Tests
{
    public sealed class BioelectricStingerTests
    {
        static readonly Vector3 Origin = new Vector3(9000f, 0f, 9000f);
        GameObject root;
        CursorLockMode previousLock;
        bool previousVisible;
        [SetUp] public void Setup()
        { root = new GameObject("M9 tests"); previousLock = Cursor.lockState; previousVisible = Cursor.visible; }
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
        BioelectricProjectile Shot(GameObject owner, Vector3 offset, Vector3 heading, float lifetime = 1f)
        {
            var shot = At("Stinger", offset).AddComponent<BioelectricProjectile>();
            shot.Launch(owner, heading, 25f, 100f, lifetime, 0.1f); return shot;
        }

        [Test]
        public void Projectile_HitsAllFourTargetTypes_OnceEach_AndNeverItsOwner()
        {
            var owner = Body("Player", new Vector3(0f, 1.2f, 0f), 2);
            var child = new GameObject("Player Default collider"); child.transform.SetParent(owner.transform, false);
            child.AddComponent<BoxCollider>();
            var targets = new Health[4];
            targets[0] = Body("Dummy", new Vector3(-6f, 1.2f, 4f), 0);
            targets[0].gameObject.AddComponent<TargetDummy>();
            for (int i = 1; i <= 2; i++)
            {
                var go = At("Ground agent", new Vector3((i - 2) * 3f, 0f, 4f)); go.layer = 8; go.SetActive(false);
                go.AddComponent<NavMeshAgent>().enabled = false;
                if (i == 1) go.AddComponent<EnemyBrain>(); else go.AddComponent<RangedBrain>();
                var body = go.GetComponent<CapsuleCollider>(); body.height = 1.8f; body.radius = 0.4f; body.center = Vector3.up * 0.9f;
                go.SetActive(true); targets[i] = go.GetComponent<Health>();
            }
            var drone = At("Drone", new Vector3(3f, 1.45f, 4f)); drone.layer = 8; drone.AddComponent<DroneBrain>();
            targets[3] = drone.GetComponent<Health>();
            var extra = new GameObject("Enemy Default collider"); extra.transform.SetParent(targets[2].transform, false);
            extra.transform.localPosition = Vector3.up * 1.2f; extra.AddComponent<BoxCollider>();
            int hits = 0; foreach (var health in targets) health.Damaged += _ => hits++;
            Physics.SyncTransforms();
            for (int i = 0; i < targets.Length; i++)
            {
                Vector3 point = targets[i].GetComponent<Collider>().bounds.center;
                var shot = Shot(owner.gameObject, owner.transform.position - Origin, point - owner.transform.position);
                shot.Advance(0.1f); shot.Advance(0.1f);
                Assert.That(shot.Flying, Is.False); Assert.That(targets[i].CurrentHealth, Is.EqualTo(75f));
            }
            Assert.That(hits, Is.EqualTo(4)); Assert.That(owner.CurrentHealth, Is.EqualTo(100f));
            for (int i = 0; i < 3; i++)
                Shot(owner.gameObject, owner.transform.position - Origin, drone.transform.position - owner.transform.position).Advance(0.1f);
            Assert.That(targets[3].IsDead, Is.True);
            Assert.That(drone.GetComponent<DroneBrain>().State, Is.EqualTo(DroneState.Dead));
            Assert.That(drone.GetComponent<RangedWeapon>().Active, Is.False);
            for (int i = 0; i < 3; i++) Assert.That(targets[i].CurrentHealth, Is.EqualTo(75f));
        }

        [UnityTest]
        public IEnumerator Projectile_SweepsThinWall_HandlesOverlap_AndDestroysAfterImpactOrLifetime()
        {
            var enemy = Body("Behind wall", new Vector3(0f, 1.2f, 4f));
            var wall = At("Thin wall", new Vector3(0f, 1.2f, 2f)).AddComponent<BoxCollider>();
            wall.size = new Vector3(2f, 3f, 0.02f); Physics.SyncTransforms();
            var shot = Shot(null, Vector3.up * 1.2f, Vector3.forward);
            int impacts = 0; shot.Impacted += (p, damaged) => { impacts++; Assert.That(damaged, Is.False); };
            shot.Advance(0.1f); shot.Advance(0.1f);
            Assert.That(impacts, Is.EqualTo(1)); Assert.That(enemy.CurrentHealth, Is.EqualTo(100f));
            var overlap = Shot(null, wall.transform.position - Origin, Vector3.forward); overlap.Advance(0.1f);
            Assert.That(overlap.Flying, Is.False);
            var expired = Shot(null, new Vector3(10f, 1.2f, 0f), Vector3.forward, 0.01f);
            expired.Advance(1f); Assert.That(expired.Flying, Is.False);
            Assert.That(expired.transform.position.z - Origin.z, Is.EqualTo(1f).Within(0.01f));
            yield return new WaitForSeconds(0.2f);
            Assert.That(shot == null && overlap == null && expired == null, Is.True);
        }

        [Test]
        public void CameraAim_UsesCenterAndWalls_WithSeparateMuzzleClearance_AndPreservesEnemyFilter()
        {
            var owner = Body("Player", Vector3.up * 0.9f, 2);
            var camera = At("Camera", new Vector3(0f, 1.5f, -4f)).AddComponent<Camera>();
            var enemy = Body("Elevated target", new Vector3(0f, 3f, 7f));
            camera.transform.LookAt(enemy.transform.position); Physics.SyncTransforms();
            var aim = new BioelectricAim();
            Assert.That(aim.TryGetPoint(camera, owner.transform, 45f, ~4, out var point), Is.True);
            Assert.That(Vector3.Distance(point, enemy.transform.position), Is.LessThan(0.5f));
            var wall = At("Camera ray wall", new Vector3(0f, 2f, 0f)).AddComponent<BoxCollider>();
            wall.size = new Vector3(2f, 2f, 0.1f); Physics.SyncTransforms();
            Assert.That(aim.TryGetPoint(camera, owner.transform, 45f, ~4, out point), Is.True);
            Assert.That(point.z - Origin.z, Is.LessThan(0.1f));
            wall.enabled = false; enemy.GetComponent<Collider>().enabled = false; Physics.SyncTransforms();
            Assert.That(aim.TryGetPoint(camera, owner.transform, 45f, ~4, out point), Is.True);
            Assert.That(Vector3.Distance(camera.transform.position, point), Is.EqualTo(45f).Within(0.02f));
            wall.enabled = true; wall.transform.position = Origin + new Vector3(0.15f, 1.05f, 0.3f);
            wall.size = new Vector3(0.3f, 0.3f, 0.05f); Physics.SyncTransforms();
            Assert.That(aim.MuzzleClear(owner.transform.position, Origin + new Vector3(0.3f, 1.2f, 0.6f), 0.1f, owner.transform, ~4), Is.False);
            wall.enabled = false; enemy.GetComponent<Collider>().enabled = true; Physics.SyncTransforms();
            Vector3 ray = enemy.transform.position - camera.transform.position;
            Assert.That(new ProjectileCollision().Trace(camera.transform.position, ray, ray.magnitude, 0.1f, owner.transform, ~4, out _, out _), Is.False);
            Assert.That(new ProjectileCollision(false).Trace(camera.transform.position, ray, ray.magnitude, 0.1f, owner.transform, ~4, out _, out _), Is.True);
        }

        BioelectricStinger Player()
        {
            var floor = At("Floor", new Vector3(0f, -0.1f, 0f)).AddComponent<BoxCollider>();
            floor.size = new Vector3(12f, 0.2f, 12f);
            var view = At("Test camera", new Vector3(0f, 1.2f, -4f)).AddComponent<Camera>();
            var go = At("Player", new Vector3(0f, 0.05f, 0f)); go.layer = 2; go.SetActive(false);
            var motor = go.AddComponent<PlayerMotor>(); Set(motor, "cameraTransform", view.transform);
            var body = go.GetComponent<CharacterController>();
            body.height = 1.8f; body.radius = 0.35f; body.center = Vector3.up * 0.9f; body.skinWidth = 0.02f;
            go.AddComponent<PlayerCombat>(); go.AddComponent<ChitinImpact>();
            var ability = go.AddComponent<BioelectricStinger>();
            var socket = new GameObject("Replaceable socket").transform; socket.SetParent(go.transform, false);
            socket.localPosition = new Vector3(0.3f, 1.2f, 0.6f);
            var template = At("Projectile template", Vector3.left * 30f).AddComponent<BioelectricProjectile>();
            Set(ability, "aimCamera", view); Set(ability, "firePoint", socket); Set(ability, "projectilePrefab", template);
            Set(ability, "windup", 0.12f); Set(ability, "recovery", 0.1f); Set(ability, "abilityCooldown", 0.5f);
            ability.Fired += p => p.transform.SetParent(root.transform);
            go.SetActive(true); Physics.SyncTransforms(); body.Move(Vector3.down * 0.2f); Cursor.lockState = CursorLockMode.Locked;
            Assert.That(go.GetComponent<PlayerCombat>().CanStartAbility, Is.True, "PlayMode requires cursor capture and a grounded fixture.");
            return ability;
        }
        static IEnumerator WaitForEnd(BioelectricStinger ability)
        {
            float timeout = Time.time + 1.5f;
            while (ability.Active && Time.time < timeout) yield return null;
            Assert.That(ability.Active, Is.False, "Stinger failed to release its action.");
        }

        [UnityTest]
        public IEnumerator Ability_WindupSingleShotRecoveryCooldown_AndMutualExclusionWithImpact()
        {
            var ability = Player(); var impact = ability.GetComponent<ChitinImpact>(); var motor = ability.GetComponent<PlayerMotor>();
            int shots = 0; ability.Fired += _ => shots++;
            Assert.That(impact.TryActivate(), Is.True); Assert.That(ability.TryActivate(), Is.False); impact.Cancel();
            Set(impact, "readyAt", Time.time); // Test the F -> R action lock, independently of R's cooldown.
            Assert.That(ability.TryActivate(), Is.True); Assert.That(ability.WindingUp, Is.True);
            Assert.That(shots, Is.Zero); Assert.That(impact.TryActivate(), Is.False); Assert.That(ability.TryActivate(), Is.False);
            Assert.That(ability.GetComponent<Health>().Invulnerable, Is.False);
            yield return WaitForEnd(ability);
            Assert.That(shots, Is.EqualTo(1)); Assert.That(motor.ActionLocked, Is.False);
            Assert.That(ability.GetComponent<PlayerCombat>().CanStartAbility, Is.True);
            Assert.That(ability.TryActivate(), Is.False);
            yield return new WaitForSeconds(ability.CooldownRemaining + 0.05f);
            Set(ability, "useAnimationEvents", true); Assert.That(ability.TryActivate(), Is.True);
            for (int i = 0; i < 10; i++) ability.AnimationFire();
            yield return WaitForEnd(ability);
            Assert.That(shots, Is.EqualTo(2)); Assert.That(motor.ActionLocked, Is.False);
            ability.AnimationFire(); Assert.That(ability.Active, Is.False);
            Assert.That(impact.TryActivate(), Is.True); impact.Cancel();
        }

        [UnityTest]
        public IEnumerator Ability_BlockedMuzzleAndMissingEvent_ReleaseControlWithoutSpawning()
        {
            var ability = Player(); int shots = 0, blocked = 0;
            ability.Fired += _ => shots++; ability.Blocked += _ => blocked++;
            var wall = At("Socket cover", new Vector3(0.15f, 1.05f, 0.3f)).AddComponent<BoxCollider>();
            wall.size = new Vector3(0.3f, 0.3f, 0.05f); Physics.SyncTransforms();
            Assert.That(ability.TryActivate(), Is.True); yield return WaitForEnd(ability);
            Assert.That(shots, Is.Zero); Assert.That(blocked, Is.EqualTo(1));
            Assert.That(ability.GetComponent<PlayerMotor>().ActionLocked, Is.False);
            wall.enabled = false; Physics.SyncTransforms();
            yield return new WaitForSeconds(ability.CooldownRemaining + 0.05f);
            Set(ability, "useAnimationEvents", true); Assert.That(ability.TryActivate(), Is.True);
            yield return WaitForEnd(ability);
            ability.AnimationFire(); Assert.That(shots, Is.Zero); Assert.That(ability.CooldownRemaining, Is.GreaterThan(0f));
            Assert.That(ability.GetComponent<PlayerMotor>().ActionLocked, Is.False);
        }

        [Test]
        public void Ability_IncompatibleStatesBlock_DeathCancelsAndKeepsDeathLock()
        {
            var ability = Player(); var combat = ability.GetComponent<PlayerCombat>(); var motor = ability.GetComponent<PlayerMotor>();
            var climber = ability.GetComponent<WallClimber>(); var mantle = ability.GetComponent<LedgeMantle>();
            foreach (var action in new[] { CombatAction.Light, CombatAction.Heavy, CombatAction.Dodge })
            { SetAuto(combat, "Action", action); Assert.That(ability.TryActivate(), Is.False); }
            SetAuto(combat, "Action", CombatAction.Idle);
            SetAuto(climber, "Attached", true); Assert.That(ability.TryActivate(), Is.False); SetAuto(climber, "Attached", false);
            SetAuto(mantle, "Active", true); Assert.That(ability.TryActivate(), Is.False); SetAuto(mantle, "Active", false);
            Set(motor, "crouched", true); Assert.That(ability.TryActivate(), Is.False); Set(motor, "crouched", false);
            Set(ability, "useAnimationEvents", true); int shots = 0; ability.Fired += _ => shots++;
            Assert.That(ability.TryActivate(), Is.True); ability.AnimationFire();
            ability.GetComponent<Health>().TakeDamage(1000f); ability.AnimationFire();
            Assert.That(ability.Active, Is.False); Assert.That(ability.TryActivate(), Is.False); Assert.That(shots, Is.Zero);
            Assert.That(combat.Action, Is.EqualTo(CombatAction.Dead)); Assert.That(motor.ActionLocked, Is.True);
        }
    }
}
