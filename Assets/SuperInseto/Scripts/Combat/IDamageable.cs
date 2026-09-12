using UnityEngine;

namespace SuperInseto
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly GameObject Source;
        public readonly Vector3 Point;
        public DamageInfo(float amount, GameObject source, Vector3 point)
        { Amount = amount; Source = source; Point = point; }
    }

    public interface IDamageable
    {
        bool TakeDamage(DamageInfo damage);
    }
}
