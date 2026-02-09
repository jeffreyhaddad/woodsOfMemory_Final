using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VitalsHUD : MonoBehaviour
{
    [Tooltip("Auto-finds PlayerVitals if left empty.")]
    public PlayerVitals vitals;

    [Header("Bar Settings")]
    public float barWidth = 200f;
    public float barHeight = 20f;
    public float barSpacing = 8f;
    public float marginLeft = 20f;
    public float marginTop = 20f;

    private Image healthFill;
    private Image hungerFill;
    private Image staminaFill;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI hungerText;
    private TextMeshProUGUI staminaText;

    void Start()
    {
        if (vitals == null)
            vitals = FindAnyObjectByType<PlayerVitals>();

        BuildHUD();

        vitals.OnVitalsChanged += RefreshBars;
        RefreshBars();
    }

    void OnDestroy()
    {
        if (vitals != null)
            vitals.OnVitalsChanged -= RefreshBars;
    }

    void RefreshBars()
    {
        healthFill.fillAmount = vitals.Health / vitals.maxHealth;
        hungerFill.fillAmount = vitals.Hunger / vitals.maxHunger;
        staminaFill.fillAmount = vitals.Stamina / vitals.maxStamina;

        healthText.text = Mathf.CeilToInt(vitals.Health).ToString();
        hungerText.text = Mathf.CeilToInt(vitals.Hunger).ToString();
        staminaText.text = Mathf.CeilToInt(vitals.Stamina).ToString();
    }

    void BuildHUD()
    {
        // Canvas
        GameObject canvasObj = new GameObject("VitalsHUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Container anchored to top-left
        GameObject container = new GameObject("VitalsContainer");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(marginLeft, -marginTop);

        // Create three bars
        float yOffset = 0f;

        // Health bar (red)
        CreateBar(container.transform, "Health", new Color(0.8f, 0.15f, 0.15f), ref yOffset,
            out healthFill, out healthText);

        // Hunger bar (orange)
        CreateBar(container.transform, "Hunger", new Color(0.9f, 0.55f, 0.1f), ref yOffset,
            out hungerFill, out hungerText);

        // Stamina bar (green)
        CreateBar(container.transform, "Stamina", new Color(0.2f, 0.75f, 0.2f), ref yOffset,
            out staminaFill, out staminaText);
    }

    void CreateBar(Transform parent, string label, Color fillColor, ref float yOffset,
        out Image fillImage, out TextMeshProUGUI valueText)
    {
        // Label
        GameObject labelObj = new GameObject(label + "Label");
        labelObj.transform.SetParent(parent, false);
        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelTMP.text = label;
        labelTMP.fontSize = 14;
        labelTMP.color = Color.white;
        labelTMP.alignment = TextAlignmentOptions.Left;
        RectTransform labelRect = labelTMP.rectTransform;
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(0, 1);
        labelRect.pivot = new Vector2(0, 1);
        labelRect.anchoredPosition = new Vector2(0, -yOffset);
        labelRect.sizeDelta = new Vector2(barWidth, 18f);

        yOffset += 18f;

        // Background (dark)
        GameObject bgObj = new GameObject(label + "BarBG");
        bgObj.transform.SetParent(parent, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.anchoredPosition = new Vector2(0, -yOffset);
        bgRect.sizeDelta = new Vector2(barWidth, barHeight);

        // Fill (colored)
        GameObject fillObj = new GameObject(label + "Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Value text (centered on bar)
        GameObject textObj = new GameObject(label + "Value");
        textObj.transform.SetParent(bgObj.transform, false);
        valueText = textObj.AddComponent<TextMeshProUGUI>();
        valueText.fontSize = 14;
        valueText.color = Color.white;
        valueText.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = valueText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        yOffset += barHeight + barSpacing;
    }
}
