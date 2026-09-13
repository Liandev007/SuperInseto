using System;
using UnityEngine;

namespace SuperInseto
{
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(Health))]
    public sealed class ChitinEnergy : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxEnergy = 100f;
        [SerializeField, Min(0f)] float startingEnergy = 100f;
        [SerializeField, Min(0f)] float regenerationRate = 15f;
        [SerializeField, Min(0f)] float regenerationDelay = 1.5f;
        [Tooltip("Runtime value. Initialized only in Awake; enabling the component does not refill it.")]
        [SerializeField] float currentEnergy;
        Health health;
        float delayRemaining;
        public float MaxEnergy => maxEnergy;
        public float CurrentEnergy => currentEnergy;
        public float DelayRemaining => delayRemaining;
        public event Action<float> Changed;
        public event Action<float> Spent;
        public event Action Depleted;
        public event Action Full;
        public event Action<float> Insufficient;

        void Awake()
        {
            health = GetComponent<Health>();
            ValidateValues(); currentEnergy = Mathf.Clamp(startingEnergy, 0f, maxEnergy);
        }
        void OnValidate() { ValidateValues(); }
        void ValidateValues()
        {
            maxEnergy = Finite(maxEnergy) ? Mathf.Max(1f, maxEnergy) : 100f;
            startingEnergy = Finite(startingEnergy) ? Mathf.Clamp(startingEnergy, 0f, maxEnergy) : maxEnergy;
            regenerationRate = Finite(regenerationRate) ? Mathf.Max(0f, regenerationRate) : 0f;
            regenerationDelay = Finite(regenerationDelay) ? Mathf.Max(0f, regenerationDelay) : 0f;
            currentEnergy = Finite(currentEnergy) ? Mathf.Clamp(currentEnergy, 0f, maxEnergy) : 0f;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public bool CanAfford(float cost) => Finite(cost) && cost >= 0f && currentEnergy >= cost;
        public bool TrySpend(float cost)
        {
            if (!isActiveAndEnabled || health.IsDead || !Finite(cost) || cost < 0f) return false;
            if (!CanAfford(cost)) { Insufficient?.Invoke(cost); return false; }
            if (cost == 0f) return true;
            delayRemaining = regenerationDelay;
            SetCurrentEnergy(currentEnergy - cost);
            Spent?.Invoke(cost);
            return true;
        }
        // Clean access for future persistence/tools; no save integration and no implicit refill/delay reset.
        public void SetCurrentEnergy(float value)
        {
            if (!Finite(value)) return;
            float previous = currentEnergy;
            currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
            if (previous == currentEnergy) return;
            Changed?.Invoke(currentEnergy);
            if (previous > 0f && currentEnergy == 0f) Depleted?.Invoke();
            if (previous < maxEnergy && currentEnergy == maxEnergy) Full?.Invoke();
        }
        void Update() { Advance(Time.deltaTime); }
        // Deterministic step also used by focused tests; never creates coroutines or regeneration jobs.
        public void Advance(float deltaTime)
        {
            if (!isActiveAndEnabled || health.IsDead || !Finite(deltaTime) || deltaTime <= 0f) return;
            float waiting = Mathf.Min(delayRemaining, deltaTime);
            delayRemaining -= waiting;
            float regenerationTime = deltaTime - waiting;
            if (regenerationTime > 0f && currentEnergy < maxEnergy && regenerationRate > 0f)
                SetCurrentEnergy(Mathf.Min(maxEnergy, currentEnergy + regenerationRate * regenerationTime));
        }
    }
}
