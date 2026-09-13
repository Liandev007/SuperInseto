using System;
using UnityEngine;

namespace SuperInseto
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] float maxHealth = 100f;
        [SerializeField] float currentHealth;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;
        public bool Invulnerable { get; set; }
        public event Action<DamageInfo> Damaged;
        public event Action Died;
        public event Action<float> Changed;
        void Awake() { maxHealth = Mathf.Max(1f, maxHealth); currentHealth = maxHealth; }

        public bool TakeDamage(float amount) => TakeDamage(new DamageInfo(amount, null, transform.position));
        public bool TakeDamage(DamageInfo damage)
        {
            if (!isActiveAndEnabled || IsDead || Invulnerable || !PositiveFinite(damage.Amount)) return false;
            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);
            bool diedNow = IsDead;
            Damaged?.Invoke(damage);
            Changed?.Invoke(currentHealth);
            if (diedNow) Died?.Invoke();
            return true;
        }

        // Explicit lifecycle operation; ordinary Heal still cannot revive dead actors.
        public void RestoreForRespawn()
        {
            Invulnerable = false;
            currentHealth = maxHealth;
            Changed?.Invoke(currentHealth);
        }

        public void Heal(float amount)
        {
            if (IsDead || !PositiveFinite(amount)) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Changed?.Invoke(currentHealth);
        }
        static bool PositiveFinite(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
