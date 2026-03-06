using UnityEngine;

/// <summary>
/// Attach to the Cabin_Door GameObject.
/// Requires a Collider on the door so PlayerInteraction can detect it.
/// Press E within range to open/close with a smooth rotation.
/// </summary>
public class CabinDoorInteractable : Interactable
{
    [Header("Door Settings")]
    [Tooltip("Degrees to rotate on the Y axis when opened")]
    public float openAngle = 90f;

    [Tooltip("Speed of the open/close animation")]
    public float rotationSpeed = 4f;

    [Header("Lock Settings")]
    public ItemData rustedKey;

    private bool isOpen = false;
    private bool isMoving = false;
    private bool isUnlocked = false;
    private bool hasReportedObjective = false;
    private Inventory inventory;
    private float lockedMessageTimer;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion targetRotation;

    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        targetRotation = closedRotation;
        inventory = FindAnyObjectByType<Inventory>();

        promptText = "Open Door";
    }

    void Update()
    {
        if (lockedMessageTimer > 0f) lockedMessageTimer -= Time.deltaTime;

        if (!isMoving) return;

        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );

        if (Quaternion.Angle(transform.localRotation, targetRotation) < 0.5f)
        {
            transform.localRotation = targetRotation;
            isMoving = false;
        }
    }

    void OnGUI()
    {
        if (lockedMessageTimer <= 0f) return;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = new Color(1f, 0.85f, 0.3f);
        style.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height - 130, 400, 30),
            "This door is locked. You need a Rusted Key.", style);
    }

    public override void OnInteract()
    {
        if (!isUnlocked)
        {
            // rustedKey may be null if not assigned in the Inspector (item is runtime-created),
            // so fall back to a name lookup via ItemRegistry.
            if (rustedKey == null)
                rustedKey = ItemRegistry.Get("Rusted Key");

            if (rustedKey != null && inventory != null && inventory.HasItem(rustedKey))
            {
                isUnlocked = true;
                inventory.RemoveItem(rustedKey, 1); // consume the key
            }
            else
            {
                lockedMessageTimer = 2.5f;
                return;
            }
        }

        isOpen = !isOpen;
        targetRotation = isOpen ? openRotation : closedRotation;
        isMoving = true;
        promptText = isOpen ? "Close Door" : "Open Door";

        if (isOpen && !hasReportedObjective)
        {
            hasReportedObjective = true;
            MissionManager.Instance?.ReportLocationReached("cabin_area");
        }
    }
}
