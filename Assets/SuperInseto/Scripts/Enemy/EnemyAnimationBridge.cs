using UnityEngine;

namespace SuperInseto
{
    // Put beside a future Animator and assign the logical root's EnemyAttack.
    public sealed class EnemyAnimationBridge : MonoBehaviour
    {
        [SerializeField] EnemyAttack attack;
        public void OpenDamageWindow() { if (attack) attack.OpenDamageWindow(); }
        public void CloseDamageWindow() { if (attack) attack.CloseDamageWindow(); }
    }
}
