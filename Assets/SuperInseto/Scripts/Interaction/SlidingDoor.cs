using UnityEngine;

namespace SuperInseto
{
    public enum DoorMode { Manual, Remote, AccessCard }

    // The interactable is a stationary jamb panel. Only the referenced leaf moves.
    public sealed class SlidingDoor : Interactable
    {
        [SerializeField] Transform leaf;
        [SerializeField] BoxCollider leafCollider;
        [SerializeField] DoorMode mode;
        [SerializeField] string requiredAccess = "SecurityLevel01";
        [SerializeField] Vector3 openOffset = new Vector3(0f, 3.15f, 0f);
        [SerializeField, Min(0.1f)] float speed = 2.5f;
        [SerializeField] LayerMask safetyMask = 4; // Player (Ignore Raycast layer).
        readonly Collider[] occupants = new Collider[16];
        Vector3 closedPosition;
        bool ready, wantsOpen;
        public bool WantsOpen => wantsOpen;
        public bool IsOpen => ready && Vector3.Distance(leaf.localPosition, closedPosition + openOffset) < 0.01f;
        public bool SafetyBlocked { get; private set; }
        public override bool Available => base.Available && ready;
        public override string Prompt => base.Prompt + (mode == DoorMode.Remote ? " — controlada por painel" : wantsOpen ? " — fechar" : " — abrir");

        void Awake()
        {
            if (!leaf || !leafCollider)
            { Debug.LogError("SlidingDoor requires a leaf and its BoxCollider.", this); return; }
            closedPosition = leaf.localPosition;
            ready = true;
        }

        public override string Interact(AccessCredentials access)
        {
            if (!ready) return "Porta indisponível.";
            if (mode == DoorMode.Remote) return "Porta bloqueada. Use o botão ou terminal próximo.";
            if (mode == DoorMode.AccessCard && (!access || !access.Has(requiredAccess)))
                return "Porta bloqueada — cartão necessário: " + requiredAccess;
            wantsOpen = !wantsOpen;
            return wantsOpen ? "Acesso liberado. Abrindo porta." : "Fechando porta; a passagem deve estar livre.";
        }

        public void OpenFromControl()
        {
            // Signals cannot bypass an access-card door.
            if (ready && mode == DoorMode.Remote) wantsOpen = true;
        }

        void Update()
        {
            if (!ready) return;
            Vector3 destination = closedPosition + (wantsOpen ? openOffset : Vector3.zero);
            Vector3 next = Vector3.MoveTowards(leaf.localPosition, destination, speed * Time.deltaTime);
            Vector3 worldNext = leaf.parent ? leaf.parent.TransformPoint(next) : next;
            Vector3 delta = worldNext - leaf.position;
            SafetyBlocked = false;
            if (!wantsOpen && delta.sqrMagnitude > 0f)
            {
                Bounds bounds = leafCollider.bounds;
                Vector3 extra = new Vector3(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z)) * 0.5f;
                int count = Physics.OverlapBoxNonAlloc(bounds.center + delta * 0.5f,
                    bounds.extents + extra + Vector3.one * 0.025f, occupants, Quaternion.identity,
                    safetyMask, QueryTriggerInteraction.Ignore);
                if (count > 0) { SafetyBlocked = true; return; }
            }
            leaf.localPosition = next;
        }
    }
}
