using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionHUD : MonoBehaviour
{
    private MissionManager missionManager;
    private TextMeshProUGUI missionTitle;
    private TextMeshProUGUI objectiveText;

    // Banner for mission start/complete notifications
    private TextMeshProUGUI bannerText;
    private Image bannerBg;
    private float bannerTimer;

    void Start()
    {
        missionManager = FindAnyObjectByType<MissionManager>();

        if (missionManager == null)
        {
            Debug.LogWarning("MissionHUD: No MissionManager found.");
            enabled = false;
            return;
        }

        BuildUI();

        missionManager.OnMissionStarted += OnMissionStarted;
        missionManager.OnObjectiveProgress += OnObjectiveProgress;
        missionManager.OnMissionCompleted += OnMissionCompleted;
        missionManager.OnAllMissionsCompleted += OnAllMissionsCompleted;

        // Show initial mission if already started
        RefreshDisplay();
    }

    void OnDestroy()
    {
        if (missionManager != null)
        {
            missionManager.OnMissionStarted -= OnMissionStarted;
            missionManager.OnObjectiveProgress -= OnObjectiveProgress;
            missionManager.OnMissionCompleted -= OnMissionCompleted;
            missionManager.OnAllMissionsCompleted -= OnAllMissionsCompleted;
        }
    }

    void Update()
    {
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
            if (bannerTimer <= 0f)
            {
                bannerBg.gameObject.SetActive(false);
            }
            else if (bannerTimer < 0.5f)
            {
                // Fade out
                float alpha = bannerTimer / 0.5f;
                bannerBg.color = new Color(0, 0, 0, 0.6f * alpha);
                bannerText.color = new Color(1, 1, 1, alpha);
            }
        }
    }

    void OnMissionStarted(Mission mission)
    {
        RefreshDisplay();
        ShowBanner("New Mission: " + mission.missionName);
    }

    void OnObjectiveProgress(MissionObjective objective)
    {
        RefreshDisplay();
    }

    void OnMissionCompleted(Mission mission)
    {
        ShowBanner("Mission Complete: " + mission.missionName);
        RefreshDisplay();
    }

    void OnAllMissionsCompleted()
    {
        missionTitle.text = "All Missions Complete";
        objectiveText.text = "You have uncovered the truth.";
        ShowBanner("You have escaped the woods!");
    }

    void RefreshDisplay()
    {
        Mission mission = missionManager.CurrentMission;
        if (mission == null)
        {
            missionTitle.text = "";
            objectiveText.text = "";
            return;
        }

        missionTitle.text = mission.missionName;

        string objectives = "";
        for (int i = 0; i < mission.objectives.Length; i++)
        {
            MissionObjective obj = mission.objectives[i];
            string check = obj.IsCompleted ? "<color=#66ff66>[Done]</color> " : "- ";
            objectives += check + obj.GetProgressText() + "\n";
        }
        objectiveText.text = objectives.TrimEnd('\n');
    }

    void ShowBanner(string text)
    {
        bannerBg.gameObject.SetActive(true);
        bannerBg.color = new Color(0, 0, 0, 0.6f);
        bannerText.text = text;
        bannerText.color = Color.white;
        bannerTimer = 4f;
    }

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("MissionHUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 85;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Mission info panel (top-right)
        GameObject panel = new GameObject("MissionPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.4f);
        panelBg.raycastTarget = false;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-15, -15);
        panelRect.sizeDelta = new Vector2(300, 120);

        // Mission title
        GameObject titleObj = new GameObject("MissionTitle");
        titleObj.transform.SetParent(panel.transform, false);
        missionTitle = titleObj.AddComponent<TextMeshProUGUI>();
        missionTitle.text = "";
        missionTitle.fontSize = 16;
        missionTitle.fontStyle = FontStyles.Bold;
        missionTitle.color = new Color(1f, 0.85f, 0.4f);
        missionTitle.alignment = TextAlignmentOptions.TopLeft;
        missionTitle.raycastTarget = false;
        RectTransform titleRect = missionTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -5);
        titleRect.sizeDelta = new Vector2(-16, 22);

        // Objective text
        GameObject objObj = new GameObject("ObjectiveText");
        objObj.transform.SetParent(panel.transform, false);
        objectiveText = objObj.AddComponent<TextMeshProUGUI>();
        objectiveText.text = "";
        objectiveText.fontSize = 13;
        objectiveText.color = new Color(0.85f, 0.85f, 0.85f);
        objectiveText.alignment = TextAlignmentOptions.TopLeft;
        objectiveText.raycastTarget = false;
        objectiveText.enableWordWrapping = true;
        RectTransform objRect = objectiveText.rectTransform;
        objRect.anchorMin = new Vector2(0, 0);
        objRect.anchorMax = new Vector2(1, 1);
        objRect.offsetMin = new Vector2(8, 8);
        objRect.offsetMax = new Vector2(-8, -30);

        // Center banner (for notifications)
        GameObject bannerObj = new GameObject("Banner");
        bannerObj.transform.SetParent(canvasObj.transform, false);
        bannerBg = bannerObj.AddComponent<Image>();
        bannerBg.color = new Color(0, 0, 0, 0.6f);
        bannerBg.raycastTarget = false;
        RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.75f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.75f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        bannerRect.sizeDelta = new Vector2(500, 50);

        GameObject bannerTextObj = new GameObject("BannerText");
        bannerTextObj.transform.SetParent(bannerObj.transform, false);
        bannerText = bannerTextObj.AddComponent<TextMeshProUGUI>();
        bannerText.text = "";
        bannerText.fontSize = 22;
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.color = Color.white;
        bannerText.raycastTarget = false;
        RectTransform btRect = bannerText.rectTransform;
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;

        bannerObj.SetActive(false);
    }
}
