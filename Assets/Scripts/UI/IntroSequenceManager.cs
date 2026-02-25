using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Orchestrates the opening intro sequence:
///   1. Block player input immediately.
///   2. Hold black screen (via OnGUI — frame-0 safe), then fade in.
///   3. Spawn a glowing note near the player.
///   4. Player reads note → full-screen Awakening text overlay.
///   5. Any key → unlock input, add Awakening journal entry, start missions.
///
/// Add this component to the "Manager" GameObject in TerrainScene.
/// </summary>
public class IntroSequenceManager : MonoBehaviour
{
    public static IntroSequenceManager Instance { get; private set; }

    // ── black fade (OnGUI-based, works on frame 0) ───────────
    private float      blackAlpha = 1f;
    private Texture2D  blackTex;

    /// <summary>True once the opening fade has fully finished.
    /// IntroNoteInteractable checks this so the note cannot be triggered
    /// while the screen is still black.</summary>
    public bool FadeComplete { get; private set; }

    // ── UI refs ──────────────────────────────────────────────
    private Transform         playerTransform;
    private TextMeshProUGUI   hintText;
    private Canvas            overlayCanvas;
    private bool              waitingForDismiss;

    // ── lifecycle ────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        PlayerMovement.inputBlocked = true;

        // 1×1 black texture — used by OnGUI every frame until the fade ends.
        // This is the only approach that is guaranteed to cover the screen on
        // frame 0 before Unity's Canvas layout system has run even once.
        blackTex = new Texture2D(1, 1);
        blackTex.SetPixel(0, 0, Color.black);
        blackTex.Apply();
    }

    void Start()
    {
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null) playerTransform = pm.transform;

        WarpPlayerToCabin();
        SpawnIntroNote();
        StartCoroutine(FadeInCoroutine());
    }

    void WarpPlayerToCabin()
    {
        if (playerTransform == null) return;

        // Find the cabin model in the scene and place the player just inside it.
        // Disable/re-enable CharacterController so it doesn't fight the teleport.
        GameObject cabin = GameObject.Find("Cabin");
        if (cabin == null)
        {
            Debug.LogWarning("IntroSequenceManager: No GameObject named 'Cabin' found — player stays at current position.");
            return;
        }

        // Place player near the back wall, centered, facing the door
        // cabin.transform.right with Y=180 points West (-X); negate it to go East (+X = back wall)
        Vector3 dest = cabin.transform.position
                       - cabin.transform.right * 4f
                       + Vector3.up * 1.2f;

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        playerTransform.position = dest;
        // Face toward the door (West = cabin.transform.right direction)
        playerTransform.rotation = Quaternion.LookRotation(cabin.transform.right, Vector3.up);
        if (cc != null) cc.enabled = true;

        // Re-sync the camera yaw so it immediately faces the same direction
        // as the warped player instead of whatever direction it started in.
        CameraFollow camFollow = FindAnyObjectByType<CameraFollow>();
        if (camFollow != null)
            camFollow.SyncYawWithTarget();

        Debug.Log("IntroSequenceManager: Warped player to cabin at " + dest);
    }

    void Update()
    {
        if (waitingForDismiss && Input.anyKeyDown)
            CompleteIntro();
    }

    // OnGUI is called every frame after the scene renders.
    // Drawing here is frame-0 safe — no layout pass required.
    void OnGUI()
    {
        if (blackAlpha <= 0f || blackTex == null) return;

        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, blackAlpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), blackTex);
        GUI.color = prev;
    }

    // ── note spawning ────────────────────────────────────────

    void SpawnIntroNote()
    {
        if (playerTransform == null) return;

        Vector3 pos = playerTransform.position
                      + playerTransform.forward * 1.5f
                      + Vector3.up * 0.8f;

        if (Terrain.activeTerrain != null)
            pos.y = Terrain.activeTerrain.SampleHeight(pos) + 0.8f;

        GameObject noteGO = new GameObject("IntroNote");
        noteGO.transform.position = pos;

        // Paper quad visual
        GameObject quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(quadGO.GetComponent<MeshCollider>());
        quadGO.transform.SetParent(noteGO.transform, false);
        quadGO.transform.localScale    = new Vector3(0.25f, 0.35f, 1f);
        quadGO.transform.localRotation = Quaternion.identity;

        Renderer rend = quadGO.GetComponent<Renderer>();
        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (lit != null)
        {
            rend.material       = new Material(lit);
            rend.material.color = new Color(0.95f, 0.90f, 0.75f); // parchment
        }
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        // Warm glow light
        GameObject lightGO = new GameObject("NoteGlow");
        lightGO.transform.SetParent(noteGO.transform, false);
        lightGO.transform.localPosition = Vector3.up * 0.3f;
        Light glow      = lightGO.AddComponent<Light>();
        glow.type       = LightType.Point;
        glow.color      = new Color(1f, 0.9f, 0.5f);
        glow.range      = 6f;
        glow.intensity  = 1.5f;
        glow.shadows    = LightShadows.None;

        noteGO.AddComponent<PickupBob>();

        IntroNoteInteractable interactable = noteGO.AddComponent<IntroNoteInteractable>();
        interactable.promptText = "Read the note";

        // Trigger collider so PlayerInteraction's OverlapSphere detects it
        SphereCollider col = noteGO.AddComponent<SphereCollider>();
        col.radius    = 0.5f;
        col.isTrigger = true;
    }

    // ── fade coroutine ───────────────────────────────────────

    IEnumerator FadeInCoroutine()
    {
        yield return new WaitForSeconds(1.5f);

        float duration = 2f;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed   += Time.deltaTime;
            blackAlpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        blackAlpha   = 0f;
        FadeComplete = true;

        ShowHint("There is a note nearby.   [E] to read.");
    }

    void ShowHint(string message)
    {
        // Canvas created here — 3.5 s into the session, layout is fully ready
        GameObject canvasGO = new GameObject("IntroHintCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject textGO = new GameObject("HintText");
        textGO.transform.SetParent(canvasGO.transform, false);
        hintText           = textGO.AddComponent<TextMeshProUGUI>();
        hintText.text      = message;
        hintText.fontSize  = 22;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color     = new Color(1f, 1f, 0.8f, 1f);

        RectTransform rt = hintText.rectTransform;
        rt.anchorMin = new Vector2(0f, 0.07f);
        rt.anchorMax = new Vector2(1f, 0.14f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ── note-read callback ───────────────────────────────────

    /// <summary>Called by IntroNoteInteractable when the player presses E.</summary>
    public void OnNoteRead()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);

        BuildAwakeningOverlay();

        // Wait one frame before listening for "any key" — the E press that
        // triggered this call would otherwise immediately dismiss the overlay.
        StartCoroutine(AllowDismissNextFrame());
    }

    IEnumerator AllowDismissNextFrame()
    {
        yield return null;
        waitingForDismiss = true;
    }

    // ── awakening overlay ────────────────────────────────────

    void BuildAwakeningOverlay()
    {
        GameObject canvasGO = new GameObject("IntroOverlayCanvas");
        overlayCanvas = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 299;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Dark background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.88f);
        StretchFull(bg.rectTransform);

        // Title
        TextMeshProUGUI title = MakeTMP(canvasGO.transform, "Title",
            "THE WOODS OF MEMORY", 42,
            new Color(1f, 0.85f, 0.3f),
            new Vector2(0.1f, 0.73f), new Vector2(0.9f, 0.86f));
        title.fontStyle = FontStyles.Bold;

        // Body
        MakeTMP(canvasGO.transform, "Body",
            GetAwakeningText(), 22,
            Color.white,
            new Vector2(0.1f, 0.30f), new Vector2(0.9f, 0.71f),
            wordWrap: true);

        // Prompt (pulsing alpha)
        TextMeshProUGUI prompt = MakeTMP(canvasGO.transform, "Prompt",
            "Press any key to continue...", 18,
            new Color(0.8f, 0.8f, 0.8f),
            new Vector2(0.1f, 0.07f), new Vector2(0.9f, 0.14f));

        StartCoroutine(PulseText(prompt));
    }

    TextMeshProUGUI MakeTMP(Transform parent, string goName, string text, float size,
                             Color color, Vector2 anchorMin, Vector2 anchorMax,
                             bool wordWrap = false)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = size;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.color              = color;
        tmp.enableWordWrapping = wordWrap;

        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return tmp;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    IEnumerator PulseText(TextMeshProUGUI tmp)
    {
        while (tmp != null)
        {
            float a = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * 1.5f));
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, a);
            yield return null;
        }
    }

    string GetAwakeningText()
    {
        JournalManager jm = JournalManager.Instance;
        if (jm != null && jm.startingEntries != null
            && jm.startingEntries.Length > 0 && jm.startingEntries[0] != null)
            return jm.startingEntries[0].body;

        return "I woke up in a cabin I don't recognize. The air is thick with the scent " +
               "of pine and damp earth. My head is pounding, and I can't remember how I " +
               "got here. Through the window, dense forest stretches in every direction. " +
               "I need to figure out where I am... and how to survive.";
    }

    // ── complete ─────────────────────────────────────────────

    void CompleteIntro()
    {
        waitingForDismiss = false;
        blackAlpha        = 0f;

        if (overlayCanvas != null)
            Destroy(overlayCanvas.gameObject);

        PlayerMovement.inputBlocked = false;
        Cursor.lockState            = CursorLockMode.Locked;
        Cursor.visible              = false;

        JournalManager jm = JournalManager.Instance;
        if (jm != null && jm.startingEntries != null && jm.startingEntries.Length > 0)
            jm.AddEntry(jm.startingEntries[0]);

        if (MissionManager.Instance != null)
            MissionManager.Instance.BeginMissions();

        // Destroy only this component, NOT the whole Manager GameObject
        Destroy(this);
    }
}
