using UnityEngine;

namespace SuperInseto
{
    // Replaceable prototype presentation. Native Unity objects are created only in Awake.
    [RequireComponent(typeof(BioelectricStinger))]
    public sealed class BioelectricStingerFeedback : MonoBehaviour
    {
        BioelectricStinger ability;
        PlayerInputReader input;
        Health health;
        LineRenderer ring;
        Material material;
        readonly Vector3[] points = new Vector3[24];
        float flashedAt = float.NegativeInfinity, blockedAt = float.NegativeInfinity;
        void Awake()
        {
            ability = GetComponent<BioelectricStinger>(); input = GetComponent<PlayerInputReader>(); health = GetComponent<Health>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) { Debug.LogError("M9 prototype feedback requires URP Unlit.", this); enabled = false; return; }
            material = new Material(shader) { name = "Bioelectric stinger prototype (runtime)" };
            material.SetColor("_BaseColor", new Color(0.05f, 1f, 0.25f));
            var visual = new GameObject("Bioelectric preparation prototype");
            visual.transform.SetParent(transform, false); visual.layer = 2;
            ring = visual.AddComponent<LineRenderer>(); ring.sharedMaterial = material;
            ring.useWorldSpace = true; ring.loop = true; ring.positionCount = points.Length;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false; ring.enabled = false; ring.widthMultiplier = 0.035f;
        }
        void OnEnable() { ability.Fired += Flash; ability.Blocked += Block; }
        void OnDisable()
        {
            ability.Fired -= Flash; ability.Blocked -= Block;
            flashedAt = blockedAt = float.NegativeInfinity;
            if (ring) ring.enabled = false;
        }
        void Flash(BioelectricProjectile projectile) { flashedAt = Time.time; }
        void Block(Vector3 origin) { blockedAt = Time.time; flashedAt = Time.time; }
        void LateUpdate()
        {
            if (!ring) return;
            Transform socket = ability.FirePoint;
            bool flash = Time.time - flashedAt < 0.12f;
            ring.enabled = !health.IsDead && socket && (ability.WindingUp || flash);
            if (!ring.enabled) return;
            float radius = flash ? 0.24f : Mathf.Lerp(0.06f, 0.16f, ability.WindupProgress);
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / points.Length;
                points[i] = socket.position + (socket.right * Mathf.Cos(angle) + socket.up * Mathf.Sin(angle)) * radius;
            }
            ring.SetPositions(points);
        }
        void OnGUI()
        {
            if (!input.Captured || health.IsDead) return;
            string status = Time.time - blockedAt < 1f ? "Disparo bloqueado"
                : ability.WindingUp ? "Preparando" : ability.Active ? "Recuperando"
                : ability.CooldownRemaining > 0f ? "Recarga: " + ability.CooldownRemaining.ToString("0.0") + " s" : "Pronto";
            GUI.Box(new Rect(12f, Screen.height - 211f, 270f, 46f),
                input.StingerBindingDisplay + " — Ferrão Bioelétrico\n" + status);
            // Center reference only, without target acquisition or aim assist.
            GUI.Label(new Rect(Screen.width * 0.5f - 4f, Screen.height * 0.5f - 9f, 12f, 18f), "+");
        }
        void OnDestroy()
        {
            if (ring) Destroy(ring.gameObject);
            if (material) Destroy(material);
        }
    }
}
