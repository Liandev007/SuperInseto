using UnityEngine;

namespace SuperInseto
{
    // Optional presentation adapter: replace VisualRoot with a rig, leaving AI and hit socket independent.
    [RequireComponent(typeof(EnemyBrain), typeof(EnemyAttack), typeof(Health))]
    public sealed class EnemyPresentation : MonoBehaviour
    {
        [SerializeField] Renderer visual;
        [SerializeField] Transform visualRoot;
        [SerializeField] Animator animator;
        EnemyBrain brain;
        EnemyAttack attack;
        EnemyNavigation navigation;
        Health health;
        MaterialPropertyBlock block;
        Quaternion restRotation;
        float flashUntil;
        Camera view;
        void Awake()
        {
            brain = GetComponent<EnemyBrain>(); attack = GetComponent<EnemyAttack>();
            navigation = GetComponent<EnemyNavigation>(); health = GetComponent<Health>();
            block = new MaterialPropertyBlock();
            if (visualRoot) restRotation = visualRoot.localRotation;
        }
        void OnEnable() { health.Damaged += Hit; brain.StateChanged += StateChanged; attack.Started += AttackStarted; }
        void OnDisable()
        { health.Damaged -= Hit; brain.StateChanged -= StateChanged; attack.Started -= AttackStarted; }
        void Start() { view = Camera.main; }
        void Hit(DamageInfo damage)
        { flashUntil = Time.time + 0.15f; if (animator) animator.SetTrigger("Hit"); }
        void StateChanged(EnemyState state)
        { if (animator) animator.SetInteger("State", (int)state); }
        void AttackStarted() { if (animator) animator.SetTrigger("Attack"); }
        void LateUpdate()
        {
            if (animator) animator.SetFloat("Speed", navigation.Speed);
            bool dead = brain.State == EnemyState.Dead;
            Color color = dead ? Color.gray : Time.time < flashUntil ? Color.white
                : attack.WindingUp ? Color.yellow : brain.State == EnemyState.Alert ? Color.yellow
                : brain.State == EnemyState.Chase || brain.State == EnemyState.Attack ? new Color(0.85f, 0.2f, 0.15f)
                : new Color(0.25f, 0.4f, 0.65f);
            if (visual)
            {
                visual.GetPropertyBlock(block); block.SetColor("_BaseColor", color); visual.SetPropertyBlock(block);
            }
            if (visualRoot && !animator)
                visualRoot.localRotation = restRotation * Quaternion.Euler(dead ? 70f : attack.Active ? -20f * Mathf.Sin(attack.Progress * Mathf.PI * 2f) : 0f, 0f, 0f);
        }
        void OnGUI()
        {
            if (!view || Vector3.Distance(view.transform.position, transform.position) > 14f) return;
            Vector3 p = view.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
            if (p.z > 0f) GUI.Box(new Rect(p.x - 105f, Screen.height - p.y, 210f, 25f),
                "Agente " + health.CurrentHealth.ToString("0") + " HP | " + brain.State);
        }
    }
}
