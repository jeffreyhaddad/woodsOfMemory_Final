using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class WelcomeSceneScript : MonoBehaviour
{
    private SettingsUI settingsUI;

    // Load-slot overlay (built procedurally)
    private GameObject loadPanel;
    private TextMeshProUGUI[] slotLabels;
    private TextMeshProUGUI feedbackText;
    private float feedbackTimer;

    void Start()
    {
        if (SettingsManager.Instance == null)
        {
            var smGo = new GameObject("SettingsManager");
            smGo.AddComponent<SettingsManager>();
        }

        settingsUI = FindAnyObjectByType<SettingsUI>();
        if (settingsUI == null)
        {
            var suiGo = new GameObject("SettingsUI");
            settingsUI = suiGo.AddComponent<SettingsUI>();
        }

        BuildLoadUI();
    }

    void Update()
    {
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0f && feedbackText != null)
                feedbackText.text = "";
        }

        // Escape closes the load panel
        if (Input.GetKeyDown(KeyCode.Escape) && loadPanel != null && loadPanel.activeSelf)
            loadPanel.SetActive(false);
    }

    // ── Existing buttons wired in the scene ──────────────────────────

    public void LoadGameScene()
    {
        SceneManager.LoadScene("TerrainScene");
    }

    public void ContinueGame()
    {
        if (SaveManager.SaveFileExists(0))
            SaveManager.RequestLoad(0);
        SceneManager.LoadScene("TerrainScene");
    }

    public static bool HasSave() => SaveManager.SaveFileExists(0);

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenSettings()
    {
        if (settingsUI == null)
            settingsUI = FindAnyObjectByType<SettingsUI>();
        settingsUI?.Open();
    }

    // ── Load Game button (procedural) ────────────────────────────────

    public void OpenLoadSlots()
    {
        if (loadPanel == null) return;
        RefreshSlotLabels();
        loadPanel.SetActive(true);
    }

    void OnSlotClicked(int slot)
    {
        // Static check works on welcome screen; instance check works in-game
        bool hasSave = SaveManager.Instance != null
            ? SaveManager.Instance.HasSave(slot)
            : SaveManager.HasSaveStatic(slot);

        if (!hasSave) { ShowFeedback("No save in Slot " + slot + "."); return; }

        loadPanel.SetActive(false);

        if (SaveManager.Instance != null)
        {
            // In-game load
            SaveManager.Instance.Load(slot);
        }
        else
        {
            // Welcome screen load: queue then go to game scene
            SaveManager.RequestLoad(slot);
            UnityEngine.SceneManagement.SceneManager.LoadScene("TerrainScene");
        }
    }

    void RefreshSlotLabels()
    {
        if (slotLabels == null) return;
        for (int i = 0; i < 3; i++)
        {
            // Use static reader so this works on the welcome screen (no SaveManager instance)
            string info = SaveManager.Instance != null
                ? SaveManager.Instance.GetSaveInfo(i + 1)
                : SaveManager.GetSaveInfoStatic(i + 1);
            slotLabels[i].text = "Slot " + (i + 1) + "  —  " + info;
        }
    }

    void ShowFeedback(string msg)
    {
        if (feedbackText == null) return;
        feedbackText.text = msg;
        feedbackTimer = 2.5f;
    }

    // ── Build the slot-selection overlay ─────────────────────────────

    void BuildLoadUI()
    {
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // Canvas (sits on top of welcome scene)
        var canvasGo = new GameObject("LoadMenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Find the existing "Exit Game" button and inject Load Game above it ──
        // This keeps Load Game visually aligned with the scene's button stack.
        RectTransform exitRT    = null;
        Transform     btnParent = null;
        Vector2       loadPos   = new Vector2(0, -60f);   // fallback
        Vector2       btnSize   = new Vector2(220, 44f);

        foreach (var btn in FindObjectsOfType<Button>(true))
        {
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null && (tmp.text.Contains("Exit") || tmp.text.Contains("Quit")))
            {
                exitRT    = btn.GetComponent<RectTransform>();
                btnParent = btn.transform.parent;
                break;
            }
        }

        if (exitRT != null)
        {
            // Shift "Exit Game" down to make a gap for Load Game
            Vector2 ep = exitRT.anchoredPosition;
            float   spacing = exitRT.sizeDelta.y + 10f;
            exitRT.anchoredPosition = new Vector2(ep.x, ep.y - spacing);

            // Load Game goes exactly where Exit Game was, same size
            loadPos = ep;
            btnSize = exitRT.sizeDelta;
        }

        // Add the Load Game button — into the same parent as existing buttons if found,
        // otherwise into our own canvas
        Transform loadParent = btnParent != null ? btnParent : canvasGo.transform;
        Vector2   anchor     = btnParent != null ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.5f);
        MakeButton(loadParent, "Load Game", anchor, loadPos, btnSize, OpenLoadSlots);

        // Dark overlay panel (slot picker)
        loadPanel = new GameObject("LoadPanel");
        loadPanel.transform.SetParent(canvasGo.transform, false);
        var overlay = loadPanel.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.78f);
        var overlayRT = overlay.rectTransform;
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;

        // Inner card
        var card = new GameObject("Card");
        card.transform.SetParent(loadPanel.transform, false);
        var cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        var cardRT = cardBg.rectTransform;
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot     = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(400, 340);

        // Title
        MakeLabel(card.transform, "LOAD GAME", 30, new Vector2(0, 130), new Vector2(340, 40));

        // 3 slot buttons
        slotLabels = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            int slot = i + 1;
            var btn = new GameObject("Slot" + slot);
            btn.transform.SetParent(card.transform, false);
            var btnImg = btn.AddComponent<Image>();
            btnImg.color = new Color(0.18f, 0.18f, 0.22f, 0.97f);
            var btnRT = btnImg.rectTransform;
            btnRT.anchorMin = new Vector2(0.5f, 0.5f);
            btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.pivot     = new Vector2(0.5f, 0.5f);
            btnRT.anchoredPosition = new Vector2(0, 55 - i * 62);
            btnRT.sizeDelta = new Vector2(340, 50);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btn.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "Slot " + slot + "  —  Empty";
            tmp.fontSize  = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            tmp.raycastTarget = false;
            var tRT = tmp.rectTransform;
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(8, 0); tRT.offsetMax = new Vector2(-8, 0);
            slotLabels[i] = tmp;

            var b = btn.AddComponent<Button>();
            b.targetGraphic = btnImg;
            int captured = slot;
            b.onClick.AddListener(() => OnSlotClicked(captured));
        }

        // Feedback text
        var fbGo = new GameObject("Feedback");
        fbGo.transform.SetParent(card.transform, false);
        feedbackText = fbGo.AddComponent<TextMeshProUGUI>();
        feedbackText.text      = "";
        feedbackText.fontSize  = 14;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.color     = new Color(0.9f, 0.4f, 0.4f);
        feedbackText.raycastTarget = false;
        var fbRT = feedbackText.rectTransform;
        fbRT.anchorMin = new Vector2(0.5f, 0.5f);
        fbRT.anchorMax = new Vector2(0.5f, 0.5f);
        fbRT.pivot     = new Vector2(0.5f, 0.5f);
        fbRT.anchoredPosition = new Vector2(0, -108);
        fbRT.sizeDelta = new Vector2(320, 24);

        // Back button
        MakeButton(card.transform, "Back",
            new Vector2(0.5f, 0.5f), new Vector2(0, -145), new Vector2(160, 40),
            () => loadPanel.SetActive(false));

        loadPanel.SetActive(false);
    }

    void MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size,
                    UnityEngine.Events.UnityAction onClick)
    {
        // ── Try to copy style from an existing scene button ──────────────
        Color  normalColor      = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        Color  highlightedColor = new Color(1f, 0.85f, 0.1f, 1f);   // yellow, matches scene
        Color  pressedColor     = new Color(0.7f, 0.6f, 0.05f, 1f);
        float  fontSize         = 20f;
        Color  textColor        = Color.white;
        ColorBlock cb = ColorBlock.defaultColorBlock;

        var existingBtn = FindObjectOfType<Button>(true);
        if (existingBtn != null)
        {
            var existingImg = existingBtn.GetComponent<Image>();
            if (existingImg != null) normalColor = existingImg.color;
            cb = existingBtn.colors;
            var existingTMP = existingBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (existingTMP != null) { fontSize = existingTMP.fontSize; textColor = existingTMP.color; }
        }
        else
        {
            cb.normalColor      = normalColor;
            cb.highlightedColor = highlightedColor;
            cb.pressedColor     = pressedColor;
        }
        // ─────────────────────────────────────────────────────────────────

        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = normalColor;
        var rt = img.rectTransform;
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = textColor;
        tmp.raycastTarget = false;
        var tRT = tmp.rectTransform;
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);
    }

    void MakeLabel(Transform parent, string text, float size, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Lbl_" + text);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.raycastTarget = false;
        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
    }
}

