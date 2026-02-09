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
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Clean up destroyed creatures
        activeWildlife.RemoveAll(c => c == null);
        activeShadows.RemoveAll(c => c == null);

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
        if (wildlifePrefabs.Length == 0) return;

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
        }
    }

    bool TryGetSpawnPoint(float radius, out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 rnd = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, radius);
            Vector3 candidate = playerTransform.position + new Vector3(rnd.x, 0, rnd.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }
}
