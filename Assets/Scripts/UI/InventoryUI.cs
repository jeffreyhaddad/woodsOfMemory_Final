using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Inventory component on the Player. Auto-finds if empty.")]
    public Inventory inventory;

    [Header("Grid Settings")]
    public int columns = 8;
    public int rows = 6;
    public float slotSize = 60f;
    public float slotSpacing = 5f;

    private GameObject panelObj;
    private Image[] slotImages;
    private Image[] iconImages;
    private TextMeshProUGUI[] quantityTexts;
    private bool isOpen = false;

    // Tooltip / use feedback
    private TextMeshProUGUI tooltipText;
    private float feedbackTimer;

    void Start()
    {
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();

        if (inventory == null)
        {
            Debug.LogWarning("InventoryUI: No Inventory found. UI disabled.");
            enabled = false;
            return;
        }

        BuildUI();
        panelObj.SetActive(false);

        inventory.OnInventoryChanged += RefreshUI;

        // Lock cursor at start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isOpen) CloseInventory();
            else OpenInventory();
        }

        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.unscaledDeltaTime;
            if (feedbackTimer <= 0f && tooltipText != null)
                tooltipText.text = "";
        }
    }

    void OpenInventory()
    {
        isOpen = true;
        panelObj.SetActive(true);
        RefreshUI();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerMovement.inputBlocked = true;
    }

    void CloseInventory()
    {
        isOpen = false;
        panelObj.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerMovement.inputBlocked = false;
    }

    void RefreshUI()
    {
        EquipmentManager equip = EquipmentManager.Instance;

        for (int i = 0; i < inventory.slots.Length && i < slotImages.Length; i++)
        {
            InventorySlot slot = inventory.slots[i];

            if (slot.IsEmpty)
            {
                slotImages[i].color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                iconImages[i].enabled = false;
                quantityTexts[i].text = "";
                quantityTexts[i].fontSize = 14;
                quantityTexts[i].alignment = TextAlignmentOptions.BottomRight;
            }
            else
            {
                // Check if this item is currently equipped
                bool isEquipped = equip != null && equip.IsEquipped(slot.item);

                // Equipped items get a gold border, others get default gray
                slotImages[i].color = isEquipped
                    ? new Color(0.6f, 0.5f, 0.1f, 0.95f)
                    : new Color(0.35f, 0.35f, 0.35f, 0.9f);

                if (slot.item.icon != null)
                {
                    iconImages[i].enabled = true;
                    iconImages[i].sprite = slot.item.icon;
                    quantityTexts[i].fontSize = 14;
                    quantityTexts[i].alignment = TextAlignmentOptions.BottomRight;
                    quantityTexts[i].text = slot.quantity > 1 ? slot.quantity.ToString() : "";
                }
                else
                {
                    // No icon — show item name centered in the slot
                    iconImages[i].enabled = false;
                    quantityTexts[i].fontSize = 10;
                    quantityTexts[i].alignment = TextAlignmentOptions.Center;
                    quantityTexts[i].enableWordWrapping = true;
                    string displayName = string.IsNullOrEmpty(slot.item.itemName)
                        ? slot.item.name  // fallback to ScriptableObject asset name
                        : slot.item.itemName;
                    string equipped = isEquipped ? " [E]" : "";
                    string qty = slot.quantity > 1 ? "\nx" + slot.quantity : "";
                    quantityTexts[i].text = displayName + equipped + qty;
                }
            }
        }
    }

    void BuildUI()
    {
        int totalSlots = columns * rows;
        slotImages = new Image[totalSlots];
        iconImages = new Image[totalSlots];
        quantityTexts = new TextMeshProUGUI[totalSlots];

        // Canvas
        GameObject canvasObj = new GameObject("InventoryCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dark background overlay
        panelObj = new GameObject("InventoryPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.7f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Grid container (centered)
        float gridWidth = columns * (slotSize + slotSpacing) - slotSpacing;
        float gridHeight = rows * (slotSize + slotSpacing) - slotSpacing;

        GameObject gridObj = new GameObject("Grid");
        gridObj.transform.SetParent(panelObj.transform, false);
        RectTransform gridRect = gridObj.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(gridWidth, gridHeight + 40f);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(gridObj.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "Inventory";
        title.fontSize = 28;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 0);
        titleRect.anchoredPosition = new Vector2(0, 5);
        titleRect.sizeDelta = new Vector2(0, 35);

        // Create slot grid
        for (int i = 0; i < totalSlots; i++)
        {
            int col = i % columns;
            int row = i / columns;

            float x = col * (slotSize + slotSpacing) - gridWidth / 2f + slotSize / 2f;
            float y = -row * (slotSize + slotSpacing) + gridHeight / 2f - slotSize / 2f;

            // Slot background
            GameObject slotObj = new GameObject("Slot_" + i);
            slotObj.transform.SetParent(gridObj.transform, false);
            Image slotImg = slotObj.AddComponent<Image>();
            slotImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);
            slotRect.anchoredPosition = new Vector2(x, y);
            slotImages[i] = slotImg;

            // Item icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.enabled = false;
            icon.preserveAspect = true;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4, 4);
            iconRect.offsetMax = new Vector2(-4, -4);
            iconImages[i] = icon;

            // Quantity text (bottom-right corner)
            GameObject qtyObj = new GameObject("Qty");
            qtyObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI qty = qtyObj.AddComponent<TextMeshProUGUI>();
            qty.fontSize = 14;
            qty.alignment = TextAlignmentOptions.BottomRight;
            qty.color = Color.white;
            qty.text = "";
            RectTransform qtyRect = qty.rectTransform;
            qtyRect.anchorMin = Vector2.zero;
            qtyRect.anchorMax = Vector2.one;
            qtyRect.offsetMin = new Vector2(2, 2);
            qtyRect.offsetMax = new Vector2(-4, -2);
            quantityTexts[i] = qty;

            // Right-click to use item
            Button slotBtn = slotObj.AddComponent<Button>();
            slotBtn.targetGraphic = slotImg;
            int slotIndex = i;
            slotBtn.onClick.AddListener(() => UseItem(slotIndex));
        }

        // Tooltip / feedback text at bottom
        GameObject tipObj = new GameObject("Tooltip");
        tipObj.transform.SetParent(gridObj.transform, false);
        tooltipText = tipObj.AddComponent<TextMeshProUGUI>();
        tooltipText.text = "";
        tooltipText.fontSize = 16;
        tooltipText.alignment = TextAlignmentOptions.Center;
        tooltipText.color = new Color(0.9f, 0.9f, 0.5f);
        tooltipText.raycastTarget = false;
        RectTransform tipRect = tooltipText.rectTransform;
        tipRect.anchorMin = new Vector2(0, 0);
        tipRect.anchorMax = new Vector2(1, 0);
        tipRect.pivot = new Vector2(0.5f, 1);
        tipRect.anchoredPosition = new Vector2(0, -10);
        tipRect.sizeDelta = new Vector2(0, 30);
    }

    void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventory.slots.Length) return;
        InventorySlot slot = inventory.slots[slotIndex];
        if (slot.IsEmpty) return;

        ItemData item = slot.item;

        // Equippable items — toggle equip on click
        if (item.equipSlot != EquipSlot.None && EquipmentManager.Instance != null)
        {
            bool wasEquipped = EquipmentManager.Instance.IsEquipped(item);
            EquipmentManager.Instance.Equip(item);
            SFXManager.PlayEquip();

            if (wasEquipped)
                ShowFeedback("Unequipped " + item.itemName);
            else
                ShowFeedback("Equipped " + item.itemName);

            RefreshUI();
            return;
        }

        // Consumable items
        if (item.useAction == ItemUseAction.None)
        {
            ShowFeedback("Can't use " + item.itemName);
            return;
        }

        PlayerVitals vitals = FindAnyObjectByType<PlayerVitals>();
        if (vitals == null) return;

        switch (item.useAction)
        {
            case ItemUseAction.EatFood:
                if (vitals.Hunger >= vitals.maxHunger)
                {
                    ShowFeedback("Not hungry");
                    return;
                }
                vitals.Eat(item.useValue);
                ShowFeedback("Ate " + item.itemName + " (+" + item.useValue + " hunger)");
                break;

            case ItemUseAction.UseBandage:
                if (vitals.Health >= vitals.maxHealth)
                {
                    ShowFeedback("Health is full");
                    return;
                }
                vitals.Health += item.useValue;
                ShowFeedback("Used " + item.itemName + " (+" + item.useValue + " health)");
                break;

            default:
                ShowFeedback("Can't use that here");
                return;
        }

        // Consume one from stack
        slot.quantity--;
        if (slot.quantity <= 0)
            slot.Clear();

        inventory.NotifyChanged();
    }

    void ShowFeedback(string msg)
    {
        if (tooltipText != null)
        {
            tooltipText.text = msg;
            feedbackTimer = 2f;
        }
    }
}
