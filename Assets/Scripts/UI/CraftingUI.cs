using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour
{
    [Header("Recipes")]
    [Tooltip("Leave empty to auto-generate default recipes.")]
    public CraftingRecipe[] recipes;

    private Inventory inventory;
    private GameObject panelObj;
    private bool isOpen = false;

    // UI references
    private GameObject recipeListContent;
    private RectTransform recipeContentRect;
    private TextMeshProUGUI detailTitle;
    private GameObject ingredientListObj;
    private TextMeshProUGUI resultText;
    private Button craftButton;
    private TextMeshProUGUI craftButtonText;

    // Category tabs
    private Button[] tabButtons;
    private ItemCategory? activeFilter = null;

    // Scroll
    private ScrollRect recipeScroll;

    // Currently selected recipe
    private CraftingRecipe selectedRecipe;

    void Start()
    {
        inventory = FindAnyObjectByType<Inventory>();

        if (inventory == null)
        {
            Debug.LogWarning("CraftingUI: No Inventory found. UI disabled.");
            enabled = false;
            return;
        }

        // Check if we have any valid (non-null) recipes assigned
        bool hasValidRecipes = false;
        if (recipes != null)
        {
            for (int i = 0; i < recipes.Length; i++)
            {
                if (recipes[i] != null) { hasValidRecipes = true; break; }
            }
        }

        if (!hasValidRecipes)
            CreateDefaultRecipes();

        BuildUI();
        panelObj.SetActive(false);

        inventory.OnInventoryChanged += OnInventoryChanged;
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= OnInventoryChanged;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isOpen) CloseCrafting();
            else OpenCrafting();
        }
    }

    void OpenCrafting()
    {
        isOpen = true;
        panelObj.SetActive(true);
        activeFilter = null;
        selectedRecipe = null;
        PopulateRecipeList();
        ClearDetail();
        RefreshTabColors();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerMovement.inputBlocked = true;

        // Stop player animation immediately so character doesn't keep walking
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            Animator anim = pm.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f);
                anim.SetFloat("Direction", 0f);
                anim.SetFloat("Vertical", 0f);
            }
        }
    }

    void CloseCrafting()
    {
        isOpen = false;
        panelObj.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerMovement.inputBlocked = false;
    }

    void OnInventoryChanged()
    {
        if (!isOpen) return;
        PopulateRecipeList();
        if (selectedRecipe != null)
            ShowRecipeDetail(selectedRecipe);
    }

    // ─── Recipe List ────────────────────────────────────────

    void PopulateRecipeList()
    {
        foreach (Transform child in recipeListContent.transform)
            Destroy(child.gameObject);

        int count = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            CraftingRecipe recipe = recipes[i];
            if (recipe == null) continue;
            if (activeFilter != null && recipe.category != activeFilter.Value)
                continue;

            CreateRecipeButton(recipe, count, CanCraft(recipe));
            count++;
        }

        // Resize content for scrolling and reset to top
        recipeContentRect.sizeDelta = new Vector2(0, count * 40 + 10);
        recipeScroll.verticalNormalizedPosition = 1f;
    }

    void CreateRecipeButton(CraftingRecipe recipe, int index, bool canCraft)
    {
        GameObject btnObj = new GameObject("Recipe_" + recipe.recipeName);
        btnObj.transform.SetParent(recipeListContent.transform, false);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = (selectedRecipe == recipe)
            ? new Color(0.4f, 0.4f, 0.15f, 0.9f)
            : new Color(0.25f, 0.25f, 0.25f, 0.9f);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0, 1);
        btnRect.anchorMax = new Vector2(1, 1);
        btnRect.pivot = new Vector2(0.5f, 1);
        btnRect.anchoredPosition = new Vector2(0, -index * 40);
        btnRect.sizeDelta = new Vector2(0, 36);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        string displayName = string.IsNullOrEmpty(recipe.recipeName) ? recipe.name : recipe.recipeName;
        text.text = displayName;
        text.fontSize = 16;
        text.color = canCraft ? new Color(0.8f, 1f, 0.8f) : new Color(0.7f, 0.7f, 0.7f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-5, 0);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        CraftingRecipe capturedRecipe = recipe;
        btn.onClick.AddListener(() => SelectRecipe(capturedRecipe));
    }

    void SelectRecipe(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        PopulateRecipeList();
        ShowRecipeDetail(recipe);
    }

    // ─── Detail Panel ───────────────────────────────────────

    void ClearDetail()
    {
        detailTitle.text = "Select a recipe";
        resultText.text = "";
        craftButton.gameObject.SetActive(false);

        foreach (Transform child in ingredientListObj.transform)
            Destroy(child.gameObject);
    }

    void ShowRecipeDetail(CraftingRecipe recipe)
    {
        string displayName = string.IsNullOrEmpty(recipe.recipeName) ? recipe.name : recipe.recipeName;
        detailTitle.text = displayName;

        foreach (Transform child in ingredientListObj.transform)
            Destroy(child.gameObject);

        bool allMet = true;
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            Ingredient ing = recipe.ingredients[i];
            int have = inventory.GetItemCount(ing.item);
            bool enough = have >= ing.quantity;
            if (!enough) allMet = false;

            CreateIngredientRow(ing, have, enough, i);
        }

        string resultName = recipe.result != null
            ? (string.IsNullOrEmpty(recipe.result.itemName) ? recipe.result.name : recipe.result.itemName)
            : "???";
        resultText.text = "Result: " + resultName + (recipe.resultQuantity > 1 ? " x" + recipe.resultQuantity : "");
        resultText.color = Color.white;

        craftButton.gameObject.SetActive(true);
        craftButton.interactable = allMet;
        craftButtonText.color = allMet ? Color.white : new Color(0.5f, 0.5f, 0.5f);
    }

    void CreateIngredientRow(Ingredient ing, int have, bool enough, int index)
    {
        GameObject rowObj = new GameObject("Ingredient_" + index);
        rowObj.transform.SetParent(ingredientListObj.transform, false);

        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.pivot = new Vector2(0.5f, 1);
        rowRect.anchoredPosition = new Vector2(0, -index * 28);
        rowRect.sizeDelta = new Vector2(0, 24);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(rowObj.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;

        string itemName = ing.item != null
            ? (string.IsNullOrEmpty(ing.item.itemName) ? ing.item.name : ing.item.itemName)
            : "???";

        text.text = itemName + "  " + have + " / " + ing.quantity;
        text.fontSize = 15;
        text.color = enough ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
        text.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-5, 0);
    }

    // ─── Craft Action ───────────────────────────────────────

    bool CanCraft(CraftingRecipe recipe)
    {
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            if (!inventory.HasItem(recipe.ingredients[i].item, recipe.ingredients[i].quantity))
                return false;
        }
        return true;
    }

    void DoCraft()
    {
        if (selectedRecipe == null || !CanCraft(selectedRecipe))
            return;

        for (int i = 0; i < selectedRecipe.ingredients.Length; i++)
        {
            Ingredient ing = selectedRecipe.ingredients[i];
            inventory.RemoveItem(ing.item, ing.quantity);
        }

        inventory.AddItem(selectedRecipe.result, selectedRecipe.resultQuantity);
        SFXManager.PlayCraft();
    }

    // ─── Category Tabs ──────────────────────────────────────

    void SetFilter(ItemCategory? filter)
    {
        activeFilter = filter;
        selectedRecipe = null;
        PopulateRecipeList();
        ClearDetail();
        RefreshTabColors();
    }

    void RefreshTabColors()
    {
        ItemCategory?[] filters = { null, ItemCategory.Tool, ItemCategory.Weapon, ItemCategory.Food, ItemCategory.Resource };
        for (int i = 0; i < tabButtons.Length && i < filters.Length; i++)
        {
            Image img = tabButtons[i].GetComponent<Image>();
            bool active = activeFilter == filters[i];
            img.color = active ? new Color(0.4f, 0.4f, 0.15f, 0.95f) : new Color(0.25f, 0.25f, 0.25f, 0.9f);
        }
    }

    // ─── Default Recipe Generation ──────────────────────────

    void CreateDefaultRecipes()
    {
        Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();

        // --- Raw resources ---
        items["Wood"]      = MakeItem("Wood",      ItemCategory.Resource, true, 20);
        items["Stone"]     = MakeItem("Stone",     ItemCategory.Resource, true, 20);
        items["Fiber"]     = MakeItem("Fiber",     ItemCategory.Resource, true, 20);
        items["Deer Hide"] = MakeItem("Deer Hide", ItemCategory.Resource, true, 10);
        items["Venison"]   = MakeItem("Venison",   ItemCategory.Food,     true, 5,  ItemUseAction.EatFood, 15f);
        items["Berries"]   = MakeItem("Berries",   ItemCategory.Food,     true, 15, ItemUseAction.EatFood, 5f);
        items["Herbs"]     = MakeItem("Herbs",     ItemCategory.Resource, true, 15);
        items["Bone"]      = MakeItem("Bone",      ItemCategory.Resource, true, 10);
        items["Feather"]   = MakeItem("Feather",   ItemCategory.Resource, true, 20);
        items["Iron Ore"]  = MakeItem("Iron Ore",  ItemCategory.Resource, true, 10);

        // --- Crafted items (with equip stats) ---
        items["Stone Axe"]      = MakeItem("Stone Axe",      ItemCategory.Tool,   false, 1, equipSlot: EquipSlot.Tool);
        items["Stone Pickaxe"]  = MakeItem("Stone Pickaxe",  ItemCategory.Tool,   false, 1, equipSlot: EquipSlot.Tool);
        items["Torch"]          = MakeItem("Torch",          ItemCategory.Tool,   true,  5, equipSlot: EquipSlot.Tool);
        items["Fishing Rod"]    = MakeItem("Fishing Rod",    ItemCategory.Tool,   false, 1, equipSlot: EquipSlot.Tool);
        items["Rope"]           = MakeItem("Rope",           ItemCategory.Tool,   true,  5);
        items["Campfire"]       = MakeItem("Campfire",       ItemCategory.Tool,   true,  3);
        items["Bone Needle"]    = MakeItem("Bone Needle",    ItemCategory.Tool,   true,  5);
        items["Wooden Spear"]   = MakeItem("Wooden Spear",   ItemCategory.Weapon, false, 1, equipSlot: EquipSlot.Weapon, damageBonus: 15f);
        items["Bow"]            = MakeItem("Bow",            ItemCategory.Weapon, false, 1, equipSlot: EquipSlot.Weapon, damageBonus: 20f);
        items["Arrow"]          = MakeItem("Arrow",          ItemCategory.Weapon, true,  20);
        items["Stone Knife"]    = MakeItem("Stone Knife",    ItemCategory.Weapon, false, 1, equipSlot: EquipSlot.Weapon, damageBonus: 10f);
        items["Cooked Venison"] = MakeItem("Cooked Venison", ItemCategory.Food,   true,  5, ItemUseAction.EatFood, 35f);
        items["Berry Stew"]     = MakeItem("Berry Stew",     ItemCategory.Food,   true,  5, ItemUseAction.EatFood, 25f);
        items["Herbal Tea"]     = MakeItem("Herbal Tea",     ItemCategory.Food,   true,  5, ItemUseAction.EatFood, 20f);
        items["Bandage"]        = MakeItem("Bandage",        ItemCategory.Resource, true, 10, ItemUseAction.UseBandage, 30f);
        items["Leather Armor"]  = MakeItem("Leather Armor",  ItemCategory.Resource, false, 1, equipSlot: EquipSlot.Armor, defenseBonus: 8f);
        items["Shelter Kit"]    = MakeItem("Shelter Kit",    ItemCategory.Resource, false, 1);
        items["Fur Bedroll"]    = MakeItem("Fur Bedroll",    ItemCategory.Resource, false, 1);

        // --- Recipes ---
        List<CraftingRecipe> list = new List<CraftingRecipe>();

        // Tools (7)
        list.Add(MakeRecipe("Stone Axe", ItemCategory.Tool,
            new Ingredient[] { Ing(items["Stone"], 2), Ing(items["Wood"], 3) },
            items["Stone Axe"], 1));

        list.Add(MakeRecipe("Stone Pickaxe", ItemCategory.Tool,
            new Ingredient[] { Ing(items["Stone"], 3), Ing(items["Wood"], 2) },
            items["Stone Pickaxe"], 1));

        list.Add(MakeRecipe("Torch", ItemCategory.Tool,
            new Ingredient[] { Ing(items["Wood"], 1), Ing(items["Fiber"], 1) },
            items["Torch"], 1));

        list.Add(MakeRecipe("Fishing Rod", ItemCategory.Tool,
            new Ingredient[] { Ing(items["Wood"], 3), Ing(items["Fiber"], 2) },
            items["Fishing Rod"], 1));

        list.Add(MakeRecipe("Rope", ItemCategory.Tool,
            new Ingredient[] { Ing(items["Fiber"], 4) },
            items["Rope"], 1));

        list.Add(MakeRecipe("Campfire", ItemCategory.Tool,
            new Ingredient[] { Ing(items["Wood"], 5), Ing(items["Stone"], 3) },
            items["Campfire"], 1));

        list.Add(MakeRecipe("Bone Needle", ItemCategory.Tool,
            new Ingredient[] { Ing(items["Bone"], 1), Ing(items["Stone"], 1) },
            items["Bone Needle"], 1));

        // Weapons (4)
        list.Add(MakeRecipe("Wooden Spear", ItemCategory.Weapon,
            new Ingredient[] { Ing(items["Wood"], 3), Ing(items["Stone"], 1) },
            items["Wooden Spear"], 1));

        list.Add(MakeRecipe("Bow", ItemCategory.Weapon,
            new Ingredient[] { Ing(items["Wood"], 2), Ing(items["Fiber"], 3) },
            items["Bow"], 1));

        list.Add(MakeRecipe("Arrows", ItemCategory.Weapon,
            new Ingredient[] { Ing(items["Wood"], 1), Ing(items["Feather"], 1), Ing(items["Stone"], 1) },
            items["Arrow"], 5));

        list.Add(MakeRecipe("Stone Knife", ItemCategory.Weapon,
            new Ingredient[] { Ing(items["Stone"], 2), Ing(items["Wood"], 1) },
            items["Stone Knife"], 1));

        // Food (3)
        list.Add(MakeRecipe("Cooked Venison", ItemCategory.Food,
            new Ingredient[] { Ing(items["Venison"], 1), Ing(items["Campfire"], 1) },
            items["Cooked Venison"], 2));

        list.Add(MakeRecipe("Berry Stew", ItemCategory.Food,
            new Ingredient[] { Ing(items["Berries"], 3), Ing(items["Herbs"], 1) },
            items["Berry Stew"], 1));

        list.Add(MakeRecipe("Herbal Tea", ItemCategory.Food,
            new Ingredient[] { Ing(items["Herbs"], 2), Ing(items["Berries"], 1) },
            items["Herbal Tea"], 1));

        // Survival / Resource (4)
        list.Add(MakeRecipe("Bandage", ItemCategory.Resource,
            new Ingredient[] { Ing(items["Fiber"], 2), Ing(items["Herbs"], 1) },
            items["Bandage"], 2));

        list.Add(MakeRecipe("Leather Armor", ItemCategory.Resource,
            new Ingredient[] { Ing(items["Deer Hide"], 4), Ing(items["Fiber"], 2) },
            items["Leather Armor"], 1));

        list.Add(MakeRecipe("Shelter Kit", ItemCategory.Resource,
            new Ingredient[] { Ing(items["Wood"], 8), Ing(items["Fiber"], 4), Ing(items["Deer Hide"], 2) },
            items["Shelter Kit"], 1));

        list.Add(MakeRecipe("Fur Bedroll", ItemCategory.Resource,
            new Ingredient[] { Ing(items["Deer Hide"], 3), Ing(items["Fiber"], 2) },
            items["Fur Bedroll"], 1));

        recipes = list.ToArray();
        Debug.Log("CraftingUI: Generated " + recipes.Length + " default recipes.");
    }

    ItemData MakeItem(string itemName, ItemCategory cat, bool stackable, int maxStack,
        ItemUseAction useAction = ItemUseAction.None, float useValue = 0f,
        EquipSlot equipSlot = EquipSlot.None, float damageBonus = 0f, float defenseBonus = 0f)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.name = itemName;
        item.itemName = itemName;
        item.category = cat;
        item.isStackable = stackable;
        item.maxStack = maxStack;
        item.useAction = useAction;
        item.useValue = useValue;
        item.equipSlot = equipSlot;
        item.damageBonus = damageBonus;
        item.defenseBonus = defenseBonus;
        ItemRegistry.Register(item);
        return item;
    }

    CraftingRecipe MakeRecipe(string name, ItemCategory cat, Ingredient[] ingredients, ItemData result, int qty)
    {
        CraftingRecipe recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
        recipe.name = name;
        recipe.recipeName = name;
        recipe.category = cat;
        recipe.ingredients = ingredients;
        recipe.result = result;
        recipe.resultQuantity = qty;
        return recipe;
    }

    Ingredient Ing(ItemData item, int qty)
    {
        return new Ingredient { item = item, quantity = qty };
    }

    // ─── Build All UI ───────────────────────────────────────

    void BuildUI()
    {
        // Ensure an EventSystem exists (required for ALL button clicks)
        if (EventSystem.current == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Canvas
        GameObject canvasObj = new GameObject("CraftingCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 91;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dark overlay
        panelObj = new GameObject("CraftingPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.75f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Main container (centered, 700x450)
        GameObject container = new GameObject("Container");
        container.transform.SetParent(panelObj.transform, false);
        Image containerBg = container.AddComponent<Image>();
        containerBg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        containerBg.raycastTarget = false;
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(700, 450);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(container.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "Crafting";
        title.fontSize = 26;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        title.raycastTarget = false;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -8);
        titleRect.sizeDelta = new Vector2(0, 30);

        // Build panels first, then tabs LAST so tabs are on top for raycasting
        BuildRecipeListPanel(container.transform);
        BuildDetailPanel(container.transform);
        BuildCategoryTabs(container.transform);
    }

    void BuildCategoryTabs(Transform parent)
    {
        GameObject tabRow = new GameObject("TabRow");
        tabRow.transform.SetParent(parent, false);
        RectTransform tabRowRect = tabRow.AddComponent<RectTransform>();
        tabRowRect.anchorMin = new Vector2(0, 1);
        tabRowRect.anchorMax = new Vector2(1, 1);
        tabRowRect.pivot = new Vector2(0.5f, 1);
        tabRowRect.anchoredPosition = new Vector2(0, -42);
        tabRowRect.sizeDelta = new Vector2(-20, 28);

        string[] tabNames = { "All", "Tools", "Weapons", "Food", "Resources" };
        ItemCategory?[] filters = { null, ItemCategory.Tool, ItemCategory.Weapon, ItemCategory.Food, ItemCategory.Resource };
        tabButtons = new Button[tabNames.Length];

        float tabWidth = 1f / tabNames.Length;

        for (int i = 0; i < tabNames.Length; i++)
        {
            GameObject tabObj = new GameObject("Tab_" + tabNames[i]);
            tabObj.transform.SetParent(tabRow.transform, false);

            Image tabBg = tabObj.AddComponent<Image>();
            bool isActive = (i == 0);
            tabBg.color = isActive ? new Color(0.4f, 0.4f, 0.15f, 0.95f) : new Color(0.25f, 0.25f, 0.25f, 0.9f);

            RectTransform tabRect = tabObj.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(tabWidth * i, 0);
            tabRect.anchorMax = new Vector2(tabWidth * (i + 1), 1);
            tabRect.offsetMin = new Vector2(2, 0);
            tabRect.offsetMax = new Vector2(-2, 0);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tabObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = tabNames[i];
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Button btn = tabObj.AddComponent<Button>();
            btn.targetGraphic = tabBg;
            tabButtons[i] = btn;

            ItemCategory? capturedFilter = filters[i];
            btn.onClick.AddListener(() => SetFilter(capturedFilter));
        }
    }

    void BuildRecipeListPanel(Transform parent)
    {
        GameObject leftPanel = new GameObject("RecipeListPanel");
        leftPanel.transform.SetParent(parent, false);
        Image leftBg = leftPanel.AddComponent<Image>();
        leftBg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        leftBg.raycastTarget = false;
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.45f, 1);
        leftRect.offsetMin = new Vector2(10, 10);
        leftRect.offsetMax = new Vector2(-5, -75);

        // Scroll view
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(leftPanel.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(5, 5);
        scrollRect.offsetMax = new Vector2(-5, -5);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scrollObj.AddComponent<RectMask2D>(); // clip by rect bounds, no stencil needed

        // Content container
        recipeListContent = new GameObject("Content");
        recipeListContent.transform.SetParent(scrollObj.transform, false);
        recipeContentRect = recipeListContent.AddComponent<RectTransform>();
        recipeContentRect.anchorMin = new Vector2(0, 1);
        recipeContentRect.anchorMax = new Vector2(1, 1);
        recipeContentRect.pivot = new Vector2(0.5f, 1);
        recipeContentRect.anchoredPosition = Vector2.zero;

        float contentHeight = recipes != null ? recipes.Length * 40 + 10 : 200;
        recipeContentRect.sizeDelta = new Vector2(0, contentHeight);

        scroll.content = recipeContentRect;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;
        recipeScroll = scroll;
    }

    void BuildDetailPanel(Transform parent)
    {
        GameObject rightPanel = new GameObject("DetailPanel");
        rightPanel.transform.SetParent(parent, false);
        Image rightBg = rightPanel.AddComponent<Image>();
        rightBg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        rightBg.raycastTarget = false;
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.45f, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = new Vector2(5, 10);
        rightRect.offsetMax = new Vector2(-10, -75);

        // Recipe title
        GameObject titleObj = new GameObject("RecipeTitle");
        titleObj.transform.SetParent(rightPanel.transform, false);
        detailTitle = titleObj.AddComponent<TextMeshProUGUI>();
        detailTitle.text = "Select a recipe";
        detailTitle.fontSize = 20;
        detailTitle.color = Color.white;
        detailTitle.alignment = TextAlignmentOptions.TopLeft;
        detailTitle.raycastTarget = false;
        RectTransform dtRect = detailTitle.rectTransform;
        dtRect.anchorMin = new Vector2(0, 1);
        dtRect.anchorMax = new Vector2(1, 1);
        dtRect.pivot = new Vector2(0.5f, 1);
        dtRect.anchoredPosition = new Vector2(0, -10);
        dtRect.sizeDelta = new Vector2(-20, 28);

        // Ingredients header
        GameObject ingHeaderObj = new GameObject("IngredientsHeader");
        ingHeaderObj.transform.SetParent(rightPanel.transform, false);
        TextMeshProUGUI ingHeader = ingHeaderObj.AddComponent<TextMeshProUGUI>();
        ingHeader.text = "Ingredients:";
        ingHeader.fontSize = 15;
        ingHeader.color = new Color(0.8f, 0.8f, 0.8f);
        ingHeader.alignment = TextAlignmentOptions.TopLeft;
        ingHeader.raycastTarget = false;
        RectTransform ihRect = ingHeader.rectTransform;
        ihRect.anchorMin = new Vector2(0, 1);
        ihRect.anchorMax = new Vector2(1, 1);
        ihRect.pivot = new Vector2(0.5f, 1);
        ihRect.anchoredPosition = new Vector2(0, -44);
        ihRect.sizeDelta = new Vector2(-20, 20);

        // Ingredient list container
        ingredientListObj = new GameObject("IngredientList");
        ingredientListObj.transform.SetParent(rightPanel.transform, false);
        RectTransform ingListRect = ingredientListObj.AddComponent<RectTransform>();
        ingListRect.anchorMin = new Vector2(0, 1);
        ingListRect.anchorMax = new Vector2(1, 1);
        ingListRect.pivot = new Vector2(0.5f, 1);
        ingListRect.anchoredPosition = new Vector2(0, -68);
        ingListRect.sizeDelta = new Vector2(-20, 150);

        // Result text
        GameObject resultObj = new GameObject("ResultText");
        resultObj.transform.SetParent(rightPanel.transform, false);
        resultText = resultObj.AddComponent<TextMeshProUGUI>();
        resultText.text = "";
        resultText.fontSize = 16;
        resultText.color = Color.white;
        resultText.alignment = TextAlignmentOptions.MidlineLeft;
        resultText.raycastTarget = false;
        RectTransform rtRect = resultText.rectTransform;
        rtRect.anchorMin = new Vector2(0, 0);
        rtRect.anchorMax = new Vector2(1, 0);
        rtRect.pivot = new Vector2(0.5f, 0);
        rtRect.anchoredPosition = new Vector2(0, 55);
        rtRect.sizeDelta = new Vector2(-20, 24);

        // Craft button
        GameObject craftBtnObj = new GameObject("CraftButton");
        craftBtnObj.transform.SetParent(rightPanel.transform, false);
        Image craftBtnBg = craftBtnObj.AddComponent<Image>();
        craftBtnBg.color = new Color(0.2f, 0.45f, 0.2f, 1f);
        RectTransform cbRect = craftBtnObj.GetComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(0.5f, 0);
        cbRect.anchorMax = new Vector2(0.5f, 0);
        cbRect.pivot = new Vector2(0.5f, 0);
        cbRect.anchoredPosition = new Vector2(0, 15);
        cbRect.sizeDelta = new Vector2(160, 36);

        GameObject craftTextObj = new GameObject("Text");
        craftTextObj.transform.SetParent(craftBtnObj.transform, false);
        craftButtonText = craftTextObj.AddComponent<TextMeshProUGUI>();
        craftButtonText.text = "Craft";
        craftButtonText.fontSize = 18;
        craftButtonText.color = Color.white;
        craftButtonText.alignment = TextAlignmentOptions.Center;
        craftButtonText.raycastTarget = false;
        RectTransform ctRect = craftButtonText.rectTransform;
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.offsetMin = Vector2.zero;
        ctRect.offsetMax = Vector2.zero;

        craftButton = craftBtnObj.AddComponent<Button>();
        craftButton.targetGraphic = craftBtnBg;

        ColorBlock colors = craftButton.colors;
        colors.normalColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        craftButton.colors = colors;

        craftButton.onClick.AddListener(DoCraft);
        craftBtnObj.SetActive(false);
    }
}
