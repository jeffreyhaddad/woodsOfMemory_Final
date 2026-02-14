using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    private GameObject panelObj;
    private GameObject mainMenu;
    private GameObject slotPanel;
    private TextMeshProUGUI slotPanelTitle;
    private TextMeshProUGUI[] slotLabels;
    private TextMeshProUGUI feedbackText;
    private float feedbackTimer;
    private bool isOpen = false;

    // Are we picking a slot for save or load?
    private enum SlotMode { None, Save, Load }
    private SlotMode slotMode = SlotMode.None;

    void Start()
    {
        BuildUI();
        panelObj.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Don't toggle pause if player is dead
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Dead)
                return;

            if (slotMode != SlotMode.None)
            {
                // Back to main menu from slot selection
                ShowMainMenu();
                return;
            }

            if (isOpen) Resume();
            else Pause();
        }

        // Feedback timer
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.unscaledDeltaTime;
            if (feedbackTimer <= 0f)
                feedbackText.text = "";
        }
    }

    void Pause()
    {
        isOpen = true;
        panelObj.SetActive(true);
        ShowMainMenu();

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Paused);
        else
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PlayerMovement.inputBlocked = true;
        }
    }

    void Resume()
    {
        isOpen = false;
        panelObj.SetActive(false);
        slotMode = SlotMode.None;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Playing);
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            PlayerMovement.inputBlocked = false;
        }
    }

    void QuitToMenu()
    {
        isOpen = false;
        panelObj.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.QuitToMenu();
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("WelcomeScene");
        }
    }

    void QuitGame()
    {
        Application.Quit();
    }

    // ─── Slot Selection ───────────────────────────────────────

    void ShowMainMenu()
    {
        slotMode = SlotMode.None;
        mainMenu.SetActive(true);
        slotPanel.SetActive(false);
    }

    void OpenSaveSlots()
    {
        slotMode = SlotMode.Save;
        mainMenu.SetActive(false);
        slotPanel.SetActive(true);
        slotPanelTitle.text = "SAVE GAME";
        RefreshSlotLabels();
    }

    void OpenLoadSlots()
    {
        slotMode = SlotMode.Load;
        mainMenu.SetActive(false);
        slotPanel.SetActive(true);
        slotPanelTitle.text = "LOAD GAME";
        RefreshSlotLabels();
    }

    void RefreshSlotLabels()
    {
        for (int i = 0; i < 3; i++)
        {
            string info = "Empty";
            if (SaveManager.Instance != null)
                info = SaveManager.Instance.GetSaveInfo(i + 1);

            string slotName = (i == 0) ? "Slot 1" : (i == 1) ? "Slot 2" : "Slot 3";
            slotLabels[i].text = slotName + "  -  " + info;
        }
    }

    void OnSlotClicked(int slot)
    {
        if (SaveManager.Instance == null) return;

        if (slotMode == SlotMode.Save)
        {
            SaveManager.Instance.Save(slot);
            ShowFeedback("Saved to Slot " + slot + "!");
            RefreshSlotLabels();
            SFXManager.PlayMenuClick();
        }
        else if (slotMode == SlotMode.Load)
        {
            if (SaveManager.Instance.HasSave(slot))
            {
                isOpen = false;
                panelObj.SetActive(false);
                slotMode = SlotMode.None;
                SaveManager.Instance.Load(slot);
                SFXManager.PlayMenuClick();
            }
            else
            {
                ShowFeedback("No save in this slot.");
            }
        }
    }

    void ShowFeedback(string msg)
    {
        feedbackText.text = msg;
        feedbackTimer = 2.5f;
    }

    // ─── UI Construction ──────────────────────────────────────

    void BuildUI()
    {
        // Ensure EventSystem exists
        if (EventSystem.current == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Canvas
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dark overlay
        panelObj = new GameObject("PausePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.7f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ── Main Menu ─────────────────────────────────────────
        mainMenu = new GameObject("MainMenu");
        mainMenu.transform.SetParent(panelObj.transform, false);
        Image mainBg = mainMenu.AddComponent<Image>();
        mainBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        mainBg.raycastTarget = false;
        RectTransform mainRect = mainMenu.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainRect.pivot = new Vector2(0.5f, 0.5f);
        mainRect.sizeDelta = new Vector2(320, 380);

        CreateText(mainMenu.transform, "PAUSED", 32, new Vector2(0, 140), new Vector2(280, 40));
        CreateButton(mainMenu.transform, "Resume", new Vector2(0, 70), Resume);
        CreateButton(mainMenu.transform, "Save Game", new Vector2(0, 15), OpenSaveSlots);
        CreateButton(mainMenu.transform, "Load Game", new Vector2(0, -40), OpenLoadSlots);
        CreateButton(mainMenu.transform, "Quit to Menu", new Vector2(0, -95), QuitToMenu);
        CreateButton(mainMenu.transform, "Quit Game", new Vector2(0, -150), QuitGame);

        // ── Slot Selection Panel ──────────────────────────────
        slotPanel = new GameObject("SlotPanel");
        slotPanel.transform.SetParent(panelObj.transform, false);
        Image slotBg = slotPanel.AddComponent<Image>();
        slotBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        slotBg.raycastTarget = false;
        RectTransform slotRect = slotPanel.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.sizeDelta = new Vector2(380, 320);

        slotPanelTitle = CreateText(slotPanel.transform, "SAVE GAME", 28, new Vector2(0, 115), new Vector2(300, 36));

        // 3 slot buttons
        slotLabels = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            int slot = i + 1; // Slots 1, 2, 3 (0 is auto-save)
            GameObject btnObj = new GameObject("SlotBtn_" + slot);
            btnObj.transform.SetParent(slotPanel.transform, false);
            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0, 50 - i * 60);
            btnRect.sizeDelta = new Vector2(320, 48);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Slot " + slot + "  -  Empty";
            text.fontSize = 17;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            slotLabels[i] = text;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            int capturedSlot = slot;
            btn.onClick.AddListener(() => OnSlotClicked(capturedSlot));
        }

        // Feedback text
        GameObject fbObj = new GameObject("Feedback");
        fbObj.transform.SetParent(slotPanel.transform, false);
        feedbackText = fbObj.AddComponent<TextMeshProUGUI>();
        feedbackText.text = "";
        feedbackText.fontSize = 15;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.color = new Color(0.4f, 0.9f, 0.4f);
        feedbackText.raycastTarget = false;
        RectTransform fbRect = feedbackText.rectTransform;
        fbRect.anchorMin = new Vector2(0.5f, 0.5f);
        fbRect.anchorMax = new Vector2(0.5f, 0.5f);
        fbRect.pivot = new Vector2(0.5f, 0.5f);
        fbRect.anchoredPosition = new Vector2(0, -100);
        fbRect.sizeDelta = new Vector2(300, 25);

        // Back button
        CreateButton(slotPanel.transform, "Back", new Vector2(0, -130), ShowMainMenu);

        slotPanel.SetActive(false);
    }

    TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject("Text_" + text);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return tmp;
    }

    void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.25f, 0.25f, 0.25f, 0.95f);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = position;
        btnRect.sizeDelta = new Vector2(220, 44);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(onClick);
    }
}
