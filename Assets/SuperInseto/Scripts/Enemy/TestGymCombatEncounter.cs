using UnityEngine;
using UnityEngine.AI;

namespace SuperInseto
{
    // Scene composition only: one instance per role, using the existing brains and shared gym bake.
    // No respawning, activation trigger or coordination between enemies.
    [DefaultExecutionOrder(-100)]
    public sealed class TestGymCombatEncounter : MonoBehaviour
    {
        [SerializeField] Health player;
        [Header("Melee")]
        [SerializeField] EnemyBrain meleePrefab;
        [SerializeField] Transform meleeSpawn;
        [SerializeField] Transform[] meleePatrol;
        [SerializeField, Range(0, 99)] int meleeAvoidancePriority = 40;
        [Header("Ranged")]
        [SerializeField] RangedBrain rangedPrefab;
        [SerializeField] Transform rangedSpawn;
        [SerializeField] Transform[] rangedPatrol;
        [SerializeField, Range(0, 99)] int rangedAvoidancePriority = 60;
        [Header("Drone")]
        [SerializeField] DroneBrain dronePrefab;
        [SerializeField] Transform droneSpawn;
        [SerializeField] Transform[] droneWaypoints;
        [SerializeField] Vector3 flightCenter = new Vector3(0f, 3f, 24f);
        [SerializeField] Vector3 flightSize = new Vector3(23f, 6f, 17f);
        [SerializeField] float floorHeight;
        [SerializeField, Min(0f)] float droneAvoidancePadding = 0.05f;
        [Header("Prototype route to the arena")]
        [SerializeField] Renderer[] routeMarkers;
        EnemyBrain melee;
        RangedBrain ranged;
        DroneBrain drone;
        NavMeshObstacle droneAvoidance;
        bool initialized;

        void Start()
        {
            if (initialized) return;
            initialized = true;
            if (!player || !meleePrefab || !rangedPrefab || !dronePrefab
                || !meleeSpawn || !rangedSpawn || !droneSpawn)
            { Debug.LogError("M7 requires the three existing prefabs, their spawns and player Health.", this); return; }

            melee = Instantiate(meleePrefab, meleeSpawn.position, meleeSpawn.rotation, transform);
            melee.Configure(player, meleePatrol);
            PlaceGroundAgent(melee.GetComponent<EnemyNavigation>(), meleeSpawn, meleeAvoidancePriority);

            ranged = Instantiate(rangedPrefab, rangedSpawn.position, rangedSpawn.rotation, transform);
            ranged.Configure(player, rangedPatrol);
            PlaceGroundAgent(ranged.GetComponent<EnemyNavigation>(), rangedSpawn, rangedAvoidancePriority);

            drone = Instantiate(dronePrefab, droneSpawn.position, droneSpawn.rotation, transform);
            drone.Configure(player, droneWaypoints);
            var flight = drone.GetComponent<DroneFlight>();
            flight.ConfigureVolume(flightCenter, flightSize, floorHeight);
            if (!flight.TryPlace(droneSpawn.position))
            { Debug.LogError("M7 drone spawn intersects geometry or its flight bounds.", this); drone.gameObject.SetActive(false); }

            // Ground agents must see a hovering drone in local avoidance, not only in their physical sweep.
            // This never drives flight or changes the baked mesh, and clears immediately on drone death.
            var body = drone.GetComponent<SphereCollider>();
            droneAvoidance = drone.gameObject.AddComponent<NavMeshObstacle>();
            droneAvoidance.shape = NavMeshObstacleShape.Capsule;
            droneAvoidance.center = body.center;
            droneAvoidance.radius = body.radius + Mathf.Max(0f, droneAvoidancePadding);
            droneAvoidance.height = droneAvoidance.radius * 2f;
            droneAvoidance.carving = false;
            drone.GetComponent<Health>().Died += ClearDroneAvoidance;

            // The graybox palette has already initialized in Awake. Native objects belong in the lifecycle.
            var markerColor = new MaterialPropertyBlock();
            markerColor.SetColor("_BaseColor", new Color(1f, 0.75f, 0.1f));
            if (routeMarkers != null)
                foreach (var marker in routeMarkers) if (marker) marker.SetPropertyBlock(markerColor);
        }

        void PlaceGroundAgent(EnemyNavigation navigation, Transform spawn, int priority)
        {
            navigation.GetComponent<NavMeshAgent>().avoidancePriority = Mathf.Clamp(priority, 0, 99);
            if (!navigation.Place(spawn.position))
            { Debug.LogError("M7 ground spawn is outside the shared gym NavMesh.", this); navigation.gameObject.SetActive(false); }
        }

        void OnDestroy()
        {
            if (melee) { melee.GetComponent<EnemyNavigation>().Shutdown(); Destroy(melee.gameObject); }
            if (ranged) { ranged.GetComponent<EnemyNavigation>().Shutdown(); Destroy(ranged.gameObject); }
            if (drone)
            {
                drone.GetComponent<Health>().Died -= ClearDroneAvoidance;
                drone.GetComponent<DroneFlight>().Shutdown(); Destroy(drone.gameObject);
            }
        }

        void ClearDroneAvoidance() { if (droneAvoidance) droneAvoidance.enabled = false; }

        void OnDrawGizmosSelected()
        {
            DrawRoute(meleePatrol, Color.red);
            DrawRoute(rangedPatrol, Color.magenta);
            DrawRoute(droneWaypoints, Color.cyan);
            Gizmos.color = Color.cyan; Gizmos.DrawWireCube(flightCenter, flightSize);
        }
        static void DrawRoute(Transform[] points, Color color)
        {
            if (points == null) return;
            Gizmos.color = color;
            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i]; var next = points[(i + 1) % points.Length];
                if (!point) continue;
                Gizmos.DrawWireSphere(point.position, 0.3f);
                if (next) Gizmos.DrawLine(point.position, next.position);
            }
        }
    }
}
