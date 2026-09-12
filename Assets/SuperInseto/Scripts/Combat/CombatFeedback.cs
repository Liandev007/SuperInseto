using UnityEngine;

namespace SuperInseto
{
    // Disposable presentation: combat never reads the placeholder or its motion.
    [RequireComponent(typeof(PlayerCombat), typeof(Health), typeof(MeleeHitbox))]
    public sealed class CombatFeedback : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        PlayerCombat combat;
        Health health;
        MeleeHitbox hitbox;
        Quaternion restingRotation;
        float hitUntil, hitDamage;
        void Awake()
        {
            combat = GetComponent<PlayerCombat>(); health = GetComponent<Health>(); hitbox = GetComponent<MeleeHitbox>();
            if (visualRoot) restingRotation = visualRoot.localRotation;
        }
        void OnEnable() { hitbox.Hit += Hit; }
        void OnDisable()
        {
            hitbox.Hit -= Hit;
            if (visualRoot) visualRoot.localRotation = restingRotation;
        }
        void Hit(float amount) { hitDamage = amount; hitUntil = Time.unscaledTime + 0.8f; }
        void LateUpdate()
        {
            if (!visualRoot) return;
            float pulse = Mathf.Sin(combat.ActionProgress * Mathf.PI);
            Vector3 angles = Vector3.zero;
            if (combat.Action == CombatAction.Light) angles.y = pulse * (combat.ComboStep % 2 == 0 ? -25f : 25f);
            else if (combat.Action == CombatAction.Heavy) angles.x = pulse * 30f;
            else if (combat.Action == CombatAction.Dodge) angles.x = pulse * 18f;
            visualRoot.localRotation = restingRotation * Quaternion.Euler(angles);
        }
        void OnGUI()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;
            string action = combat.Action == CombatAction.Light ? "Leve " + combat.ComboStep
                : combat.Action == CombatAction.Heavy ? "Pesado" : combat.Action == CombatAction.Dodge ? "Esquiva"
                : combat.Action == CombatAction.Dead ? "Sem vida" : "Pronto";
            string hit = Time.unscaledTime < hitUntil ? " | Acerto: " + hitDamage : "";
            GUI.Box(new Rect(12f, Screen.height - 105f, 270f, 92f),
                "M3 — Vida " + health.CurrentHealth.ToString("0") + "/" + health.MaxHealth.ToString("0")
                + "\n" + action + hit + "\nMouse E: leve | Mouse D: pesado\nQ: esquiva");
        }
    }
}
