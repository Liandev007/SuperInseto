using UnityEngine;

namespace SuperInseto
{
    // A presentation selection, never a second gameplay state machine.
    public enum PlayerVisualState
    {
        Locomotion, Takeoff, Floating, Landing, Climb, Mantle, Light1, Light2, Light3,
        HeavyRight, HeavyLeft, Dodge, HitReaction, Death, ChitinImpact, BioelectricStinger
    }

    public static class PlayerAnimationState
    {
        public static readonly string[] Names = {
            "Locomotion", "Takeoff", "Floating", "Landing", "Climb", "Mantle", "Light1", "Light2", "Light3",
            "HeavyRight", "HeavyLeft", "Dodge", "HitReaction", "Death", "ChitinImpact", "BioelectricStinger"
        };
        public static readonly int[] Hashes = new int[Names.Length];
        public static readonly int[] TimeHashes = new int[Names.Length];
        public static readonly string[] TimeNames = new string[Names.Length];
        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int LocomotionRate = Animator.StringToHash("LocomotionRate");
        public static readonly int ClimbRate = Animator.StringToHash("ClimbRate");
        public static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        public static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        public static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
        public static readonly int IsClimbing = Animator.StringToHash("IsClimbing");
        public static readonly int IsMantling = Animator.StringToHash("IsMantling");
        public static readonly int IsDodging = Animator.StringToHash("IsDodging");
        public static readonly int IsDead = Animator.StringToHash("IsDead");
        public static readonly int AttackIndex = Animator.StringToHash("AttackIndex");

        static PlayerAnimationState()
        {
            for (int i = 0; i < Names.Length; i++)
            {
                Hashes[i] = Animator.StringToHash("Base Layer." + Names[i]);
                TimeNames[i] = Names[i] + "Time";
                TimeHashes[i] = Animator.StringToHash(TimeNames[i]);
            }
        }
        public static bool HasMotionTime(PlayerVisualState state) => state != PlayerVisualState.Locomotion
            && state != PlayerVisualState.Floating && state != PlayerVisualState.Climb;

        // Preserve M3's exact windup/contact/recovery clocks while sampling the corresponding clip poses.
        // Each state has its own time parameter so a combo crossfade does not rewind the outgoing pose.
        public static float ContactTime(float elapsed, float duration, Vector2 window, Vector2 poses)
        {
            duration = Mathf.Max(0.001f, duration);
            float open = Mathf.Clamp(window.x, 0.001f, duration);
            float close = Mathf.Clamp(window.y, open, duration);
            float first = Mathf.Clamp01(poses.x), last = Mathf.Clamp(poses.y, first, 1f);
            if (elapsed < open) return Mathf.Lerp(0f, first, Mathf.Clamp01(elapsed / open));
            if (elapsed < close) return Mathf.Lerp(first, last, Mathf.InverseLerp(open, close, elapsed));
            return Mathf.Lerp(last, 1f, Mathf.InverseLerp(close, duration, elapsed));
        }
        public static Vector3 MantleOffset(float progress, Vector3 start, Vector3 middle, Vector3 end)
        {
            float t = Mathf.Clamp01(progress);
            return t < 0.55f ? Vector3.Lerp(start, middle, Mathf.SmoothStep(0f, 1f, t / 0.55f))
                : Vector3.Lerp(middle, end, Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.45f));
        }
    }
}
