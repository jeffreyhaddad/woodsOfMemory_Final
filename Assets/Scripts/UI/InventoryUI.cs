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
    private int columns = 5;
    private int rows = 6;
    private float slotSize = 60f;
    private float slotSpacing = 5f;

    private GameObject panelObj;
    private RectTransform panelRect;
    private RectTransform gridRect;
    private Image[] slotImages;
    private Image[] iconImages;
    private TextMeshProUGUI[] quantityTexts;
    private GameObject[] slotObjects;
    private bool isOpen = false;

    // Tooltip / use feedback
    private TextMeshProUGUI tooltipText;
    private float feedbackTimer;

    // Drag-to-reorder state
    private int dragFromSlot = -1;
    private ItemData dragItem;
    private int dragQuantity;
    private GameObject heldIconObj;
    private Image heldIconImage;
    private TextMeshProUGUI heldQtyText;

    // Hover tracking (for Q-drop)
    private int hoveredSlotIndex = -1;

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
        if (Input.GetKeyDown(SettingsManager.GetKey(GameAction.Inventory)))
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

        // Follow cursor while dragging
        if (dragItem != null && heldIconObj != null)
            heldIconObj.transform.position = Input.mousePosition;

        // Q = drop hovered slot
        if (isOpen && Input.GetKeyDown(KeyCode.Q))
        {
            if (dragItem != null)
                DropDraggedItem();
            else if (hoveredSlotIndex >= 0)
                DropSlot(hoveredSlotIndex);
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

        ShowFeedback("Drag to reorder  |  Drag outside to drop  |  Click to use");
    }

    void CloseInventory()
    {
        // If dragging, return item to its source slot
        if (dragItem != null)
            ReturnDragToSource();

        isOpen = false;
        panelObj.SetActive(false);
        hoveredSlotIndex = -1;

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
            bool isHovered = (i == hoveredSlotIndex);

            if (slot.IsEmpty)
            {
                slotImages[i].color = isHovered
                    ? new Color(0.3f, 0.3f, 0.3f, 0.9f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.8f);
                iconImages[i].enabled = false;
                quantityTexts[i].text = "";
                quantityTexts[i].fontSize = 14;
                quantityTexts[i].alignment = TextAlignmentOptions.BottomRight;
            }
            else
            {
                bool isEquipped = equip != null && equip.IsEquipped(slot.item);

                Color baseColor = isEquipped
                    ? new Color(0.6f, 0.5f, 0.1f, 0.95f)
                    : new Color(0.35f, 0.35f, 0.35f, 0.9f);

                slotImages[i].color = isHovered ? baseColor * 1.35f : baseColor;

                // If icon isn't set yet, let ItemRegistry try to load it now.
                // Covers items that bypass the normal Register() flow (e.g. inspector-assigned
                // ItemData on PickupItem components placed directly in the scene).
                if (slot.item.icon == null)
                    ItemRegistry.Register(slot.item);

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
                    iconImages[i].enabled = false;
                    quantityTexts[i].fontSize = 10;
                    quantityTexts[i].alignment = TextAlignmentOptions.Center;
                    quantityTexts[i].textWrappingMode = TMPro.TextWrappingModes.Normal;
                    string displayName = string.IsNullOrEmpty(slot.item.itemName)
                        ? slot.item.name
                        : slot.item.itemName;
                    string equipped = isEquipped ? " [E]" : "";
                    string qty = slot.quantity > 1 ? "\nx" + slot.quantity.ToString() : "";
                    quantityTexts[i].text = string.Concat(displayName, equipped, qty);
                }
            }
        }
    }

    void BuildUI()
    {
        int totalSlots = columns * rows;
        slotImages    = new Image[totalSlots];
        iconImages    = new Image[totalSlots];
        quantityTexts = new TextMeshProUGUI[totalSlots];
        slotObjects   = new GameObject[totalSlots];

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
        panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Grid container (centered)
        float gridWidth  = columns * (slotSize + slotSpacing) - slotSpacing;
        float gridHeight = rows    * (slotSize + slotSpacing) - slotSpacing;

        GameObject gridObj = new GameObject("Grid");
        gridObj.transform.SetParent(panelObj.transform, false);
        gridRect = gridObj.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot     = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(gridWidth, gridHeight + 40f);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(gridObj.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text      = "Inventory";
        title.fontSize  = 28;
        title.alignment = TextAlignmentOptions.Center;
        title.color     = Color.white;
        title.raycastTarget = false;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin       = new Vector2(0, 1);
        titleRect.anchorMax       = new Vector2(1, 1);
        titleRect.pivot           = new Vector2(0.5f, 0);
        titleRect.anchoredPosition = new Vector2(0, 3);
        titleRect.sizeDelta       = new Vector2(0, 20);

        // Slot grid
        for (int i = 0; i < totalSlots; i++)
        {
            int col = i % columns;
            int row = i / columns;

            float x = col * (slotSize + slotSpacing) - gridWidth  / 2f + slotSize / 2f;
            float y = -row * (slotSize + slotSpacing) + gridHeight / 2f - slotSize / 2f;

            // Slot background
            GameObject slotObj = new GameObject("Slot_" + i);
            slotObj.transform.SetParent(gridObj.transform, false);
            Image slotImg = slotObj.AddComponent<Image>();
            slotImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.anchorMin       = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax       = new Vector2(0.5f, 0.5f);
            slotRect.pivot           = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta       = new Vector2(slotSize, slotSize);
            slotRect.anchoredPosition = new Vector2(x, y);
            slotImages[i]  = slotImg;
            slotObjects[i] = slotObj;

            // Item icon (raycastTarget off so hits go to slotObj)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            Image icon = iconObj.AddComponent<Image>();
            icon.enabled       = false;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4, 4);
            iconRect.offsetMax = new Vector2(-4, -4);
            iconImages[i] = icon;

            // Quantity text
            GameObject qtyObj = new GameObject("Qty");
            qtyObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI qty = qtyObj.AddComponent<TextMeshProUGUI>();
            qty.fontSize       = 14;
            qty.alignment      = TextAlignmentOptions.BottomRight;
            qty.color          = Color.white;
            qty.text           = "";
            qty.raycastTarget  = false;
            RectTransform qtyRect = qty.rectTransform;
            qtyRect.anchorMin = Vector2.zero;
            qtyRect.anchorMax = Vector2.one;
            qtyRect.offsetMin = new Vector2(2, 2);
            qtyRect.offsetMax = new Vector2(-4, -2);
            quantityTexts[i] = qty;

            // EventTrigger: click, drag, hover
            EventTrigger trigger = slotObj.AddComponent<EventTrigger>();
            int slotIndex = i;

            // Click (fires only when NO drag occurred)
            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener((data) => UseItem(slotIndex));
            trigger.triggers.Add(clickEntry);

            // Begin drag
            var beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDragEntry.callback.AddListener((data) => OnBeginDrag(slotIndex, (PointerEventData)data));
            trigger.triggers.Add(beginDragEntry);

            // Drag
            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => OnDrag((PointerEventData)data));
            trigger.triggers.Add(dragEntry);

            // End drag
            var endDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDragEntry.callback.AddListener((data) => OnEndDrag((PointerEventData)data));
            trigger.triggers.Add(endDragEntry);

            // Hover enter
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => { hoveredSlotIndex = slotIndex; RefreshUI(); });
            trigger.triggers.Add(enterEntry);

            // Hover exit
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) => { if (hoveredSlotIndex == slotIndex) { hoveredSlotIndex = -1; RefreshUI(); } });
            trigger.triggers.Add(exitEntry);
        }

        // Tooltip / feedback text at bottom
        GameObject tipObj = new GameObject("Tooltip");
        tipObj.transform.SetParent(gridObj.transform, false);
        tooltipText = tipObj.AddComponent<TextMeshProUGUI>();
        tooltipText.text       = "";
        tooltipText.fontSize   = 15;
        tooltipText.alignment  = TextAlignmentOptions.Center;
        tooltipText.color      = new Color(0.9f, 0.9f, 0.5f);
        tooltipText.raycastTarget = false;
        RectTransform tipRect = tooltipText.rectTransform;
        tipRect.anchorMin       = new Vector2(0, 0);
        tipRect.anchorMax       = new Vector2(1, 0);
        tipRect.pivot           = new Vector2(0.5f, 1);
        tipRect.anchoredPosition = new Vector2(0, -10);
        tipRect.sizeDelta       = new Vector2(0, 30);

        // Floating held-item icon — child of canvas (not panel) so it's always on top
        heldIconObj = new GameObject("HeldIcon");
        heldIconObj.transform.SetParent(canvasObj.transform, false);
        RectTransform heldRect = heldIconObj.AddComponent<RectTransform>();
        heldRect.sizeDelta = new Vector2(slotSize, slotSize);
        heldRect.pivot     = new Vector2(0.5f, 0.5f);
        heldIconImage                = heldIconObj.AddComponent<Image>();
        heldIconImage.preserveAspect = true;
        heldIconImage.raycastTarget  = false;
        heldIconImage.color          = new Color(1f, 1f, 1f, 0.85f);

        GameObject heldQtyObj = new GameObject("HeldQty");
        heldQtyObj.transform.SetParent(heldIconObj.transform, false);
        heldQtyText               = heldQtyObj.AddComponent<TextMeshProUGUI>();
        heldQtyText.fontSize      = 14;
        heldQtyText.alignment     = TextAlignmentOptions.BottomRight;
        heldQtyText.color         = Color.white;
        heldQtyText.raycastTarget = false;
        RectTransform heldQtyRect = heldQtyText.rectTransform;
        heldQtyRect.anchorMin = Vector2.zero;
        heldQtyRect.anchorMax = Vector2.one;
        heldQtyRect.offsetMin = new Vector2(2, 2);
        heldQtyRect.offsetMax = new Vector2(-4, -2);

        heldIconObj.SetActive(false);
    }

    // ─── Drag handlers ────────────────────────────────────────────────────────

    void OnBeginDrag(int slotIndex, PointerEventData eventData)
    {
        InventorySlot slot = inventory.slots[slotIndex];
        if (slot.IsEmpty) return;

        dragFromSlot = slotIndex;
        dragItem     = slot.item;
        dragQuantity = slot.quantity;
        slot.Clear();
        inventory.NotifyChanged();

        // Show floating icon
        heldIconImage.sprite  = dragItem.icon;
        heldIconImage.enabled = dragItem.icon != null;
        heldQtyText.text      = dragQuantity > 1 ? dragQuantity.ToString() : "";
        heldIconObj.transform.position = eventData.position;
        heldIconObj.SetActive(true);
    }

    void OnDrag(PointerEventData eventData)
    {
        if (heldIconObj != null)
            heldIconObj.transform.position = eventData.position;
    }

    void OnEndDrag(PointerEventData eventData)
    {
        if (dragItem == null) return;

        // Find which slot (if any) the cursor is over
        int targetSlot = GetSlotIndex(eventData.pointerEnter);

        if (targetSlot >= 0)
        {
            // Dropped on a valid slot — reorder
            PlaceDragOnSlot(targetSlot);
        }
        else if (CursorIsInPanel(eventData.position))
        {
            // Dropped on panel background — return to source
            ReturnDragToSource();
        }
        else
        {
            // Dropped outside the inventory — drop to world
            DropDraggedItem();
        }
    }

    void PlaceDragOnSlot(int targetIndex)
    {
        InventorySlot target = inventory.slots[targetIndex];

        if (target.IsEmpty)
        {
            target.item     = dragItem;
            target.quantity = dragQuantity;
        }
        else if (SameItem(target.item, dragItem) && dragItem.isStackable)
        {
            int space = dragItem.maxStack - target.quantity;
            int toAdd = Mathf.Min(space, dragQuantity);
            target.quantity += toAdd;
            int leftover = dragQuantity - toAdd;
            if (leftover > 0)
            {
                // Remaining goes back to source slot
                inventory.slots[dragFromSlot].item     = dragItem;
                inventory.slots[dragFromSlot].quantity = leftover;
            }
        }
        else
        {
            // Swap
            inventory.slots[dragFromSlot].item     = target.item;
            inventory.slots[dragFromSlot].quantity = target.quantity;
            target.item     = dragItem;
            target.quantity = dragQuantity;
        }

        ClearDrag();
        inventory.NotifyChanged();
    }

    void ReturnDragToSource()
    {
        if (dragItem == null) return;
        inventory.slots[dragFromSlot].item     = dragItem;
        inventory.slots[dragFromSlot].quantity = dragQuantity;
        ClearDrag();
        inventory.NotifyChanged();
    }

    void DropDraggedItem()
    {
        if (dragItem == null) return;
        string name = dragItem.itemName;
        SpawnDroppedItem(dragItem, dragQuantity);
        ClearDrag();
        inventory.NotifyChanged();
        ShowFeedback("Dropped " + name);
    }

    void DropSlot(int slotIndex)
    {
        InventorySlot slot = inventory.slots[slotIndex];
        if (slot.IsEmpty) return;
        string name = slot.item.itemName;
        SpawnDroppedItem(slot.item, slot.quantity);
        slot.Clear();
        inventory.NotifyChanged();
        ShowFeedback("Dropped " + name);
    }

    void ClearDrag()
    {
        dragItem     = null;
        dragQuantity = 0;
        dragFromSlot = -1;
        if (heldIconObj != null)
            heldIconObj.SetActive(false);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    int GetSlotIndex(GameObject go)
    {
        if (go == null) return -1;
        Transform t = go.transform;
        while (t != null)
        {
            for (int i = 0; i < slotObjects.Length; i++)
                if (slotObjects[i] == t.gameObject)
                    return i;
            t = t.parent;
        }
        return -1;
    }

    bool CursorIsInPanel(Vector2 screenPos)
    {
        if (gridRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(gridRect, screenPos, null);
    }

    static bool SameItem(ItemData a, ItemData b)
    {
        if (a == b) return true;
        if (a == null || b == null) return false;
        return !string.IsNullOrEmpty(a.itemName) && a.itemName == b.itemName;
    }

    // ─── Drop into world ──────────────────────────────────────────────────────

    void SpawnDroppedItem(ItemData item, int quantity)
    {
        PlayerVitals vitals = FindAnyObjectByType<PlayerVitals>();
        Vector3 pos = vitals != null
            ? vitals.transform.position + vitals.transform.forward * 1.2f + Vector3.up * 0.5f
            : Vector3.zero;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Dropped_" + item.itemName;
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 0.25f;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = CategoryColor(item.category);
        go.GetComponent<Renderer>().material = mat;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        if (vitals != null)
            rb.AddForce(vitals.transform.forward * 3f + Vector3.up * 2f, ForceMode.VelocityChange);

        DroppedItem dropped = go.AddComponent<DroppedItem>();
        dropped.item     = item;
        dropped.quantity = quantity;
    }

    static Color CategoryColor(ItemCategory cat)
    {
        switch (cat)
        {
            case ItemCategory.Resource:  return new Color(0.6f, 0.4f, 0.2f);
            case ItemCategory.Tool:      return new Color(0.5f, 0.5f, 0.6f);
            case ItemCategory.Weapon:    return new Color(0.7f, 0.2f, 0.2f);
            case ItemCategory.Food:      return new Color(0.3f, 0.7f, 0.3f);
            case ItemCategory.QuestItem: return new Color(0.8f, 0.7f, 0.1f);
            default:                     return Color.white;
        }
    }

    // ─── Use item (click without drag) ────────────────────────────────────────

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

            ShowFeedback(wasEquipped ? "Unequipped " + item.itemName : "Equipped " + item.itemName);
            RefreshUI();
            return;
        }

        // Consumable items
        if (item.useAction == ItemUseAction.None)
        {
            string msg = item.itemName == "Venison"
                ? "Cook Venison before eating"
                : "Can't use " + item.itemName;
            ShowFeedback(msg);
            return;
        }

        PlayerVitals vitals = FindAnyObjectByType<PlayerVitals>();
        if (vitals == null) return;

        switch (item.useAction)
        {
            case ItemUseAction.EatFood:
                if (vitals.Hunger >= vitals.maxHunger) { ShowFeedback("Not hungry"); return; }
                vitals.Eat(item.useValue);
                ShowFeedback("Ate " + item.itemName + " (+" + item.useValue + " hunger)");
                break;

            case ItemUseAction.UseBandage:
                if (vitals.Health >= vitals.maxHealth) { ShowFeedback("Health is full"); return; }
                vitals.Health += item.useValue;
                ShowFeedback("Used " + item.itemName + " (+" + item.useValue + " health)");
                break;

            case ItemUseAction.PlaceCampfire:
                PlaceCampfire(vitals.transform);
                ShowFeedback("Placed campfire");
                break;

            default:
                ShowFeedback("Can't use that here");
                return;
        }

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
            feedbackTimer = 3f;
        }
    }

    // ─── Campfire placement ───────────────────────────────────────────────────

    void PlaceCampfire(Transform player)
    {
        Vector3 pos = player.position + player.forward * 1.5f;

        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f))
            pos.y = hit.point.y;

        GameObject campfire = new GameObject("Campfire");
        campfire.transform.position = pos;

        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.transform.SetParent(campfire.transform, false);
        baseObj.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
        Destroy(baseObj.GetComponent<Collider>());
        baseObj.GetComponent<Renderer>().sharedMaterial = GetCampfireBaseMat();

        for (int i = 0; i < 4; i++)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.transform.SetParent(campfire.transform, false);
            log.transform.localScale = new Vector3(0.08f, 0.3f, 0.08f);
            float angle = i * 90f + Random.Range(-20f, 20f);
            log.transform.localPosition = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * 0.15f,
                0.1f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * 0.15f);
            log.transform.localRotation = Quaternion.Euler(Random.Range(60f, 80f), angle, 0f);
            Destroy(log.GetComponent<Collider>());
            log.GetComponent<Renderer>().sharedMaterial = GetCampfireLogMat();
        }

        GameObject lightObj = new GameObject("CampfireLight");
        lightObj.transform.SetParent(campfire.transform, false);
        lightObj.transform.localPosition = Vector3.up * 0.5f;
        Light fireLight = lightObj.AddComponent<Light>();
        fireLight.type      = LightType.Point;
        fireLight.color     = new Color(1f, 0.6f, 0.2f);
        fireLight.intensity = 2.5f;
        fireLight.range     = 15f;
        fireLight.shadows   = LightShadows.Soft;
        CampfireFlicker flicker = lightObj.AddComponent<CampfireFlicker>();
        flicker.baseIntensity = 2.5f;

        GameObject particleObj = new GameObject("CampfireFire");
        particleObj.transform.SetParent(campfire.transform, false);
        particleObj.transform.localPosition = Vector3.up * 0.15f;
        CreateCampfireParticles(particleObj);

        AudioSource audio = campfire.AddComponent<AudioSource>();
        audio.spatialBlend = 1f;
        audio.minDistance  = 2f;
        audio.maxDistance  = 15f;
        audio.loop         = true;
        audio.volume       = 0.4f;
        audio.clip         = GenerateCrackleClip();
        audio.Play();

        SFXManager.PlayCraft();
    }

    // ─── Campfire static caches ───────────────────────────────────────────────

    private static Material  s_campfireBaseMat;
    private static Material  s_campfireLogMat;
    private static AudioClip s_crackleClip;

    static Material GetCampfireBaseMat()
    {
        if (s_campfireBaseMat == null)
        {
            s_campfireBaseMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            s_campfireBaseMat.color = new Color(0.15f, 0.1f, 0.05f);
        }
        return s_campfireBaseMat;
    }

    static Material GetCampfireLogMat()
    {
        if (s_campfireLogMat == null)
        {
            s_campfireLogMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            s_campfireLogMat.color = new Color(0.35f, 0.2f, 0.08f);
        }
        return s_campfireLogMat;
    }

    AudioClip GenerateCrackleClip()
    {
        if (s_crackleClip != null) return s_crackleClip;

        int sampleRate = 22050;
        int length = sampleRate * 3;
        float[] samples = new float[length];

        Random.State prevState = Random.state;
        Random.InitState(42);

        float lastSample = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            float white = Random.Range(-1f, 1f);
            lastSample = (lastSample + 0.05f * white) / 1.05f;
            float pop = (Random.value > 0.997f) ? Random.Range(0.2f, 0.5f) * (Random.value > 0.5f ? 1f : -1f) : 0f;
            float rumble = Mathf.Sin(2f * Mathf.PI * 30f * t) * 0.05f;
            samples[i] = Mathf.Clamp(lastSample * 2f + pop + rumble, -1f, 1f);
        }

        Random.state = prevState;

        s_crackleClip = AudioClip.Create("Crackle", length, 1, sampleRate, false);
        s_crackleClip.SetData(samples, 0);
        return s_crackleClip;
    }

    void CreateCampfireParticles(GameObject obj)
    {
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.maxParticles  = 50;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(0.8f, 2f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0.2f, 0.9f),
            new Color(1f, 0.3f, 0.05f, 0.7f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.5f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 40;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 20f;
        shape.radius    = 0.1f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve shrink = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, shrink);

        ParticleSystem.ColorOverLifetimeModule colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.6f),
                new GradientColorKey(new Color(0.2f, 0.05f, 0.02f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.5f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLife.color = grad;

        ParticleSystemRenderer rend = obj.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader != null)
        {
            Material mat = new Material(particleShader);
            mat.SetColor("_BaseColor", new Color(1f, 0.7f, 0.3f, 0.8f));
            mat.SetFloat("_Surface", 1);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.renderQueue = 3000;
            rend.material = mat;
        }
    }
}
