using UnityEngine;

namespace SuperInseto
{
    [RequireComponent(typeof(PlayerRespawn))]
    public sealed class RespawnFeedback : MonoBehaviour
    {
        PlayerRespawn respawn;
        float messageUntil;
        void Awake() { respawn = GetComponent<PlayerRespawn>(); }
        void OnEnable() { respawn.CheckpointActivated += Activated; }
        void OnDisable() { respawn.CheckpointActivated -= Activated; }
        void Activated(Checkpoint checkpoint) { messageUntil = Time.unscaledTime + 2.5f; }
        void OnGUI()
        {
            float width = Mathf.Min(400f, Screen.width - 24f);
            if (respawn.State != RespawnState.Alive)
                GUI.Box(new Rect((Screen.width - width) / 2f, Screen.height * 0.4f, width, 70f),
                    "DERROTADO\nRetornando ao checkpoint...");
            else if (Time.unscaledTime < messageUntil)
                GUI.Box(new Rect((Screen.width - width) / 2f, Screen.height * 0.25f, width, 40f), "CHECKPOINT ATIVADO");
        }
    }
}
