using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class DeathScreenUI : MonoBehaviour
{
    private GameObject panelObj;
    private PlayerVitals vitals;

    void Start()
    {
        vitals = FindAnyObjectByType<PlayerVitals>();

        if (vitals == null)
        {
            enabled = false;
            return;
        }

        BuildUI();
        panelObj.SetActive(false);

        vitals.OnPlayerDeath += ShowDeathScreen;
    }

    void OnDestroy()
    {
        if (vitals != null)
            vitals.OnPlayerDeath -= ShowDeathScreen;
    }

    void ShowDeathScreen()
    {
        panelObj.SetActive(true);

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Dead);
        else
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PlayerMovement.inputBlocked = true;
        }
    }

    void Respawn()
    {
        panelObj.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RespawnPlayer();
        }
        else
        {
            // Fallback without GameManager
            vitals.Health = vitals.maxHealth;
            vitals.Hunger = vitals.maxHunger;
            vitals.Stamina = vitals.maxStamina;
            vitals.enabled = true;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            PlayerMovement.inputBlocked = false;
        }
    }

    void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("WelcomeScene");
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
        GameObject canvasObj = new GameObject("DeathCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dark red overlay
        panelObj = new GameObject("DeathPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.15f, 0f, 0f, 0.85f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // "YOU DIED" title
        GameObject titleObj = new GameObject("DeathTitle");
        titleObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "YOU HAVE PERISHED";
        title.fontSize = 48;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.9f, 0.2f, 0.2f);
        title.raycastTarget = false;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 80);
        titleRect.sizeDelta = new Vector2(600, 60);

        // Subtitle
        GameObject subObj = new GameObject("DeathSub");
        subObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI sub = subObj.AddComponent<TextMeshProUGUI>();
        sub.text = "The woods have claimed another soul...";
        sub.fontSize = 20;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = new Color(0.7f, 0.7f, 0.7f);
        sub.fontStyle = FontStyles.Italic;
        sub.raycastTarget = false;
        RectTransform subRect = sub.rectTransform;
        subRect.anchorMin = new Vector2(0.5f, 0.5f);
        subRect.anchorMax = new Vector2(0.5f, 0.5f);
        subRect.pivot = new Vector2(0.5f, 0.5f);
        subRect.anchoredPosition = new Vector2(0, 20);
        subRect.sizeDelta = new Vector2(500, 30);

        // Respawn button
        CreateButton(panelObj.transform, "Respawn", new Vector2(0, -50), Respawn);

        // Quit to Menu button
        CreateButton(panelObj.transform, "Quit to Menu", new Vector2(0, -110), QuitToMenu);
    }

    void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.25f, 0.1f, 0.1f, 0.95f);
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
