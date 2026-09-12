using UnityEngine;

namespace SuperInseto
{
    [DefaultExecutionOrder(-125)] // Spawn after the existing ground bake; the drone does not join its sources.
    public sealed class TestGymDroneSpawner : MonoBehaviour
    {
        [SerializeField] DroneBrain dronePrefab;
        [SerializeField] Transform spawnPoint;
        [SerializeField] Transform[] waypoints;
        [SerializeField] Health player;
        [SerializeField] Vector3 flightCenter;
        [SerializeField] Vector3 flightSize = new Vector3(14f, 5f, 13f);
        [SerializeField] float floorHeight;
        DroneBrain spawned;
        void Start()
        {
            if (!dronePrefab || !spawnPoint || !player)
            { Debug.LogError("M6 requires drone prefab, spawn and player Health.", this); return; }
            spawned = Instantiate(dronePrefab, spawnPoint.position, spawnPoint.rotation, transform);
            spawned.Configure(player, waypoints);
            var flight = spawned.GetComponent<DroneFlight>();
            flight.ConfigureVolume(flightCenter, flightSize, floorHeight);
            if (!flight.TryPlace(spawnPoint.position))
            { Debug.LogError("M6 spawn intersects geometry or lies outside its flight volume.", this); spawned.gameObject.SetActive(false); }
        }
        void OnDestroy() { if (spawned) { spawned.GetComponent<DroneFlight>().Shutdown(); Destroy(spawned.gameObject); } }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan; Gizmos.DrawWireCube(flightCenter, flightSize);
            if (spawnPoint) Gizmos.DrawWireSphere(spawnPoint.position, 0.45f);
            if (waypoints == null) return;
            for (int i = 0; i < waypoints.Length; i++)
            {
                var p = waypoints[i]; var next = waypoints[(i + 1) % waypoints.Length];
                if (!p) continue;
                Gizmos.DrawWireSphere(p.position, 0.25f);
                if (next) Gizmos.DrawLine(p.position, next.position);
            }
        }
    }
}
