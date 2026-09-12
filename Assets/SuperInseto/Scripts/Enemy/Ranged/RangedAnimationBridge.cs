using UnityEngine;

namespace SuperInseto
{
    // Attach beside a future Animator. Only the weapon controls eligibility and shot count.
    public sealed class RangedAnimationBridge : MonoBehaviour
    {
        [SerializeField] RangedWeapon weapon;
        public void Fire() { if (weapon) weapon.AnimationFire(); }
    }
}
