using UnityEngine;

namespace SuperInseto
{
    // Presentation only. No ability costs, cooldowns, hit damage or gameplay states live here.
    [CreateAssetMenu(menuName = "Super Inseto/Player Animation Preview")]
    public sealed class PlayerAnimationDefinition : ScriptableObject
    {
        public const string ResourcePath = "SuperInseto/PlayerAnimationPreview";
        public GameObject visualPrefab;
        public RuntimeAnimatorController controller;
        [HideInInspector] public string sourceFingerprint;

        [Header("Model (rebuild preview after changing height/orientation)")]
        [Min(0.5f)] public float modelHeight = 1.75f;
        public Vector3 modelEulerOffset;
        public Vector3 baseVisualOffset;
        [Header("Locomotion")]
        [Min(0.1f)] public float walkThreshold = 2f;
        [Min(0.1f)] public float runThreshold = 4.2f;
        [Range(1f, 1.3f)] public float sprintRate = 1.15f;
        [Min(0.1f)] public float climbReferenceSpeed = 2.2f;
        [Header("Crossfades (seconds)")]
        [Range(0f, 0.25f)] public float locomotionBlend = 0.1f;
        [Range(0f, 0.15f)] public float actionBlend = 0.06f;
        [Range(0f, 0.15f)] public float traversalBlend = 0.06f;
        [Range(0f, 0.2f)] public float recoveryBlend = 0.08f;
        [Header("Existing M3 action commit; no extra lock or cooldown")]
        [Range(0f, 1f)] public float lightMoveMultiplier = 0.35f;
        [Range(0f, 1f)] public float heavyMoveMultiplier = 0.1f;
        [Range(0.01f, 0.2f)] public float lightBrakeDuration = 0.08f;
        [Range(0.01f, 0.2f)] public float heavyBrakeDuration = 0.05f;
        [Header("Contact intervals in SOURCE clips (normalized); mapped to M3 windows")]
        public Vector2 light1Contact = new Vector2(10f / 26f, 17f / 26f);
        public Vector2 light2Contact = new Vector2(19f / 41f, 24f / 41f);
        public Vector2 light3Contact = new Vector2(19f / 37f, 23f / 37f);
        public Vector2 heavyContact = new Vector2(12f / 34f, 17f / 34f);
        [Header("Ability poses; gameplay windup reaches this pose before emitting")]
        [Range(0.01f, 0.99f)] public float impactPose = 12f / 51f;
        [Range(0.01f, 0.99f)] public float stingerPose = 12f / 41f;
        [Min(0f)] public float castForwardOffset = 0.12f;
        [Header("Air / lifecycle (presentation durations only)")]
        [Min(0.02f)] public float takeoffDuration = 0.16f;
        [Min(0.02f)] public float landingDuration = 0.22f;
        [Min(0.02f)] public float hitReactionDuration = 0.22f;
        [Min(0f)] public float hitQueueLifetime = 0.25f;
        [Min(0f)] public float deathHoldDuration = 0.3f;
        [Header("Mantle offsets in metres; only the visual child is moved")]
        public Vector3 mantleStartOffset = new Vector3(0f, -0.18f, -0.08f);
        public Vector3 mantleMiddleOffset = new Vector3(0f, -0.08f, -0.04f);
        public Vector3 mantleEndOffset = Vector3.zero;

        public bool IsUsable
        {
            get
            {
                if (!visualPrefab || !controller) return false;
                var animator = visualPrefab.GetComponentInChildren<Animator>(true);
                return animator && animator.avatar && animator.avatar.isValid && animator.avatar.isHuman;
            }
        }
    }
}
