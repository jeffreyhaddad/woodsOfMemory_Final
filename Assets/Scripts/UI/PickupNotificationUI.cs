using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows small "+N ItemName" popup labels when items are picked up.
/// Multiple pickups of the same item within the display window accumulate into one entry.
/// </summary>
public class PickupNotificationUI : MonoBehaviour
{
    public static PickupNotificationUI Instance { get; private set; }

    private Transform container;

    const float HoldDuration  = 2.2f;
    const float FadeInTime    = 0.12f;
    const float FadeOutTime   = 0.4f;
    const float SlotHeight    = 20f;

    // Tracks active entries so same-item pickups can accumulate
    private readonly Dictionary<string, ActiveEntry> active = new Dictionary<string, ActiveEntry>();

    class ActiveEntry
    {
        public TextMeshProUGUI label;
        public CanvasGroup group;
        public int qty;
        public float remaining; // time left before fade-out starts
        public string itemName;
    }

    void Awake()
    {
        Instance = this;
        BuildUI();
    }

    void Start()
    {
        Inventory inv = FindAnyObjectByType<Inventory>();
        if (inv != null)
            inv.OnItemAdded += Show;
    }

    void OnDestroy()
    {
        Inventory inv = FindAnyObjectByType<Inventory>();
        if (inv != null)
            inv.OnItemAdded -= Show;
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("PickupNotifCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Container: bottom-left, above hotbar
        GameObject containerObj = new GameObject("NotifContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        var layout = containerObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment     = TextAnchor.LowerLeft;
        layout.spacing            = 2f;
        layout.childControlWidth  = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;
        RectTransform ct = containerObj.GetComponent<RectTransform>();
        ct.anchorMin       = new Vector2(0f, 0f);
        ct.anchorMax       = new Vector2(0f, 0f);
        ct.pivot           = new Vector2(0f, 0f);
        ct.anchoredPosition = new Vector2(18f, 75f); // left side, above hotbar row
        ct.sizeDelta       = new Vector2(200f, 160f);
        container = containerObj.transform;
    }

    public void Show(string itemName, int qty)
    {
        if (string.IsNullOrEmpty(itemName)) return;
        if (SettingsManager.Instance != null && !SettingsManager.Instance.ShowPickupNotifications) return;

        if (active.TryGetValue(itemName, out ActiveEntry existing))
        {
            existing.qty      += qty;
            existing.remaining = HoldDuration;
            existing.group.alpha = 1f;
            UpdateLabel(existing);
            return;
        }

        StartCoroutine(RunEntry(itemName, qty));
    }

    IEnumerator RunEntry(string itemName, int qty)
    {
        GameObject obj = new GameObject("Notif");
        obj.transform.SetParent(container, false);

        // RectTransform must be added before any other UI component
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, SlotHeight);

        CanvasGroup group = obj.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
        label.fontSize      = 12;
        label.color         = Color.white;
        label.raycastTarget = false;
        label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        var entry = new ActiveEntry
        {
            label     = label,
            group     = group,
            qty       = qty,
            remaining = HoldDuration,
            itemName  = itemName
        };
        active[itemName] = entry;
        UpdateLabel(entry);

        // Fade in
        float t = 0f;
        while (t < FadeInTime)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / FadeInTime);
            yield return null;
        }
        group.alpha = 1f;

        // Hold (entry.remaining can be reset by accumulated pickups)
        while (entry.remaining > 0f)
        {
            entry.remaining -= Time.deltaTime;
            UpdateLabel(entry);
            yield return null;
        }

        // Fade out
        t = FadeOutTime;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / FadeOutTime);
            yield return null;
        }

        active.Remove(itemName);
        Destroy(obj);
    }

    static void UpdateLabel(ActiveEntry e)
    {
        e.label.text = $"<color=#aaff99>+{e.qty}</color>  {e.itemName}";
    }
}
