using UnityEngine;

namespace SuperInseto
{
    // Set on a scene instance, never randomly generated at runtime or shared on a reusable prefab.
    public sealed class PersistentId : MonoBehaviour
    {
        [SerializeField] string id;
        public string Id => id;
        public static void Assign(GameObject instance, string stableId)
        {
            var identity = instance.GetComponent<PersistentId>();
            if (!identity) identity = instance.AddComponent<PersistentId>();
            identity.id = stableId;
        }
    }
}
