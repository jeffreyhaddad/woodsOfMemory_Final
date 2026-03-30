using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen ending sequence shown when all missions are complete.
/// Fades in the "Out" journal entry text, then returns to the main menu.
/// Auto-created by GameManager.
/// </summary>
public class EndingScreenUI : MonoBehaviour
{
    private Canvas      endCanvas;
    private Image       blackBg;
    private TextMeshProUGUI bodyText;
    private TextMeshProUGUI promptText;

    private bool anyKeyEnabled = false;

    void Start()
    {
        BuildUI();

        if (MissionManager.Instance != null)
            MissionManager.Instance.OnAllMissionsCompleted += OnAllMissionsCompleted;
    }

    void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnAllMissionsCompleted -= OnAllMissionsCompleted;
    }

    void Update()
    {
        if (anyKeyEnabled && Input.anyKeyDown)
        {
            anyKeyEnabled = false;
            // Transition to the credits/game-complete screen.
            // Fall back to quitting directly if GameCompleteUI isn't present.
            GameCompleteUI credits = FindAnyObjectByType<GameCompleteUI>();
            if (credits != null)
                credits.ShowEndingScreen();
            else
                GameManager.Instance?.QuitToMenu();
        }
    }

    void OnAllMissionsCompleted()
    {
        StartCoroutine(PlayEnding());
    }

    IEnumerator PlayEnding()
    {
        // Lock player
        GameManager.Instance?.SetState(GameState.Cutscene);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        endCanvas.gameObject.SetActive(true);

        // Fade black in
        yield return Fade(blackBg, 0f, 1f, 1.5f);

        // Show body text
        bodyText.gameObject.SetActive(true);
        yield return Fade(bodyText, 0f, 1f, 2f);

        // Hold so player can read
        yield return new WaitForSeconds(5f);

        // Show prompt
        promptText.gameObject.SetActive(true);
        yield return Fade(promptText, 0f, 1f, 1f);

        anyKeyEnabled = true;
    }

    IEnumerator Fade(Graphic graphic, float from, float to, float duration)
    {
        Color c = graphic.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            graphic.color = c;
            yield return null;
        }
        c.a = to;
        graphic.color = c;
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("EndingCanvas");
        endCanvas = canvasObj.AddComponent<Canvas>();
        endCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        endCanvas.sortingOrder = 300;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ((CanvasScaler)canvasObj.GetComponent<CanvasScaler>()).referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen black background
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        blackBg = bgObj.AddComponent<Image>();
        blackBg.color = new Color(0f, 0f, 0f, 0f);
        blackBg.raycastTarget = false;
        RectTransform bgRT = blackBg.rectTransform;
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

        // Body text — the "Out" entry
        GameObject textObj = new GameObject("BodyText");
        textObj.transform.SetParent(canvasObj.transform, false);
        bodyText = textObj.AddComponent<TextMeshProUGUI>();
        bodyText.text =
            "The trees are behind me. I made it.\n\n" +
            "There are gaps I can feel but not see — spaces where memories used to live, " +
            "smooth and sealed over like scar tissue. I don't know exactly what I've lost. " +
            "You can't inventory an absence.\n\n" +
            "But I remember my name. I remember why I came in. " +
            "I remember the notes I left for myself — all those previous versions of me " +
            "who got close but not close enough.\n\n" +
            "I made it. I'm the one who finally made it.\n\n" +
            "I'm not going back.";
        bodyText.fontSize  = 26;
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.color     = new Color(0.92f, 0.88f, 0.80f, 0f);
        bodyText.raycastTarget = false;
        RectTransform textRT = bodyText.rectTransform;
        textRT.anchorMin        = new Vector2(0.15f, 0.2f);
        textRT.anchorMax        = new Vector2(0.85f, 0.85f);
        textRT.offsetMin        = Vector2.zero;
        textRT.offsetMax        = Vector2.zero;
        bodyText.gameObject.SetActive(false);

        // "Press any key" prompt
        GameObject promptObj = new GameObject("Prompt");
        promptObj.transform.SetParent(canvasObj.transform, false);
        promptText = promptObj.AddComponent<TextMeshProUGUI>();
        promptText.text      = "Press any key to continue";
        promptText.fontSize   = 18;
        promptText.alignment  = TextAlignmentOptions.Center;
        promptText.color      = new Color(0.6f, 0.6f, 0.6f, 0f);
        promptText.fontStyle  = FontStyles.Italic;
        promptText.raycastTarget = false;
        RectTransform promptRT = promptText.rectTransform;
        promptRT.anchorMin        = new Vector2(0.3f, 0.08f);
        promptRT.anchorMax        = new Vector2(0.7f, 0.15f);
        promptRT.offsetMin        = Vector2.zero;
        promptRT.offsetMax        = Vector2.zero;
        promptText.gameObject.SetActive(false);

        endCanvas.gameObject.SetActive(false);
    }
}
