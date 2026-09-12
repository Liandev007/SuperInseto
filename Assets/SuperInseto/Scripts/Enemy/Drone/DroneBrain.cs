using System;
using UnityEngine;

namespace SuperInseto
{
    public enum DroneState { Patrol, Alert, Approach, Attack, Reposition, Search, Return, Dead }
    public enum DroneDistance { TooClose, Close, Preferred, Far }

    [RequireComponent(typeof(Health), typeof(DroneFlight), typeof(EnemyPerception))]
    [RequireComponent(typeof(RangedWeapon))]
    public sealed class DroneBrain : MonoBehaviour
    {
        [SerializeField] Health target;
        [SerializeField] Transform[] waypoints;
        [SerializeField, Min(0.05f)] float arrivalDistance = 0.25f;
        [SerializeField, Min(0f)] float patrolWaitTime = 1f;
        [SerializeField, Min(1f)] float waypointTimeout = 12f;
        [SerializeField, Min(0f)] float alertDuration = 0.45f;
        [SerializeField, Min(0.1f)] float loseSightDelay = 3f;
        [SerializeField, Min(0.1f)] float searchDuration = 1f;
        [SerializeField, Min(0.1f)] float maxChaseRange = 14f;
        [SerializeField, Min(0.1f)] float preferredMinRange = 3f;
        [SerializeField, Min(0.1f)] float preferredMaxRange = 6.5f;
        [SerializeField, Min(0.1f)] float tooCloseRange = 2f;
        [SerializeField, Min(0.1f)] float retreatDistance = 1.8f;
        [SerializeField, Min(0.1f)] float repositionDuration = 1f;
        [SerializeField, Min(0.1f)] float repositionPause = 1.2f;
        [SerializeField, Min(0.1f)] float deathDisplayTime = 2f;
        Health health;
        Collider targetBody;
        DroneFlight flight;
        EnemyPerception perception;
        RangedWeapon weapon;
        Vector3 lastKnown, retreatPoint;
        float lastSeen = float.NegativeInfinity, stateSince, waitSince = -1f, waypointSince, retreatUntil, retryAt;
        bool retreating;
        int waypointIndex;
        public DroneState State { get; private set; }
        public event Action<DroneState> StateChanged;
        float Minimum => Mathf.Max(0.2f, preferredMinRange);
        float Maximum => Mathf.Max(Minimum, preferredMaxRange);
        public DroneDistance ClassifyDistance(float horizontalDistance)
        {
            if (horizontalDistance < Mathf.Clamp(tooCloseRange, 0.1f, Minimum)) return DroneDistance.TooClose;
            if (horizontalDistance < Minimum) return DroneDistance.Close;
            return horizontalDistance <= Maximum ? DroneDistance.Preferred : DroneDistance.Far;
        }
        void Awake()
        {
            health = GetComponent<Health>(); flight = GetComponent<DroneFlight>();
            perception = GetComponent<EnemyPerception>(); weapon = GetComponent<RangedWeapon>();
        }
        void OnEnable() { health.Died += Die; health.Damaged += Hit; }
        void OnDisable() { health.Died -= Die; health.Damaged -= Hit; weapon.Cancel(); flight.Stop(); }
        public void Configure(Health player, Transform[] points)
        { target = player; targetBody = target ? target.GetComponent<Collider>() : null; waypoints = points; }
        void Start() { if (!targetBody && target) targetBody = target.GetComponent<Collider>(); waypointSince = Time.time; }
        void SetState(DroneState next)
        {
            if (State == next) return;
            State = next; stateSince = Time.time; waitSince = -1f;
            if (next == DroneState.Return) waypointSince = Time.time;
            StateChanged?.Invoke(next);
        }
        void Update()
        {
            if (State == DroneState.Dead)
            { if (Time.time - stateSince >= deathDisplayTime) gameObject.SetActive(false); return; }
            bool alive = target && target.isActiveAndEnabled && !target.IsDead;
            bool visible = alive && Vector3.Distance(transform.position, target.transform.position) <= maxChaseRange
                && perception.CanSee(target, targetBody);
            if (visible) { lastKnown = target.transform.position; lastSeen = Time.time; }
            if ((State == DroneState.Patrol || State == DroneState.Return || State == DroneState.Search) && visible)
            { flight.Stop(); SetState(DroneState.Alert); }
            if (State == DroneState.Patrol || State == DroneState.Return) { Patrol(); return; }
            if (!alive) { weapon.Cancel(); flight.Stop(); retreating = false; SetState(DroneState.Return); return; }
            flight.Face(weapon.Aiming ? weapon.AimPoint : lastKnown); // Can face the player while flying backward.
            if (State == DroneState.Search)
            {
                flight.Stop();
                if (Time.time - stateSince >= searchDuration) SetState(DroneState.Return);
                return;
            }
            if (!visible && Time.time - lastSeen > loseSightDelay)
            { weapon.Cancel(); flight.Stop(); retreating = false; SetState(DroneState.Search); return; }
            if (State == DroneState.Alert)
            {
                flight.MoveTo(flight.CombatPoint(transform.position), true);
                if (Time.time - stateSince >= alertDuration) SetState(DroneState.Approach);
                return;
            }
            if (!visible)
            {
                weapon.Cancel(); retreating = false; SetState(DroneState.Approach);
                flight.MoveTo(flight.CombatPoint(lastKnown), true); return;
            }
            float distance = Vector3.ProjectOnPlane(lastKnown - transform.position, Vector3.up).magnitude;
            var band = ClassifyDistance(distance);
            if (band == DroneDistance.TooClose || (band == DroneDistance.Close && !weapon.Active) || retreating)
            { Reposition(); return; }
            if (weapon.Active) { flight.Stop(); SetState(DroneState.Attack); return; }
            if (band == DroneDistance.Far)
            { SetState(DroneState.Approach); flight.MoveTo(flight.CombatPoint(lastKnown), true); return; }
            // Descend smoothly before firing, keeping the logical collider reachable by M3 ground attacks.
            if (Mathf.Abs(transform.position.y - flight.CombatHeight) > 0.1f)
            { SetState(DroneState.Approach); flight.MoveTo(flight.CombatPoint(transform.position), true); return; }
            flight.Stop(); SetState(DroneState.Approach);
            Vector3 direction = Vector3.ProjectOnPlane(lastKnown - transform.position, Vector3.up);
            if (Vector3.Angle(transform.forward, direction) < 15f && weapon.TryStart(target, targetBody, maxChaseRange))
                SetState(DroneState.Attack);
        }
        void Reposition()
        {
            weapon.Cancel(); SetState(DroneState.Reposition);
            if (retreating)
            {
                if (Time.time >= retreatUntil || Vector3.Distance(transform.position, retreatPoint) < arrivalDistance)
                { flight.Stop(); retreating = false; retryAt = Time.time + repositionPause; }
                else flight.MoveTo(retreatPoint, true);
                return;
            }
            // A pause makes a close drone catchable. No continuous kiting against walls.
            flight.MoveTo(flight.CombatPoint(transform.position), true);
            if (Time.time < retryAt) return;
            retryAt = Time.time + repositionPause;
            Vector3 away = Vector3.ProjectOnPlane(transform.position - lastKnown, Vector3.up).normalized;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            float current = Vector3.ProjectOnPlane(transform.position - lastKnown, Vector3.up).magnitude;
            for (int i = 0; i < 3; i++)
            {
                float angle = i == 0 ? 0f : i == 1 ? 60f : -60f;
                Vector3 candidate = flight.CombatPoint(transform.position + Quaternion.AngleAxis(angle, Vector3.up) * away * retreatDistance);
                if (Vector3.ProjectOnPlane(candidate - lastKnown, Vector3.up).magnitude < current + 0.5f || !flight.ClearRoute(candidate)) continue;
                retreatPoint = candidate; retreating = true; retreatUntil = Time.time + repositionDuration;
                flight.MoveTo(retreatPoint, true); break;
            }
        }
        void Patrol()
        {
            if (waypoints == null || waypoints.Length == 0) { flight.Stop(); return; }
            waypointIndex %= waypoints.Length;
            var point = waypoints[waypointIndex];
            if (!point || Time.time - waypointSince > waypointTimeout) { NextWaypoint(); return; }
            Vector3 destination = flight.Constrain(point.position);
            flight.Face(destination);
            if (Vector3.Distance(transform.position, destination) <= arrivalDistance)
            {
                flight.Stop();
                if (State == DroneState.Return) SetState(DroneState.Patrol);
                if (waitSince < 0f) waitSince = Time.time;
                if (Time.time - waitSince >= patrolWaitTime) NextWaypoint();
            }
            else flight.MoveTo(destination, false);
        }
        void NextWaypoint()
        { waypointIndex = (waypointIndex + 1) % waypoints.Length; waypointSince = Time.time; waitSince = -1f; flight.Stop(); }
        void Hit(DamageInfo damage)
        {
            if (health.IsDead || State == DroneState.Dead || !target || damage.Source != target.gameObject) return;
            lastKnown = target.transform.position; lastSeen = Time.time;
            if (State == DroneState.Patrol || State == DroneState.Return || State == DroneState.Search)
            { flight.Stop(); SetState(DroneState.Alert); }
        }
        void Die()
        {
            weapon.Cancel(); retreating = false; flight.Shutdown(); SetState(DroneState.Dead);
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, Minimum); Gizmos.DrawWireSphere(transform.position, Maximum);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, tooCloseRange);
            if (waypoints == null) return;
            for (int i = 0; i < waypoints.Length; i++)
            {
                var point = waypoints[i]; var next = waypoints[(i + 1) % waypoints.Length];
                if (!point) continue;
                Gizmos.DrawWireSphere(point.position, arrivalDistance);
                if (next) Gizmos.DrawLine(point.position, next.position);
            }
        }
    }
}
