using UnityEngine;
using TMPro;

/// <summary>
/// Exclamation mark that pops above a zombie's head the moment it spots the player.
/// Lives at scene root to avoid parent scale issues.
/// </summary>
public class ZombieAlertIndicator : MonoBehaviour
{
    private GameObject indicatorRoot;
    private TextMeshPro tmp;
    private float timer;
    private bool active;
    private Camera mainCam;

    private const float WorldHeight = 8.5f; // above the health bar
    private const float Duration    = 1.6f;

    void Start()
    {
        mainCam = Camera.main;
        Build();
    }

    void Build()
    {
        indicatorRoot = new GameObject("ZombieAlert");
        // Scene root — no parent
        indicatorRoot.transform.position = transform.position + Vector3.up * WorldHeight;
        indicatorRoot.SetActive(false);

        tmp = indicatorRoot.AddComponent<TextMeshPro>();
        tmp.text               = "!";
        tmp.fontSize           = 9f;
        tmp.fontStyle          = FontStyles.Bold;
        tmp.color              = new Color(1f, 0.15f, 0.05f, 1f); // red-orange
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        RectTransform rt = indicatorRoot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 2f);
    }

    /// <summary>Call this when the zombie first spots the player.</summary>
    public void ShowAlert()
    {
        if (indicatorRoot == null) return;
        timer  = Duration;
        active = true;
        indicatorRoot.SetActive(true);
        indicatorRoot.transform.localScale = Vector3.one * 1.8f; // start big
    }

    void LateUpdate()
    {
        if (!active || indicatorRoot == null) return;

        // Follow zombie
        indicatorRoot.transform.position = transform.position + Vector3.up * WorldHeight;

        // Billboard
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
            indicatorRoot.transform.rotation = mainCam.transform.rotation;

        float t = 1f - (timer / Duration);

        // Scale punch: 1.8 → 1.0 over first 25% of duration
        float scale = Mathf.Lerp(1.8f, 1f, Mathf.Clamp01(t / 0.25f));
        indicatorRoot.transform.localScale = Vector3.one * scale;

        // Fade out over last 35%
        Color c = tmp.color;
        c.a       = timer < Duration * 0.35f ? timer / (Duration * 0.35f) : 1f;
        tmp.color = c;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            active = false;
            indicatorRoot.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (indicatorRoot != null)
            Destroy(indicatorRoot);
    }
}
