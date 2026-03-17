using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 5-slot hotbar that mirrors inventory row 0 (slots 0-4).
/// Press 1-5 to use/equip the item in that inventory slot.
/// Durability bar appears only when the item is currently equipped.
/// </summary>
public class HotbarUI : MonoBehaviour
{
    public static HotbarUI Instance { get; private set; }

    const int   SlotCount = 5;
    const float SlotSize  = 48f;
    const float Spacing   = 5f;

    private Inventory   inventory;
    private InventoryUI inventoryUI;

    private Image[]           slotBgs        = new Image[SlotCount];
    private Image[]           slotInners     = new Image[SlotCount];
    private Image[]           slotIcons      = new Image[SlotCount];
    private TextMeshProUGUI[] slotQtys       = new TextMeshProUGUI[SlotCount];
    private TextMeshProUGUI[] slotNums       = new TextMeshProUGUI[SlotCount];
    private Image[]           durabilityBgs  = new Image[SlotCount];
    private Image[]           durabilityBars = new Image[SlotCount];

    private int   flashSlot    = -1;
    private float flashTimer   = 0f;
    private int   selectedSlot = -1;

    private bool equipSubscribed      = false;
    private bool durabilitySubscribed = false;

    void Awake() => Instance = this;

    void Start()
    {
        inventory   = FindAnyObjectByType<Inventory>();
        inventoryUI = FindAnyObjectByType<InventoryUI>();

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshUI;

        BuildUI();
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= RefreshUI;
        if (ToolDurabilityManager.Instance != null)
            ToolDurabilityManager.Instance.OnDurabilityChanged -= RefreshUI;
    }

    void Update()
    {
        // Subscribe to EquipmentManager once it's available (created by GameManager)
        if (!equipSubscribed && EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += RefreshUI;
            equipSubscribed = true;
            RefreshUI();
        }

        if (!durabilitySubscribed && ToolDurabilityManager.Instance != null)
        {
            ToolDurabilityManager.Instance.OnDurabilityChanged += RefreshUI;
            durabilitySubscribed = true;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
            if (inventoryUI != null && inventoryUI.IsInventoryOpen) continue;

            selectedSlot = i;
            TryUseSlot(i);
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                flashSlot = -1;
                RefreshSlotColors();
            }
        }
    }

    void TryUseSlot(int index)
    {
        if (inventory == null || inventory.slots[index].IsEmpty) return;

        flashSlot  = index;
        flashTimer = 0.18f;
        RefreshSlotColors();

        inventoryUI?.TryUseItem(inventory.slots[index].item);
    }

    void BuildUI()
    {
        float totalWidth = SlotCount * SlotSize + (SlotCount - 1) * Spacing;

        GameObject canvasObj = new GameObject("HotbarCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 88;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject rowObj = new GameObject("HotbarRow");
        rowObj.transform.SetParent(canvasObj.transform, false);
        RectTransform row = rowObj.AddComponent<RectTransform>();
        row.anchorMin        = new Vector2(0.5f, 0f);
        row.anchorMax        = new Vector2(0.5f, 0f);
        row.pivot            = new Vector2(0.5f, 0f);
        row.anchoredPosition = new Vector2(0f, 12f);
        row.sizeDelta        = new Vector2(totalWidth, SlotSize);

        for (int i = 0; i < SlotCount; i++)
        {
            float x = i * (SlotSize + Spacing) - totalWidth / 2f + SlotSize / 2f;

            // Slot background
            GameObject slotObj = new GameObject("HSlot_" + i);
            slotObj.transform.SetParent(rowObj.transform, false);
            Image bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.82f);
            RectTransform sr = slotObj.GetComponent<RectTransform>();
            sr.anchorMin        = new Vector2(0.5f, 0.5f);
            sr.anchorMax        = new Vector2(0.5f, 0.5f);
            sr.pivot            = new Vector2(0.5f, 0.5f);
            sr.sizeDelta        = new Vector2(SlotSize, SlotSize);
            sr.anchoredPosition = new Vector2(x, 0f);
            slotBgs[i] = bg;

            // Inner panel (shows as border when selected)
            GameObject innerObj = new GameObject("Inner");
            innerObj.transform.SetParent(slotObj.transform, false);
            Image inner = innerObj.AddComponent<Image>();
            inner.color         = new Color(0.15f, 0.15f, 0.15f, 0.82f);
            inner.raycastTarget = false;
            RectTransform innerR = innerObj.GetComponent<RectTransform>();
            innerR.anchorMin = Vector2.zero;
            innerR.anchorMax = Vector2.one;
            innerR.offsetMin = new Vector2(2f, 2f);
            innerR.offsetMax = new Vector2(-2f, -2f);
            slotInners[i] = inner;

            // Item icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.enabled        = false;
            icon.preserveAspect = true;
            icon.raycastTarget  = false;
            RectTransform ir = iconObj.GetComponent<RectTransform>();
            ir.anchorMin = Vector2.zero;
            ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(4f, 8f);   // bottom space reserved for durability bar
            ir.offsetMax = new Vector2(-4f, -4f);
            slotIcons[i] = icon;

            // Quantity label (top-right)
            GameObject qtyObj = new GameObject("Qty");
            qtyObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI qty = qtyObj.AddComponent<TextMeshProUGUI>();
            qty.fontSize             = 11;
            qty.alignment            = TextAlignmentOptions.TopRight;
            qty.color                = Color.white;
            qty.raycastTarget        = false;
            RectTransform qr = qty.rectTransform;
            qr.anchorMin = Vector2.zero;
            qr.anchorMax = Vector2.one;
            qr.offsetMin = new Vector2(2f, 8f);
            qr.offsetMax = new Vector2(-3f, -2f);
            slotQtys[i] = qty;

            // Slot number (top-left, subtle)
            GameObject numObj = new GameObject("Num");
            numObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI num = numObj.AddComponent<TextMeshProUGUI>();
            num.text          = (i + 1).ToString();
            num.fontSize      = 9;
            num.alignment     = TextAlignmentOptions.TopLeft;
            num.color         = new Color(1f, 1f, 1f, 0.4f);
            num.raycastTarget = false;
            RectTransform nr = num.rectTransform;
            nr.anchorMin = Vector2.zero;
            nr.anchorMax = Vector2.one;
            nr.offsetMin = new Vector2(3f, 0f);
            nr.offsetMax = new Vector2(0f, -2f);
            slotNums[i] = num;

            // Durability bar background (full width, bottom of slot)
            GameObject durBgObj = new GameObject("DurBg");
            durBgObj.transform.SetParent(slotObj.transform, false);
            Image durBg = durBgObj.AddComponent<Image>();
            durBg.color         = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            durBg.raycastTarget = false;
            durBg.enabled       = false;
            RectTransform durBgR = durBgObj.GetComponent<RectTransform>();
            durBgR.anchorMin        = new Vector2(0f, 0f);
            durBgR.anchorMax        = new Vector2(1f, 0f);
            durBgR.pivot            = new Vector2(0.5f, 0f);
            durBgR.anchoredPosition = new Vector2(0f, 2f);
            durBgR.sizeDelta        = new Vector2(-6f, 4f);
            durabilityBgs[i] = durBg;

            // Durability bar fill (left-anchored, width set at runtime)
            GameObject durObj = new GameObject("DurBar");
            durObj.transform.SetParent(slotObj.transform, false);
            Image dur = durObj.AddComponent<Image>();
            dur.color         = Color.green;
            dur.raycastTarget = false;
            dur.enabled       = false;
            RectTransform durR = durObj.GetComponent<RectTransform>();
            durR.anchorMin        = new Vector2(0f, 0f);
            durR.anchorMax        = new Vector2(0f, 0f);
            durR.pivot            = new Vector2(0f, 0f);
            durR.anchoredPosition = new Vector2(3f, 2f);
            durR.sizeDelta        = new Vector2(SlotSize - 6f, 4f);
            durabilityBars[i] = dur;
        }
    }

    void RefreshUI()
    {
        if (inventory == null) return;

        EquipmentManager equip = EquipmentManager.Instance;

        for (int i = 0; i < SlotCount; i++)
        {
            InventorySlot slot = inventory.slots[i];
            bool showDur = false;

            if (slot.IsEmpty)
            {
                slotIcons[i].enabled             = false;
                slotQtys[i].text                 = "";
                slotQtys[i].fontSize             = 11;
                slotQtys[i].alignment            = TextAlignmentOptions.TopRight;
                slotQtys[i].textWrappingMode     = TMPro.TextWrappingModes.NoWrap;
            }
            else
            {
                ItemData item = slot.item;

                if (item.icon == null)
                    ItemRegistry.Register(item);

                int qty  = slot.quantity;
                float fade = qty <= 0 ? 0.35f : 1f;

                if (item.icon != null)
                {
                    slotIcons[i].enabled             = true;
                    slotIcons[i].sprite              = item.icon;
                    slotIcons[i].color               = new Color(1f, 1f, 1f, fade);
                    slotQtys[i].fontSize             = 11;
                    slotQtys[i].alignment            = TextAlignmentOptions.TopRight;
                    slotQtys[i].textWrappingMode     = TMPro.TextWrappingModes.NoWrap;
                    slotQtys[i].text                 = qty > 1 ? qty.ToString() : (qty == 0 ? "0" : "");
                }
                else
                {
                    slotIcons[i].enabled = false;
                    string displayName   = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
                    if (displayName.Length > 7) displayName = displayName.Substring(0, 6) + ".";
                    string qtyStr        = qty > 1 ? "\nx" + qty : (qty == 0 ? "\nx0" : "");
                    slotQtys[i].fontSize         = 9;
                    slotQtys[i].alignment        = TextAlignmentOptions.Center;
                    slotQtys[i].textWrappingMode = TMPro.TextWrappingModes.Normal;
                    slotQtys[i].text = $"<color=#{(qty <= 0 ? "888888" : "dddddd")}>{displayName}{qtyStr}</color>";
                }

                // Durability bar: only visible when item is currently equipped
                if (equip != null && equip.IsEquipped(item)
                    && ToolDurabilityManager.Instance != null
                    && ToolDurabilityManager.Instance.HasDurability(item.itemName))
                {
                    showDur = true;
                    float ratio    = ToolDurabilityManager.Instance.GetFraction(item.itemName);
                    Color barColor = ratio > 0.6f ? Color.green
                                   : ratio > 0.3f ? new Color(1f, 0.65f, 0f)
                                   : Color.red;
                    durabilityBars[i].rectTransform.sizeDelta = new Vector2((SlotSize - 6f) * ratio, 4f);
                    durabilityBars[i].color = barColor;
                }
            }

            durabilityBgs[i].enabled  = showDur;
            durabilityBars[i].enabled = showDur;
        }

        RefreshSlotColors();
    }

    void RefreshSlotColors()
    {
        if (inventory == null) return;

        for (int i = 0; i < SlotCount; i++)
        {
            bool hasItem   = !inventory.slots[i].IsEmpty;
            Color innerColor = hasItem
                ? new Color(0.22f, 0.22f, 0.22f, 0.88f)
                : new Color(0.15f, 0.15f, 0.15f, 0.82f);

            if (i == flashSlot)
            {
                slotBgs[i].color    = new Color(0.55f, 0.45f, 0.1f, 0.95f);
                slotInners[i].color = new Color(0.55f, 0.45f, 0.1f, 0.95f);
            }
            else if (i == selectedSlot)
            {
                slotBgs[i].color    = new Color(0.75f, 0.62f, 0.15f, 1f);
                slotInners[i].color = innerColor;
            }
            else
            {
                slotBgs[i].color    = innerColor;
                slotInners[i].color = innerColor;
            }
        }
    }
}
