using UnityEngine;

namespace SuperInseto
{
    // Read on demand by M9 after camera-facing rotation; never caches the previous frame's socket position.
    public sealed class BioelectricCastOrigin : MonoBehaviour
    {
        Transform leftHand, rightHand, player;
        Animator sourceAnimator;
        float forwardOffset;
        bool warned;
        public void Initialize(Animator animator, Transform logicalPlayer, float offset)
        {
            sourceAnimator = animator; player = logicalPlayer; forwardOffset = Mathf.Max(0f, offset);
            if (animator && animator.avatar && animator.avatar.isValid && animator.isHuman)
            {
                leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }
        }
        public bool TryGetPosition(out Vector3 position)
        {
            if (sourceAnimator && sourceAnimator.isActiveAndEnabled && leftHand && rightHand && player)
            {
                position = (leftHand.position + rightHand.position) * 0.5f + player.forward * forwardOffset;
                if (Finite(position)) return true;
            }
            position = Vector3.zero;
            if (!warned) { warned = true; WarnMissingHands(); }
            return false; // M9 retains its authored FirePoint and all obstruction checks.
        }
        static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsInfinity(v.x)
            && !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z);
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        void WarnMissingHands() { Debug.LogWarning("M14.5: hand bones unavailable; using the existing M9 FirePoint.", this); }
    }
}
