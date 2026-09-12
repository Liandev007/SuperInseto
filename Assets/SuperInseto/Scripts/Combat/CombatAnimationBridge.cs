using UnityEngine;

namespace SuperInseto
{
    // Place beside the future Animator. Clip events forward to logic on the player root.
    public sealed class CombatAnimationBridge : MonoBehaviour
    {
        [SerializeField] PlayerCombat combat;
        public void OpenDamageWindow() { if (combat) combat.AnimationOpenDamageWindow(); }
        public void CloseDamageWindow() { if (combat) combat.AnimationCloseDamageWindow(); }
    }
}
