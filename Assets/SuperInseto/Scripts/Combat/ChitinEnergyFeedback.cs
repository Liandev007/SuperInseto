using UnityEngine;

namespace SuperInseto
{
    [RequireComponent(typeof(ChitinEnergy))]
    public sealed class ChitinEnergyFeedback : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float insufficientDisplayTime = 1f;
        ChitinEnergy energy;
        float deniedAt = float.NegativeInfinity;
        float required;
        void Awake() { energy = GetComponent<ChitinEnergy>(); }
        void OnEnable() { energy.Insufficient += Denied; }
        void OnDisable() { energy.Insufficient -= Denied; deniedAt = float.NegativeInfinity; }
        void Denied(float cost) { required = cost; deniedAt = Time.time; }
        void OnGUI()
        {
            // One prototype box; reads the actual value, including runtime Inspector adjustments.
            bool denied = Time.time - deniedAt < insufficientDisplayTime;
            string status = "ENERGIA " + energy.CurrentEnergy.ToString("0.0") + "/" + energy.MaxEnergy.ToString("0");
            if (denied) status += "\nEnergia insuficiente — custo " + required.ToString("0");
            GUI.Box(new Rect(12f, Screen.height - 264f, 270f, 46f), status);
        }
    }
}
