using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Horizontal compass bar — cardinal directions + discovery-based POI markers.
///
/// Inspired by RDR2 / The Witcher 3: world landmarks are hidden until the player
/// physically explores near them. Once discovered they remain permanently.
///
/// Other systems register landmarks via the static RegisterPOI() method.
/// Auto-discovered POIs (e.g. main cabin where the player spawns) are visible
/// from the very start; everything else is revealed through exploration.
/// </summary>
public class CompassUI : MonoBehaviour
{
    // ─── POI types ────────────────────────────────────────────

    public enum POIType { Location, Fire, Exit }

    private class POI
    {
        public string  label;
        public Vector3 worldPos;
        public Color   color;
        public POIType type;
        public float   discoveryRadius; // metres — enter this to discover
        public bool    discovered;
    }

    // ─── Static registry — survives Start() ordering issues ───

    private static readonly List<POI> _pois = new List<POI>();

    /// <summary>
    /// Register a world landmark.
    /// startDiscovered = true  → visible immediately (player already knows this place).
    /// startDiscovered = false → hidden; appears once player enters discoveryRadius.
    /// Safe to call before CompassUI.Start() or from any other Start().
    /// </summary>
    public static void RegisterPOI(
        string  label,
        Vector3 worldPos,
        bool    startDiscovered,
        float   discoveryRadius = 30f,
        Color   color           = default,
        POIType type            = POIType.Location)
    {
        if (color == default(Color)) color = new Color(1f, 0.85f, 0.3f);

        // Deduplicate — same label within 5 m counts as the same POI
        for (int i = 0; i < _pois.Count; i++)
            if (_pois[i].label == label && Vector3.Distance(_pois[i].worldPos, worldPos) < 5f)
                return;

        _pois.Add(new POI
        {
            label           = label,
            worldPos        = worldPos,
            color           = color,
            type            = type,
            discoveryRadius = discoveryRadius,
            discovered      = startDiscovered
        });
    }

    /// <summary>
    /// Immediately mark a POI as discovered by exact label match.
    /// Useful as a backup when the player enters a MissionTrigger collider.
    /// </summary>
    public static void DiscoverPOI(string label)
    {
        for (int i = 0; i < _pois.Count; i++)
            if (_pois[i].label == label) { _pois[i].discovered = true; return; }
    }

    // ─── Settings ─────────────────────────────────────────────

    [Header("Compass Bar")]
    [Tooltip("Pixel width of the visible compass strip")]
    public float compassWidth = 600f;

    [Header("Discovery")]
    [Tooltip("How often (seconds) to check if the player has discovered a new POI")]
    public float discoveryCheckRate = 0.4f;

    [Header("Markers")]
    [Tooltip("Maximum POI markers visible at once")]
    public int maxMarkers = 8;

    // ─── Runtime ──────────────────────────────────────────────

    private Transform         player;
    private RectTransform     compassBar;
    private TextMeshProUGUI[] cardinalLabels;
    private List<POIMarker>   markerPool = new List<POIMarker>();
    private float             discoveryTimer;

    // Cardinals
    private static readonly string[] CardinalNames  = {"N","NE","E","SE","S","SW","W","NW"};
    private static readonly float[]  CardinalAngles = {0f,45f,90f,135f,180f,225f,270f,315f};

    private class POIMarker
    {
        public GameObject      root;
        public RectTransform   rect;
        public Image           icon;
        public TextMeshProUGUI distLabel;
        public TextMeshProUGUI nameLabel;
    }

    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        // Clear on each new play session so stale scene data never leaks
        _pois.Clear();
    }

    void Start()
    {
        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        if (pv != null) player = pv.transform;
        if (player == null) { enabled = false; return; }

        // Auto-register all FinalBoneFireScript objects that exist in the scene
        // (player-placed ones will register themselves via RegisterPOI when lit)
        ScanSceneFires();

        BuildUI();
        discoveryTimer = 0f; // check immediately on first Update
    }

    void Update()
    {
        if (player == null) return;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

        discoveryTimer -= Time.deltaTime;
        if (discoveryTimer <= 0f)
        {
            CheckDiscoveries();
            discoveryTimer = discoveryCheckRate;
        }

        UpdateCompass();
        UpdateMarkers();
    }

    // ─── Discovery check ──────────────────────────────────────

    void CheckDiscoveries()
    {
        Vector3 pp = player.position;
        for (int i = 0; i < _pois.Count; i++)
        {
            if (_pois[i].discovered) continue;
            if (Vector3.Distance(pp, _pois[i].worldPos) <= _pois[i].discoveryRadius)
                _pois[i].discovered = true;
        }
    }

    void ScanSceneFires()
    {
        FinalBoneFireScript[] fires =
            FindObjectsByType<FinalBoneFireScript>(FindObjectsSortMode.None);

        for (int i = 0; i < fires.Length; i++)
        {
            RegisterPOI("Campfire", fires[i].transform.position,
                startDiscovered: false,
                discoveryRadius: 18f,
                new Color(1f, 0.55f, 0.15f),
                POIType.Fire);
        }
    }

    // ─── Compass rendering ────────────────────────────────────

    void UpdateCompass()
    {
        float yaw  = player.eulerAngles.y;
        float half = compassWidth * 0.5f;

        for (int i = 0; i < cardinalLabels.Length; i++)
        {
            float offset = CompassOffset(CardinalAngles[i], yaw);
            cardinalLabels[i].rectTransform.anchoredPosition = new Vector2(offset, 0);

            float alpha = Mathf.Clamp01(1f - Mathf.Abs(offset) / half);
            Color c = cardinalLabels[i].color;
            c.a = alpha;
            cardinalLabels[i].color = c;
        }
    }

    void UpdateMarkers()
    {
        float yaw  = player.eulerAngles.y;
        float half = compassWidth * 0.5f;
        Vector3 pp = player.position;

        // Reset all markers
        for (int i = 0; i < markerPool.Count; i++)
            markerPool[i].root.SetActive(false);

        int shown = 0;
        for (int i = 0; i < _pois.Count && shown < markerPool.Count; i++)
        {
            POI poi = _pois[i];
            if (!poi.discovered) continue;

            Vector3 toTarget = poi.worldPos - pp;
            toTarget.y = 0;
            float dist = toTarget.magnitude;

            // Skip if the player is standing on top of it (avoids zero-vector LookRotation)
            if (dist < 2f) continue;

            float angle  = Quaternion.LookRotation(toTarget).eulerAngles.y;
            float offset = CompassOffset(angle, yaw);

            // Skip if outside the visible compass strip
            if (Mathf.Abs(offset) > half) continue;

            POIMarker m = markerPool[shown];
            m.root.SetActive(true);
            m.rect.anchoredPosition = new Vector2(offset, -22);
            m.icon.color = poi.color;

            // Distance label — suppress when very close (already there)
            if (dist < 20f)
                m.distLabel.text = "";
            else if (dist >= 1000f)
                m.distLabel.text = (dist / 1000f).ToString("0.0") + "km";
            else
                m.distLabel.text = Mathf.RoundToInt(dist) + "m";

            m.nameLabel.text = poi.label;

            // Fade near the edges of the compass strip
            float edgeFade = Mathf.Clamp01(1f - Mathf.Abs(offset) / half);
            SetAlpha(m.icon,       edgeFade);
            SetAlpha(m.distLabel,  edgeFade);
            SetAlpha(m.nameLabel,  edgeFade);

            shown++;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────

    float CompassOffset(float worldAngle, float playerYaw)
        => (Mathf.DeltaAngle(playerYaw, worldAngle) / 180f) * compassWidth;

    static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    // ─── UI Construction ──────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("CompassCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Mask — clips content outside the bar
        GameObject maskObj = new GameObject("CompassMask");
        maskObj.transform.SetParent(canvasObj.transform, false);
        RectTransform compassMask = maskObj.AddComponent<RectTransform>();
        compassMask.anchorMin = new Vector2(0.5f, 1f);
        compassMask.anchorMax = new Vector2(0.5f, 1f);
        compassMask.pivot     = new Vector2(0.5f, 1f);
        compassMask.anchoredPosition = new Vector2(0, -8);
        compassMask.sizeDelta        = new Vector2(compassWidth + 20f, 55f);

        Image maskBg = maskObj.AddComponent<Image>();
        maskBg.color         = new Color(0f, 0f, 0f, 0.35f);
        maskBg.raycastTarget = false;
        maskObj.AddComponent<Mask>().showMaskGraphic = true;

        // Center tick
        GameObject tick = new GameObject("CenterTick");
        tick.transform.SetParent(maskObj.transform, false);
        Image tickImg = tick.AddComponent<Image>();
        tickImg.color         = new Color(1f, 1f, 1f, 0.7f);
        tickImg.raycastTarget = false;
        RectTransform tickRect = tick.GetComponent<RectTransform>();
        tickRect.anchorMin = tickRect.anchorMax = new Vector2(0.5f, 1f);
        tickRect.pivot           = new Vector2(0.5f, 1f);
        tickRect.anchoredPosition = Vector2.zero;
        tickRect.sizeDelta        = new Vector2(2f, 10f);

        // Compass bar (parent for scrolling cardinal labels)
        GameObject barObj = new GameObject("CompassBar");
        barObj.transform.SetParent(maskObj.transform, false);
        compassBar = barObj.AddComponent<RectTransform>();
        compassBar.anchorMin = compassBar.anchorMax = new Vector2(0.5f, 0.5f);
        compassBar.pivot             = new Vector2(0.5f, 0.5f);
        compassBar.anchoredPosition  = new Vector2(0, 4);
        compassBar.sizeDelta         = new Vector2(compassWidth, 20f);

        // Cardinal labels
        cardinalLabels = new TextMeshProUGUI[CardinalNames.Length];
        for (int i = 0; i < CardinalNames.Length; i++)
        {
            GameObject lbl = new GameObject("Cardinal_" + CardinalNames[i]);
            lbl.transform.SetParent(compassBar.transform, false);

            TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
            tmp.text    = CardinalNames[i];
            tmp.fontSize = (i % 2 == 0) ? 16 : 12;
            tmp.fontStyle = (i % 2 == 0) ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

            tmp.color = CardinalNames[i] == "N"
                ? new Color(1f, 0.3f, 0.3f)       // North = red
                : (i % 2 == 0)
                    ? new Color(0.9f, 0.9f, 0.9f)  // E / S / W = light grey
                    : new Color(0.6f, 0.6f, 0.6f); // diagonals = dark grey

            RectTransform r = tmp.rectTransform;
            r.sizeDelta = new Vector2(30, 20);
            cardinalLabels[i] = tmp;
        }

        // POI marker pool
        GameObject pool = new GameObject("MarkerPool");
        pool.transform.SetParent(maskObj.transform, false);

        for (int i = 0; i < maxMarkers; i++)
        {
            POIMarker m = new POIMarker();

            m.root = new GameObject("Marker_" + i);
            m.root.transform.SetParent(pool.transform, false);
            m.rect = m.root.AddComponent<RectTransform>();
            m.rect.anchorMin = m.rect.anchorMax = new Vector2(0.5f, 0.5f);
            m.rect.pivot     = new Vector2(0.5f, 0.5f);
            m.rect.sizeDelta = new Vector2(12, 12);

            // Diamond icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(m.root.transform, false);
            m.icon               = iconObj.AddComponent<Image>();
            m.icon.color         = Color.yellow;
            m.icon.raycastTarget = false;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot      = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta  = new Vector2(10, 10);
            iconRect.localRotation = Quaternion.Euler(0, 0, 45); // rotated square = diamond

            // Distance label (below icon)
            GameObject distObj = new GameObject("Dist");
            distObj.transform.SetParent(m.root.transform, false);
            m.distLabel = distObj.AddComponent<TextMeshProUGUI>();
            m.distLabel.text         = "";
            m.distLabel.fontSize     = 10;
            m.distLabel.alignment    = TextAlignmentOptions.Center;
            m.distLabel.color        = new Color(0.8f, 0.8f, 0.8f);
            m.distLabel.raycastTarget = false;
            m.distLabel.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            RectTransform distRect = m.distLabel.rectTransform;
            distRect.anchorMin = distRect.anchorMax = new Vector2(0.5f, 0.5f);
            distRect.pivot           = new Vector2(0.5f, 1f);
            distRect.anchoredPosition = new Vector2(0, -8);
            distRect.sizeDelta        = new Vector2(80, 14);

            // Name label (above icon)
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(m.root.transform, false);
            m.nameLabel = nameObj.AddComponent<TextMeshProUGUI>();
            m.nameLabel.text         = "";
            m.nameLabel.fontSize     = 9;
            m.nameLabel.alignment    = TextAlignmentOptions.Center;
            m.nameLabel.color        = new Color(0.9f, 0.85f, 0.6f);
            m.nameLabel.raycastTarget = false;
            m.nameLabel.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            RectTransform nameRect = m.nameLabel.rectTransform;
            nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot           = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0, 8);
            nameRect.sizeDelta        = new Vector2(100, 14);

            m.root.SetActive(false);
            markerPool.Add(m);
        }
    }
}
