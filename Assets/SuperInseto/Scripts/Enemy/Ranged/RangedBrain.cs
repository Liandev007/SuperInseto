using System;
using UnityEngine;

namespace SuperInseto
{
    public enum RangedState { Patrol, Alert, Approach, RangedAttack, Reposition, Return, Dead }
    public enum RangedDistance { TooClose, Close, Preferred, Far, OutOfCombat }

    [RequireComponent(typeof(Health), typeof(EnemyNavigation), typeof(EnemyPerception))]
    [RequireComponent(typeof(RangedWeapon), typeof(RangedPositioning))]
    public sealed class RangedBrain : MonoBehaviour
    {
        [SerializeField] Health target;
        [SerializeField] Transform[] patrolPoints;
        [SerializeField, Min(0f)] float patrolWait = 1f;
        [SerializeField, Min(0f)] float alertDuration = 0.45f;
        [SerializeField, Min(0.1f)] float loseSightDelay = 3f;
        [SerializeField, Min(0.1f)] float chaseRange = 18f;
        [SerializeField, Min(0.1f)] float preferredMinRange = 4f;
        [SerializeField, Min(0.1f)] float preferredMaxRange = 8f;
        [SerializeField, Min(0.1f)] float tooCloseRange = 2.7f;
        [SerializeField, Min(0.1f)] float maxCombatRange = 15f;
        [SerializeField, Min(0.1f)] float repositionTimeout = 2f;
        [SerializeField, Min(0.1f)] float repositionRetryDelay = 1f;
        Health health;
        Collider targetBody;
        EnemyNavigation navigation;
        EnemyPerception perception;
        RangedWeapon weapon;
        RangedPositioning positioning;
        Vector3 lastKnown, retreatPoint;
        float lastSeen = float.NegativeInfinity, stateSince, waitSince = -1f, retreatUntil, retryAt, turnUntil;
        bool retreating;
        int patrolIndex;
        public RangedState State { get; private set; }
        public event Action<RangedState> StateChanged;
        float Minimum => Mathf.Max(0.2f, preferredMinRange);
        float Maximum => Mathf.Max(Minimum, preferredMaxRange);
        float CombatMaximum => Mathf.Max(Maximum, maxCombatRange);
        public RangedDistance ClassifyDistance(float distance)
        {
            if (distance < Mathf.Clamp(tooCloseRange, 0.1f, Minimum)) return RangedDistance.TooClose;
            if (distance < Minimum) return RangedDistance.Close;
            if (distance <= Maximum) return RangedDistance.Preferred;
            return distance <= CombatMaximum ? RangedDistance.Far : RangedDistance.OutOfCombat;
        }
        void Awake()
        {
            health = GetComponent<Health>(); navigation = GetComponent<EnemyNavigation>();
            perception = GetComponent<EnemyPerception>(); weapon = GetComponent<RangedWeapon>();
            positioning = GetComponent<RangedPositioning>();
        }
        void OnEnable() { health.Died += Die; health.Damaged += Hit; }
        void OnDisable()
        { health.Died -= Die; health.Damaged -= Hit; weapon.Cancel(); navigation.Stop(); }
        public void Configure(Health player, Transform[] points)
        { target = player; targetBody = target ? target.GetComponent<Collider>() : null; patrolPoints = points; }
        void Start() { if (!targetBody && target) targetBody = target.GetComponent<Collider>(); }
        void SetState(RangedState next)
        {
            if (State == next) return;
            State = next; stateSince = Time.time; waitSince = -1f;
            StateChanged?.Invoke(next);
        }
        void Update()
        {
            if (State == RangedState.Dead || health.IsDead || !navigation.Ready) return;
            bool alive = target && target.isActiveAndEnabled && !target.IsDead;
            float distance = alive ? Vector3.Distance(transform.position, target.transform.position) : float.PositiveInfinity;
            bool visible = alive && distance <= Mathf.Max(CombatMaximum, chaseRange) && perception.CanSee(target, targetBody);
            if (visible) { lastKnown = target.transform.position; lastSeen = Time.time; }
            if ((State == RangedState.Patrol || State == RangedState.Return) && visible)
            { navigation.Stop(); SetState(RangedState.Alert); }
            if (State == RangedState.Patrol || State == RangedState.Return) { Patrol(); return; }
            if (!alive || Time.time - lastSeen > loseSightDelay)
            {
                weapon.Cancel(); navigation.Stop(); retreating = false;
                SetState(RangedState.Return); return;
            }
            if (State == RangedState.Alert)
            {
                navigation.Face(lastKnown);
                if (Time.time - stateSince >= alertDuration) SetState(RangedState.Approach);
                return;
            }
            if (!visible)
            {
                // Walking away turns the shared navigator away from the player. Finish the bounded
                // retreat using memory, then turn back before deciding the player was lost.
                if (retreating) { ContinueRetreat(); return; }
                if (State == RangedState.Reposition && Time.time < turnUntil)
                { navigation.Stop(); navigation.Face(lastKnown); return; }
                weapon.Cancel(); retreating = false; SetState(RangedState.Approach);
                navigation.GoTo(lastKnown, true); return;
            }
            var band = ClassifyDistance(distance);
            if (band == RangedDistance.TooClose || (band == RangedDistance.Close && !weapon.Active)
                || (retreating && distance < Minimum))
            { Reposition(); return; }
            if (retreating) { retreating = false; navigation.Stop(); }
            if (weapon.Active)
            { navigation.Stop(); navigation.Face(weapon.AimPoint); SetState(RangedState.RangedAttack); return; }
            if (band == RangedDistance.Far || band == RangedDistance.OutOfCombat)
            { SetState(RangedState.Approach); navigation.GoTo(lastKnown, true); return; }
            navigation.Stop(); navigation.Face(lastKnown); SetState(RangedState.Approach);
            Vector3 direction = Vector3.ProjectOnPlane(lastKnown - transform.position, Vector3.up);
            if (Vector3.Angle(transform.forward, direction) < 15f && weapon.TryStart(target, targetBody, CombatMaximum))
                SetState(RangedState.RangedAttack);
        }
        void Reposition()
        {
            weapon.Cancel(); SetState(RangedState.Reposition);
            if (retreating)
            {
                ContinueRetreat();
                return;
            }
            navigation.Stop(); navigation.Face(lastKnown);
            if (Time.time < retryAt) return;
            retryAt = Time.time + repositionRetryDelay;
            if (!positioning.TryFindRetreat(target.transform.position, out retreatPoint)) return;
            retreating = true; retreatUntil = Time.time + repositionTimeout;
            navigation.GoTo(retreatPoint, true);
        }
        void ContinueRetreat()
        {
            if (Time.time >= retreatUntil || Vector3.Distance(transform.position, retreatPoint) < 0.35f)
            {
                navigation.Stop(); retreating = false;
                retryAt = Time.time + repositionRetryDelay;
                turnUntil = Time.time + 0.6f;
                navigation.Face(lastKnown);
            }
            else navigation.GoTo(retreatPoint, true);
        }
        void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) { navigation.Stop(); return; }
            patrolIndex %= patrolPoints.Length;
            var point = patrolPoints[patrolIndex];
            if (!point) { patrolIndex = (patrolIndex + 1) % patrolPoints.Length; return; }
            if (Vector3.Distance(transform.position, point.position) < 0.35f)
            {
                navigation.Stop();
                if (State == RangedState.Return) SetState(RangedState.Patrol);
                if (waitSince < 0f) waitSince = Time.time;
                if (Time.time - waitSince >= patrolWait)
                { patrolIndex = (patrolIndex + 1) % patrolPoints.Length; waitSince = -1f; }
            }
            else navigation.GoTo(point.position, false);
        }
        void Hit(DamageInfo damage)
        {
            if (health.IsDead || State == RangedState.Dead || !target || damage.Source != target.gameObject) return;
            lastKnown = target.transform.position; lastSeen = Time.time;
            if (State == RangedState.Patrol || State == RangedState.Return)
            { navigation.Stop(); SetState(RangedState.Alert); }
        }
        void Die()
        {
            weapon.Cancel(); retreating = false; navigation.Shutdown(); SetState(RangedState.Dead);
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, Minimum);
            Gizmos.DrawWireSphere(transform.position, Maximum);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, tooCloseRange);
            if (patrolPoints == null) return;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                var point = patrolPoints[i]; var next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (!point) continue;
                Gizmos.DrawWireSphere(point.position, 0.25f);
                if (next) Gizmos.DrawLine(point.position, next.position);
            }
        }
    }
}
