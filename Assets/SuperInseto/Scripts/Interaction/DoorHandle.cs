using UnityEngine;

namespace SuperInseto
{
    // A second stationary handle on the other side uses the same door state and access check.
    public sealed class DoorHandle : Interactable
    {
        [SerializeField] SlidingDoor door;
        public override bool Available => base.Available && door && door.Available;
        public override string Prompt => door ? door.Prompt : base.Prompt;
        public override string Interact(AccessCredentials access) => door ? door.Interact(access) : "Porta indisponível.";
    }
}
