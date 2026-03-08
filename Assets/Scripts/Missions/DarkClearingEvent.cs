using System.Collections;
using UnityEngine;

/// <summary>
/// Attached to the Dark Clearing trigger zone.
/// When the player enters, triggers two assault waves of shadow creatures.
/// Wave kills feed into MissionManager's KillCreature objective automatically
/// via the existing CreatureAI death reporting.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DarkClearingEvent : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("Shadow creatures spawned in the first immediate wave")]
    public int wave1Count = 4;
    [Tooltip("Shadow creatures spawned in the second wave")]
    public int wave2Count = 4;
    [Tooltip("Seconds between wave 1 and wave 2")]
    public float wave2Delay = 30f;
    [Tooltip("Radius of the spawn ring around the clearing center")]
    public float spawnRadius = 18f;
    [Tooltip("Pause before wave 1 so the player sees the clearing first")]
    public float initialDelay = 2.5f;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.GetComponent<PlayerMovement>() == null &&
            other.GetComponentInParent<PlayerMovement>() == null) return;

        // Only fire during Mission 5
        if (MissionManager.Instance == null ||
            MissionManager.Instance.CurrentMission?.missionName != "Into the Dark") return;

        triggered = true;
        StartCoroutine(WaveSequence());
    }

    IEnumerator WaveSequence()
    {
        CreatureSpawner spawner = FindAnyObjectByType<CreatureSpawner>();
        if (spawner == null) yield break;

        Vector3 center = transform.position;

        // Brief pause — let the player take in the clearing before being swarmed
        yield return new WaitForSeconds(initialDelay);

        // Wave 1
        spawner.SpawnShadowWaveAt(center, wave1Count, spawnRadius);
        Debug.Log("[DarkClearingEvent] Wave 1 triggered.");

        yield return new WaitForSeconds(wave2Delay);

        // Wave 2 — only if mission is still active
        if (MissionManager.Instance != null &&
            MissionManager.Instance.CurrentMission?.missionName == "Into the Dark")
        {
            spawner.SpawnShadowWaveAt(center, wave2Count, spawnRadius);
            Debug.Log("[DarkClearingEvent] Wave 2 triggered.");
        }
    }
}
