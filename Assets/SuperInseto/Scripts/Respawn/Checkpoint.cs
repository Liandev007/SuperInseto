using UnityEngine;

namespace SuperInseto
{
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] string persistentId;
        public string PersistentId => persistentId;
        [SerializeField, Min(0)] int progressIndex;
        [SerializeField] Transform spawnPoint;
        [SerializeField] Renderer marker;
        [SerializeField] Color inactiveColor = new Color(0.3f, 0.35f, 0.4f);
        [SerializeField] Color activeColor = new Color(0.05f, 1f, 0.7f);
        [SerializeField] bool active;
        MaterialPropertyBlock properties;
        public int ProgressIndex => progressIndex;
        public bool IsActive => active;
        public Vector3 Position => spawnPoint ? spawnPoint.position : transform.position;
        public Quaternion Rotation => spawnPoint ? spawnPoint.rotation : transform.rotation;

        void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            var body = GetComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            properties = new MaterialPropertyBlock();
            SetActiveCheckpoint(false);
        }
        public void SetActiveCheckpoint(bool value)
        {
            active = value;
            if (!marker || properties == null) return;
            marker.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", active ? activeColor : inactiveColor);
            marker.SetPropertyBlock(properties);
        }
        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerRespawn>();
            if (player) player.TryActivateCheckpoint(this);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Position + Vector3.up, 0.4f);
            Gizmos.DrawRay(Position + Vector3.up, Rotation * Vector3.forward * 2f);
        }
    }
}
