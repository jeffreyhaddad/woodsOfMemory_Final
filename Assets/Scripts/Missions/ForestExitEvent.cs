using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attached to the Forest Exit trigger zone.
/// When the player enters during Mission 6, spawns a final shadow wave.
/// Kills feed into MissionManager's KillCreature objective via CreatureAI death reporting.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ForestExitEvent : MonoBehaviour
{
    public int waveCount   = 6;
    public float spawnRadius = 14f;
    public float initialDelay = 2f;

    const string Mission6Name = "The Escape";

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.GetComponent<PlayerMovement>() == null &&
            other.GetComponentInParent<PlayerMovement>() == null) return;

        if (MissionManager.Instance == null ||
            MissionManager.Instance.CurrentMission?.missionName != Mission6Name) return;

        triggered = true;
        MissionHUD.Instance?.ShowBanner("You can see the edge of the forest...");
        StartCoroutine(FlashAndSpawn());
    }

    IEnumerator FlashAndSpawn()
    {
        // Cold white flash — different feel from the purple clearing
        GameObject canvasObj = new GameObject("ExitFlash");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        DontDestroyOnLoad(canvasObj);

        GameObject imgObj = new GameObject("FlashImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 1f, 0f);
        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float fadeIn  = 0.3f;
        float hold    = 0.2f;
        float fadeOut = 1.0f;
        float t = 0f;

        while (t < fadeIn)  { t += Time.deltaTime; img.color = new Color(0.9f, 0.9f, 1f, Mathf.Lerp(0f, 0.6f, t / fadeIn));  yield return null; }
        img.color = new Color(0.9f, 0.9f, 1f, 0.6f);
        yield return new WaitForSeconds(hold);
        t = 0f;
        while (t < fadeOut) { t += Time.deltaTime; img.color = new Color(0.9f, 0.9f, 1f, Mathf.Lerp(0.6f, 0f, t / fadeOut)); yield return null; }

        Destroy(canvasObj);

        yield return new WaitForSeconds(initialDelay);

        // Spawn the wave around the player so they can't just run through
        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        Vector3 center = pv != null ? pv.transform.position : transform.position;

        CreatureSpawner spawner = FindAnyObjectByType<CreatureSpawner>();
        if (spawner != null)
        {
            spawner.SpawnShadowWaveAt(center, waveCount, spawnRadius);
            MissionHUD.Instance?.ShowBanner("They won't let you leave.");
            Debug.Log("[ForestExitEvent] Final wave spawned.");
        }
    }
}
