using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CreatureSpawner : MonoBehaviour
{
    [Header("Wildlife Spawning")]
    public GameObject[] wildlifePrefabs;
    public int maxWildlife = 8;
    public float wildlifeSpawnRadius = 80f;
    public float wildlifeSpawnInterval = 30f;

    [Header("Shadow Creature Spawning")]
    public GameObject shadowPrefab;
    public int maxShadowCreatures = 5;
    public float shadowSpawnRadius = 50f;
    public float shadowSpawnInterval = 15f;

    [Header("General")]
    [Tooltip("Minimum distance from player for spawning")]
    public float minSpawnDistance = 30f;

    private DayNightCycle dayNight;
    private Transform playerTransform;
    private List<CreatureAI> activeWildlife = new List<CreatureAI>();
    private List<CreatureAI> activeShadows = new List<CreatureAI>();
    private float wildlifeTimer;
    private float shadowTimer;

    void Start()
    {
        dayNight = FindAnyObjectByType<DayNightCycle>();
        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        if (pv != null)
            playerTransform = pv.transform;

        // Auto-load prefabs if none assigned in Inspector
        TryLoadPrefabsFromPath();
    }

    void TryLoadPrefabsFromPath()
    {
#if UNITY_EDITOR
        var deer = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Creatures/DeerPrefab.prefab");
        var shadow = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Creatures/ZombieCreaturePrefab.prefab");

        if (deer != null && (wildlifePrefabs == null || wildlifePrefabs.Length == 0))
        {
            wildlifePrefabs = new GameObject[] { deer };
            Debug.Log("[CreatureSpawner] Auto-loaded Deer prefab from Assets/Prefabs/Creatures/");
        }
        if (shadow != null && shadowPrefab == null)
        {
            shadowPrefab = shadow;
            Debug.Log("[CreatureSpawner] Auto-loaded Shadow Creature prefab from Assets/Prefabs/Creatures/");
        }
#endif
    }

    private float cleanupTimer;

    void Update()
    {
        if (playerTransform == null) return;

        // Clean up destroyed creatures periodically (manual reverse loop avoids closure allocation)
        cleanupTimer -= Time.deltaTime;
        if (cleanupTimer <= 0f)
        {
            for (int i = activeWildlife.Count - 1; i >= 0; i--)
                if (activeWildlife[i] == null) activeWildlife.RemoveAt(i);
            for (int i = activeShadows.Count - 1; i >= 0; i--)
                if (activeShadows[i] == null) activeShadows.RemoveAt(i);
            cleanupTimer = 5f;
        }

        // Wildlife spawning (always)
        wildlifeTimer -= Time.deltaTime;
        if (wildlifeTimer <= 0f && activeWildlife.Count < maxWildlife)
        {
            SpawnWildlife();
            wildlifeTimer = wildlifeSpawnInterval;
        }

        // Shadow creature spawning (night only)
        if (dayNight != null && dayNight.IsNight)
        {
            shadowTimer -= Time.deltaTime;
            if (shadowTimer <= 0f && activeShadows.Count < maxShadowCreatures)
            {
                SpawnShadowCreature();
                shadowTimer = shadowSpawnInterval;
            }
        }
    }

    void SpawnWildlife()
    {
        if (wildlifePrefabs == null || wildlifePrefabs.Length == 0)
        {
            Debug.LogWarning("[CreatureSpawner] No wildlife prefabs assigned!");
            return;
        }

        if (TryGetSpawnPoint(wildlifeSpawnRadius, out Vector3 point))
        {
            GameObject prefab = wildlifePrefabs[Random.Range(0, wildlifePrefabs.Length)];
            GameObject go = Instantiate(prefab, point, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            CreatureAI ai = go.GetComponent<CreatureAI>();
            if (ai != null)
            {
                ai.OnCreatureDeath += c => activeWildlife.Remove(c);
                activeWildlife.Add(ai);
            }
            Debug.Log($"[CreatureSpawner] Spawned wildlife '{go.name}' at {point}");
        }
    }

    void SpawnShadowCreature()
    {
        if (shadowPrefab == null) return;

        if (TryGetSpawnPoint(shadowSpawnRadius, out Vector3 point))
        {
            GameObject go = Instantiate(shadowPrefab, point, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            CreatureAI ai = go.GetComponent<CreatureAI>();
            if (ai != null)
            {
                ai.OnCreatureDeath += c => activeShadows.Remove(c);
                activeShadows.Add(ai);
            }
            Debug.Log($"[CreatureSpawner] Spawned shadow creature '{go.name}' at {point}");
        }
    }

    /// <summary>
    /// Spawns <paramref name="count"/> shadow creatures evenly distributed
    /// in a ring around <paramref name="center"/> at the given radius.
    /// Called by DarkClearingEvent for the Mission 5 assault wave.
    /// </summary>
    public void SpawnShadowWaveAt(Vector3 center, int count, float radius)
    {
        if (shadowPrefab == null) return;

        int layerMask = ~LayerMask.GetMask("Creature", "Player");

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            float x = center.x + Mathf.Sin(angle) * radius;
            float z = center.z + Mathf.Cos(angle) * radius;

            // Cast straight down from very high up — hits the actual top surface of
            // whatever geometry exists (terrain, rocks, platforms).  This is the only
            // approach that is immune to NavMesh bake errors or terrain offset issues.
            float y;
            if (Physics.Raycast(new Vector3(x, 10000f, z), Vector3.down,
                    out RaycastHit hit, 20000f, layerMask))
            {
                y = hit.point.y;
            }
            else
            {
                // Physics fallback: terrain API
                Terrain t = Terrain.activeTerrain;
                y = t != null
                    ? t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y
                    : center.y;
            }

            Vector3 spawnPos = new Vector3(x, y + 0.5f, z);

            GameObject go = Instantiate(shadowPrefab, spawnPos,
                Quaternion.Euler(0, Random.Range(0f, 360f), 0));

            // NavMeshAgent.OnEnable (fired during Instantiate) auto-snaps the agent
            // to whatever NavMesh it finds, which can be BELOW the visible terrain
            // surface when the bake is off.  Disable → reposition → re-enable forces
            // the agent to re-initialize from our confirmed surface position instead.
            NavMeshAgent a = go.GetComponent<NavMeshAgent>();
            if (a != null)
            {
                a.enabled = false;
                go.transform.position = spawnPos;
                a.enabled = true;
            }

            CreatureAI ai = go.GetComponent<CreatureAI>();
            if (ai != null)
            {
                ai.OnCreatureDeath += c => activeShadows.Remove(c);
                activeShadows.Add(ai);
            }

            Debug.Log($"[ShadowWave] Spawned creature {i} at {spawnPos} (raycast surface)");
        }

        Debug.Log($"[CreatureSpawner] Spawned {count}-creature wave around {center}");
    }

    bool TryGetSpawnPoint(float radius, out Vector3 result)
    {
        Terrain terrain = Terrain.activeTerrain; // cache once outside loop

        // First try NavMesh-based spawn
        for (int i = 0; i < 10; i++)
        {
            Vector2 rnd = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, radius);
            Vector3 candidate = playerTransform.position + new Vector3(rnd.x, 0, rnd.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                result = ClampToTerrainSurface(hit.position, terrain);
                return true;
            }
        }

        // Fallback: use terrain height if NavMesh isn't baked
        if (terrain != null)
        {
            Vector2 rnd = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, radius);
            Vector3 candidate = playerTransform.position + new Vector3(rnd.x, 0, rnd.y);
            candidate.y = terrain.SampleHeight(candidate) + terrain.transform.position.y;
            result = candidate;
            return true;
        }

        Debug.LogWarning("[CreatureSpawner] Could not find spawn point! Is NavMesh baked or Terrain present?");
        result = Vector3.zero;
        return false;
    }

    /// Pins a position's Y to the terrain surface if the terrain is higher,
    /// preventing creatures from spawning underground on uneven terrain.
    static Vector3 ClampToTerrainSurface(Vector3 pos, Terrain terrain)
    {
        float groundY;
        if (terrain != null)
        {
            groundY = terrain.SampleHeight(pos) + terrain.transform.position.y;
        }
        else if (Physics.Raycast(pos + Vector3.up * 200f, Vector3.down, out RaycastHit hit, 400f))
        {
            groundY = hit.point.y;
        }
        else
        {
            return pos;
        }
        pos.y = Mathf.Max(pos.y, groundY + 0.05f);
        return pos;
    }
}
