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
    private GameObject promptContainer;

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

        promptContainer.SetActive(false);
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
                ShowPrompt(currentTarget.promptText); // only rebuild string on target change
            }

            currentTarget.OnFocus();
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
        promptUI.text = "<color=#ffe680>[E]</color>  " + text;
        promptContainer.SetActive(true);
    }

    void HidePrompt()
    {
        currentTarget = null;
        promptContainer.SetActive(false);
    }

    void CreatePromptUI()
    {
        GameObject canvasObj = new GameObject("InteractionPromptCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Small dark pill background — less jarring than bare white text
        promptContainer = new GameObject("PromptBG");
        promptContainer.transform.SetParent(canvasObj.transform, false);
        var bg = promptContainer.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0f, 0f, 0f, 0.48f);
        RectTransform bgRect = promptContainer.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.09f);
        bgRect.anchorMax = new Vector2(0.5f, 0.09f);
        bgRect.pivot     = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(160f, 20f);

        // Text inside the pill
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptContainer.transform, false);
        promptUI = textObj.AddComponent<TextMeshProUGUI>();
        promptUI.fontSize  = 11;
        promptUI.alignment = TextAlignmentOptions.Center;
        promptUI.color     = new Color(0.88f, 0.88f, 0.88f);
        promptUI.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        promptUI.raycastTarget    = false;
        RectTransform rect = promptUI.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(6f, 1f);
        rect.offsetMax = new Vector2(-6f, -1f);
    }
}
