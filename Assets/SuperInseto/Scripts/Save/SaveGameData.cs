using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperInseto
{
    [Serializable]
    public sealed class SaveGameData
    {
        // No field defaults: missing essential JSON fields must not silently become a valid save.
        public int saveVersion;
        public string checkpointId;
        public bool hasAccessCard;
        public string[] destroyedObjectIds;
        public string[] poweredReceiverIds;

        public bool IsValid()
        {
            if (saveVersion != 1 || !ValidId(checkpointId) || destroyedObjectIds == null
                || poweredReceiverIds == null || destroyedObjectIds.Length > 4096 || poweredReceiverIds.Length > 4096)
                return false;
            var ids = new HashSet<string>(StringComparer.Ordinal) { checkpointId };
            foreach (string id in destroyedObjectIds) if (!ValidId(id) || !ids.Add(id)) return false;
            foreach (string id in poweredReceiverIds) if (!ValidId(id) || !ids.Add(id)) return false;
            return true;
        }
        public static bool ValidId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || id != id.Trim()) return false;
            foreach (char c in id) if (char.IsControl(c)) return false;
            return true;
        }
        public static bool TryParse(string json, out SaveGameData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json) || json.Length > 1024 * 1024) return false;
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal)) return false;
            try
            {
                var parsed = JsonUtility.FromJson<SaveGameData>(trimmed);
                if (parsed == null || !parsed.IsValid()) return false;
                data = parsed; return true;
            }
            catch (ArgumentException) { return false; }
        }
    }
}
