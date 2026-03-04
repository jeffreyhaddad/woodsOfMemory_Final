using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Full-screen settings panel with Audio, Camera, and Keybinds tabs.
/// Programmatic UI — no prefabs or Canvas hierarchy needed in editor.
/// Call Open() / Close() from PauseMenuUI or WelcomeSceneScript.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    // ── Panel & Tabs ──────────────────────────────────────────
    private GameObject panelObj;
    private Action onCloseCallback;

    private enum Tab { Audio, Camera, Keybinds }
    private Tab activeTab = Tab.Audio;

    private GameObject audioTabContent;
    private GameObject cameraTabContent;
    private GameObject keybindsTabContent;

    private Button[] tabBtns;
    private Image[]  tabBtnImages;

    // ── Audio Tab ─────────────────────────────────────────────
    private Slider sfxSlider;
    private TextMeshProUGUI sfxValueText;
    private Slider musicSlider;
    private TextMeshProUGUI musicValueText;
    private Slider ambSlider;
    private TextMeshProUGUI ambValueText;

    // ── Camera Tab ────────────────────────────────────────────
    private Slider sensXSlider;
    private TextMeshProUGUI sensXValueText;
    private Slider sensYSlider;
    private TextMeshProUGUI sensYValueText;
    private Slider fovSlider;
    private TextMeshProUGUI fovValueText;
    private Button invertYBtn;
    private TextMeshProUGUI invertYBtnText;

    // ── Keybinds Tab ──────────────────────────────────────────
    private Dictionary<GameAction, Button>          keybindButtons   = new Dictionary<GameAction, Button>();
    private Dictionary<GameAction, TextMeshProUGUI> keybindBtnTexts  = new Dictionary<GameAction, TextMeshProUGUI>();
    private Dictionary<GameAction, Image>           keybindBtnImages = new Dictionary<GameAction, Image>();

    private GameAction? listeningAction = null;
    private TextMeshProUGUI conflictWarning;
    private float conflictTimer;

    // ── Colors ────────────────────────────────────────────────
    private static readonly Color BgDark     = new Color(0.10f, 0.10f, 0.10f, 0.97f);
    private static readonly Color BtnNormal  = new Color(0.22f, 0.22f, 0.22f, 0.95f);
    private static readonly Color TabActive  = new Color(0.30f, 0.28f, 0.12f, 0.98f);
    private static readonly Color TabInact   = new Color(0.18f, 0.18f, 0.18f, 0.95f);
    private static readonly Color SliderFill = new Color(0.25f, 0.65f, 0.25f, 1f);
    private static readonly Color ListenClr  = new Color(0.35f, 0.15f, 0.50f, 0.95f);
    private static readonly Color GreenClr   = new Color(0.20f, 0.70f, 0.20f, 0.95f);

    // ─────────────────────────────────────────────────────────

    void Start()
    {
        BuildUI();
        panelObj.SetActive(false);
    }

    void Update()
    {
        if (listeningAction.HasValue && Input.anyKeyDown)
        {
            foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(k)) continue;
                if (k == KeyCode.Escape)
                {
                    CancelListen();
                    break;
                }
                // Skip mouse buttons (they can't be set as keybinds here)
                if (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6) continue;

                GameAction act = listeningAction.Value;
                if (SettingsManager.Instance != null &&
                    SettingsManager.Instance.IsKeyConflict(act, k, out GameAction conflict))
                {
                    ShowConflict("Already bound to " + conflict.ToString());
                    CancelListen();
                    break;
                }

                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.SetKeybind(act, k);

                RefreshKeybindButton(act);
                CancelListen();
                break;
            }
        }

        // Conflict warning fade
        if (conflictTimer > 0f)
        {
            conflictTimer -= Time.unscaledDeltaTime;
            if (conflictTimer <= 0f && conflictWarning != null)
                conflictWarning.text = "";
        }
    }

    // ── Public API ────────────────────────────────────────────

    public void Open(Action onClose = null)
    {
        onCloseCallback = onClose;
        panelObj.SetActive(true);
        PopulateFromSettings();
        ShowTab(Tab.Audio);
    }

    public void Close()
    {
        listeningAction = null;
        panelObj.SetActive(false);
        onCloseCallback?.Invoke();
    }

    // ── Tab Switching ─────────────────────────────────────────

    void ShowTab(Tab tab)
    {
        activeTab = tab;
        audioTabContent.SetActive(tab == Tab.Audio);
        cameraTabContent.SetActive(tab == Tab.Camera);
        keybindsTabContent.SetActive(tab == Tab.Keybinds);
        RefreshTabColors();
    }

    void RefreshTabColors()
    {
        Tab[] tabs = { Tab.Audio, Tab.Camera, Tab.Keybinds };
        for (int i = 0; i < tabBtnImages.Length; i++)
            tabBtnImages[i].color = (tabs[i] == activeTab) ? TabActive : TabInact;
    }

    // ── Populate from SettingsManager ─────────────────────────

    void PopulateFromSettings()
    {
        if (SettingsManager.Instance == null) return;

        sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.SFXVolume);
        sfxValueText.text = Mathf.RoundToInt(SettingsManager.Instance.SFXVolume * 100f) + "%";

        musicSlider.SetValueWithoutNotify(SettingsManager.Instance.MusicVolume);
        musicValueText.text = Mathf.RoundToInt(SettingsManager.Instance.MusicVolume * 100f) + "%";

        ambSlider.SetValueWithoutNotify(SettingsManager.Instance.AmbientVolume);
        ambValueText.text = Mathf.RoundToInt(SettingsManager.Instance.AmbientVolume * 100f) + "%";

        sensXSlider.SetValueWithoutNotify(SettingsManager.Instance.SensitivityX);
        sensXValueText.text = SettingsManager.Instance.SensitivityX.ToString("F1");

        sensYSlider.SetValueWithoutNotify(SettingsManager.Instance.SensitivityY);
        sensYValueText.text = SettingsManager.Instance.SensitivityY.ToString("F1");

        fovSlider.SetValueWithoutNotify(SettingsManager.Instance.FieldOfView);
        fovValueText.text = Mathf.RoundToInt(SettingsManager.Instance.FieldOfView).ToString();

        bool inv = SettingsManager.Instance.InvertY;
        invertYBtnText.text = inv ? "ON" : "OFF";
        invertYBtn.GetComponent<Image>().color = inv ? GreenClr : BtnNormal;

        foreach (GameAction act in System.Enum.GetValues(typeof(GameAction)))
            RefreshKeybindButton(act);
    }

    void RefreshKeybindButton(GameAction act)
    {
        if (!keybindBtnTexts.ContainsKey(act)) return;
        KeyCode k = SettingsManager.Instance != null
            ? SettingsManager.Instance.GetKeybind(act)
            : SettingsManager.GetKey(act);
        keybindBtnTexts[act].text = k.ToString();
        keybindBtnImages[act].color = BtnNormal;
    }

    void CancelListen()
    {
        if (listeningAction.HasValue && keybindBtnTexts.ContainsKey(listeningAction.Value))
            RefreshKeybindButton(listeningAction.Value);
        listeningAction = null;
    }

    void ShowConflict(string msg)
    {
        if (conflictWarning == null) return;
        conflictWarning.text = msg;
        conflictTimer = 2f;
    }

    // ─── UI Construction ──────────────────────────────────────

    void BuildUI()
    {
        if (EventSystem.current == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Canvas
        GameObject canvasObj = new GameObject("SettingsCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dark overlay
        panelObj = new GameObject("SettingsPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image overlay = panelObj.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.75f);
        RectTransform overlayRect = panelObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;

        // Centered card (520 × 470)
        GameObject card = new GameObject("SettingsCard");
        card.transform.SetParent(panelObj.transform, false);
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = BgDark;
        cardBg.raycastTarget = false;
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(520, 470);

        // Title
        MakeLabel(card.transform, "SETTINGS", 26, new Vector2(0, 205), new Vector2(480, 36), Color.white);

        // Tab buttons row
        BuildTabRow(card.transform);

        // Tab content areas (all anchored below tab row)
        audioTabContent    = MakeContentArea(card.transform, "AudioTab");
        cameraTabContent   = MakeContentArea(card.transform, "CameraTab");
        keybindsTabContent = MakeContentArea(card.transform, "KeybindsTab");

        BuildAudioTab(audioTabContent.transform);
        BuildCameraTab(cameraTabContent.transform);
        BuildKeybindsTab(keybindsTabContent.transform);

        // Bottom buttons
        BuildBottomRow(card.transform);
    }

    void BuildTabRow(Transform parent)
    {
        string[] names = { "AUDIO", "CAMERA", "KEYBINDS" };
        tabBtns      = new Button[names.Length];
        tabBtnImages = new Image[names.Length];

        float totalW = 460f;
        float btnW   = totalW / names.Length;
        float startX = -totalW / 2f + btnW / 2f;
        float rowY   = 163f;

        for (int i = 0; i < names.Length; i++)
        {
            GameObject tabObj = new GameObject("TabBtn_" + names[i]);
            tabObj.transform.SetParent(parent, false);
            Image img = tabObj.AddComponent<Image>();
            img.color = (i == 0) ? TabActive : TabInact;
            tabBtnImages[i] = img;

            RectTransform r = tabObj.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot     = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(startX + i * btnW, rowY);
            r.sizeDelta = new Vector2(btnW - 4f, 30f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tabObj.transform, false);
            TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text      = names[i];
            txt.fontSize  = 14;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color     = Color.white;
            txt.raycastTarget = false;
            RectTransform tr = txt.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;

            Button btn = tabObj.AddComponent<Button>();
            btn.targetGraphic = img;
            tabBtns[i] = btn;
            Tab captured = (Tab)i;
            btn.onClick.AddListener(() => ShowTab(captured));
        }
    }

    GameObject MakeContentArea(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(0, 30f);
        r.sizeDelta = new Vector2(480f, 280f);
        return go;
    }

    // ── Audio Tab ─────────────────────────────────────────────

    void BuildAudioTab(Transform parent)
    {
        float startY = 110f;
        float step   = 70f;

        (sfxSlider,   sfxValueText)   = MakeSliderRow(parent, "SFX Volume",     0f, 1f, startY,         v =>
        {
            sfxValueText.text = Mathf.RoundToInt(v * 100f) + "%";
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetSFXVolume(v);
        });
        (musicSlider, musicValueText) = MakeSliderRow(parent, "Music Volume",   0f, 1f, startY - step,   v =>
        {
            musicValueText.text = Mathf.RoundToInt(v * 100f) + "%";
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetMusicVolume(v);
        });
        (ambSlider,   ambValueText)   = MakeSliderRow(parent, "Ambient Volume", 0f, 1f, startY - step*2, v =>
        {
            ambValueText.text = Mathf.RoundToInt(v * 100f) + "%";
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetAmbientVolume(v);
        });
    }

    // ── Camera Tab ────────────────────────────────────────────

    void BuildCameraTab(Transform parent)
    {
        float startY = 110f;
        float step   = 60f;

        (sensXSlider, sensXValueText) = MakeSliderRow(parent, "Sensitivity X", 0.5f, 5f, startY, v =>
        {
            sensXValueText.text = v.ToString("F1");
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetSensitivityX(v);
        });
        (sensYSlider, sensYValueText) = MakeSliderRow(parent, "Sensitivity Y", 0.5f, 5f, startY - step, v =>
        {
            sensYValueText.text = v.ToString("F1");
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetSensitivityY(v);
        });
        (fovSlider, fovValueText) = MakeSliderRow(parent, "Field of View", 60f, 110f, startY - step * 2, v =>
        {
            fovValueText.text = Mathf.RoundToInt(v).ToString();
            if (SettingsManager.Instance != null) SettingsManager.Instance.SetFOV(v);
        });

        // Invert Y row
        float rowY = startY - step * 3f;
        MakeLabel(parent, "Invert Y", 16, new Vector2(-90f, rowY), new Vector2(180f, 30f), Color.white);

        GameObject btnObj = new GameObject("InvertYBtn");
        btnObj.transform.SetParent(parent, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = BtnNormal;
        RectTransform br = btnObj.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
        br.pivot     = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = new Vector2(150f, rowY);
        br.sizeDelta = new Vector2(80f, 30f);

        GameObject btxtObj = new GameObject("Text");
        btxtObj.transform.SetParent(btnObj.transform, false);
        invertYBtnText = btxtObj.AddComponent<TextMeshProUGUI>();
        invertYBtnText.text = "OFF";
        invertYBtnText.fontSize = 15;
        invertYBtnText.alignment = TextAlignmentOptions.Center;
        invertYBtnText.color = Color.white;
        invertYBtnText.raycastTarget = false;
        RectTransform btxtRect = invertYBtnText.rectTransform;
        btxtRect.anchorMin = Vector2.zero;
        btxtRect.anchorMax = Vector2.one;
        btxtRect.offsetMin = btxtRect.offsetMax = Vector2.zero;

        invertYBtn = btnObj.AddComponent<Button>();
        invertYBtn.targetGraphic = btnImg;
        invertYBtn.onClick.AddListener(() =>
        {
            if (SettingsManager.Instance == null) return;
            bool newVal = !SettingsManager.Instance.InvertY;
            SettingsManager.Instance.SetInvertY(newVal);
            invertYBtnText.text = newVal ? "ON" : "OFF";
            btnImg.color = newVal ? GreenClr : BtnNormal;
        });
    }

    // ── Keybinds Tab ──────────────────────────────────────────

    void BuildKeybindsTab(Transform parent)
    {
        GameAction[] actions = (GameAction[])System.Enum.GetValues(typeof(GameAction));
        float startY = 120f;
        float step   = 38f;

        for (int i = 0; i < actions.Length; i++)
        {
            GameAction act = actions[i];
            float rowY = startY - i * step;

            MakeLabel(parent, act.ToString(), 15, new Vector2(-90f, rowY), new Vector2(180f, 28f), Color.white);

            GameObject btnObj = new GameObject("KbBtn_" + act);
            btnObj.transform.SetParent(parent, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = BtnNormal;
            RectTransform br = btnObj.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot     = new Vector2(0.5f, 0.5f);
            br.anchoredPosition = new Vector2(155f, rowY);
            br.sizeDelta = new Vector2(140f, 28f);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = SettingsManager.GetKey(act).ToString();
            txt.fontSize = 13;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.raycastTarget = false;
            RectTransform tr = txt.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            keybindButtons[act]   = btn;
            keybindBtnTexts[act]  = txt;
            keybindBtnImages[act] = btnImg;

            GameAction capturedAct = act;
            btn.onClick.AddListener(() => StartListening(capturedAct));
        }

        // Conflict warning
        GameObject warnObj = new GameObject("ConflictWarning");
        warnObj.transform.SetParent(parent, false);
        conflictWarning = warnObj.AddComponent<TextMeshProUGUI>();
        conflictWarning.text = "";
        conflictWarning.fontSize = 13;
        conflictWarning.alignment = TextAlignmentOptions.Center;
        conflictWarning.color = new Color(1f, 0.3f, 0.3f);
        conflictWarning.raycastTarget = false;
        RectTransform wr = conflictWarning.rectTransform;
        wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
        wr.pivot = new Vector2(0.5f, 0.5f);
        wr.anchoredPosition = new Vector2(0, startY - actions.Length * step - 12f);
        wr.sizeDelta = new Vector2(440f, 24f);
    }

    void StartListening(GameAction act)
    {
        // Cancel previous listening
        if (listeningAction.HasValue && listeningAction.Value != act)
            CancelListen();

        listeningAction = act;
        keybindBtnTexts[act].text = "[Press a key...]";
        keybindBtnImages[act].color = ListenClr;
    }

    // ── Bottom Row ────────────────────────────────────────────

    void BuildBottomRow(Transform parent)
    {
        MakeButton(parent, "Reset Defaults", new Vector2(-90f, -200f), () =>
        {
            if (SettingsManager.Instance != null)
                SettingsManager.Instance.ResetToDefaults();
            PopulateFromSettings();
        });

        MakeButton(parent, "Back", new Vector2(90f, -200f), Close);
    }

    // ── Reusable Helpers ──────────────────────────────────────

    (Slider, TextMeshProUGUI) MakeSliderRow(Transform parent, string labelText, float minVal, float maxVal, float y, UnityEngine.Events.UnityAction<float> onChange)
    {
        // Label
        MakeLabel(parent, labelText, 15, new Vector2(-105f, y), new Vector2(150f, 28f), Color.white);

        // Slider background
        GameObject sliderRoot = new GameObject("Slider_" + labelText);
        sliderRoot.transform.SetParent(parent, false);
        RectTransform sliderRootRect = sliderRoot.AddComponent<RectTransform>();
        sliderRootRect.anchorMin = sliderRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRootRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRootRect.anchoredPosition = new Vector2(65f, y);
        sliderRootRect.sizeDelta = new Vector2(220f, 20f);

        // Background image
        Image bgImg = sliderRoot.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderRoot.transform, false);
        RectTransform faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0, 0.25f);
        faRect.anchorMax = new Vector2(1, 0.75f);
        faRect.offsetMin = new Vector2(5, 0);
        faRect.offsetMax = new Vector2(-5, 0);

        // Fill
        GameObject fillImg = new GameObject("Fill");
        fillImg.transform.SetParent(fillArea.transform, false);
        Image fill = fillImg.AddComponent<Image>();
        fill.color = SliderFill;
        RectTransform fillRect = fillImg.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        // Handle slide area
        GameObject handleArea = new GameObject("HandleSlideArea");
        handleArea.transform.SetParent(sliderRoot.transform, false);
        RectTransform haRect = handleArea.AddComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = new Vector2(10, 0);
        haRect.offsetMax = new Vector2(-10, 0);

        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform hRect = handle.GetComponent<RectTransform>();
        hRect.sizeDelta = new Vector2(20f, 20f);

        // Slider component
        Slider slider = sliderRoot.AddComponent<Slider>();
        slider.fillRect   = fillRect;
        slider.handleRect = hRect;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue  = minVal;
        slider.maxValue  = maxVal;
        slider.value     = (minVal + maxVal) * 0.5f;
        slider.onValueChanged.AddListener(onChange);

        // Value label to the right
        GameObject valObj = new GameObject("ValText");
        valObj.transform.SetParent(parent, false);
        TextMeshProUGUI valTxt = valObj.AddComponent<TextMeshProUGUI>();
        valTxt.text = "";
        valTxt.fontSize = 14;
        valTxt.alignment = TextAlignmentOptions.Center;
        valTxt.color = new Color(0.85f, 0.85f, 0.85f);
        valTxt.raycastTarget = false;
        RectTransform vr = valTxt.rectTransform;
        vr.anchorMin = vr.anchorMax = new Vector2(0.5f, 0.5f);
        vr.pivot = new Vector2(0.5f, 0.5f);
        vr.anchoredPosition = new Vector2(200f, y);
        vr.sizeDelta = new Vector2(56f, 28f);

        return (slider, valTxt);
    }

    void MakeLabel(Transform parent, string text, float fontSize, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject("Lbl_" + text);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = color;
        tmp.raycastTarget = false;
        RectTransform r = tmp.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    void MakeButton(Transform parent, string label, Vector2 pos, Action onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = BtnNormal;
        RectTransform r = btnObj.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(160f, 38f);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 17;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.raycastTarget = false;
        RectTransform tr = txt.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick());
    }
}
