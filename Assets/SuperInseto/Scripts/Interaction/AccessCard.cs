using UnityEngine;

namespace SuperInseto
{
    public sealed class AccessCard : Interactable
    {
        [SerializeField] string accessId = "SecurityLevel01";
        bool collected;
        public override bool Available => base.Available && !collected;
        public override string Interact(AccessCredentials access)
        {
            if (collected || !access) return "Cartão indisponível.";
            if (string.IsNullOrWhiteSpace(accessId)) return "Cartão sem identificação configurada.";
            access.Grant(accessId);
            collected = true;
            gameObject.SetActive(false);
            return "Cartão adquirido: " + accessId;
        }
    }
}
