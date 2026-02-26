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
    private Inventory inventory;

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

    public override void OnInteract()
    {
        if (!isUnlocked)
        {
            if (rustedKey != null && inventory != null && inventory.HasItem(rustedKey))
            {
                isUnlocked = true;
                inventory.RemoveItem(rustedKey, 1);
                Debug.Log("Door unlocked with Rusted Key");
            }
            else
            {
                Debug.Log("This door is locked. You need a rusted key.");
                return;
            }
        }

        isOpen = !isOpen;
        targetRotation = isOpen ? openRotation : closedRotation;
        isMoving = true;
        promptText = isOpen ? "Close Door" : "Open Door";
    }
}
