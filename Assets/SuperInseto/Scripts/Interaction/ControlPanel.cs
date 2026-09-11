using UnityEngine;
using UnityEngine.Events;

namespace SuperInseto
{
    public sealed class ControlPanel : Interactable
    {
        [SerializeField] UnityEvent onActivated = new UnityEvent();
        [SerializeField] string successMessage = "Acesso liberado.";
        [SerializeField] bool oneShot = true;
        bool activated;
        public override string Interact(AccessCredentials access)
        {
            if (activated && oneShot) return "Painel já acionado. Acesso liberado.";
            activated = true;
            onActivated.Invoke();
            return successMessage;
        }
    }
}
