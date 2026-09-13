using UnityEngine;

namespace SuperInseto
{
    // One reusable prototype ring; no collider, mesh dependency, camera mutation or objects per activation.
    [RequireComponent(typeof(ChitinImpact))]
    public sealed class ChitinImpactFeedback : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] float pulseDuration = 0.3f;
        [SerializeField] Color preparationColor = new Color(0.6f, 0.8f, 0.2f);
        [SerializeField] Color impactColor = new Color(0.1f, 1f, 0.35f);
        ChitinImpact ability;
        PlayerInputReader input;
        Health health;
        LineRenderer ring;
        Material material;
        readonly Vector3[] points = new Vector3[48];
        Vector3 pulseOrigin;
        float pulseAt = float.NegativeInfinity, pulseRadius;
        int hits;

        void Awake()
        {
            ability = GetComponent<ChitinImpact>(); input = GetComponent<PlayerInputReader>(); health = GetComponent<Health>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) { Debug.LogError("M8 prototype ring requires URP Unlit.", this); enabled = false; return; }
            material = new Material(shader) { name = "Chitin impact prototype (runtime)" };
            var visual = new GameObject("Chitin impact prototype ring");
            visual.transform.SetParent(transform, false); visual.layer = 2;
            ring = visual.AddComponent<LineRenderer>(); ring.sharedMaterial = material;
            ring.useWorldSpace = true; ring.loop = true; ring.positionCount = points.Length;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false; ring.enabled = false;
        }
        void OnEnable() { ability.Impacted += Impact; ability.Finished += Finish; }
        void OnDisable()
        {
            ability.Impacted -= Impact; ability.Finished -= Finish;
            pulseAt = float.NegativeInfinity;
            if (ring) ring.enabled = false;
        }
        void Impact(Vector3 origin, float radius, int count)
        { pulseOrigin = origin; pulseRadius = radius; hits = count; pulseAt = Time.time; }
        void Finish() { if (ring && Time.time - pulseAt >= pulseDuration) ring.enabled = false; }
        void LateUpdate()
        {
            if (!ring) return;
            float phase = (Time.time - pulseAt) / Mathf.Max(0.05f, pulseDuration);
            bool pulse = phase >= 0f && phase < 1f;
            ring.enabled = !health.IsDead && (ability.WindingUp || pulse);
            if (!ring.enabled) return;
            Vector3 center = pulse ? pulseOrigin : ability.Origin;
            float radius = pulse ? Mathf.Lerp(0.15f, pulseRadius, phase) : Mathf.Lerp(0.15f, 0.6f, ability.WindupProgress);
            material.SetColor("_BaseColor", pulse ? impactColor : preparationColor);
            ring.widthMultiplier = pulse ? Mathf.Lerp(0.12f, 0.015f, phase) : 0.045f;
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / points.Length;
                points[i] = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            }
            ring.SetPositions(points);
        }
        void OnGUI()
        {
            if (!input.Captured || health.IsDead) return;
            string status = ability.WindingUp ? "Preparando" : ability.Active ? "Recuperando"
                : ability.CooldownRemaining > 0f ? "Recarga: " + ability.CooldownRemaining.ToString("0.0") + " s" : "Pronto";
            string hit = Time.time - pulseAt < 1.2f ? " | Alvos: " + hits : "";
            GUI.Box(new Rect(12f, Screen.height - 158f, 270f, 46f),
                input.ImpactBindingDisplay + " — Impacto Quitinoso\n" + status + hit);
        }
        void OnDestroy()
        {
            if (ring) Destroy(ring.gameObject);
            if (material) Destroy(material);
        }
    }
}
