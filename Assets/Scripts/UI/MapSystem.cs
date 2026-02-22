using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MapSystem — circular minimap (bottom-right) with fog-of-war + full-screen map (M key).
///
/// The map texture is baked from real terrain data at startup:
///   • Terrain layer splatmap → accurate ground colours (grass, dirt, rock, etc.)
///   • TerrainData.treeInstances → each tree painted as a dark-green canopy circle
///   • Low-lying areas tinted blue for water/rivers
///   • Fog-of-war reveals the map as the player explores
///
/// Auto-attached by GameManager — no editor setup required.
/// </summary>
public class MapSystem : MonoBehaviour
{
    [Header("View")]
    [Tooltip("World-space radius (metres) visible in the small minimap")]
    public float minimapRadius = 120f;
    [Tooltip("World-space radius of fog revealed around the player per tick")]
    public float revealRadius  = 40f;

    // ─── Texture resolutions ──────────────────────────────────
    private const int   MapRes    = 512;   // map texture  (higher = more detail, ~30 ms to bake)
    private const int   FogRes    = 256;   // fog texture  (lower = cheaper GPU upload per frame)
    private const float MmPx      = 180f;  // minimap circle diameter (pixels, at 1920×1080)
    private const float FullMapPx = 800f;  // full-map panel size

    // ─── Textures ─────────────────────────────────────────────
    private Texture2D mapTex;    // baked terrain + tree map
    private Texture2D fogTex;    // fog-of-war overlay
    private Color32[] fogPixels;
    private bool[]    fogRevealed;
    private bool      fogDirty;
    private float     fogTimer;

    // ─── Terrain bounds ───────────────────────────────────────
    private float tOX, tOZ, tSX, tSZ;

    // ─── References ───────────────────────────────────────────
    private Transform      player;
    private MissionManager missions;

    // ─── Minimap UI ───────────────────────────────────────────
    private RawImage mmMapImg;   // scrolls with player
    private RawImage mmFogImg;

    // ─── Full-map UI ──────────────────────────────────────────
    private GameObject    fullRoot;
    private RectTransform fpDot;
    private RectTransform foDot;

    // ─── Objective cache ──────────────────────────────────────
    private MissionTrigger[] triggerCache;
    private CreatureAI[]     creatureCache;
    private Vector3?         cachedObjPos;
    private float            scanCooldown;

    // ──────────────────────────────────────────────────────────

    void Start()
    {
        PlayerVitals pv = FindAnyObjectByType<PlayerVitals>();
        if (pv != null) player = pv.transform;
        missions = FindAnyObjectByType<MissionManager>();

        CacheTerrain();
        BakeMapTexture();    // build the detailed terrain + tree texture
        BuildFogTexture();
        BuildMinimapUI();
        BuildFullMapUI();
    }

    void Update()
    {
        if (player == null) return;

        if (GameManager.Instance != null)
        {
            GameState gs = GameManager.Instance.CurrentState;
            if (gs == GameState.Paused || gs == GameState.Dead) return;
        }

        if (Input.GetKeyDown(KeyCode.M))
            OpenFullMap(!fullRoot.activeSelf);

        fogTimer -= Time.deltaTime;
        if (fogTimer <= 0f) { RevealFog(); fogTimer = 0.1f; }

        if (fogDirty)
        {
            fogTex.SetPixels32(fogPixels);
            fogTex.Apply(false);
            fogDirty = false;
        }

        SyncMinimapUVs();

        if (fullRoot.activeSelf)
        {
            scanCooldown -= Time.deltaTime;
            if (scanCooldown <= 0f) { ScanObjective(); scanCooldown = 2f; }
            UpdateFullMapDots();
        }
    }

    // ─── Terrain ──────────────────────────────────────────────

    void CacheTerrain()
    {
        Terrain t = Terrain.activeTerrain;
        if (t != null)
        {
            tOX = t.transform.position.x;
            tOZ = t.transform.position.z;
            tSX = t.terrainData.size.x;
            tSZ = t.terrainData.size.z;
        }
        else { tOX = -500f; tOZ = -500f; tSX = 1000f; tSZ = 1000f; }
    }

    // ─── Map Texture Bake ─────────────────────────────────────
    // Reads actual terrain layer weights (splatmap) and tree positions
    // to produce a rich overhead map — no camera required.

    void BakeMapTexture()
    {
        mapTex = new Texture2D(MapRes, MapRes, TextureFormat.RGB24, false);
        mapTex.filterMode = FilterMode.Bilinear;
        mapTex.wrapMode   = TextureWrapMode.Clamp;

        Color[] pixels = new Color[MapRes * MapRes];

        Terrain t = Terrain.activeTerrain;
        if (t == null)
        {
            Color fill = new Color(0.18f, 0.34f, 0.12f);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
            mapTex.SetPixels(pixels); mapTex.Apply(false);
            return;
        }

        TerrainData td = t.terrainData;

        // ── 1. Determine layer colours ────────────────────────
        TerrainLayer[] layers = td.terrainLayers;
        Color[] layerColors = BuildLayerColors(layers);

        // ── 2. Get full splatmap (alphamap) ───────────────────
        int aRes = td.alphamapResolution;
        float[,,] alpha = td.GetAlphamaps(0, 0, aRes, aRes);
        // alpha[row, col, layer]  where row=Z, col=X in terrain space

        // ── 3. Get heightmap ──────────────────────────────────
        int   hmRes = td.heightmapResolution;
        float maxH  = td.size.y;
        if (maxH < 0.001f) maxH = 1f;

        // ── 4. Fill base colour from splatmap + height ────────
        for (int z = 0; z < MapRes; z++)
        for (int x = 0; x < MapRes; x++)
        {
            float nx = (float)x / (MapRes - 1);
            float nz = (float)z / (MapRes - 1);

            // Height (normalised)
            int hmX = Mathf.Clamp(Mathf.RoundToInt(nx * (hmRes - 1)), 0, hmRes - 1);
            int hmZ = Mathf.Clamp(Mathf.RoundToInt(nz * (hmRes - 1)), 0, hmRes - 1);
            float h = td.GetHeight(hmX, hmZ) / maxH;

            // Splatmap sample
            int aX = Mathf.Clamp(Mathf.RoundToInt(nx * (aRes - 1)), 0, aRes - 1);
            int aZ = Mathf.Clamp(Mathf.RoundToInt(nz * (aRes - 1)), 0, aRes - 1);

            Color c = Color.black;
            for (int l = 0; l < layerColors.Length; l++)
                c += layerColors[l] * alpha[aZ, aX, l];

            // If no splatmap layers, fall back to height colour
            if (layerColors.Length == 0) c = HeightToColor(h);

            // Water: tint very low-lying areas blue
            if (h < 0.05f) c = Color.Lerp(new Color(0.10f, 0.28f, 0.48f), c, h / 0.05f);

            // Subtle shadow: darken valleys, brighten peaks
            float shade = Mathf.Lerp(0.72f, 1.05f, h);
            c = new Color(c.r * shade, c.g * shade, c.b * shade);

            pixels[z * MapRes + x] = c;
        }

        // ── 5. Paint tree canopies ────────────────────────────
        PaintTrees(pixels, td);

        mapTex.SetPixels(pixels);
        mapTex.Apply(false);
    }

    // ── Layer colour table ─────────────────────────────────────
    // Matches by layer name keywords; falls back to index-based defaults.

    static Color[] BuildLayerColors(TerrainLayer[] layers)
    {
        if (layers == null || layers.Length == 0) return new Color[0];

        // Defaults by layer index (most terrains: 0=grass, 1=dirt, 2=rock, 3=cliff/snow)
        Color[] defaults =
        {
            new Color(0.22f, 0.40f, 0.14f), // grass
            new Color(0.36f, 0.28f, 0.16f), // dirt
            new Color(0.46f, 0.43f, 0.36f), // rock
            new Color(0.52f, 0.48f, 0.40f), // cliff
        };

        Color[] result = new Color[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            string n = (layers[i]?.name ?? "").ToLowerInvariant();

            if      (n.Contains("grass") || n.Contains("meadow"))           result[i] = new Color(0.22f, 0.40f, 0.14f);
            else if (n.Contains("dirt")  || n.Contains("ground") ||
                     n.Contains("soil")  || n.Contains("gravel"))           result[i] = new Color(0.36f, 0.28f, 0.16f);
            else if (n.Contains("rock")  || n.Contains("stone") ||
                     n.Contains("cliff") || n.Contains("mount"))            result[i] = new Color(0.46f, 0.43f, 0.36f);
            else if (n.Contains("sand"))                                    result[i] = new Color(0.62f, 0.56f, 0.36f);
            else if (n.Contains("snow")  || n.Contains("ice"))              result[i] = new Color(0.82f, 0.86f, 0.90f);
            else if (n.Contains("mud")   || n.Contains("marsh"))            result[i] = new Color(0.30f, 0.26f, 0.14f);
            else if (i < defaults.Length)                                   result[i] = defaults[i];
            else                                                            result[i] = new Color(0.22f, 0.38f, 0.14f);
        }
        return result;
    }

    static Color HeightToColor(float h)
    {
        if      (h < 0.05f) return new Color(0.10f, 0.28f, 0.48f); // water
        else if (h < 0.15f) return new Color(0.10f, 0.22f, 0.07f);
        else if (h < 0.30f) return new Color(0.18f, 0.34f, 0.11f);
        else if (h < 0.50f) return new Color(0.24f, 0.38f, 0.15f);
        else if (h < 0.70f) return new Color(0.34f, 0.40f, 0.22f);
        else                return new Color(0.52f, 0.49f, 0.40f);
    }

    // ── Tree painting ──────────────────────────────────────────
    // Each tree in terrainData.treeInstances is painted as a small
    // dark-green canopy disk, giving forest areas the clustered look
    // you see on real game maps.

    void PaintTrees(Color[] pixels, TerrainData td)
    {
        TreeInstance[] trees = td.treeInstances;
        if (trees == null || trees.Length == 0) return;

        // Two shades for natural-looking variation
        Color canopyDark = new Color(0.06f, 0.16f, 0.04f);
        Color canopyMid  = new Color(0.10f, 0.22f, 0.06f);

        int r = 3; // canopy radius in map pixels  (~2-3m per pixel at 512 res on 1km terrain)

        for (int i = 0; i < trees.Length; i++)
        {
            // tree.position.x/z are normalised [0,1] within terrain bounds
            int cx = Mathf.Clamp(Mathf.RoundToInt(trees[i].position.x * (MapRes - 1)), 0, MapRes - 1);
            int cz = Mathf.Clamp(Mathf.RoundToInt(trees[i].position.z * (MapRes - 1)), 0, MapRes - 1);

            for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                int dist2 = dx * dx + dz * dz;
                if (dist2 > r * r) continue;

                int px = Mathf.Clamp(cx + dx, 0, MapRes - 1);
                int pz = Mathf.Clamp(cz + dz, 0, MapRes - 1);
                int idx = pz * MapRes + px;

                // Outer ring = slightly lighter; centre = darkest
                Color target = (dist2 <= 1) ? canopyDark : canopyMid;
                pixels[idx] = Color.Lerp(pixels[idx], target, 0.90f);
            }
        }
    }

    // ─── Fog Texture ──────────────────────────────────────────

    void BuildFogTexture()
    {
        fogTex = new Texture2D(FogRes, FogRes, TextureFormat.RGBA32, false);
        fogTex.filterMode = FilterMode.Bilinear;
        fogTex.wrapMode   = TextureWrapMode.Clamp;

        fogPixels   = new Color32[FogRes * FogRes];
        fogRevealed = new bool[FogRes * FogRes];

        Color32 dark = new Color32(10, 10, 10, 242);
        for (int i = 0; i < fogPixels.Length; i++) fogPixels[i] = dark;

        fogTex.SetPixels32(fogPixels);
        fogTex.Apply(false);
    }

    void RevealFog()
    {
        float uvX = Mathf.Clamp01((player.position.x - tOX) / tSX);
        float uvZ = Mathf.Clamp01((player.position.z - tOZ) / tSZ);

        int cx = Mathf.RoundToInt(uvX * (FogRes - 1));
        int cz = Mathf.RoundToInt(uvZ * (FogRes - 1));
        int r  = Mathf.Max(10, Mathf.RoundToInt(revealRadius / tSX * FogRes));

        for (int z = Mathf.Max(0, cz - r); z <= Mathf.Min(FogRes - 1, cz + r); z++)
        for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(FogRes - 1, cx + r); x++)
        {
            int dx = x - cx, dz = z - cz;
            if (dx * dx + dz * dz > r * r) continue;

            int idx = z * FogRes + x;
            if (fogRevealed[idx]) continue;
            fogRevealed[idx] = true;
            fogPixels[idx]   = new Color32(0, 0, 0, 0);
            fogDirty         = true;
        }
    }

    // ─── Minimap UI ───────────────────────────────────────────

    void BuildMinimapUI()
    {
        Canvas    canvas = MakeCanvas("MinimapCanvas", 88);
        Sprite    knob   = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        Transform root   = canvas.transform;

        // Decorative ring
        {
            var go  = UIGo("Ring", root);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.10f, 0.05f, 1f);
            if (knob != null) { img.sprite = knob; img.type = Image.Type.Simple; }
            img.raycastTarget = false;
            SetBR(RT(go), MmPx + 10f, -15f, 15f);
        }

        // Circular mask
        var maskGO = UIGo("MmMask", root);
        {
            var img = maskGO.AddComponent<Image>();
            if (knob != null) { img.sprite = knob; img.type = Image.Type.Simple; }
            img.color = Color.white;
            img.raycastTarget = false;
            maskGO.AddComponent<Mask>().showMaskGraphic = false;
            SetBR(RT(maskGO), MmPx, -20f, 20f);
        }
        Transform mm = maskGO.transform;

        // Map texture (scrolled so player stays centred)
        {
            var go = UIGo("Map", mm);
            mmMapImg = go.AddComponent<RawImage>();
            mmMapImg.texture = mapTex;
            mmMapImg.raycastTarget = false;
            Stretch(RT(go));
        }

        // Fog overlay (same uvRect as map)
        {
            var go = UIGo("Fog", mm);
            mmFogImg = go.AddComponent<RawImage>();
            mmFogImg.texture = fogTex;
            mmFogImg.raycastTarget = false;
            Stretch(RT(go));
        }

        // Player dot (white, always at centre)
        {
            var go  = UIGo("PDot", mm);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            if (knob != null) { img.sprite = knob; img.type = Image.Type.Simple; }
            img.raycastTarget = false;
            var rt = RT(go); Centre(rt); rt.sizeDelta = new Vector2(8f, 8f);
        }

        // "N" north label
        {
            var go  = UIGo("N", mm);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "N"; tmp.fontSize = 11; tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(1f, 0.28f, 0.28f);
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -4f); rt.sizeDelta = new Vector2(18f, 16f);
        }

        // "[M] Map" key hint
        {
            var go  = UIGo("MHint", root);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "[M] Map"; tmp.fontSize = 10;
            tmp.color = new Color(0.7f, 0.65f, 0.5f, 0.75f);
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, MmPx + 26f);
            rt.sizeDelta = new Vector2(MmPx, 14f);
        }
    }

    // ─── Full-Map UI ──────────────────────────────────────────

    void BuildFullMapUI()
    {
        Canvas    canvas = MakeCanvas("FullMapCanvas", 92);
        Transform root   = canvas.transform;

        fullRoot = UIGo("FullRoot", root);
        Stretch(fullRoot.AddComponent<RectTransform>());
        Transform fr = fullRoot.transform;

        // Screen dimmer
        {
            var go  = UIGo("Dim", fr);
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.82f); img.raycastTarget = true;
            Stretch(RT(go));
        }

        // Border — BEFORE panel so it renders behind it
        {
            var go  = UIGo("Border", fr);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.20f, 0.10f, 1f); img.raycastTarget = false;
            var rt = RT(go); Centre(rt); rt.sizeDelta = new Vector2(FullMapPx + 6f, FullMapPx + 6f);
        }

        // Map panel — AFTER border so it renders on top
        var panel = UIGo("MapPanel", fr);
        {
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.06f, 0.10f, 0.04f, 1f); img.raycastTarget = false;
            var rt = RT(panel); Centre(rt); rt.sizeDelta = new Vector2(FullMapPx, FullMapPx);
        }
        Transform pt = panel.transform;

        // Full terrain map (uvRect 0,0,1,1 = entire world)
        {
            var go = UIGo("FullMap", pt);
            var ri = go.AddComponent<RawImage>();
            ri.texture = mapTex; ri.uvRect = new Rect(0f, 0f, 1f, 1f);
            ri.raycastTarget = false;
            Stretch(RT(go));
        }

        // Fog overlay (entire terrain)
        {
            var go = UIGo("FullFog", pt);
            var ri = go.AddComponent<RawImage>();
            ri.texture = fogTex; ri.uvRect = new Rect(0f, 0f, 1f, 1f);
            ri.raycastTarget = false;
            Stretch(RT(go));
        }

        // Player dot (cyan)
        {
            var go  = UIGo("FPDot", pt);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.9f, 1f, 1f); img.raycastTarget = false;
            fpDot = RT(go); Centre(fpDot); fpDot.sizeDelta = new Vector2(10f, 10f);
        }

        // Objective dot (yellow)
        {
            var go  = UIGo("FODot", pt);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 0.85f, 0.1f, 1f); img.raycastTarget = false;
            foDot = RT(go); Centre(foDot); foDot.sizeDelta = new Vector2(12f, 12f);
            go.SetActive(false);
        }

        // Title
        {
            var go  = UIGo("Title", fr);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "MAP"; tmp.fontSize = 22; tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.92f, 0.78f, 0.38f);
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            var rt = tmp.rectTransform;
            Centre(rt); rt.anchoredPosition = new Vector2(0f, FullMapPx * 0.5f + 30f);
            rt.sizeDelta = new Vector2(200f, 30f);
        }

        // Close hint
        {
            var go  = UIGo("CloseHint", fr);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "[M]  Close Map"; tmp.fontSize = 13;
            tmp.color = new Color(0.65f, 0.62f, 0.5f, 0.8f);
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            var rt = tmp.rectTransform;
            Centre(rt); rt.anchoredPosition = new Vector2(0f, -(FullMapPx * 0.5f + 24f));
            rt.sizeDelta = new Vector2(300f, 20f);
        }

        AddLegendLabel(fr, "● You",       new Color(0.2f, 0.9f, 1f),  -120f);
        AddLegendLabel(fr, "● Objective", new Color(1f, 0.85f, 0.1f),  100f);

        fullRoot.SetActive(false);
    }

    void AddLegendLabel(Transform parent, string text, Color color, float xOffset)
    {
        var go  = UIGo("Leg", parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 12; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
        var rt = tmp.rectTransform;
        Centre(rt);
        rt.anchoredPosition = new Vector2(xOffset, -(FullMapPx * 0.5f + 24f));
        rt.sizeDelta = new Vector2(130f, 18f);
    }

    // ─── Per-Frame Updates ────────────────────────────────────

    void SyncMinimapUVs()
    {
        if (mmMapImg == null || mmFogImg == null) return;

        float uvX = (player.position.x - tOX) / tSX;
        float uvZ = (player.position.z - tOZ) / tSZ;
        float uvW = Mathf.Clamp(minimapRadius * 2f / tSX, 0.01f, 1f);
        float uvH = Mathf.Clamp(minimapRadius * 2f / tSZ, 0.01f, 1f);

        var rect = new Rect(uvX - uvW * 0.5f, uvZ - uvH * 0.5f, uvW, uvH);
        mmMapImg.uvRect = rect;
        mmFogImg.uvRect = rect;
    }

    void UpdateFullMapDots()
    {
        float px = ((player.position.x - tOX) / tSX - 0.5f) * FullMapPx;
        float pz = ((player.position.z - tOZ) / tSZ - 0.5f) * FullMapPx;
        fpDot.anchoredPosition = new Vector2(px, pz);

        if (cachedObjPos.HasValue)
        {
            foDot.gameObject.SetActive(true);
            float ox = ((cachedObjPos.Value.x - tOX) / tSX - 0.5f) * FullMapPx;
            float oz = ((cachedObjPos.Value.z - tOZ) / tSZ - 0.5f) * FullMapPx;
            foDot.anchoredPosition = new Vector2(ox, oz);
        }
        else foDot.gameObject.SetActive(false);
    }

    // ─── Objective Scan ───────────────────────────────────────

    void ScanObjective()
    {
        cachedObjPos = null;
        if (missions == null) return;

        Mission m = missions.CurrentMission;
        if (m == null) return;

        for (int i = 0; i < m.objectives.Length; i++)
        {
            MissionObjective obj = m.objectives[i];
            if (obj.IsCompleted) continue;

            if (obj.objectiveType == ObjectiveType.ReachLocation)
            {
                if (triggerCache == null)
                    triggerCache = FindObjectsByType<MissionTrigger>(FindObjectsSortMode.None);
                for (int j = 0; j < triggerCache.Length; j++)
                {
                    if (triggerCache[j] == null) continue;
                    string ln = triggerCache[j].locationName;
                    bool match =
                        obj.description.IndexOf(ln, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (ln.IndexOf("cabin", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                         obj.description.IndexOf("cabin", System.StringComparison.OrdinalIgnoreCase) >= 0);
                    if (match) { cachedObjPos = triggerCache[j].transform.position; return; }
                }
            }
            else if (obj.objectiveType == ObjectiveType.KillCreature)
            {
                if (creatureCache == null)
                    creatureCache = FindObjectsByType<CreatureAI>(FindObjectsSortMode.None);
                float best = float.MaxValue;
                for (int j = 0; j < creatureCache.Length; j++)
                {
                    CreatureAI c = creatureCache[j];
                    if (c == null || c.CurrentHealth <= 0 || c.data == null) continue;
                    if (!c.data.creatureName.Equals(obj.targetCreatureName,
                            System.StringComparison.OrdinalIgnoreCase)) continue;
                    float d = (c.transform.position - player.position).sqrMagnitude;
                    if (d < best) { best = d; cachedObjPos = c.transform.position; }
                }
                if (cachedObjPos.HasValue) return;
            }
        }
    }

    // ─── Map Toggle ───────────────────────────────────────────

    void OpenFullMap(bool open)
    {
        fullRoot.SetActive(open);
        if (open) { ScanObjective(); scanCooldown = 2f; UpdateFullMapDots(); }
    }

    // ─── Layout Helpers ───────────────────────────────────────

    static Canvas MakeCanvas(string name, int sortOrder)
    {
        var go     = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight  = 0.5f;
        return canvas;
    }

    static GameObject UIGo(string name, Transform parent)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); return go; }

    static RectTransform RT(GameObject go)
        => go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();

    static void SetBR(RectTransform rt, float size, float ox, float oy)
    {
        rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(ox, oy); rt.sizeDelta = new Vector2(size, size);
    }

    static void Centre(RectTransform rt)
    { rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); }

    static void Stretch(RectTransform rt)
    { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
}
