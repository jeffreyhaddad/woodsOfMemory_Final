using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("How many degrees the door rotates when opened")]
    public float openAngle = 90f;

    [Tooltip("How fast the door opens/closes")]
    public float rotationSpeed = 3f;

    [Tooltip("Which key opens the door")]
    public KeyCode interactKey = KeyCode.E;

    private bool isOpen = false;
    private bool isPlayerNear = false;
    private bool isMoving = false;
    private Animator playerAnimator;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion targetRotation;

    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        targetRotation = closedRotation;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerAnimator = player.GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(interactKey) && !isMoving)
            ToggleDoor();

        if (isMoving)
        {
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
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;
        targetRotation = isOpen ? openRotation : closedRotation;
        isMoving = true;

        if (isOpen)
            playerAnimator?.SetTrigger("DoorOpen");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = false;
    }
}
