using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    const string Mission5Name = "Into the Dark";

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.GetComponent<PlayerMovement>() == null &&
            other.GetComponentInParent<PlayerMovement>() == null) return;

        // Only fire during Mission 5
        if (MissionManager.Instance == null ||
            MissionManager.Instance.CurrentMission?.missionName != Mission5Name) return;

        triggered = true;
        MissionHUD.Instance?.ShowBanner("You've entered the Dark Clearing...");
        StartCoroutine(PurpleFlash());
        StartCoroutine(WaveSequence());
    }

    IEnumerator PurpleFlash()
    {
        // Build a full-screen purple overlay canvas
        GameObject canvasObj = new GameObject("DarkClearingFlash");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        DontDestroyOnLoad(canvasObj);

        GameObject imgObj = new GameObject("FlashImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.color = new Color(0.45f, 0f, 0.7f, 0f);
        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Fade in to peak opacity
        float fadeIn = 0.4f;
        float hold   = 0.3f;
        float fadeOut = 1.2f;
        float t = 0f;

        while (t < fadeIn)
        {
            t += Time.deltaTime;
            img.color = new Color(0.45f, 0f, 0.7f, Mathf.Lerp(0f, 0.75f, t / fadeIn));
            yield return null;
        }

        // Hold briefly
        img.color = new Color(0.45f, 0f, 0.7f, 0.75f);
        yield return new WaitForSeconds(hold);

        // Fade out slowly
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            img.color = new Color(0.45f, 0f, 0.7f, Mathf.Lerp(0.75f, 0f, t / fadeOut));
            yield return null;
        }

        Destroy(canvasObj);
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
            MissionManager.Instance.CurrentMission?.missionName == Mission5Name)
        {
            spawner.SpawnShadowWaveAt(center, wave2Count, spawnRadius);
            Debug.Log("[DarkClearingEvent] Wave 2 triggered.");
        }
    }
}
