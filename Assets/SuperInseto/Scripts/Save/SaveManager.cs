using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SuperInseto
{
    // All gym spawners run in Awake; doors, puzzles and M13 finish Start before startup restoration.
    [DefaultExecutionOrder(500)]
    public sealed class SaveManager : MonoBehaviour
    {
        public const string FileName = "superinseto_save.json";
        const string AccessCardId = "SecurityLevel01";
        [SerializeField] PlayerRespawn player;
        [Tooltip("Optional scene subtree; empty scans this scene once. Useful for isolated test fixtures.")]
        [SerializeField] Transform scope;
        [SerializeField, Min(0f)] float writeDelay = 0.15f;
        readonly Dictionary<string, Component> registry = new Dictionary<string, Component>(StringComparer.Ordinal);
        readonly Dictionary<string, DestructibleObject> destructibles = new Dictionary<string, DestructibleObject>(StringComparer.Ordinal);
        readonly Dictionary<string, BioelectricReceiver> receivers = new Dictionary<string, BioelectricReceiver>(StringComparer.Ordinal);
        AccessCredentials credentials;
        SaveFileStore store;
        bool ready, subscribed, dirty;
        float saveAt, messageUntil;
        string message;
        public string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public bool Ready => ready;
        public event Action Saved;
        public event Action Loaded;

        void OnEnable() { if (ready) Subscribe(); }
        void Start()
        {
            if (!player || !(credentials = player.GetComponent<AccessCredentials>()))
            { Debug.LogWarning("M14: PlayerRespawn/AccessCredentials não configurados; persistência desabilitada.", this); return; }
            if (!BuildRegistry())
            { Debug.LogWarning("M14: corrija os IDs; nenhum save será lido ou sobrescrito nesta sessão.", this); return; }
            if (store == null) store = new SaveFileStore(SavePath);
            SaveReadResult result = store.Read(out var data, out var error);
            if (result == SaveReadResult.Loaded)
            {
                ApplyWorld(data);
                var checkpoint = registry.TryGetValue(data.checkpointId, out var entry) ? entry as Checkpoint : null;
                if (!checkpoint) Debug.LogWarning("M14: checkpoint salvo ausente; usando checkpoint inicial.", this);
                player.RestoreCheckpointForLoad(checkpoint);
                Show("PROGRESSO CARREGADO"); Loaded?.Invoke();
            }
            else if (result == SaveReadResult.Invalid)
                Debug.LogWarning("M14: " + error + " Iniciando com os padrões da cena.", this);
            // Initial checkpoint and restoration events must never overwrite the file before reading it.
            ready = true; Subscribe();
        }
        T[] Collect<T>() where T : Component => scope ? scope.GetComponentsInChildren<T>(true)
            : FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool BuildRegistry()
        {
            bool valid = true;
            foreach (var checkpoint in Collect<Checkpoint>())
            {
                if (checkpoint.gameObject.scene != gameObject.scene) continue;
                valid &= Register(checkpoint.PersistentId, checkpoint);
            }
            foreach (var identity in Collect<PersistentId>())
            {
                if (identity.gameObject.scene != gameObject.scene) continue;
                var destructible = identity.GetComponent<DestructibleObject>();
                var receiver = identity.GetComponent<BioelectricReceiver>();
                if ((!destructible && !receiver) || (destructible && receiver))
                { Debug.LogWarning("M14: PersistentId precisa de exatamente um DestructibleObject ou BioelectricReceiver.", identity); valid = false; continue; }
                if (!Register(identity.Id, identity)) { valid = false; continue; }
                if (destructible) destructibles.Add(identity.Id, destructible);
                else receivers.Add(identity.Id, receiver);
            }
            return valid;
        }
        bool Register(string id, Component component)
        {
            if (!SaveGameData.ValidId(id) || registry.ContainsKey(id))
            { Debug.LogWarning("M14: ID persistível vazio, inválido ou duplicado: '" + id + "'.", component); return false; }
            registry.Add(id, component); return true;
        }
        void ApplyWorld(SaveGameData data)
        {
            foreach (string id in data.destroyedObjectIds)
                if (destructibles.TryGetValue(id, out var item) && item) item.RestoreDestroyed();
            foreach (string id in data.poweredReceiverIds)
                if (receivers.TryGetValue(id, out var node) && node) node.ReceiveBioelectricImpact();
            // Existing condition callbacks derive barrier, puzzle progress and SlidingDoor state.
            if (data.hasAccessCard) credentials.Grant(AccessCardId);
            foreach (var card in Collect<AccessCard>())
                if (card.gameObject.scene == gameObject.scene) card.RestoreCollected(credentials);
        }
        void Subscribe()
        {
            if (subscribed) return;
            subscribed = true;
            player.CheckpointActivated += CheckpointChanged;
            credentials.Granted += CredentialChanged;
            foreach (var item in destructibles.Values) if (item) item.Destroyed += RequestSave;
            foreach (var node in receivers.Values) if (node) node.Powered += RequestSave;
        }
        void OnDisable()
        {
            if (!subscribed) return;
            subscribed = false;
            if (player) player.CheckpointActivated -= CheckpointChanged;
            if (credentials) credentials.Granted -= CredentialChanged;
            foreach (var item in destructibles.Values) if (item) item.Destroyed -= RequestSave;
            foreach (var node in receivers.Values) if (node) node.Powered -= RequestSave;
        }
        void CheckpointChanged(Checkpoint checkpoint) { RequestSave(); }
        void CredentialChanged(string id) { if (id == AccessCardId) RequestSave(); }
        void RequestSave()
        {
            if (!ready) return;
            if (!dirty) saveAt = Time.unscaledTime + Mathf.Max(0f, writeDelay);
            dirty = true;
        }
        void LateUpdate() { if (dirty && Time.unscaledTime >= saveAt) Flush(); }
        void OnApplicationPause(bool paused) { if (paused) Flush(); }
        void OnApplicationQuit() { Flush(); } // Unity also calls this when leaving Play Mode, before scene teardown.
        void Flush()
        {
            if (!ready || !dirty) return;
            dirty = false;
            var checkpoint = player ? player.CurrentCheckpoint : null;
            if (!checkpoint || !registry.TryGetValue(checkpoint.PersistentId, out var entry) || entry != checkpoint)
            { Debug.LogWarning("M14: checkpoint atual sem ID registrado; arquivo anterior preservado.", this); return; }
            var destroyed = new List<string>();
            foreach (var pair in destructibles) if (pair.Value && pair.Value.IsDestroyed) destroyed.Add(pair.Key);
            var powered = new List<string>();
            foreach (var pair in receivers) if (pair.Value && pair.Value.IsPowered) powered.Add(pair.Key);
            destroyed.Sort(StringComparer.Ordinal); powered.Sort(StringComparer.Ordinal);
            var data = new SaveGameData { saveVersion = 1, checkpointId = checkpoint.PersistentId,
                hasAccessCard = credentials.Has(AccessCardId), destroyedObjectIds = destroyed.ToArray(), poweredReceiverIds = powered.ToArray() };
            if (!store.Write(data, out var error))
            { Debug.LogWarning("M14: não foi possível salvar: " + error, this); Show("FALHA AO SALVAR"); return; }
            Show("PROGRESSO SALVO"); Saved?.Invoke();
        }
        [ContextMenu("M14/Clear Save (disk only)")]
        public void ClearSave()
        {
            if (store == null) store = new SaveFileStore(SavePath);
            if (!store.Clear(out var error)) { Debug.LogWarning("M14: " + error, this); return; }
            dirty = false;
            Show("SAVE APAGADO — reinicie o Play");
            // World is intentionally untouched. The next new progress event may create a new save.
        }
        void Show(string text) { message = text; messageUntil = Time.unscaledTime + 2.5f; }
        void OnGUI()
        {
            if (Time.unscaledTime >= messageUntil || string.IsNullOrEmpty(message)) return;
            float width = Mathf.Min(400f, Screen.width - 24f);
            GUI.Box(new Rect((Screen.width - width) / 2f, Screen.height * 0.18f, width, 36f), message);
        }
    }
}
