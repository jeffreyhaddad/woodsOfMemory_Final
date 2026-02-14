using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Horizontal compass bar at the top of the screen.
/// Shows cardinal directions and markers pointing toward current mission objectives.
/// Finds objective targets dynamically: MissionTriggers, PickupItems, CreatureAIs.
/// Auto-creates its own UI — no editor setup required.
/// </summary>
public class CompassUI : MonoBehaviour
{
    [Header("Compass")]
    [Tooltip("Width of the compass bar in pixels")]
    public float compassWidth = 600f;
    [Tooltip("How often to scan for objective targets (seconds)")]
    public float scanInterval = 2f;
    [Tooltip("Max markers visible at once")]
    public int maxMarkers = 5;

    private Transform playerTransform;
    private MissionManager missionManager;

    // UI
    private RectTransform compassBar;
    private RectTransform compassMask;
    private TextMeshProUGUI[] cardinalLabels;
    private List<ObjectiveMarker> markers = new List<ObjectiveMarker>();
    private GameObject markerPool;

    // Scanning
    private float scanTimer;
    private List<TrackedTarget> trackedTargets = new List<TrackedTarget>();

    private struct TrackedTarget
    {
        public Vector3 position;
        public string label;
        public Color color;
    }

    private class ObjectiveMarker
    {
        public GameObject root;
        public RectTransform rect;
        public Image icon;
        public TextMeshProUGUI distText;
        public TextMeshProUGUI nameText;
    }

    // Cardinal direction setup
    private static readonly string[] cardinalNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private static readonly float[] cardinalAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

    void Start()
    {
        missionManager = FindAnyObjectByType<MissionManager>();
        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        if (pv != null) playerTransform = pv.transform;

        if (playerTransform == null || missionManager == null)
        {
            enabled = false;
            return;
        }

        BuildUI();
        scanTimer = 0f; // Scan immediately
    }

    void Update()
    {
        if (playerTransform == null) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

        // Periodic scan for targets
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            ScanForTargets();
            scanTimer = scanInterval;
        }

        UpdateCompass();
        UpdateMarkers();
    }

    // ─── Compass Logic ────────────────────────────────────────

    void UpdateCompass()
    {
        float playerYaw = playerTransform.eulerAngles.y;

        // Move cardinal labels along the bar
        for (int i = 0; i < cardinalLabels.Length; i++)
        {
            float angle = cardinalAngles[i];
            float offset = GetCompassOffset(angle, playerYaw);
            cardinalLabels[i].rectTransform.anchoredPosition = new Vector2(offset, 0);

            // Fade labels near edges
            float absOffset = Mathf.Abs(offset);
            float half = compassWidth * 0.5f;
            float alpha = Mathf.Clamp01(1f - (absOffset / half));
            Color c = cardinalLabels[i].color;
            c.a = alpha;
            cardinalLabels[i].color = c;
        }
    }

    void UpdateMarkers()
    {
        float playerYaw = playerTransform.eulerAngles.y;

        // Hide all markers first
        for (int i = 0; i < markers.Count; i++)
            markers[i].root.SetActive(false);

        // Show markers for tracked targets
        int shown = 0;
        for (int t = 0; t < trackedTargets.Count && shown < markers.Count; t++)
        {
            TrackedTarget target = trackedTargets[t];
            Vector3 dir = target.position - playerTransform.position;
            dir.y = 0;
            float dist = dir.magnitude;

            float targetAngle = Quaternion.LookRotation(dir).eulerAngles.y;
            float offset = GetCompassOffset(targetAngle, playerYaw);

            // Only show if within compass range
            float half = compassWidth * 0.5f;
            if (Mathf.Abs(offset) > half) continue;

            ObjectiveMarker m = markers[shown];
            m.root.SetActive(true);
            m.rect.anchoredPosition = new Vector2(offset, -22);
            m.icon.color = target.color;

            // Distance text
            if (dist > 1000f)
                m.distText.text = (dist / 1000f).ToString("0.0") + "km";
            else
                m.distText.text = Mathf.RoundToInt(dist) + "m";

            m.nameText.text = target.label;

            // Fade near edges
            float alpha = Mathf.Clamp01(1f - (Mathf.Abs(offset) / half));
            Color ic = m.icon.color; ic.a = alpha; m.icon.color = ic;
            Color dc = m.distText.color; dc.a = alpha; m.distText.color = dc;
            Color nc = m.nameText.color; nc.a = alpha; m.nameText.color = nc;

            shown++;
        }
    }

    float GetCompassOffset(float worldAngle, float playerYaw)
    {
        // Difference between world angle and player facing
        float delta = Mathf.DeltaAngle(playerYaw, worldAngle);
        // Map to compass bar position (180 degrees = full width)
        return (delta / 180f) * compassWidth;
    }

    // ─── Target Scanning ──────────────────────────────────────

    void ScanForTargets()
    {
        trackedTargets.Clear();

        Mission mission = missionManager.CurrentMission;
        if (mission == null) return;

        for (int i = 0; i < mission.objectives.Length; i++)
        {
            MissionObjective obj = mission.objectives[i];
            if (obj.IsCompleted) continue;

            switch (obj.objectiveType)
            {
                case ObjectiveType.ReachLocation:
                    ScanForTriggers(obj.description);
                    break;

                case ObjectiveType.CollectItem:
                    ScanForPickups(obj.targetItemName, obj.description);
                    break;

                case ObjectiveType.KillCreature:
                    ScanForCreatures(obj.targetCreatureName, obj.description);
                    break;

                case ObjectiveType.CraftItem:
                    // Crafting is done at the inventory — no world position
                    // Point toward nearest resource relevant to crafting
                    break;

                case ObjectiveType.SurviveNight:
                    // No specific location
                    break;
            }
        }

        // Sort by distance so closest targets get shown first
        trackedTargets.Sort((a, b) =>
        {
            float da = Vector3.Distance(playerTransform.position, a.position);
            float db = Vector3.Distance(playerTransform.position, b.position);
            return da.CompareTo(db);
        });
    }

    void ScanForTriggers(string objectiveDesc)
    {
        MissionTrigger[] triggers = FindObjectsByType<MissionTrigger>(FindObjectsSortMode.None);
        for (int i = 0; i < triggers.Length; i++)
        {
            // Match cabin triggers to cabin objectives, exit to exit objectives
            string locName = triggers[i].locationName.ToLower();
            string desc = objectiveDesc.ToLower();
            if (desc.Contains(locName) || desc.Contains("cabin") && locName.Contains("cabin")
                || desc.Contains("exit") && locName.Contains("exit"))
            {
                trackedTargets.Add(new TrackedTarget
                {
                    position = triggers[i].transform.position,
                    label = triggers[i].locationName,
                    color = new Color(1f, 0.85f, 0.3f) // Gold
                });
            }
        }
    }

    void ScanForPickups(string itemName, string objectiveDesc)
    {
        if (string.IsNullOrEmpty(itemName)) return;

        PickupItem[] pickups = FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        int found = 0;
        float closestDist = float.MaxValue;
        PickupItem closest = null;

        for (int i = 0; i < pickups.Length; i++)
        {
            if (pickups[i].itemData == null) continue;
            if (pickups[i].itemData.itemName != itemName) continue;

            float dist = Vector3.Distance(playerTransform.position, pickups[i].transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = pickups[i];
            }
            found++;
        }

        // Only show the closest pickup to avoid compass clutter
        if (closest != null)
        {
            trackedTargets.Add(new TrackedTarget
            {
                position = closest.transform.position,
                label = itemName + (found > 1 ? " (+" + (found - 1) + " more)" : ""),
                color = new Color(0.4f, 0.85f, 1f) // Light blue
            });
        }
    }

    void ScanForCreatures(string creatureName, string objectiveDesc)
    {
        if (string.IsNullOrEmpty(creatureName)) return;

        CreatureAI[] creatures = FindObjectsByType<CreatureAI>(FindObjectsSortMode.None);
        float closestDist = float.MaxValue;
        CreatureAI closest = null;
        int found = 0;

        for (int i = 0; i < creatures.Length; i++)
        {
            if (creatures[i].data == null) continue;
            if (!creatures[i].data.creatureName.Equals(creatureName, System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (creatures[i].CurrentHealth <= 0) continue;

            float dist = Vector3.Distance(playerTransform.position, creatures[i].transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = creatures[i];
            }
            found++;
        }

        if (closest != null)
        {
            trackedTargets.Add(new TrackedTarget
            {
                position = closest.transform.position,
                label = creatureName + (found > 1 ? " (+" + (found - 1) + " more)" : ""),
                color = new Color(1f, 0.35f, 0.35f) // Red
            });
        }
    }

    // ─── UI Construction ──────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("CompassCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Compass container with mask (clips content outside the bar)
        GameObject maskObj = new GameObject("CompassMask");
        maskObj.transform.SetParent(canvasObj.transform, false);
        compassMask = maskObj.AddComponent<RectTransform>();
        compassMask.anchorMin = new Vector2(0.5f, 1f);
        compassMask.anchorMax = new Vector2(0.5f, 1f);
        compassMask.pivot = new Vector2(0.5f, 1f);
        compassMask.anchoredPosition = new Vector2(0, -8);
        compassMask.sizeDelta = new Vector2(compassWidth + 20f, 55f);

        // Background bar
        Image maskBg = maskObj.AddComponent<Image>();
        maskBg.color = new Color(0f, 0f, 0f, 0.35f);
        maskBg.raycastTarget = false;
        Mask mask = maskObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Center tick mark
        GameObject tick = new GameObject("CenterTick");
        tick.transform.SetParent(maskObj.transform, false);
        Image tickImg = tick.AddComponent<Image>();
        tickImg.color = new Color(1f, 1f, 1f, 0.7f);
        tickImg.raycastTarget = false;
        RectTransform tickRect = tick.GetComponent<RectTransform>();
        tickRect.anchorMin = new Vector2(0.5f, 1f);
        tickRect.anchorMax = new Vector2(0.5f, 1f);
        tickRect.pivot = new Vector2(0.5f, 1f);
        tickRect.anchoredPosition = Vector2.zero;
        tickRect.sizeDelta = new Vector2(2f, 10f);

        // Compass bar (holds cardinal labels)
        GameObject barObj = new GameObject("CompassBar");
        barObj.transform.SetParent(maskObj.transform, false);
        compassBar = barObj.AddComponent<RectTransform>();
        compassBar.anchorMin = new Vector2(0.5f, 0.5f);
        compassBar.anchorMax = new Vector2(0.5f, 0.5f);
        compassBar.pivot = new Vector2(0.5f, 0.5f);
        compassBar.anchoredPosition = new Vector2(0, 4);
        compassBar.sizeDelta = new Vector2(compassWidth, 20f);

        // Cardinal direction labels
        cardinalLabels = new TextMeshProUGUI[cardinalNames.Length];
        for (int i = 0; i < cardinalNames.Length; i++)
        {
            GameObject lbl = new GameObject("Cardinal_" + cardinalNames[i]);
            lbl.transform.SetParent(compassBar.transform, false);
            TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
            tmp.text = cardinalNames[i];
            tmp.fontSize = (i % 2 == 0) ? 16 : 12; // Main directions bigger
            tmp.fontStyle = (i % 2 == 0) ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;

            // N=white, S=white, E/W=light gray, diagonals=darker
            if (cardinalNames[i] == "N")
                tmp.color = new Color(1f, 0.3f, 0.3f); // Red for North
            else if (i % 2 == 0)
                tmp.color = new Color(0.9f, 0.9f, 0.9f);
            else
                tmp.color = new Color(0.6f, 0.6f, 0.6f);

            RectTransform r = tmp.rectTransform;
            r.sizeDelta = new Vector2(30, 20);
            cardinalLabels[i] = tmp;
        }

        // Objective markers pool
        markerPool = new GameObject("MarkerPool");
        markerPool.transform.SetParent(maskObj.transform, false);

        for (int i = 0; i < maxMarkers; i++)
        {
            ObjectiveMarker m = new ObjectiveMarker();

            m.root = new GameObject("Marker_" + i);
            m.root.transform.SetParent(markerPool.transform, false);
            m.rect = m.root.AddComponent<RectTransform>();
            m.rect.anchorMin = new Vector2(0.5f, 0.5f);
            m.rect.anchorMax = new Vector2(0.5f, 0.5f);
            m.rect.pivot = new Vector2(0.5f, 0.5f);
            m.rect.sizeDelta = new Vector2(12, 12);

            // Diamond icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(m.root.transform, false);
            m.icon = iconObj.AddComponent<Image>();
            m.icon.color = Color.yellow;
            m.icon.raycastTarget = false;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(10, 10);
            iconRect.localRotation = Quaternion.Euler(0, 0, 45); // Diamond shape

            // Distance label
            GameObject distObj = new GameObject("Dist");
            distObj.transform.SetParent(m.root.transform, false);
            m.distText = distObj.AddComponent<TextMeshProUGUI>();
            m.distText.text = "";
            m.distText.fontSize = 10;
            m.distText.alignment = TextAlignmentOptions.Center;
            m.distText.color = new Color(0.8f, 0.8f, 0.8f);
            m.distText.raycastTarget = false;
            m.distText.enableWordWrapping = false;
            RectTransform distRect = m.distText.rectTransform;
            distRect.anchorMin = new Vector2(0.5f, 0.5f);
            distRect.anchorMax = new Vector2(0.5f, 0.5f);
            distRect.pivot = new Vector2(0.5f, 1f);
            distRect.anchoredPosition = new Vector2(0, -8);
            distRect.sizeDelta = new Vector2(80, 14);

            // Name label (above icon)
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(m.root.transform, false);
            m.nameText = nameObj.AddComponent<TextMeshProUGUI>();
            m.nameText.text = "";
            m.nameText.fontSize = 9;
            m.nameText.alignment = TextAlignmentOptions.Center;
            m.nameText.color = new Color(0.9f, 0.85f, 0.6f);
            m.nameText.raycastTarget = false;
            m.nameText.enableWordWrapping = false;
            RectTransform nameRect = m.nameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0, 8);
            nameRect.sizeDelta = new Vector2(100, 14);

            m.root.SetActive(false);
            markers.Add(m);
        }
    }
}
