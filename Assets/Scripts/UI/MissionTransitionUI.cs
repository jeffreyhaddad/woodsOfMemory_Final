using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen cinematic transition between missions.
/// Fades in on mission complete, shows summary, then fades out into the next mission.
/// Add to any GameObject in the gameplay scene — builds its own UI.
/// </summary>
public class MissionTransitionUI : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("How long the fade-in takes")]
    public float fadeInDuration = 1f;
    [Tooltip("How long the completed mission info stays on screen")]
    public float holdDuration = 3f;
    [Tooltip("Brief pause before showing the next mission")]
    public float pauseBetween = 0.5f;
    [Tooltip("How long the next mission info stays on screen")]
    public float nextMissionHold = 2.5f;
    [Tooltip("How long the fade-out takes")]
    public float fadeOutDuration = 1f;

    private MissionManager missionManager;

    // UI elements
    private CanvasGroup canvasGroup;
    private GameObject panelObj;
    private TextMeshProUGUI chapterLabel;
    private TextMeshProUGUI missionNameText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI statusLabel;
    private GameObject dividerObj;
    private TextMeshProUGUI nextLabel;
    private TextMeshProUGUI nextMissionName;
    private TextMeshProUGUI nextDescription;
    private TextMeshProUGUI continueHint;

    // Transition state
    private enum TransitionPhase { Idle, FadeIn, ShowCompleted, Pause, ShowNext, FadeOut }
    private TransitionPhase phase = TransitionPhase.Idle;
    private float phaseTimer;
    private Mission completedMission;
    private int completedIndex;
    private bool skipped;

    // Chapter subtitles for flavor
    private static readonly string[] chapterSubtitles = new string[]
    {
        "The First Night",
        "Hunter and Hunted",
        "Echoes of the Past",
        "Tools of Survival",
        "Into the Darkness",
        "Freedom"
    };

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

        missionManager.OnMissionCompleted += OnMissionCompleted;
    }

    void OnDestroy()
    {
        if (missionManager != null)
            missionManager.OnMissionCompleted -= OnMissionCompleted;
    }

    void OnMissionCompleted(Mission mission)
    {
        // Don't show transition for the final mission — GameCompleteUI handles that
        if (missionManager.AllMissionsComplete)
            return;

        completedMission = mission;
        completedIndex = missionManager.CurrentMissionIndex - 1; // already advanced
        skipped = false;
        StartTransition();
    }

    void StartTransition()
    {
        panelObj.SetActive(true);
        canvasGroup.alpha = 0f;

        // Populate completed mission info
        int chapterNum = completedIndex + 1;
        string subtitle = (completedIndex >= 0 && completedIndex < chapterSubtitles.Length)
            ? chapterSubtitles[completedIndex] : "";

        chapterLabel.text = "Chapter " + chapterNum + " Complete";
        missionNameText.text = completedMission.missionName;
        descriptionText.text = completedMission.description;
        statusLabel.text = "All objectives completed";

        // Hide next mission info initially
        dividerObj.SetActive(false);
        nextLabel.gameObject.SetActive(false);
        nextMissionName.gameObject.SetActive(false);
        nextDescription.gameObject.SetActive(false);
        continueHint.gameObject.SetActive(false);

        // Pause game
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerMovement.inputBlocked = true;

        phase = TransitionPhase.FadeIn;
        phaseTimer = fadeInDuration;

        SFXManager.PlayMenuClick();
    }

    void Update()
    {
        if (phase == TransitionPhase.Idle) return;

        // Use unscaled time since game is paused
        float dt = Time.unscaledDeltaTime;

        // Allow skip with click or space
        if (!skipped && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (phase == TransitionPhase.ShowCompleted || phase == TransitionPhase.Pause)
            {
                // Skip to showing next mission
                skipped = true;
                ShowNextMission();
                phase = TransitionPhase.ShowNext;
                phaseTimer = nextMissionHold;
                return;
            }
            else if (phase == TransitionPhase.ShowNext)
            {
                // Skip to fade out
                phase = TransitionPhase.FadeOut;
                phaseTimer = fadeOutDuration;
                return;
            }
        }

        phaseTimer -= dt;

        switch (phase)
        {
            case TransitionPhase.FadeIn:
                canvasGroup.alpha = 1f - (phaseTimer / fadeInDuration);
                if (phaseTimer <= 0f)
                {
                    canvasGroup.alpha = 1f;
                    phase = TransitionPhase.ShowCompleted;
                    phaseTimer = holdDuration;
                }
                break;

            case TransitionPhase.ShowCompleted:
                if (phaseTimer <= 0f)
                {
                    phase = TransitionPhase.Pause;
                    phaseTimer = pauseBetween;
                }
                break;

            case TransitionPhase.Pause:
                if (phaseTimer <= 0f)
                {
                    ShowNextMission();
                    phase = TransitionPhase.ShowNext;
                    phaseTimer = nextMissionHold;
                }
                break;

            case TransitionPhase.ShowNext:
                if (phaseTimer <= 0f)
                {
                    phase = TransitionPhase.FadeOut;
                    phaseTimer = fadeOutDuration;
                }
                break;

            case TransitionPhase.FadeOut:
                canvasGroup.alpha = phaseTimer / fadeOutDuration;
                if (phaseTimer <= 0f)
                {
                    EndTransition();
                }
                break;
        }
    }

    void ShowNextMission()
    {
        Mission next = missionManager.CurrentMission;
        if (next == null) return;

        int nextChapter = completedIndex + 2;
        string subtitle = (completedIndex + 1 < chapterSubtitles.Length)
            ? chapterSubtitles[completedIndex + 1] : "";

        // Transition the text
        chapterLabel.text = "Chapter " + nextChapter;
        missionNameText.text = next.missionName;
        descriptionText.text = next.description;
        statusLabel.text = "";

        // Show next mission objectives
        dividerObj.SetActive(true);
        nextLabel.gameObject.SetActive(true);
        nextLabel.text = "New Objectives:";
        nextMissionName.gameObject.SetActive(true);

        string objectives = "";
        for (int i = 0; i < next.objectives.Length; i++)
        {
            objectives += "  - " + next.objectives[i].description + "\n";
        }
        nextMissionName.text = objectives.TrimEnd('\n');

        nextDescription.gameObject.SetActive(false);
        continueHint.gameObject.SetActive(true);
        continueHint.text = "Click or press Space to continue...";

        SFXManager.PlayMenuClick();
    }

    void EndTransition()
    {
        phase = TransitionPhase.Idle;
        canvasGroup.alpha = 0f;
        panelObj.SetActive(false);

        // Resume game
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

    // ─── UI Construction ──────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("MissionTransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // Above HUD, below GameComplete
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen panel
        panelObj = new GameObject("TransitionPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.02f, 0.05f, 0.95f);
        panelBg.raycastTarget = true; // Block clicks through
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        canvasGroup = panelObj.AddComponent<CanvasGroup>();

        // Chapter label (e.g. "Chapter 1 Complete")
        chapterLabel = CreateText(panelObj.transform, "", 20, new Color(0.7f, 0.6f, 0.4f),
            new Vector2(0, 140), new Vector2(600, 30), FontStyles.Normal, TextAlignmentOptions.Center);

        // Mission name (large)
        missionNameText = CreateText(panelObj.transform, "", 42, new Color(0.95f, 0.85f, 0.45f),
            new Vector2(0, 90), new Vector2(700, 55), FontStyles.Bold, TextAlignmentOptions.Center);

        // Description
        descriptionText = CreateText(panelObj.transform, "", 18, new Color(0.75f, 0.73f, 0.68f),
            new Vector2(0, 30), new Vector2(600, 80), FontStyles.Italic, TextAlignmentOptions.Center);

        // Status
        statusLabel = CreateText(panelObj.transform, "", 16, new Color(0.45f, 0.75f, 0.45f),
            new Vector2(0, -20), new Vector2(400, 25), FontStyles.Normal, TextAlignmentOptions.Center);

        // Divider line
        dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(panelObj.transform, false);
        Image divImg = dividerObj.AddComponent<Image>();
        divImg.color = new Color(0.5f, 0.45f, 0.3f, 0.4f);
        divImg.raycastTarget = false;
        RectTransform divRect = dividerObj.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.5f, 0.5f);
        divRect.anchorMax = new Vector2(0.5f, 0.5f);
        divRect.pivot = new Vector2(0.5f, 0.5f);
        divRect.anchoredPosition = new Vector2(0, -55);
        divRect.sizeDelta = new Vector2(400, 1);

        // "New Objectives:" label
        nextLabel = CreateText(panelObj.transform, "", 16, new Color(0.7f, 0.6f, 0.4f),
            new Vector2(0, -80), new Vector2(500, 25), FontStyles.Normal, TextAlignmentOptions.Center);

        // Next mission objectives list
        nextMissionName = CreateText(panelObj.transform, "", 17, new Color(0.85f, 0.83f, 0.78f),
            new Vector2(0, -120), new Vector2(500, 100), FontStyles.Normal, TextAlignmentOptions.Center);

        // (unused, kept for flexibility)
        nextDescription = CreateText(panelObj.transform, "", 15, new Color(0.6f, 0.58f, 0.55f),
            new Vector2(0, -170), new Vector2(500, 40), FontStyles.Normal, TextAlignmentOptions.Center);

        // Continue hint
        continueHint = CreateText(panelObj.transform, "", 14, new Color(0.5f, 0.5f, 0.5f),
            new Vector2(0, -220), new Vector2(400, 25), FontStyles.Normal, TextAlignmentOptions.Center);
    }

    TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, Color color,
        Vector2 position, Vector2 size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return tmp;
    }
}
