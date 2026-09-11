using UnityEngine;

namespace SuperInseto
{
    [RequireComponent(typeof(CharacterController), typeof(WallDetector))]
    [RequireComponent(typeof(LedgeMantle))]
    public sealed class WallClimber : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float climbSpeed = 2.2f;
        [SerializeField, Min(0.02f)] float adhesionTolerance = 0.2f;
        [SerializeField, Min(0.1f)] float adhesionSpeed = 4f;
        [SerializeField, Min(0.1f)] float reattachDelay = 0.35f;
        CharacterController body;
        WallDetector detector;
        LedgeMantle mantle;
        Vector3 normal;
        float canAttachAt;
        public bool Attached { get; private set; }
        public bool Moving { get; private set; }
        public bool Mantling => mantle != null && mantle.Active;

        void Awake()
        {
            body = GetComponent<CharacterController>();
            detector = GetComponent<WallDetector>();
            mantle = GetComponent<LedgeMantle>();
        }

        public bool TryAttach()
        {
            if (Time.time < canAttachAt || !detector.Find(transform.forward, out var wall)) return false;
            normal = wall.normal;
            float desired = body.radius + body.skinWidth + 0.025f;
            body.Move(-normal * Mathf.Max(0f, wall.distance - desired));
            if (!detector.Find(-normal, out var nearby)
                || nearby.distance > desired + adhesionTolerance) return false;
            Attached = true;
            Moving = false;
            FaceWall();
            return true;
        }

        public void Detach()
        {
            if (mantle) mantle.Cancel();
            Attached = false;
            Moving = false;
            canAttachAt = Time.time + reattachDelay;
        }

        public void Tick(Vector2 input, bool release, float dt)
        {
            if (!Attached) return;
            if (release) { Detach(); return; }
            if (mantle.Active)
            {
                bool landed = mantle.Tick(dt);
                Moving = mantle.Active;
                if (landed || !mantle.Active) Detach();
                return;
            }
            if (!detector.Find(-normal, out var wall, adhesionTolerance)
                || Vector3.Dot(normal, wall.normal) < 0.9f)
            { Detach(); return; }

            normal = wall.normal;
            FaceWall();
            var right = Vector3.Cross(Vector3.up, -normal).normalized;
            var up = Vector3.ProjectOnPlane(Vector3.up, normal).normalized;
            Vector3 travel = (up * input.y + right * input.x) * climbSpeed * dt;
            // Predict the chest probe leaving the top BEFORE moving beyond the last
            // valid attachment. A blocked/no-support ledge holds here; S and release work.
            if (input.y > 0.05f && !detector.FindAt(transform.position + up * input.y * climbSpeed * dt,
                    -normal, out _, adhesionTolerance))
            {
                if (mantle.TryBegin(wall)) { Moving = true; return; }
                travel -= up * input.y * climbSpeed * dt;
            }
            float desiredDistance = body.radius + body.skinWidth + 0.025f;
            float correction = Mathf.Clamp(wall.distance - desiredDistance, -adhesionSpeed * dt, adhesionSpeed * dt);
            var before = transform.position;
            CollisionFlags flags = body.Move(travel - normal * correction);
            Moving = Vector3.ProjectOnPlane(transform.position - before, normal).sqrMagnitude > 0.000001f;
            // Top loss was handled predictively above. Lateral loss/removed walls still detach.
            if ((input.y < -0.05f && (flags & CollisionFlags.Below) != 0)
                || !detector.Find(-normal, out var after, adhesionTolerance)
                || Vector3.Dot(normal, after.normal) < 0.9f
                || after.distance > desiredDistance + adhesionTolerance)
                Detach();
        }

        void FaceWall()
        {
            transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(-normal, Vector3.up), Vector3.up);
        }
        void OnDisable() { Detach(); }
    }
}
