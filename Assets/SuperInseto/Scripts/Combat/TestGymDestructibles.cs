using UnityEngine;

namespace SuperInseto
{
    // Existing gym enemies also use prefab setup. Spawn once, before the navigation Start build collects Health roots.
    [DefaultExecutionOrder(-210)]
    public sealed class TestGymDestructibles : MonoBehaviour
    {
        [SerializeField] DestructibleObject cratePrefab;
        [SerializeField] DestructibleObject panelPrefab;
        [SerializeField] Transform[] cratePoints;
        [SerializeField] Transform panelPoint;
        [SerializeField] string[] crateSaveIds = { "m11_crate_a", "m11_crate_b" };
        [SerializeField] string panelSaveId = "m11_panel";
        void Awake()
        {
            if (!cratePrefab || !panelPrefab || !panelPoint || cratePoints == null || cratePoints.Length != 2)
            { Debug.LogError("M11 gym needs two crate points, a panel point and both prefabs.", this); return; }
            for (int i = 0; i < cratePoints.Length; i++)
            {
                var point = cratePoints[i];
                if (!point) { Debug.LogError("M11 crate point missing.", this); continue; }
                var crate = Instantiate(cratePrefab, point.position, point.rotation, transform);
                PersistentId.Assign(crate.gameObject, crateSaveIds != null && i < crateSaveIds.Length ? crateSaveIds[i] : "");
            }
            var panel = Instantiate(panelPrefab, panelPoint.position, panelPoint.rotation, transform);
            PersistentId.Assign(panel.gameObject, panelSaveId);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            if (cratePoints != null) foreach (var point in cratePoints)
                if (point) Gizmos.DrawWireCube(point.position + Vector3.up * 0.65f, new Vector3(1.4f, 1.3f, 1.4f));
            if (panelPoint) Gizmos.DrawWireCube(panelPoint.position + Vector3.up * 1.2f, new Vector3(1.4f, 1.8f, 0.18f));
        }
    }
}
