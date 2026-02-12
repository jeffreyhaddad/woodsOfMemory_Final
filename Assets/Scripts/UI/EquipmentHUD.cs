using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentHUD : MonoBehaviour
{
    private TextMeshProUGUI weaponText;
    private TextMeshProUGUI toolText;
    private TextMeshProUGUI armorText;
    private GameObject container;

    void Start()
    {
        BuildHUD();

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += RefreshHUD;

        RefreshHUD();
    }

    void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= RefreshHUD;
    }

    void RefreshHUD()
    {
        EquipmentManager equip = EquipmentManager.Instance;
        if (equip == null) return;

        bool anyEquipped = equip.EquippedWeapon != null || equip.EquippedTool != null || equip.EquippedArmor != null;
        container.SetActive(anyEquipped);
        if (!anyEquipped) return;

        weaponText.text = equip.EquippedWeapon != null
            ? "Weapon: " + equip.EquippedWeapon.itemName + " (+" + equip.EquippedWeapon.damageBonus + " dmg)"
            : "";
        toolText.text = equip.EquippedTool != null
            ? "Tool: " + equip.EquippedTool.itemName
            : "";
        armorText.text = equip.EquippedArmor != null
            ? "Armor: " + equip.EquippedArmor.itemName + " (+" + equip.EquippedArmor.defenseBonus + " def)"
            : "";
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
        toolText = CreateLine(container.transform, 1);
        armorText = CreateLine(container.transform, 2);
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
