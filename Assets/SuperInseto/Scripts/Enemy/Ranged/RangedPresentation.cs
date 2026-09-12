using UnityEngine;

namespace SuperInseto
{
    [RequireComponent(typeof(RangedBrain), typeof(RangedWeapon), typeof(Health))]
    public sealed class RangedPresentation : MonoBehaviour
    {
        [SerializeField] Renderer visual;
        [SerializeField] Transform visualRoot;
        [SerializeField] Animator animator;
        RangedBrain brain;
        RangedWeapon weapon;
        EnemyNavigation navigation;
        Health health;
        MaterialPropertyBlock block;
        Quaternion restRotation;
        float hitUntil, fireUntil;
        Camera view;
        void Awake()
        {
            brain = GetComponent<RangedBrain>(); weapon = GetComponent<RangedWeapon>();
            navigation = GetComponent<EnemyNavigation>(); health = GetComponent<Health>();
            block = new MaterialPropertyBlock();
            if (visualRoot) restRotation = visualRoot.localRotation;
        }
        void OnEnable() { health.Damaged += Hit; brain.StateChanged += Changed; weapon.Fired += Fired; }
        void OnDisable() { health.Damaged -= Hit; brain.StateChanged -= Changed; weapon.Fired -= Fired; }
        void Start() { view = Camera.main; }
        void Hit(DamageInfo info) { hitUntil = Time.time + 0.15f; if (animator) animator.SetTrigger("Hit"); }
        void Fired() { fireUntil = Time.time + 0.12f; if (animator) animator.SetTrigger("Fire"); }
        void Changed(RangedState state) { if (animator) animator.SetInteger("State", (int)state); }
        void LateUpdate()
        {
            bool dead = brain.State == RangedState.Dead;
            if (animator) { animator.SetFloat("Speed", navigation.Speed); animator.SetBool("Aim", weapon.Aiming); }
            Color color = dead ? Color.gray : Time.time < hitUntil ? Color.white : weapon.Aiming ? Color.yellow
                : Time.time < fireUntil ? Color.red : new Color(0.6f, 0.22f, 0.7f);
            if (visual) { visual.GetPropertyBlock(block); block.SetColor("_BaseColor", color); visual.SetPropertyBlock(block); }
            if (visualRoot && !animator)
                visualRoot.localRotation = restRotation * Quaternion.Euler(dead ? 70f : Time.time < fireUntil ? -8f : 0f, 0f, 0f);
        }
        void OnGUI()
        {
            if (!view || Vector3.Distance(view.transform.position, transform.position) > 18f) return;
            Vector3 p = view.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
            if (p.z > 0f) GUI.Box(new Rect(p.x - 120f, Screen.height - p.y, 240f, 25f),
                "Atirador " + health.CurrentHealth.ToString("0") + " HP | " + brain.State);
        }
    }
}
