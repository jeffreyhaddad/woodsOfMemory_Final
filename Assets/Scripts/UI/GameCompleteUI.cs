using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class GameCompleteUI : MonoBehaviour
{
    const string BestTimeKey = "WOM_BestTime";

    private GameObject      panelObj;
    private TextMeshProUGUI completionTimeText;
    private TextMeshProUGUI recordText;

    void Start()
    {
        BuildUI();
        panelObj.SetActive(false);
    }

    /// <summary>Called by EndingScreenUI after the cinematic text sequence completes.</summary>
    public void ShowEndingScreen(float completionTime = 0f)
    {
        if (panelObj == null) return;

        panelObj.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerMovement.inputBlocked    = true;
        PlayerMovement.movementBlocked = true;

        // Populate timer text
        completionTimeText.text = "Escaped in  " + FormatTime(completionTime);

        // Best-time logic
        float prev = PlayerPrefs.GetFloat(BestTimeKey, float.MaxValue);
        bool firstRun = (prev >= float.MaxValue - 1f);
        bool newRecord = completionTime < prev;

        if (firstRun)
        {
            recordText.text  = "First Escape!";
            recordText.color = new Color(0.45f, 0.85f, 0.65f);
            PlayerPrefs.SetFloat(BestTimeKey, completionTime);
        }
        else if (newRecord)
        {
            recordText.text  = "NEW RECORD!  Previous best: " + FormatTime(prev);
            recordText.color = new Color(0.95f, 0.80f, 0.25f);
            PlayerPrefs.SetFloat(BestTimeKey, completionTime);
        }
        else
        {
            recordText.text  = "Best time:  " + FormatTime(prev) + "  —  Can you beat it?";
            recordText.color = new Color(0.55f, 0.55f, 0.55f);
        }

        PlayerPrefs.Save();
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("WelcomeScene");
    }

    static string FormatTime(float totalSeconds)
    {
        int m = (int)(totalSeconds / 60);
        int s = (int)(totalSeconds % 60);
        return $"{m:00}:{s:00}";
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
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;   // above EndingScreenUI (300)
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dark panel
        panelObj = new GameObject("EndingPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.08f, 0.14f, 0.97f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Gold top accent bar
        MakeRect(panelObj.transform, "TopBar",
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, 510f), new Vector2(0f, 4f),
            new Color(0.78f, 0.66f, 0.29f));

        // Title
        CreateLabel(panelObj.transform, "YOU ESCAPED THE WOODS", 44,
            FontStyles.Bold, new Color(0.90f, 0.80f, 0.40f), new Vector2(0, 290), new Vector2(860, 60));

        // Gold rule under title
        MakeRect(panelObj.transform, "Rule",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 256f), new Vector2(560f, 2f),
            new Color(0.78f, 0.66f, 0.29f, 0.6f));

        // Completion time — large gold number
        completionTimeText = CreateLabel(panelObj.transform, "", 40,
            FontStyles.Bold, new Color(0.95f, 0.88f, 0.55f), new Vector2(0, 202), new Vector2(620, 54));

        // Record / best-time line — filled in ShowEndingScreen
        recordText = CreateLabel(panelObj.transform, "", 20,
            FontStyles.Italic, new Color(0.55f, 0.55f, 0.55f), new Vector2(0, 158), new Vector2(680, 30));

        // Story text
        string story =
            "The gate creaks open and daylight floods through the trees.\n\n" +
            "You remember now. You came here to forget — to let the woods\n" +
            "take the pain away. But forgetting means losing yourself.\n\n" +
            "Others weren't so lucky. Their memories fed the shadows,\n" +
            "and the forest grew stronger. But you fought back.\n" +
            "You survived. You remembered.\n\n" +
            "The woods will wait for the next lost soul.\n" +
            "But it won't be you. Not today.";
        CreateLabel(panelObj.transform, story, 17,
            FontStyles.Normal, new Color(0.78f, 0.76f, 0.68f), new Vector2(0, 10), new Vector2(680, 230));

        // Credits
        CreateLabel(panelObj.transform, "The Woods of Memory", 24,
            FontStyles.Bold, new Color(0.58f, 0.52f, 0.38f), new Vector2(0, -175), new Vector2(460, 34));
        CreateLabel(panelObj.transform, "by Jeffrey Haddad & Anthony Diab", 15,
            FontStyles.Normal, new Color(0.44f, 0.44f, 0.44f), new Vector2(0, -207), new Vector2(460, 24));

        // Return to Menu button
        MakeButton(panelObj.transform, "Return to Menu", new Vector2(0, -270), new Vector2(220, 46));
    }

    TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize,
        FontStyles style, Color color, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = fontSize;
        tmp.fontStyle        = style;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = color;
        tmp.raycastTarget    = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        RectTransform r = tmp.rectTransform;
        r.anchorMin       = new Vector2(0.5f, 0.5f);
        r.anchorMax       = new Vector2(0.5f, 0.5f);
        r.pivot           = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta        = size;
        return tmp;
    }

    void MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        RectTransform r = img.rectTransform;
        r.anchorMin       = anchorMin;
        r.anchorMax       = anchorMax;
        r.pivot           = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta        = size;
    }

    void MakeButton(Transform parent, string label, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = new GameObject("MenuButton");
        btnObj.transform.SetParent(parent, false);
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.14f, 0.22f, 0.32f, 0.95f);
        RectTransform r = btnObj.GetComponent<RectTransform>();
        r.anchorMin       = new Vector2(0.5f, 0.5f);
        r.anchorMax       = new Vector2(0.5f, 0.5f);
        r.pivot           = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta        = size;

        // Gold border
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor    = new Color(0.78f, 0.66f, 0.29f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 20;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = new Color(0.90f, 0.80f, 0.40f);
        tmp.raycastTarget = false;
        RectTransform tr = tmp.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.22f, 0.34f, 0.48f);
        cb.pressedColor     = new Color(0.08f, 0.14f, 0.22f);
        btn.colors = cb;
        btn.onClick.AddListener(ReturnToMenu);
    }
}
