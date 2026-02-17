using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    // ============================================
    // VARIABLES - These control how the door works
    // ============================================

    [Header("Door Settings")]
    [Tooltip("How many degrees the door rotates when opened")]
    public float openAngle = 90f;

    [Tooltip("How fast the door opens/closes")]
    public float rotationSpeed = 3f;

    [Tooltip("Which key opens the door")]
    public KeyCode interactKey = KeyCode.E;

    [Header("State Tracking")]
    // These track what's happening with the door
    private bool isOpen = false;           // Is the door currently open?
    private bool isPlayerNear = false;     // Is the player close enough to interact?
    private bool isMoving = false;         // Is the door currently opening/closing?

    // These store rotation values
    private Quaternion closedRotation;     // The door's rotation when closed
    private Quaternion openRotation;       // The door's rotation when open
    private Quaternion targetRotation;     // Where we want the door to rotate to

    // ============================================
    // START - Runs once when the game begins
    // ============================================
    void Start()
    {
        // Save the door's starting rotation as "closed"
        closedRotation = transform.localRotation;

        // Calculate what "open" rotation looks like
        // We take the closed rotation and add the openAngle on the Y axis
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        // Door starts closed, so target is closed rotation
        targetRotation = closedRotation;
    }

    // ============================================
    // UPDATE - Runs every frame (60+ times per second)
    // ============================================
    void Update()
    {
        // CHECK FOR PLAYER INPUT
        // If player is near AND presses the interact key AND door isn't already moving
        if (isPlayerNear && Input.GetKeyDown(interactKey) && !isMoving)
        {
            // Toggle the door state (if open, close it; if closed, open it)
            ToggleDoor();
        }

        // ROTATE THE DOOR SMOOTHLY
        // If we're not already at our target rotation
        if (isMoving)
        {
            // Smoothly rotate towards the target
            // Quaternion.Lerp blends between current rotation and target rotation
            // Time.deltaTime makes it frame-rate independent
            // rotationSpeed controls how fast it happens
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,  // Where we are now
                targetRotation,           // Where we want to be
                Time.deltaTime * rotationSpeed  // How fast to get there
            );

            // Check if we've reached the target rotation
            // Quaternion.Angle gives us the difference in degrees
            if (Quaternion.Angle(transform.localRotation, targetRotation) < 0.5f)
            {
                // We're close enough, snap to exact target and stop moving
                transform.localRotation = targetRotation;
                isMoving = false;
            }
        }
    }

    // ============================================
    // TOGGLE DOOR - Switches between open and closed
    // ============================================
    void ToggleDoor()
    {
        // Flip the isOpen state
        isOpen = !isOpen;

        // Set the target rotation based on new state
        if (isOpen)
        {
            targetRotation = openRotation;
            Debug.Log("Door opening...");
        }
        else
        {
            targetRotation = closedRotation;
            Debug.Log("Door closing...");
        }

        // Start the movement
        isMoving = true;
    }

    // ============================================
    // TRIGGER DETECTION - Detects when player enters/exits
    // ============================================

    // This runs when something enters the trigger collider
    void OnTriggerEnter(Collider other)
    {
        // Check if the thing that entered is the player
        // We check the tag - make sure your Player has the "Player" tag!
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            Debug.Log("Player near door - Press " + interactKey + " to interact");
        }
    }

    // This runs when something exits the trigger collider
    void OnTriggerExit(Collider other)
    {
        // Check if it's the player leaving
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            Debug.Log("Player left door area");
        }
    }
}