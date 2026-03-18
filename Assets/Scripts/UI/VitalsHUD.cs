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
    private Image oxygenFill;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI hungerText;
    private TextMeshProUGUI staminaText;
    private TextMeshProUGUI oxygenText;
    private TextMeshProUGUI hungerLabel;
    private TextMeshProUGUI staminaLabel;
    private TextMeshProUGUI oxygenLabel;

    private static readonly Color hungerNormal    = new Color(0.9f,  0.55f, 0.1f);
    private static readonly Color hungerCritical  = new Color(1f,   0.15f, 0.05f);
    private static readonly Color staminaNormal   = new Color(0.2f, 0.75f, 0.2f);
    private static readonly Color staminaExhausted = new Color(0.85f, 0.15f, 0.05f);

    // Track pulse states so we don't write to Image.color every frame when nothing has changed
    private bool lastHungerCritical  = false;
    private bool lastStaminaCritical = false;

    void Start()
    {
        if (vitals == null)
            vitals = FindAnyObjectByType<PlayerVitals>();

        if (vitals == null)
        {
            Debug.LogWarning("VitalsHUD: No PlayerVitals found. HUD disabled.");
            return;
        }

        BuildHUD();

        vitals.OnVitalsChanged += RefreshBars;
        RefreshBars();
    }

    void OnDestroy()
    {
        if (vitals != null)
            vitals.OnVitalsChanged -= RefreshBars;
    }

    void Update()
    {
        
       
        if (vitals == null || hungerFill == null) return;

        float hungerPct         = vitals.Hunger / vitals.maxHunger;
        bool  isHungerCritical  = hungerPct < 0.20f;
        if (isHungerCritical)
        {
            // Pulse rate increases as hunger approaches zero
            float severity = 1f - hungerPct / 0.20f;
            float rate     = 1.8f + severity * 3.5f;
            float pulse    = 0.5f + 0.5f * Mathf.Sin(Time.time * rate * Mathf.PI * 2f);

            hungerFill.color = Color.Lerp(hungerNormal, hungerCritical, pulse);
            if (hungerLabel != null) hungerLabel.color = Color.Lerp(Color.white, hungerCritical, pulse * 0.8f);
            lastHungerCritical = true;
        }
        else if (lastHungerCritical)
        {
            hungerFill.color = hungerNormal;
            if (hungerLabel != null) hungerLabel.color = Color.white;
            lastHungerCritical = false;
        }

        // Stamina exhaustion pulse — bar and label shift green → red and pulse
        if (staminaFill != null)
        {
            float staminaPct     = vitals.Stamina / vitals.maxStamina;
            bool  staminaCritical = vitals.IsExhausted || staminaPct < 0.15f;
            if (staminaCritical)
            {
                float severity = vitals.IsExhausted ? 1f : 1f - staminaPct / 0.15f;
                float rate     = 2f + severity * 3f;
                float pulse    = 0.5f + 0.5f * Mathf.Sin(Time.time * rate * Mathf.PI * 2f);

                staminaFill.color = Color.Lerp(staminaNormal, staminaExhausted, pulse);
                if (staminaLabel != null) staminaLabel.color = Color.Lerp(Color.white, staminaExhausted, pulse * 0.8f);
                lastStaminaCritical = true;
            }
            else if (lastStaminaCritical)
            {
                staminaFill.color = staminaNormal;
                if (staminaLabel != null) staminaLabel.color = Color.white;
                lastStaminaCritical = false;
            }
        }
    }

    void RefreshBars()
    {
        healthFill.fillAmount = vitals.Health / vitals.maxHealth;
        hungerFill.fillAmount = vitals.Hunger / vitals.maxHunger;
        staminaFill.fillAmount = vitals.Stamina / vitals.maxStamina;
        oxygenFill.fillAmount = vitals.Oxygen / vitals.maxOxygen;

        healthText.text = Mathf.CeilToInt(vitals.Health).ToString();
        hungerText.text = Mathf.CeilToInt(vitals.Hunger).ToString();
        staminaText.text = Mathf.CeilToInt(vitals.Stamina).ToString();
        oxygenText.text = Mathf.CeilToInt(vitals.Oxygen).ToString();
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
            out healthFill, out healthText, out _);

        // Hunger bar (orange)
        CreateBar(container.transform, "Hunger", hungerNormal, ref yOffset,
            out hungerFill, out hungerText, out hungerLabel);

        // Stamina bar (green)
        CreateBar(container.transform, "Stamina", new Color(0.2f, 0.75f, 0.2f), ref yOffset,
            out staminaFill, out staminaText, out staminaLabel);
       GameObject OxygenBar= CreateBar(container.transform, "Oxygen", new Color(0.2f, 0.5f, 1f), ref yOffset,
            out oxygenFill, out oxygenText, out oxygenLabel);
    }

    GameObject  CreateBar(Transform parent, string label, Color fillColor, ref float yOffset,
        out Image fillImage, out TextMeshProUGUI valueText, out TextMeshProUGUI labelOut)
    {
        // Label
        GameObject labelObj = new GameObject(label + "Label");
        labelObj.transform.SetParent(parent, false);
        TextMeshProUGUI labelTMP = labelObj.AddComponent<TextMeshProUGUI>();
        labelOut = labelTMP;
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
        return bgObj;
    }
}
