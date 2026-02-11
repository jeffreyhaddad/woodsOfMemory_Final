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

    void Start()
    {
        cam = Camera.main;

        if (promptUI == null)
            CreatePromptUI();

        promptUI.gameObject.SetActive(false);
    }

    void Update()
    {
        CheckForInteractable();

        if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
        {
            currentTarget.OnInteract();
            HidePrompt();
        }
    }

    void CheckForInteractable()
    {
        // Find all colliders within interact distance of the player
        Collider[] nearby = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, interactDistance);

        Interactable closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < nearby.Length; i++)
        {
            // Skip self
            if (nearby[i].transform == transform || nearby[i].transform.IsChildOf(transform))
                continue;

            Interactable interactable = nearby[i].GetComponentInParent<Interactable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, nearby[i].transform.position);
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
        promptUI.fontSize = 24;
        promptUI.alignment = TextAlignmentOptions.Center;
        promptUI.color = Color.white;
        promptUI.enableWordWrapping = false;

        // Position at bottom-center of screen
        RectTransform rect = promptUI.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.15f);
        rect.anchorMax = new Vector2(0.5f, 0.15f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400f, 50f);
    }
}
