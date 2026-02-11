using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class JournalUI : MonoBehaviour
{
    private JournalManager journal;
    private GameObject panelObj;
    private bool isOpen = false;

    // UI references
    private GameObject entryListContent;
    private RectTransform entryContentRect;
    private ScrollRect entryScroll;
    private TextMeshProUGUI detailTitle;
    private TextMeshProUGUI detailBody;
    private ScrollRect detailScroll;

    // Category tabs
    private Button[] tabButtons;
    private JournalCategory? activeFilter = null;

    private JournalEntry selectedEntry;

    // Notification
    private TextMeshProUGUI notifText;
    private Image notifBg;
    private float notifTimer;

    void Start()
    {
        journal = JournalManager.Instance;
        if (journal == null)
            journal = FindAnyObjectByType<JournalManager>();

        if (journal == null)
        {
            enabled = false;
            return;
        }

        BuildUI();
        panelObj.SetActive(false);

        journal.OnEntryAdded += OnEntryAdded;
    }

    void OnDestroy()
    {
        if (journal != null)
            journal.OnEntryAdded -= OnEntryAdded;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (isOpen) CloseJournal();
            else OpenJournal();
        }

        // Notification fade
        if (notifTimer > 0f)
        {
            notifTimer -= Time.deltaTime;
            if (notifTimer <= 0f)
                notifBg.gameObject.SetActive(false);
            else if (notifTimer < 0.5f)
            {
                float alpha = notifTimer / 0.5f;
                notifBg.color = new Color(0.15f, 0.1f, 0.05f, 0.85f * alpha);
                notifText.color = new Color(1, 0.9f, 0.6f, alpha);
            }
        }
    }

    void OnEntryAdded(JournalEntry entry)
    {
        // Show notification
        notifBg.gameObject.SetActive(true);
        notifBg.color = new Color(0.15f, 0.1f, 0.05f, 0.85f);
        notifText.text = "New Journal Entry: " + entry.title;
        notifText.color = new Color(1, 0.9f, 0.6f);
        notifTimer = 3f;

        if (isOpen)
            PopulateEntryList();
    }

    void OpenJournal()
    {
        isOpen = true;
        panelObj.SetActive(true);
        activeFilter = null;
        selectedEntry = null;
        PopulateEntryList();
        ClearDetail();
        RefreshTabColors();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerMovement.inputBlocked = true;
    }

    void CloseJournal()
    {
        isOpen = false;
        panelObj.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerMovement.inputBlocked = false;
    }

    // ─── Entry List ──────────────────────────────────────────

    void PopulateEntryList()
    {
        foreach (Transform child in entryListContent.transform)
            Destroy(child.gameObject);

        IReadOnlyList<JournalEntry> entries = journal.DiscoveredEntries;
        int count = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            JournalEntry entry = entries[i];
            if (entry == null) continue;
            if (activeFilter != null && entry.category != activeFilter.Value) continue;

            CreateEntryButton(entry, count);
            count++;
        }

        entryContentRect.sizeDelta = new Vector2(0, count * 36 + 10);
        entryScroll.verticalNormalizedPosition = 1f;
    }

    void CreateEntryButton(JournalEntry entry, int index)
    {
        GameObject btnObj = new GameObject("Entry_" + entry.title);
        btnObj.transform.SetParent(entryListContent.transform, false);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = (selectedEntry == entry)
            ? new Color(0.4f, 0.35f, 0.15f, 0.9f)
            : new Color(0.2f, 0.2f, 0.2f, 0.9f);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0, 1);
        btnRect.anchorMax = new Vector2(1, 1);
        btnRect.pivot = new Vector2(0.5f, 1);
        btnRect.anchoredPosition = new Vector2(0, -index * 36);
        btnRect.sizeDelta = new Vector2(0, 32);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;

        // Category color prefix
        string catColor = entry.category == JournalCategory.Story ? "#88bbff" :
                          entry.category == JournalCategory.Clue ? "#ffcc66" : "#88ff88";
        text.text = "<color=" + catColor + ">[" + entry.category + "]</color> " + entry.title;
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 0);
        textRect.offsetMax = new Vector2(-4, 0);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        JournalEntry capturedEntry = entry;
        btn.onClick.AddListener(() => SelectEntry(capturedEntry));
    }

    void SelectEntry(JournalEntry entry)
    {
        selectedEntry = entry;
        PopulateEntryList();
        ShowDetail(entry);
    }

    // ─── Detail Panel ────────────────────────────────────────

    void ClearDetail()
    {
        detailTitle.text = "Select an entry";
        detailBody.text = "";
    }

    void ShowDetail(JournalEntry entry)
    {
        detailTitle.text = entry.title;
        detailBody.text = entry.body;
        detailScroll.verticalNormalizedPosition = 1f;
    }

    // ─── Category Tabs ───────────────────────────────────────

    void SetFilter(JournalCategory? filter)
    {
        activeFilter = filter;
        selectedEntry = null;
        PopulateEntryList();
        ClearDetail();
        RefreshTabColors();
    }

    void RefreshTabColors()
    {
        JournalCategory?[] filters = { null, JournalCategory.Story, JournalCategory.Clue, JournalCategory.Mission };
        for (int i = 0; i < tabButtons.Length && i < filters.Length; i++)
        {
            Image img = tabButtons[i].GetComponent<Image>();
            bool active = activeFilter == filters[i];
            img.color = active ? new Color(0.4f, 0.35f, 0.15f, 0.95f) : new Color(0.25f, 0.25f, 0.25f, 0.9f);
        }
    }

    // ─── Build UI ────────────────────────────────────────────

    void BuildUI()
    {
        if (EventSystem.current == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Canvas
        GameObject canvasObj = new GameObject("JournalCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 92;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dark overlay
        panelObj = new GameObject("JournalPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.02f, 0.04f, 0.8f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Container
        GameObject container = new GameObject("Container");
        container.transform.SetParent(panelObj.transform, false);
        Image containerBg = container.AddComponent<Image>();
        containerBg.color = new Color(0.08f, 0.07f, 0.06f, 0.95f);
        containerBg.raycastTarget = false;
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(800, 500);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(container.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "Journal";
        title.fontSize = 28;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.9f, 0.6f);
        title.raycastTarget = false;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -8);
        titleRect.sizeDelta = new Vector2(0, 32);

        BuildCategoryTabs(container.transform);
        BuildEntryListPanel(container.transform);
        BuildDetailPanel(container.transform);
        BuildNotification(canvasObj.transform);
    }

    void BuildCategoryTabs(Transform parent)
    {
        GameObject tabRow = new GameObject("TabRow");
        tabRow.transform.SetParent(parent, false);
        RectTransform tabRowRect = tabRow.AddComponent<RectTransform>();
        tabRowRect.anchorMin = new Vector2(0, 1);
        tabRowRect.anchorMax = new Vector2(1, 1);
        tabRowRect.pivot = new Vector2(0.5f, 1);
        tabRowRect.anchoredPosition = new Vector2(0, -44);
        tabRowRect.sizeDelta = new Vector2(-20, 26);

        string[] tabNames = { "All", "Story", "Clues", "Missions" };
        JournalCategory?[] filters = { null, JournalCategory.Story, JournalCategory.Clue, JournalCategory.Mission };
        tabButtons = new Button[tabNames.Length];
        float tabWidth = 1f / tabNames.Length;

        for (int i = 0; i < tabNames.Length; i++)
        {
            GameObject tabObj = new GameObject("Tab_" + tabNames[i]);
            tabObj.transform.SetParent(tabRow.transform, false);
            Image tabBg = tabObj.AddComponent<Image>();
            tabBg.color = (i == 0) ? new Color(0.4f, 0.35f, 0.15f, 0.95f) : new Color(0.25f, 0.25f, 0.25f, 0.9f);
            RectTransform tabRect = tabObj.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(tabWidth * i, 0);
            tabRect.anchorMax = new Vector2(tabWidth * (i + 1), 1);
            tabRect.offsetMin = new Vector2(2, 0);
            tabRect.offsetMax = new Vector2(-2, 0);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tabObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = tabNames[i];
            text.fontSize = 13;
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
            JournalCategory? capturedFilter = filters[i];
            btn.onClick.AddListener(() => SetFilter(capturedFilter));
        }
    }

    void BuildEntryListPanel(Transform parent)
    {
        GameObject leftPanel = new GameObject("EntryListPanel");
        leftPanel.transform.SetParent(parent, false);
        Image leftBg = leftPanel.AddComponent<Image>();
        leftBg.color = new Color(0.12f, 0.11f, 0.1f, 0.95f);
        leftBg.raycastTarget = false;
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.38f, 1);
        leftRect.offsetMin = new Vector2(10, 10);
        leftRect.offsetMax = new Vector2(-5, -76);

        // Scroll view
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(leftPanel.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(4, 4);
        scrollRect.offsetMax = new Vector2(-4, -4);

        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scrollObj.AddComponent<RectMask2D>();

        entryListContent = new GameObject("Content");
        entryListContent.transform.SetParent(scrollObj.transform, false);
        entryContentRect = entryListContent.AddComponent<RectTransform>();
        entryContentRect.anchorMin = new Vector2(0, 1);
        entryContentRect.anchorMax = new Vector2(1, 1);
        entryContentRect.pivot = new Vector2(0.5f, 1);
        entryContentRect.anchoredPosition = Vector2.zero;
        entryContentRect.sizeDelta = new Vector2(0, 200);

        scroll.content = entryContentRect;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 20f;
        entryScroll = scroll;
    }

    void BuildDetailPanel(Transform parent)
    {
        GameObject rightPanel = new GameObject("DetailPanel");
        rightPanel.transform.SetParent(parent, false);
        Image rightBg = rightPanel.AddComponent<Image>();
        rightBg.color = new Color(0.1f, 0.09f, 0.08f, 0.95f);
        rightBg.raycastTarget = false;
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.38f, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = new Vector2(5, 10);
        rightRect.offsetMax = new Vector2(-10, -76);

        // Entry title
        GameObject dtObj = new GameObject("DetailTitle");
        dtObj.transform.SetParent(rightPanel.transform, false);
        detailTitle = dtObj.AddComponent<TextMeshProUGUI>();
        detailTitle.text = "Select an entry";
        detailTitle.fontSize = 20;
        detailTitle.fontStyle = FontStyles.Bold;
        detailTitle.color = new Color(1f, 0.9f, 0.6f);
        detailTitle.alignment = TextAlignmentOptions.TopLeft;
        detailTitle.raycastTarget = false;
        RectTransform dtRect = detailTitle.rectTransform;
        dtRect.anchorMin = new Vector2(0, 1);
        dtRect.anchorMax = new Vector2(1, 1);
        dtRect.pivot = new Vector2(0.5f, 1);
        dtRect.anchoredPosition = new Vector2(0, -10);
        dtRect.sizeDelta = new Vector2(-20, 28);

        // Scrollable body
        GameObject bodyScrollObj = new GameObject("BodyScroll");
        bodyScrollObj.transform.SetParent(rightPanel.transform, false);
        RectTransform bsRect = bodyScrollObj.AddComponent<RectTransform>();
        bsRect.anchorMin = new Vector2(0, 0);
        bsRect.anchorMax = new Vector2(1, 1);
        bsRect.offsetMin = new Vector2(10, 10);
        bsRect.offsetMax = new Vector2(-10, -45);

        detailScroll = bodyScrollObj.AddComponent<ScrollRect>();
        detailScroll.horizontal = false;
        detailScroll.vertical = true;
        bodyScrollObj.AddComponent<RectMask2D>();

        GameObject bodyContent = new GameObject("BodyContent");
        bodyContent.transform.SetParent(bodyScrollObj.transform, false);
        RectTransform bcRect = bodyContent.AddComponent<RectTransform>();
        bcRect.anchorMin = new Vector2(0, 1);
        bcRect.anchorMax = new Vector2(1, 1);
        bcRect.pivot = new Vector2(0.5f, 1);
        bcRect.anchoredPosition = Vector2.zero;
        bcRect.sizeDelta = new Vector2(0, 600);

        detailBody = bodyContent.AddComponent<TextMeshProUGUI>();
        detailBody.text = "";
        detailBody.fontSize = 15;
        detailBody.color = new Color(0.85f, 0.82f, 0.75f);
        detailBody.alignment = TextAlignmentOptions.TopLeft;
        detailBody.enableWordWrapping = true;
        detailBody.raycastTarget = false;

        detailScroll.content = bcRect;
        detailScroll.movementType = ScrollRect.MovementType.Clamped;
        detailScroll.scrollSensitivity = 20f;
    }

    void BuildNotification(Transform canvasTransform)
    {
        GameObject notifObj = new GameObject("JournalNotification");
        notifObj.transform.SetParent(canvasTransform, false);
        notifBg = notifObj.AddComponent<Image>();
        notifBg.color = new Color(0.15f, 0.1f, 0.05f, 0.85f);
        notifBg.raycastTarget = false;
        RectTransform nRect = notifObj.GetComponent<RectTransform>();
        nRect.anchorMin = new Vector2(0.5f, 0.12f);
        nRect.anchorMax = new Vector2(0.5f, 0.12f);
        nRect.pivot = new Vector2(0.5f, 0.5f);
        nRect.sizeDelta = new Vector2(400, 36);

        GameObject nTextObj = new GameObject("NotifText");
        nTextObj.transform.SetParent(notifObj.transform, false);
        notifText = nTextObj.AddComponent<TextMeshProUGUI>();
        notifText.text = "";
        notifText.fontSize = 15;
        notifText.color = new Color(1, 0.9f, 0.6f);
        notifText.alignment = TextAlignmentOptions.Center;
        notifText.raycastTarget = false;
        RectTransform ntRect = notifText.rectTransform;
        ntRect.anchorMin = Vector2.zero;
        ntRect.anchorMax = Vector2.one;
        ntRect.offsetMin = Vector2.zero;
        ntRect.offsetMax = Vector2.zero;

        notifObj.SetActive(false);
    }
}
