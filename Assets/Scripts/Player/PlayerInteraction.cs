using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Max distance to detect interactable objects")]
    public float interactDistance = 4f;

    [Header("UI")]
    [Tooltip("Leave empty to auto-create the prompt UI")]
    public TextMeshProUGUI promptUI;

    private Camera cam;
    private Interactable currentTarget;

    /// <summary>Public read-only access to the current interaction target.</summary>
    public Interactable CurrentTarget => currentTarget;

    // Pre-allocated buffer for non-allocating physics query
    private readonly Collider[] overlapBuffer = new Collider[16];
    private int checkCounter;

    void Start()
    {
        cam = Camera.main;

        if (promptUI == null)
            CreatePromptUI();

        promptUI.gameObject.SetActive(false);
    }

    void Update()
    {
        // Only scan every 5th frame — interaction detection doesn't need per-frame precision
        if (++checkCounter % 5 == 0)
            CheckForInteractable();

        if (currentTarget != null && Input.GetKeyDown(SettingsManager.GetKey(GameAction.Interact)))
        {
            currentTarget.OnInteract();
            HidePrompt();
        }
    }

    void CheckForInteractable()
    {
        // Non-allocating overlap query using pre-allocated buffer
        int count = Physics.OverlapSphereNonAlloc(
            transform.position + Vector3.up * 0.5f, interactDistance, overlapBuffer);

        Interactable closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            // Skip self
            if (overlapBuffer[i].transform == transform || overlapBuffer[i].transform.IsChildOf(transform))
                continue;

            Interactable interactable = overlapBuffer[i].GetComponentInParent<Interactable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, overlapBuffer[i].transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        if (closest != null)
        {
            if (currentTarget != closest)
            {
                if (currentTarget != null)
                    currentTarget.OnLoseFocus();
                currentTarget = closest;
            }

            currentTarget.OnFocus();
            ShowPrompt(currentTarget.promptText);
        }
        else
        {
            if (currentTarget != null)
            {
                currentTarget.OnLoseFocus();
                currentTarget = null;
            }
            HidePrompt();
        }
    }

    void ShowPrompt(string text)
    {
        promptUI.text = "[E] " + text;
        promptUI.gameObject.SetActive(true);
    }

    void HidePrompt()
    {
        currentTarget = null;
        promptUI.gameObject.SetActive(false);
    }

    void CreatePromptUI()
    {
        // Create a screen-space canvas for the prompt
        GameObject canvasObj = new GameObject("InteractionPromptCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Create the text element
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(canvasObj.transform, false);

        promptUI = textObj.AddComponent<TextMeshProUGUI>();
        promptUI.text = "[E] Interact";
        promptUI.fontSize = 15;
        promptUI.alignment = TextAlignmentOptions.Center;
        promptUI.color = Color.white;
        promptUI.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

        // Position at bottom-center of screen
        RectTransform rect = promptUI.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.15f);
        rect.anchorMax = new Vector2(0.5f, 0.15f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300f, 30f);
    }
}
