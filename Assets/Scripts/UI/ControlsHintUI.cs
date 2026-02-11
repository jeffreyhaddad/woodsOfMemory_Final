using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlsHintUI : MonoBehaviour
{
    [Header("Timing")]
    public float showDuration = 8f;
    public float fadeDuration = 1.5f;

    private CanvasGroup canvasGroup;
    private float timer;
    private bool fading = false;

    void Start()
    {
        BuildUI();
        timer = showDuration;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= fadeDuration && !fading)
            fading = true;

        if (fading)
        {
            float alpha = Mathf.Max(0, timer / fadeDuration);
            canvasGroup.alpha = alpha;
        }

        if (timer <= 0f)
            Destroy(canvasGroup.gameObject);
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("ControlsHintCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        // Container (bottom-left)
        GameObject container = new GameObject("ControlsPanel");
        container.transform.SetParent(canvasObj.transform, false);
        Image bg = container.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.5f);
        bg.raycastTarget = false;
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(15, 15);
        rect.sizeDelta = new Vector2(260, 250);

        string controls =
            "<b>Controls</b>\n" +
            "<color=#cccccc>" +
            "WASD - Move\n" +
            "Mouse - Look\n" +
            "Shift - Run\n" +
            "Ctrl - Crouch\n" +
            "Space - Jump\n" +
            "E - Interact/Pickup\n" +
            "LMB - Attack\n" +
            "Tab - Inventory\n" +
            "C - Crafting\n" +
            "J - Journal\n" +
            "Esc - Pause</color>";

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(container.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = controls;
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12, 8);
        textRect.offsetMax = new Vector2(-8, -8);
    }
}
