using UnityEngine;

namespace SuperInseto
{
    [DefaultExecutionOrder(-150)] // After the single M4 navigation build (-200).
    public sealed class TestGymRangedSpawner : MonoBehaviour
    {
        [SerializeField] RangedBrain agentPrefab;
        [SerializeField] Transform spawnPoint;
        [SerializeField] Transform[] patrolPoints;
        [SerializeField] Health player;
        RangedBrain spawned;
        void Start()
        {
            if (!agentPrefab || !spawnPoint || !player)
            { Debug.LogError("M5 requires a ranged prefab, spawn and player Health.", this); return; }
            spawned = Instantiate(agentPrefab, spawnPoint.position, spawnPoint.rotation, transform);
            spawned.Configure(player, patrolPoints);
            if (!spawned.GetComponent<EnemyNavigation>().Place(spawnPoint.position))
            { Debug.LogError("M5 spawn is outside the gym NavMesh.", this); spawned.gameObject.SetActive(false); }
        }
        void OnDestroy()
        {
            if (!spawned) return;
            spawned.GetComponent<EnemyNavigation>().Shutdown(); Destroy(spawned.gameObject);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            if (spawnPoint) Gizmos.DrawWireSphere(spawnPoint.position + Vector3.up, 0.4f);
            if (patrolPoints == null) return;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                var p = patrolPoints[i]; var next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (!p) continue;
                Gizmos.DrawWireSphere(p.position, 0.25f);
                if (next) Gizmos.DrawLine(p.position, next.position);
            }
        }
    }
}
