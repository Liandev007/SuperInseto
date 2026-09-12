using System;
using UnityEngine;

namespace SuperInseto
{
    public enum EnemyState { Patrol, Alert, Chase, Attack, Return, Dead }

    [RequireComponent(typeof(EnemyNavigation), typeof(EnemyPerception), typeof(EnemyAttack))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] Health target;
        [SerializeField] Transform[] patrolPoints;
        [SerializeField, Min(0f)] float patrolWait = 1f;
        [SerializeField, Min(0f)] float alertDuration = 0.45f;
        [SerializeField, Min(0.1f)] float loseSightDelay = 3f;
        [SerializeField, Min(0.1f)] float chaseRange = 13f;
        Health health;
        Collider targetBody;
        EnemyNavigation navigation;
        EnemyPerception perception;
        EnemyAttack attack;
        Vector3 lastKnown;
        float lastSeen = float.NegativeInfinity, stateSince, waitSince = -1f;
        int patrolIndex;
        public EnemyState State { get; private set; }
        public event Action<EnemyState> StateChanged;

        void Awake()
        {
            health = GetComponent<Health>(); navigation = GetComponent<EnemyNavigation>();
            perception = GetComponent<EnemyPerception>(); attack = GetComponent<EnemyAttack>();
        }
        void OnEnable() { health.Died += Die; health.Damaged += Hit; }
        void OnDisable()
        {
            health.Died -= Die; health.Damaged -= Hit;
            attack.Cancel(); navigation.Stop();
        }
        public void Configure(Health player, Transform[] points)
        { target = player; targetBody = target ? target.GetComponent<Collider>() : null; patrolPoints = points; }
        void Start() { if (!targetBody && target) targetBody = target.GetComponent<Collider>(); }
        void SetState(EnemyState next)
        {
            if (State == next) return;
            State = next; stateSince = Time.time; waitSince = -1f;
            StateChanged?.Invoke(next);
        }
        void Update()
        {
            if (State == EnemyState.Dead || health.IsDead || !navigation.Ready) return;
            bool targetAlive = target && target.isActiveAndEnabled && !target.IsDead;
            bool visible = targetAlive && Vector3.Distance(transform.position, target.transform.position) <= chaseRange
                && perception.CanSee(target, targetBody);
            if (visible) { lastKnown = target.transform.position; lastSeen = Time.time; }
            if (State == EnemyState.Attack)
            {
                // Lock orientation for the full swing: the telegraph can be dodged.
                navigation.Stop();
                if (!targetAlive) attack.Cancel();
                if (!attack.Active) SetState(targetAlive ? EnemyState.Chase : EnemyState.Return);
                return;
            }
            if ((State == EnemyState.Patrol || State == EnemyState.Return) && visible)
            { navigation.Stop(); SetState(EnemyState.Alert); }
            if (State == EnemyState.Alert)
            {
                navigation.Face(lastKnown);
                if (Time.time - stateSince >= alertDuration) SetState(EnemyState.Chase);
                return;
            }
            if (State == EnemyState.Chase)
            {
                if (!targetAlive || Time.time - lastSeen > loseSightDelay)
                { navigation.Stop(); SetState(EnemyState.Return); return; }
                if (visible && Vector3.Distance(transform.position, target.transform.position) <= attack.Range)
                {
                    navigation.Stop(); navigation.Face(lastKnown);
                    Vector3 direction = Vector3.ProjectOnPlane(lastKnown - transform.position, Vector3.up);
                    if (Vector3.Angle(transform.forward, direction) < 20f && attack.TryStart()) SetState(EnemyState.Attack);
                }
                else navigation.GoTo(lastKnown, true);
                return;
            }
            Patrol();
        }
        void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) { navigation.Stop(); return; }
            patrolIndex %= patrolPoints.Length;
            Transform point = patrolPoints[patrolIndex];
            if (!point) { patrolIndex = (patrolIndex + 1) % patrolPoints.Length; return; }
            if (Vector3.Distance(transform.position, point.position) < 0.35f)
            {
                navigation.Stop();
                if (State == EnemyState.Return) SetState(EnemyState.Patrol);
                if (waitSince < 0f) waitSince = Time.time;
                if (Time.time - waitSince >= patrolWait)
                { patrolIndex = (patrolIndex + 1) % patrolPoints.Length; waitSince = -1f; }
            }
            else navigation.GoTo(point.position, false);
        }
        void Hit(DamageInfo damage)
        {
            if (health.IsDead || State == EnemyState.Dead || !target || damage.Source != target.gameObject) return;
            lastKnown = target.transform.position; lastSeen = Time.time;
            if (State == EnemyState.Patrol || State == EnemyState.Return)
            { navigation.Stop(); SetState(EnemyState.Alert); }
        }
        void Die()
        {
            attack.Cancel(); navigation.Shutdown(); SetState(EnemyState.Dead);
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
        void OnDrawGizmosSelected()
        {
            if (patrolPoints == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (!patrolPoints[i]) continue;
                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.25f);
                var next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (next) Gizmos.DrawLine(patrolPoints[i].position, next.position);
            }
        }
    }
}
