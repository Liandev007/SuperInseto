using System;
using UnityEngine;

namespace SuperInseto
{
    public sealed class BioelectricReceiver : MonoBehaviour, IBioelectricReceiver
    {
        [SerializeField] Color offColor = new Color(0.12f, 0.16f, 0.18f);
        [SerializeField] Color onColor = new Color(0.05f, 1f, 0.7f);
        [SerializeField] bool powered;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        public bool IsPowered => powered;
        public event Action Powered;
        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            ShowState();
        }
        void ShowState()
        {
            block.SetColor("_BaseColor", powered ? onColor : offColor);
            foreach (var renderer in renderers) if (renderer) renderer.SetPropertyBlock(block);
        }
        public bool ReceiveBioelectricImpact()
        {
            if (!isActiveAndEnabled || powered) return false;
            powered = true; // Latch before invoking puzzle callbacks.
            ShowState(); Powered?.Invoke();
            return true;
        }
    }
}
