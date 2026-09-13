using System;
using UnityEngine;
using UnityEngine.AI;

namespace SuperInseto
{
    [RequireComponent(typeof(Health))]
    public sealed class DestructibleObject : MonoBehaviour
    {
        [SerializeField] GameObject intactVisual;
        [SerializeField] GameObject brokenVisual;
        [SerializeField] bool disableColliderOnDestroy = true;
        [SerializeField] bool disableVisualOnDestroy = true;
        [SerializeField] bool isDestroyed;
        Health health;
        Collider[] colliders;
        NavMeshObstacle[] obstacles;
        bool started;
        public bool IsDestroyed => isDestroyed;
        public event Action<DamageInfo> Damaged;
        public event Action Destroyed;

        void Awake()
        {
            health = GetComponent<Health>();
            colliders = GetComponentsInChildren<Collider>(true);
            obstacles = GetComponentsInChildren<NavMeshObstacle>(true);
            if (brokenVisual && brokenVisual != gameObject) brokenVisual.SetActive(false);
        }
        void OnEnable()
        {
            health.Damaged += Hit; health.Died += Break;
            if (started && health.IsDead) Break();
        }
        void Start() { started = true; if (health.IsDead) Break(); }
        void OnDisable() { health.Damaged -= Hit; health.Died -= Break; }
        void Hit(DamageInfo damage) { if (!isDestroyed) Damaged?.Invoke(damage); }
        // Binary persistence only. Reuses Health/death and the existing collider/puzzle consequences.
        public void RestoreDestroyed()
        {
            if (!isDestroyed) health.TakeDamage(health.MaxHealth);
        }
        void Break()
        {
            if (isDestroyed) return;
            isDestroyed = true; // Latch before presentation/events; the Health receiver remains on the root.
            if (disableColliderOnDestroy)
            {
                foreach (var collider in colliders) if (collider) collider.enabled = false;
                foreach (var obstacle in obstacles) if (obstacle) obstacle.enabled = false;
            }
            if (disableVisualOnDestroy && intactVisual && intactVisual != gameObject) intactVisual.SetActive(false);
            if (brokenVisual && brokenVisual != gameObject) brokenVisual.SetActive(true);
            Destroyed?.Invoke();
        }
    }
}
