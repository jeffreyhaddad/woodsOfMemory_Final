using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class GameCompleteUI : MonoBehaviour
{
    private GameObject panelObj;
    private MissionManager missionManager;

    void Start()
    {
        missionManager = FindAnyObjectByType<MissionManager>();
        if (missionManager == null)
        {
            enabled = false;
            return;
        }

        BuildUI();
        panelObj.SetActive(false);

        missionManager.OnAllMissionsCompleted += ShowEndingScreen;
    }

    void OnDestroy()
    {
        if (missionManager != null)
            missionManager.OnAllMissionsCompleted -= ShowEndingScreen;
    }

    void ShowEndingScreen()
    {
        panelObj.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerMovement.inputBlocked = true;
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("WelcomeScene");
    }

    void BuildUI()
    {
        if (EventSystem.current == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObj = new GameObject("GameCompleteCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen overlay
        panelObj = new GameObject("EndingPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.9f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Title
        CreateText(panelObj.transform, "YOU ESCAPED THE WOODS", 44,
            new Color(0.9f, 0.8f, 0.4f), new Vector2(0, 120), new Vector2(800, 55));

        // Story conclusion
        string conclusion =
            "The gate creaks open and daylight floods through the trees.\n\n" +
            "You remember now. You came here to forget — to let the woods\n" +
            "take the pain away. But forgetting means losing yourself.\n\n" +
            "Others weren't so lucky. Their memories fed the shadows,\n" +
            "and the forest grew stronger. But you fought back.\n" +
            "You survived. You remembered.\n\n" +
            "The woods will wait for the next lost soul.\n" +
            "But it won't be you. Not today.";

        CreateText(panelObj.transform, conclusion, 18,
            new Color(0.8f, 0.78f, 0.7f), new Vector2(0, -30), new Vector2(700, 250));

        // Credits
        CreateText(panelObj.transform, "The Woods of Memory", 26,
            new Color(0.6f, 0.55f, 0.4f), new Vector2(0, -200), new Vector2(400, 35));

        CreateText(panelObj.transform, "by Jeffrey Haddad & Anthony Diab", 16,
            new Color(0.5f, 0.5f, 0.5f), new Vector2(0, -230), new Vector2(400, 25));

        // Return button
        GameObject btnObj = new GameObject("MenuButton");
        btnObj.transform.SetParent(panelObj.transform, false);
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.25f, 0.22f, 0.15f, 0.95f);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0, -290);
        btnRect.sizeDelta = new Vector2(220, 44);

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "Return to Menu";
        btnText.fontSize = 20;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.raycastTarget = false;
        RectTransform btRect = btnText.rectTransform;
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(ReturnToMenu);
    }

    void CreateText(Transform parent, string text, float fontSize, Color color, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
