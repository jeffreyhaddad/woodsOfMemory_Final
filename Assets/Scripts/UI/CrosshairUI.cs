using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair")]
    public float size = 4f;
    public Color color = new Color(1f, 1f, 1f, 0.5f);
    public Color interactColor = new Color(1f, 0.85f, 0.3f, 0.9f);

    private Image dot;
    private PlayerInteraction interaction;

    void Start()
    {
        interaction = FindAnyObjectByType<PlayerInteraction>();
        BuildUI();
    }

    void Update()
    {
        // Hide when UI is open
        bool hidden = PlayerMovement.inputBlocked;
        dot.enabled = !hidden;

        // Change color when near an interactable
        if (!hidden && interaction != null)
        {
            bool hasTarget = interaction.CurrentTarget != null;
            dot.color = hasTarget ? interactColor : color;
        }
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("CrosshairCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject dotObj = new GameObject("Dot");
        dotObj.transform.SetParent(canvasObj.transform, false);
        dot = dotObj.AddComponent<Image>();
        dot.color = color;
        dot.raycastTarget = false;

        RectTransform rect = dotObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
    }
}
