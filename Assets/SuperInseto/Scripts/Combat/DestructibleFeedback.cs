using System.Collections;
using UnityEngine;

namespace SuperInseto
{
    [RequireComponent(typeof(DestructibleObject))]
    public sealed class DestructibleFeedback : MonoBehaviour
    {
        [SerializeField, Min(0.02f)] float flashDuration = 0.12f;
        DestructibleObject destructible;
        Health health;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        Coroutine flash;
        void Awake()
        {
            destructible = GetComponent<DestructibleObject>(); health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock(); // Native state belongs in Awake, never a field initializer.
        }
        void OnEnable() { destructible.Damaged += Hit; destructible.Destroyed += Broken; }
        void OnDisable()
        {
            destructible.Damaged -= Hit; destructible.Destroyed -= Broken;
            StopFlash(); Clear();
        }
        void Hit(DamageInfo damage)
        {
            if (health.IsDead || destructible.IsDestroyed) return;
            StopFlash(); block.SetColor("_BaseColor", Color.white);
            foreach (var renderer in renderers) if (renderer) renderer.SetPropertyBlock(block);
            flash = StartCoroutine(Restore());
        }
        IEnumerator Restore()
        { yield return new WaitForSeconds(Mathf.Max(0.02f, flashDuration)); Clear(); flash = null; }
        void StopFlash() { if (flash != null) StopCoroutine(flash); flash = null; }
        void Clear()
        {
            block.Clear();
            foreach (var renderer in renderers) if (renderer) renderer.SetPropertyBlock(block);
        }
        void Broken()
        {
            StopFlash(); block.SetColor("_BaseColor", new Color(0.15f, 0.16f, 0.17f));
            foreach (var renderer in renderers) if (renderer) renderer.SetPropertyBlock(block);
        }
    }
}
