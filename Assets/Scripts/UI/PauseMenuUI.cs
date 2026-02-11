using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    private GameObject panelObj;
    private bool isOpen = false;

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

            if (isOpen) Resume();
            else Pause();
        }
    }

    void Pause()
    {
        isOpen = true;
        panelObj.SetActive(true);

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

    void SaveGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save(1); // Manual save to slot 1
            Debug.Log("Game saved!");
        }
        else
            Debug.LogWarning("No SaveManager found in scene.");
    }

    void LoadGame()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave(1))
        {
            isOpen = false;
            panelObj.SetActive(false);
            SaveManager.Instance.Load(1);
        }
        else
            Debug.LogWarning("No save found to load.");
    }

    void QuitGame()
    {
        Application.Quit();
    }

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

        // Container box
        GameObject container = new GameObject("Container");
        container.transform.SetParent(panelObj.transform, false);
        Image containerBg = container.AddComponent<Image>();
        containerBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        containerBg.raycastTarget = false;
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(320, 380);

        // Title
        CreateText(container.transform, "PAUSED", 32, new Vector2(0, 140), new Vector2(280, 40));

        // Buttons
        CreateButton(container.transform, "Resume", new Vector2(0, 70), Resume);
        CreateButton(container.transform, "Save Game", new Vector2(0, 15), SaveGame);
        CreateButton(container.transform, "Load Game", new Vector2(0, -40), LoadGame);
        CreateButton(container.transform, "Quit to Menu", new Vector2(0, -95), QuitToMenu);
        CreateButton(container.transform, "Quit Game", new Vector2(0, -150), QuitGame);
    }

    void CreateText(Transform parent, string text, float fontSize, Vector2 position, Vector2 size)
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
