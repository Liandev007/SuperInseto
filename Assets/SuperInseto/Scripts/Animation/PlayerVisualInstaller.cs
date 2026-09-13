using UnityEngine;

namespace SuperInseto
{
    // Runs after the approved components initialize, before M14 startup load (order 500).
    [DefaultExecutionOrder(75)]
    [DisallowMultipleComponent]
    public sealed class PlayerVisualInstaller : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [Tooltip("Optional override. Otherwise use the preview built from the supplied FBXs in Resources.")]
        [SerializeField] PlayerAnimationDefinition definition;
        Renderer[] oldRenderers;
        bool[] oldRendererStates;
        GameObject instance;
        PlayerAnimationBridge bridge;
        BioelectricCastOrigin castOrigin;
        PlayerCombat combat;
        BioelectricStinger stinger;
        CombatFeedback feedback;
        bool started, installed;

        void Start() { started = true; TryInstall(); }
        void OnEnable()
        {
            if (!started) return;
            if (installed) EnableVisual(); else TryInstall();
        }
        void TryInstall()
        {
            if (!definition) definition = Resources.Load<PlayerAnimationDefinition>(PlayerAnimationDefinition.ResourcePath);
            if (!visualRoot || !definition || !definition.IsUsable)
            {
                Warn("M14.5: preview unavailable. Gameplay keeps its placeholder. In the Editor use Super Inseto/M14.5/Build or Rebuild Player Preview.");
                return;
            }
            combat = GetComponent<PlayerCombat>(); stinger = GetComponent<BioelectricStinger>();
            feedback = GetComponent<CombatFeedback>();
            if (!combat || !GetComponent<PlayerMotor>() || !GetComponent<Health>()) return;
            oldRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            oldRendererStates = new bool[oldRenderers.Length];
            for (int i = 0; i < oldRenderers.Length; i++) oldRendererStates[i] = oldRenderers[i].enabled;
            instance = Instantiate(definition.visualPrefab, visualRoot, false);
            instance.name = "PlayerVisualRoot";
            instance.SetActive(false);
            var animator = instance.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = definition.controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            castOrigin = animator.gameObject.AddComponent<BioelectricCastOrigin>();
            castOrigin.Initialize(animator, transform, definition.castForwardOffset);
            bridge = animator.gameObject.AddComponent<PlayerAnimationBridge>();
            if (!bridge.Initialize(gameObject, instance.transform, animator, definition))
            {
                Destroy(instance); instance = null;
                Warn("M14.5: visual contract invalid; original gameplay and placeholder retained.");
                return;
            }
            installed = true;
            EnableVisual();
        }
        void EnableVisual()
        {
            instance.SetActive(true);
            for (int i = 0; i < oldRenderers.Length; i++) if (oldRenderers[i]) oldRenderers[i].enabled = false;
            if (feedback) feedback.SetAnimatedVisual(true);
            combat.ConfigurePresentationCommit(true, definition.lightMoveMultiplier, definition.heavyMoveMultiplier,
                definition.lightBrakeDuration, definition.heavyBrakeDuration);
            if (stinger) stinger.SetAnimatedCastOrigin(castOrigin);
        }
        void OnDisable()
        {
            if (!installed) return;
            if (instance) instance.SetActive(false);
            for (int i = 0; i < oldRenderers.Length; i++) if (oldRenderers[i]) oldRenderers[i].enabled = oldRendererStates[i];
            if (feedback) feedback.SetAnimatedVisual(false);
            if (combat) combat.ConfigurePresentationCommit(false);
            if (stinger) stinger.SetAnimatedCastOrigin(null);
        }
        void OnDestroy() { if (instance) Destroy(instance); }
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        void Warn(string message) { Debug.LogWarning(message, this); }
    }
}
