using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SuperInseto
{
    // Test-scene setup only. Uses the installed Unity navigation module; no saved bake or new package required.
    [DefaultExecutionOrder(-200)]
    public sealed class TestGymNavigation : MonoBehaviour
    {
        [SerializeField] EnemyBrain agentPrefab;
        [SerializeField] Transform spawnPoint;
        [SerializeField] Transform[] patrolPoints;
        [SerializeField] Health player;
        [SerializeField] BoxCollider[] doorLeaves;
        [SerializeField] Vector3 boundsCenter = new Vector3(0f, 4f, 0f);
        [SerializeField] Vector3 boundsSize = new Vector3(24f, 12f, 30f);
        NavMeshData data;
        NavMeshDataInstance instance;
        EnemyBrain spawned;
        readonly List<NavMeshObstacle> obstacles = new List<NavMeshObstacle>();

        void Start()
        {
            if (!agentPrefab || !spawnPoint || !player)
            { Debug.LogError("M4 gym requires an agent prefab, spawn and player Health.", this); return; }
            var markups = new List<NavMeshBuildMarkup>();
            foreach (var health in FindObjectsByType<Health>(FindObjectsSortMode.None))
                markups.Add(new NavMeshBuildMarkup { root = health.transform, ignoreFromBuild = true });
            if (doorLeaves != null)
                foreach (var leaf in doorLeaves)
                {
                    if (!leaf) continue;
                    markups.Add(new NavMeshBuildMarkup { root = leaf.transform, ignoreFromBuild = true });
                    var obstacle = leaf.gameObject.AddComponent<NavMeshObstacle>();
                    obstacle.shape = NavMeshObstacleShape.Box; obstacle.center = leaf.center; obstacle.size = leaf.size;
                    obstacle.carving = true; obstacle.carveOnlyStationary = false;
                    obstacles.Add(obstacle);
                }
            var sources = new List<NavMeshBuildSource>();
            var bounds = new Bounds(boundsCenter, boundsSize);
            Physics.SyncTransforms();
            NavMeshBuilder.CollectSources(bounds, ~4, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = 0.4f; settings.agentHeight = 1.8f;
            settings.agentClimb = 0.3f; settings.agentSlope = 45f;
            data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (!data) { Debug.LogError("M4 NavMesh build failed.", this); return; }
            instance = NavMesh.AddNavMeshData(data);
            spawned = Instantiate(agentPrefab, spawnPoint.position, spawnPoint.rotation, transform);
            spawned.Configure(player, patrolPoints);
            if (!spawned.GetComponent<EnemyNavigation>().Place(spawnPoint.position))
            { Debug.LogError("M4 spawn is outside the NavMesh. Check the gym bounds and floor.", this); spawned.gameObject.SetActive(false); }
        }
        void OnDestroy()
        {
            if (spawned)
            {
                spawned.GetComponent<EnemyNavigation>().Shutdown();
                Destroy(spawned.gameObject);
            }
            if (instance.valid) instance.Remove();
            if (data) Destroy(data);
            foreach (var obstacle in obstacles) if (obstacle) Destroy(obstacle);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            if (spawnPoint) Gizmos.DrawWireSphere(spawnPoint.position + Vector3.up, 0.4f);
            if (patrolPoints == null) return;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                var point = patrolPoints[i]; var next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (!point) continue;
                Gizmos.DrawWireSphere(point.position, 0.25f);
                if (next) Gizmos.DrawLine(point.position, next.position);
            }
        }
    }
}
