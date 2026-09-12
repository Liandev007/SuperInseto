using UnityEngine;

namespace SuperInseto
{
    // Replace only VisualRoot later. Health, state and weapon events are the future presentation hooks.
    [RequireComponent(typeof(DroneBrain), typeof(Health), typeof(RangedWeapon))]
    public sealed class DronePresentation : MonoBehaviour
    {
        [SerializeField] Renderer bodyVisual;
        [SerializeField] Transform visualRoot;
        DroneBrain brain;
        Health health;
        RangedWeapon weapon;
        MaterialPropertyBlock block;
        Quaternion restRotation;
        Camera view;
        float hitUntil, fireUntil;
        void Awake()
        {
            brain = GetComponent<DroneBrain>(); health = GetComponent<Health>(); weapon = GetComponent<RangedWeapon>();
            block = new MaterialPropertyBlock();
            if (visualRoot) restRotation = visualRoot.localRotation;
        }
        void OnEnable() { health.Damaged += Hit; weapon.Fired += Fired; }
        void OnDisable() { health.Damaged -= Hit; weapon.Fired -= Fired; }
        void Start() { view = Camera.main; }
        void Hit(DamageInfo damage) { hitUntil = Time.time + 0.15f; }
        void Fired() { fireUntil = Time.time + 0.12f; }
        void LateUpdate()
        {
            bool dead = brain.State == DroneState.Dead;
            Color color = dead ? Color.gray : Time.time < hitUntil ? Color.white
                : weapon.Aiming || brain.State == DroneState.Alert ? Color.yellow
                : Time.time < fireUntil ? Color.red : new Color(0.1f, 0.75f, 0.65f);
            if (bodyVisual)
            { bodyVisual.GetPropertyBlock(block); block.SetColor("_BaseColor", color); bodyVisual.SetPropertyBlock(block); }
            if (visualRoot) visualRoot.localRotation = restRotation * Quaternion.Euler(dead ? 25f : 0f, 0f, dead ? 30f : 0f);
        }
        void OnGUI()
        {
            if (!view || Vector3.Distance(view.transform.position, transform.position) > 16f) return;
            Vector3 p = view.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
            if (p.z > 0f) GUI.Box(new Rect(p.x - 110f, Screen.height - p.y, 220f, 24f),
                "Drone " + health.CurrentHealth.ToString("0") + " HP | " + brain.State);
        }
    }
}
