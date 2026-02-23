using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a radial progress ring around the crosshair while the player is cooking.
/// Auto-created by GameManager. Call StartCooking / StopCooking from cooking scripts.
/// </summary>
public class CookingProgressUI : MonoBehaviour
{
    public static CookingProgressUI Instance { get; private set; }

    private Image ringFill;
    private TextMeshProUGUI cookLabel;
    private GameObject uiRoot;

    private float cookDuration;
    private float cookTimer;
    private bool cooking;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildUI();
        uiRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─── Public API ───────────────────────────────────────────

    public void StartCooking(float duration)
    {
        cookDuration = duration;
        cookTimer = 0f;
        cooking = true;
        ringFill.fillAmount = 1f;
        uiRoot.SetActive(true);
    }

    public void StopCooking()
    {
        cooking = false;
        uiRoot.SetActive(false);
    }

    // ─── Internal ─────────────────────────────────────────────

    void Update()
    {
        if (!cooking) return;
        cookTimer += Time.deltaTime;
        ringFill.fillAmount = 1f - Mathf.Clamp01(cookTimer / cookDuration);
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("CookingProgressCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // just above the crosshair (50)
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        uiRoot = new GameObject("CookingUI");
        uiRoot.transform.SetParent(canvasObj.transform, false);

        // Procedural ring texture — a white circle with a transparent hole
        Texture2D ringTex = MakeRingTexture(128, 0.58f);
        Sprite ringSprite = Sprite.Create(ringTex,
            new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));

        // Dark background ring (always visible — shows the "empty" part)
        Image bgImg = MakeImage(uiRoot.transform, ringSprite,
            new Color(0.1f, 0.1f, 0.1f, 0.55f), new Vector2(60, 60));
        bgImg.raycastTarget = false;

        // Orange fill ring (drains clockwise as timer counts down)
        Image fillImg = MakeImage(uiRoot.transform, ringSprite,
            new Color(1f, 0.52f, 0.08f, 0.95f), new Vector2(60, 60));
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Radial360;
        fillImg.fillOrigin = (int)Image.Origin360.Top;
        fillImg.fillClockwise = true;
        fillImg.fillAmount = 1f;
        fillImg.raycastTarget = false;
        ringFill = fillImg;

        // "Cooking..." label placed just below the ring
        GameObject labelObj = new GameObject("CookLabel");
        labelObj.transform.SetParent(uiRoot.transform, false);
        cookLabel = labelObj.AddComponent<TextMeshProUGUI>();
        cookLabel.text = "Cooking...";
        cookLabel.fontSize = 15;
        cookLabel.color = new Color(1f, 0.75f, 0.3f, 1f);
        cookLabel.alignment = TextAlignmentOptions.Center;
        cookLabel.raycastTarget = false;
        RectTransform lr = cookLabel.rectTransform;
        lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
        lr.pivot = new Vector2(0.5f, 0.5f);
        lr.anchoredPosition = new Vector2(0, -44);
        lr.sizeDelta = new Vector2(160, 22);
    }

    // Helper — makes a centered Image at (0,0) on the given parent
    static Image MakeImage(Transform parent, Sprite sprite, Color color, Vector2 size)
    {
        GameObject obj = new GameObject("Img");
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        return img;
    }

    // Generates a white ring texture with a transparent centre hole.
    // holeRatio controls how thick the ring is (0 = solid disc, 1 = nothing).
    static Texture2D MakeRingTexture(int size, float holeRatio)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerR = size * 0.5f;
        float innerR = outerR * holeRatio;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);

                // Smooth the edges with a 1-pixel anti-alias band
                float outerAlpha = Mathf.Clamp01(outerR - d);
                float innerAlpha = Mathf.Clamp01(d - innerR);
                float alpha = Mathf.Min(outerAlpha, innerAlpha);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
