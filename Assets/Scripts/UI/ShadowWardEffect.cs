using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Shadow Ward consumable effect:
///   - Full-screen purple tint that fades in, holds, then fades out
///   - Screen-edge directional arrow pointing toward the Dark Clearing
///   - Using another ward while active simply resets the timer
/// </summary>
public class ShadowWardEffect : MonoBehaviour
{
    public static ShadowWardEffect Instance { get; private set; }

    [Header("Timing")]
    public float visionDuration = 45f;  // total seconds the effect lasts
    public float fadeInTime     = 2f;
    public float fadeOutTime    = 3f;

    [Header("Visuals")]
    public float overlayMaxAlpha = 0.35f;

    // ── UI references (built at runtime) ──────────────────────
    private Canvas  wardCanvas;
    private Image   overlay;
    private RectTransform arrowRect;
    private TextMeshProUGUI arrowGlyph;
    private TextMeshProUGUI dirLabel;

    // ── State ──────────────────────────────────────────────────
    private bool      isActive   = false;
    private float     timeLeft   = 0f;
    private Transform playerTransform;
    private Coroutine activeCoroutine;
    private Camera    cachedCamera;
    private Vector3?  cachedClearingPos;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
        SetIndicatorVisible(false);
    }

    void Update()
    {
        if (!isActive) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) return; // coroutine handles deactivation

        UpdateDirectionIndicator();
    }

    // ─── Public API ───────────────────────────────────────────

    /// <summary>Activate (or refresh) the Shadow Ward vision effect.</summary>
    public void Activate()
    {
        if (playerTransform == null)
            playerTransform = FindAnyObjectByType<PlayerVitals>()?.transform;
        if (cachedCamera == null)
            cachedCamera = Camera.main;
        // Cache the clearing world position once — it never moves
        if (cachedClearingPos == null)
            cachedClearingPos = CompassUI.GetPOIPosition("Dark Clearing");

        if (activeCoroutine != null)
        {
            // Already active — just reset the timer so the overlay stays up
            timeLeft = visionDuration;
            return;
        }

        isActive = true;
        timeLeft = visionDuration;
        SetIndicatorVisible(true);
        activeCoroutine = StartCoroutine(VisionSequence());
    }

    // ─── Vision Sequence ──────────────────────────────────────

    IEnumerator VisionSequence()
    {
        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(0f, overlayMaxAlpha, elapsed / fadeInTime);
            overlay.color = new Color(0.35f, 0f, 0.6f, a);
            yield return null;
        }

        // Hold until timeLeft runs out (timeLeft ticks in Update)
        // Wait until 3s remain, then start fade-out
        while (timeLeft > fadeOutTime)
            yield return null;

        // Fade out
        float startAlpha = overlay.color.a;
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutTime);
            overlay.color = new Color(0.35f, 0f, 0.6f, a);
            yield return null;
        }

        overlay.color = new Color(0.35f, 0f, 0.6f, 0f);
        isActive = false;
        activeCoroutine = null;
        SetIndicatorVisible(false);
    }

    // ─── Direction Indicator ──────────────────────────────────

    void UpdateDirectionIndicator()
    {
        if (cachedCamera == null || playerTransform == null) return;
        if (cachedClearingPos == null) return;

        // Project world position to screen space
        Vector3 screenPos = cachedCamera.WorldToScreenPoint(cachedClearingPos.Value);

        // If behind camera, flip so the indicator still points the right way
        bool behindCamera = screenPos.z < 0f;
        if (behindCamera)
        {
            screenPos.x = Screen.width  - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 dir = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Check whether the target is visible on screen
        float margin = 80f;
        bool onScreen = !behindCamera
            && screenPos.x > margin && screenPos.x < Screen.width  - margin
            && screenPos.y > margin && screenPos.y < Screen.height - margin;

        Vector2 indicatorScreenPos;

        if (onScreen)
        {
            indicatorScreenPos = new Vector2(screenPos.x, screenPos.y);
        }
        else
        {
            // Clamp direction vector to screen edge
            float padding = 70f;
            float halfW = Screen.width  * 0.5f - padding;
            float halfH = Screen.height * 0.5f - padding;

            float cos = Mathf.Cos(angle * Mathf.Deg2Rad);
            float sin = Mathf.Sin(angle * Mathf.Deg2Rad);
            float scale = Mathf.Min(
                halfW / (Mathf.Abs(cos) + 0.001f),
                halfH / (Mathf.Abs(sin) + 0.001f));

            indicatorScreenPos = screenCenter + new Vector2(cos * scale, sin * scale);
        }

        // Convert screen position → canvas local position
        RectTransform canvasRect = wardCanvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, indicatorScreenPos, null, out Vector2 localPos);
        arrowRect.localPosition = localPos;

        // Rotate the glyph to point toward the clearing (► points right = 0°)
        arrowGlyph.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

        // Pulse opacity based on remaining time
        float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 3f);
        Color c = new Color(0.8f, 0.4f, 1f, pulse);
        arrowGlyph.color = c;
        dirLabel.color   = new Color(1f, 1f, 1f, pulse * 0.9f);

        // Hide label when on-screen (clutter), show when edge
        dirLabel.gameObject.SetActive(!onScreen);
    }

    // ─── UI Construction ──────────────────────────────────────

    void BuildUI()
    {
        // Canvas — very high sort order so it's always on top
        GameObject canvasObj = new GameObject("ShadowWardCanvas");
        wardCanvas = canvasObj.AddComponent<Canvas>();
        wardCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        wardCanvas.sortingOrder = 210;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen purple overlay
        GameObject overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        overlay = overlayObj.AddComponent<Image>();
        overlay.raycastTarget = false;
        overlay.color = new Color(0.35f, 0f, 0.6f, 0f);
        RectTransform overlayRect = overlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // Direction indicator container — positioned by UpdateDirectionIndicator each frame
        GameObject arrowContainer = new GameObject("DirectionIndicator");
        arrowContainer.transform.SetParent(canvasObj.transform, false);
        arrowRect = arrowContainer.AddComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(60f, 60f);

        // Arrow glyph — ► rotated each frame
        GameObject glyphObj = new GameObject("Glyph");
        glyphObj.transform.SetParent(arrowContainer.transform, false);
        arrowGlyph = glyphObj.AddComponent<TextMeshProUGUI>();
        arrowGlyph.text      = "►";
        arrowGlyph.fontSize  = 32;
        arrowGlyph.alignment = TextAlignmentOptions.Center;
        arrowGlyph.raycastTarget = false;
        RectTransform gr = arrowGlyph.rectTransform;
        gr.anchorMin = Vector2.zero;
        gr.anchorMax = Vector2.one;
        gr.offsetMin = Vector2.zero;
        gr.offsetMax = Vector2.zero;

        // Label — "Dark Clearing" shown at screen edge only
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(arrowContainer.transform, false);
        dirLabel = labelObj.AddComponent<TextMeshProUGUI>();
        dirLabel.text      = "Dark Clearing";
        dirLabel.fontSize  = 16;
        dirLabel.alignment = TextAlignmentOptions.Center;
        dirLabel.raycastTarget = false;
        RectTransform lr = dirLabel.rectTransform;
        lr.anchorMin        = new Vector2(0.5f, 0f);
        lr.anchorMax        = new Vector2(0.5f, 0f);
        lr.pivot            = new Vector2(0.5f, 1f);
        lr.anchoredPosition = new Vector2(0f, -4f);
        lr.sizeDelta        = new Vector2(140f, 24f);
    }

    void SetIndicatorVisible(bool visible)
    {
        if (arrowRect != null)
            arrowRect.gameObject.SetActive(visible);
    }
}
