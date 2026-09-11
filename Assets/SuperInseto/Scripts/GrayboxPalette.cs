using UnityEngine;

namespace SuperInseto
{
    // Only prototype colour; no imported art or dependency on shader asset GUIDs.
    public sealed class GrayboxPalette : MonoBehaviour
    {
        [SerializeField] Color baseColor = new Color(0.32f, 0.38f, 0.43f);
        [SerializeField] Color climbableColor = new Color(0.9f, 0.48f, 0.12f);
        Material plain, climbable;
        void Awake()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) { Debug.LogError("URP Lit shader missing.", this); return; }
            plain = new Material(shader) { name = "Graybox (runtime)" };
            climbable = new Material(shader) { name = "Climbable (runtime)" };
            plain.SetColor("_BaseColor", baseColor);
            climbable.SetColor("_BaseColor", climbableColor);
            plain.SetFloat("_Smoothness", 0.1f);
            climbable.SetFloat("_Smoothness", 0.1f);
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = renderer.GetComponentInParent<ClimbableSurface>() ? climbable : plain;
        }
        void OnDestroy()
        {
            if (plain) Destroy(plain);
            if (climbable) Destroy(climbable);
        }
    }
}
