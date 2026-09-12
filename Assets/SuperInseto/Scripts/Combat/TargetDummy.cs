using UnityEngine;

namespace SuperInseto
{
    [RequireComponent(typeof(Health))]
    public sealed class TargetDummy : MonoBehaviour
    {
        [SerializeField] Renderer visual;
        [SerializeField, Min(0.02f)] float flashDuration = 0.14f;
        Health health;
        Camera view;
        float flashUntil;
        Color restingColor;
        readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        void Awake() { health = GetComponent<Health>(); }
        void OnEnable() { health.Damaged += Hit; health.Died += Die; }
        void OnDisable() { health.Damaged -= Hit; health.Died -= Die; }
        void Start()
        {
            view = Camera.main;
            restingColor = new Color(0.65f, 0.3f, 0.18f);
        }
        void Hit(DamageInfo damage) { flashUntil = Time.time + flashDuration; }
        void Die() { gameObject.SetActive(false); }
        void Update()
        {
            if (!visual) return;
            visual.GetPropertyBlock(block);
            block.SetColor("_BaseColor", Time.time < flashUntil ? Color.white : restingColor);
            visual.SetPropertyBlock(block);
        }
        void OnGUI()
        {
            if (!view || Vector3.Distance(view.transform.position, transform.position) > 12f) return;
            Vector3 point = view.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
            if (point.z <= 0f) return;
            GUI.Box(new Rect(point.x - 75f, Screen.height - point.y, 150f, 24f),
                "Alvo " + health.CurrentHealth.ToString("0") + "/" + health.MaxHealth.ToString("0"));
        }
    }
}
