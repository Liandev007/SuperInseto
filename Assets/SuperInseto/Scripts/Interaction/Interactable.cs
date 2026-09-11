using UnityEngine;

namespace SuperInseto
{
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] string displayName = "Objeto";
        [SerializeField] Transform aimPoint;
        [SerializeField] Color tint = new Color(0.2f, 0.65f, 0.85f);
        public Vector3 AimPoint => aimPoint ? aimPoint.position : transform.position;
        public virtual string Prompt => "[E] " + displayName;
        public virtual bool Available => isActiveAndEnabled;
        public abstract string Interact(AccessCredentials access);

        protected virtual void Start()
        {
            // The graybox palette runs in Awake; retain its material and override only colour.
            var renderer = GetComponent<Renderer>();
            if (!renderer) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            renderer.SetPropertyBlock(block);
        }
    }
}
