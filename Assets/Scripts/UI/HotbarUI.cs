using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 5-slot hotbar always visible at the bottom-center of the screen.
///
/// Assigning items:
///   Open inventory (Tab/I), hover over any item, then press 1-5 to assign it to that hotbar slot.
///
/// Using items:
///   Press 1-5 while the inventory is closed to use/equip the assigned item.
/// </summary>
public class HotbarUI : MonoBehaviour
{
    public static HotbarUI Instance { get; private set; }

    const int SlotCount  = 5;
    const float SlotSize = 48f;
    const float Spacing  = 5f;

    private readonly ItemData[] hotbarItems = new ItemData[SlotCount];

    private Inventory   inventory;
    private InventoryUI inventoryUI;

    private Image[]            slotBgs   = new Image[SlotCount];
    private Image[]            slotIcons = new Image[SlotCount];
    private TextMeshProUGUI[]  slotQtys  = new TextMeshProUGUI[SlotCount];
    private TextMeshProUGUI[]  slotNums  = new TextMeshProUGUI[SlotCount];

    private int   flashSlot  = -1;
    private float flashTimer = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        inventory   = FindAnyObjectByType<Inventory>();
        inventoryUI = FindAnyObjectByType<InventoryUI>();

        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshUI;
            inventory.OnItemAdded += OnItemAdded;
        }

        BuildUI();
    }

    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshUI;
            inventory.OnItemAdded -= OnItemAdded;
        }
    }

    void OnItemAdded(string itemName, int qty)
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.HotbarAutoPopulate) return;

        // If this item is already on the hotbar, nothing to do
        for (int i = 0; i < SlotCount; i++)
            if (hotbarItems[i] != null && hotbarItems[i].itemName == itemName) return;

        // Find the ItemData from inventory and fill the first empty slot
        for (int i = 0; i < SlotCount; i++)
        {
            if (hotbarItems[i] != null) continue;
            for (int s = 0; s < inventory.slots.Length; s++)
            {
                if (!inventory.slots[s].IsEmpty && inventory.slots[s].item.itemName == itemName)
                {
                    hotbarItems[i] = inventory.slots[s].item;
                    RefreshUI();
                    return;
                }
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

            // Inventory open + hovering a slot → assign that item to this hotbar position
            if (inventoryUI != null && inventoryUI.IsInventoryOpen && inventoryUI.HoveredSlot >= 0)
            {
                hotbarItems[i] = inventoryUI.GetHoveredItem();
                RefreshUI();
                return;
            }

            // Inventory closed → use / equip the hotbar item
            TryUseSlot(i);
        }

        // Flash highlight decay
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
        if (hotbarItems[index] == null) return;
        if (inventory == null || !inventory.HasItem(hotbarItems[index]))
        {
            // Item was consumed/removed — clear the slot
            hotbarItems[index] = null;
            RefreshUI();
            return;
        }

        flashSlot  = index;
        flashTimer = 0.18f;
        RefreshSlotColors();

        inventoryUI?.TryUseItem(hotbarItems[index]);
    }

    void BuildUI()
    {
        float totalWidth = SlotCount * SlotSize + (SlotCount - 1) * Spacing;

        GameObject canvasObj = new GameObject("HotbarCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 88;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Row container — bottom-center
        GameObject rowObj = new GameObject("HotbarRow");
        rowObj.transform.SetParent(canvasObj.transform, false);
        RectTransform row = rowObj.AddComponent<RectTransform>();
        row.anchorMin       = new Vector2(0.5f, 0f);
        row.anchorMax       = new Vector2(0.5f, 0f);
        row.pivot           = new Vector2(0.5f, 0f);
        row.anchoredPosition = new Vector2(0f, 12f);
        row.sizeDelta       = new Vector2(totalWidth, SlotSize);

        for (int i = 0; i < SlotCount; i++)
        {
            float x = i * (SlotSize + Spacing) - totalWidth / 2f + SlotSize / 2f;

            // Slot background
            GameObject slotObj = new GameObject("HSlot_" + i);
            slotObj.transform.SetParent(rowObj.transform, false);
            Image bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.82f);
            RectTransform sr = slotObj.GetComponent<RectTransform>();
            sr.anchorMin       = new Vector2(0.5f, 0.5f);
            sr.anchorMax       = new Vector2(0.5f, 0.5f);
            sr.pivot           = new Vector2(0.5f, 0.5f);
            sr.sizeDelta       = new Vector2(SlotSize, SlotSize);
            sr.anchoredPosition = new Vector2(x, 0f);
            slotBgs[i] = bg;

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
            ir.offsetMin = new Vector2(4f, 4f);
            ir.offsetMax = new Vector2(-4f, -4f);
            slotIcons[i] = icon;

            // Quantity label (bottom-right)
            GameObject qtyObj = new GameObject("Qty");
            qtyObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI qty = qtyObj.AddComponent<TextMeshProUGUI>();
            qty.fontSize      = 11;
            qty.alignment     = TextAlignmentOptions.BottomRight;
            qty.color         = Color.white;
            qty.raycastTarget = false;
            RectTransform qr = qty.rectTransform;
            qr.anchorMin = Vector2.zero;
            qr.anchorMax = Vector2.one;
            qr.offsetMin = new Vector2(2f, 2f);
            qr.offsetMax = new Vector2(-3f, -2f);
            slotQtys[i] = qty;

            // Slot number label (top-left, subtle)
            GameObject numObj = new GameObject("Num");
            numObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI num = numObj.AddComponent<TextMeshProUGUI>();
            num.text      = (i + 1).ToString();
            num.fontSize  = 9;
            num.alignment = TextAlignmentOptions.TopLeft;
            num.color     = new Color(1f, 1f, 1f, 0.4f);
            num.raycastTarget = false;
            RectTransform nr = num.rectTransform;
            nr.anchorMin = Vector2.zero;
            nr.anchorMax = Vector2.one;
            nr.offsetMin = new Vector2(3f, 0f);
            nr.offsetMax = new Vector2(0f, -2f);
            slotNums[i] = num;
        }
    }

    void RefreshUI()
    {
        if (inventory == null) return;

        for (int i = 0; i < SlotCount; i++)
        {
            ItemData item = hotbarItems[i];

            if (item == null)
            {
                slotIcons[i].enabled = false;
                slotQtys[i].text     = "";
                slotQtys[i].fontSize = 11;
                slotQtys[i].alignment = TMPro.TextAlignmentOptions.BottomRight;
                slotQtys[i].textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                continue;
            }

            // Try to load icon if not yet assigned (mirrors InventoryUI behaviour)
            if (item.icon == null)
                ItemRegistry.Register(item);

            int qty = inventory.GetItemCount(item);
            float fade = qty <= 0 ? 0.35f : 1f;

            if (item.icon != null)
            {
                slotIcons[i].enabled = true;
                slotIcons[i].sprite  = item.icon;
                slotIcons[i].color   = new Color(1f, 1f, 1f, fade);
                slotQtys[i].fontSize  = 11;
                slotQtys[i].alignment = TMPro.TextAlignmentOptions.BottomRight;
                slotQtys[i].textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                slotQtys[i].text = qty > 1 ? qty.ToString() : (qty == 0 ? "0" : "");
            }
            else
            {
                // No icon — show item name as text fallback (same as InventoryUI)
                slotIcons[i].enabled = false;
                string displayName = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
                // Abbreviate to fit the small slot
                if (displayName.Length > 7) displayName = displayName.Substring(0, 6) + ".";
                string qtyStr = qty > 1 ? "\nx" + qty : (qty == 0 ? "\nx0" : "");
                slotQtys[i].fontSize  = 9;
                slotQtys[i].alignment = TMPro.TextAlignmentOptions.Center;
                slotQtys[i].textWrappingMode = TMPro.TextWrappingModes.Normal;
                slotQtys[i].text = $"<color=#{(qty <= 0 ? "888888" : "dddddd")}>{displayName}{qtyStr}</color>";
            }
        }

        RefreshSlotColors();
    }

    void RefreshSlotColors()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (i == flashSlot)
                slotBgs[i].color = new Color(0.55f, 0.45f, 0.1f, 0.95f); // gold flash on use
            else if (hotbarItems[i] != null)
                slotBgs[i].color = new Color(0.22f, 0.22f, 0.22f, 0.88f);
            else
                slotBgs[i].color = new Color(0.15f, 0.15f, 0.15f, 0.82f);
        }
    }
}
