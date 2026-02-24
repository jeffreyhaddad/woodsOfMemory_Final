using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentHUD : MonoBehaviour
{
    private TextMeshProUGUI weaponText;
    private TextMeshProUGUI toolText;
    private TextMeshProUGUI armorText;
    private GameObject container;

    // Durability bar
    private GameObject durabilityBarObj;
    private Image      durabilityFill;
    private bool       durabilitySubscribed;

    void Start()
    {
        BuildHUD();

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += RefreshHUD;

        RefreshHUD();
    }

    void Update()
    {
        // Subscribe to ToolDurabilityManager once it's available
        if (!durabilitySubscribed && ToolDurabilityManager.Instance != null)
        {
            ToolDurabilityManager.Instance.OnDurabilityChanged += RefreshHUD;
            durabilitySubscribed = true;
        }
    }

    void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= RefreshHUD;
        if (ToolDurabilityManager.Instance != null)
            ToolDurabilityManager.Instance.OnDurabilityChanged -= RefreshHUD;
    }

    void RefreshHUD()
    {
        EquipmentManager equip = EquipmentManager.Instance;
        if (equip == null) return;

        bool anyEquipped = equip.EquippedWeapon != null || equip.EquippedTool != null || equip.EquippedArmor != null;
        container.SetActive(anyEquipped);
        if (!anyEquipped) return;

        weaponText.text = equip.EquippedWeapon != null
            ? $"Weapon: {equip.EquippedWeapon.itemName} (+{equip.EquippedWeapon.damageBonus} dmg)"
            : "";
        toolText.text = equip.EquippedTool != null
            ? $"Tool: {equip.EquippedTool.itemName}"
            : "";
        armorText.text = equip.EquippedArmor != null
            ? $"Armor: {equip.EquippedArmor.itemName} (+{equip.EquippedArmor.defenseBonus} def)"
            : "";

        // Durability bar — only for tools with maxDurability > 0
        string toolName = equip.EquippedTool?.itemName ?? "";
        bool showBar = ToolDurabilityManager.Instance != null
                    && !string.IsNullOrEmpty(toolName)
                    && ToolDurabilityManager.Instance.HasDurability(toolName);

        durabilityBarObj.SetActive(showBar);

        if (showBar)
        {
            float frac = ToolDurabilityManager.Instance.GetFraction(toolName);

            // Scale fill width by setting anchorMax.x (standard Unity fill bar)
            durabilityFill.rectTransform.anchorMax = new Vector2(frac, 1f);

            // Green → yellow → red based on remaining fraction
            durabilityFill.color = frac > 0.5f
                ? Color.Lerp(new Color(1f, 0.85f, 0f), new Color(0.2f, 0.85f, 0.2f), (frac - 0.5f) * 2f)
                : Color.Lerp(new Color(0.85f, 0.1f, 0.1f), new Color(1f, 0.85f, 0f), frac * 2f);

            // Resize container to show the bar row
            container.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 80);
        }
        else
        {
            container.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 60);
        }
    }

    void BuildHUD()
    {
        GameObject canvasObj = new GameObject("EquipmentHUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 81;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        container = new GameObject("EquipContainer");
        container.transform.SetParent(canvasObj.transform, false);
        Image bg = container.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.4f);
        bg.raycastTarget = false;
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -170);
        rect.sizeDelta = new Vector2(220, 60);

        weaponText = CreateLine(container.transform, 0);
        toolText   = CreateLine(container.transform, 1);
        armorText  = CreateLine(container.transform, 2);

        BuildDurabilityBar(container.transform);
    }

    void BuildDurabilityBar(Transform parent)
    {
        // Background
        durabilityBarObj = new GameObject("DurabilityBar");
        durabilityBarObj.transform.SetParent(parent, false);
        Image barBg = durabilityBarObj.AddComponent<Image>();
        barBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        barBg.raycastTarget = false;
        RectTransform bgRect = durabilityBarObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.pivot     = new Vector2(0, 1);
        bgRect.anchoredPosition = new Vector2(8, -58);
        bgRect.sizeDelta        = new Vector2(-16, 8);

        // Fill (anchored left, width driven by anchorMax.x in RefreshHUD)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(durabilityBarObj.transform, false);
        durabilityFill = fillObj.AddComponent<Image>();
        durabilityFill.color = new Color(0.2f, 0.85f, 0.2f);
        durabilityFill.raycastTarget = false;
        RectTransform fillRect = durabilityFill.rectTransform;
        fillRect.anchorMin  = new Vector2(0, 0);
        fillRect.anchorMax  = new Vector2(1, 1);
        fillRect.offsetMin  = Vector2.zero;
        fillRect.offsetMax  = Vector2.zero;

        durabilityBarObj.SetActive(false);
    }

    TextMeshProUGUI CreateLine(Transform parent, int index)
    {
        GameObject obj = new GameObject("EquipLine_" + index);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 13;
        text.color = new Color(0.9f, 0.85f, 0.5f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(8, -4 - index * 18);
        rt.sizeDelta = new Vector2(-16, 18);
        return text;
    }
}
