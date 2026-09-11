using UnityEngine;

namespace SuperInseto
{
    public sealed class InteractionProbe
    {
        readonly Collider[] nearby = new Collider[64];

        public Interactable Find(Vector3 origin, Transform view, float reach, float halfAngle, LayerMask mask)
        {
            if (!view) return null;
            int count = Physics.OverlapSphereNonAlloc(origin, reach, nearby, mask, QueryTriggerInteraction.Ignore);
            Interactable best = null;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var target = nearby[i].GetComponentInParent<Interactable>();
                if (!target || !target.Available) continue;
                Vector3 point = target.AimPoint;
                float distance = Vector3.Distance(origin, point);
                float angle = Vector3.Angle(view.forward, point - view.position);
                if (distance > reach || angle > halfAngle) continue;
                if (!Visible(origin, point, target, mask) || !Visible(view.position, point, target, mask)) continue;
                float score = angle + distance * 0.2f;
                if (score < bestScore) { best = target; bestScore = score; }
            }
            return best;
        }

        static bool Visible(Vector3 from, Vector3 to, Interactable target, LayerMask mask)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.0001f) return true;
            return !Physics.Raycast(from, delta.normalized, out var hit, delta.magnitude + 0.01f,
                       mask, QueryTriggerInteraction.Ignore)
                || hit.collider.GetComponentInParent<Interactable>() == target;
        }
    }
}
