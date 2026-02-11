using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The player

    [Header("Camera Position")]
    [Tooltip("Distance behind the character")]
    public float distance = 3f;
    [Tooltip("Height of the orbit point (character's shoulder)")]
    public float shoulderHeight = 1.5f;
    [Tooltip("Horizontal offset — positive puts character on the left side of screen")]
    public float shoulderOffsetX = 0.7f;

    [Header("Mouse Sensitivity")]
    public float sensitivityX = 3f;
    public float sensitivityY = 2f;

    [Header("Vertical Limits")]
    public float minPitch = -35f;
    public float maxPitch = 55f;

    [Header("Smoothing")]
    public float positionSmoothing = 15f;

    [Header("Collision")]
    public float collisionRadius = 0.2f;
    public LayerMask collisionLayers = ~0;

    private float yaw;
    private float pitch = 8f;

    void Start()
    {
        // Initialize yaw from current player facing
        if (target != null)
            yaw = target.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (PlayerMovement.inputBlocked) return;

        // ── Mouse Input ──
        yaw += Input.GetAxis("Mouse X") * sensitivityX;
        pitch -= Input.GetAxis("Mouse Y") * sensitivityY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ── Rotate Player to Face Camera Direction ──
        target.rotation = Quaternion.Euler(0, yaw, 0);

        // ── Camera Orbit ──
        // Pivot point is at the character's shoulder
        Vector3 pivot = target.position + Vector3.up * shoulderHeight;

        // Camera sits behind and to the right of the pivot, respecting pitch
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 cameraOffset = orbitRotation * new Vector3(shoulderOffsetX, 0, -distance);
        Vector3 desiredPosition = pivot + cameraOffset;

        // ── Camera Collision ──
        // Pull camera forward if something blocks the line of sight
        Vector3 dirFromPivot = desiredPosition - pivot;
        float wantedDist = dirFromPivot.magnitude;

        if (Physics.SphereCast(pivot, collisionRadius, dirFromPivot.normalized, out RaycastHit hit, wantedDist, collisionLayers))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                // Place camera just in front of the hit
                desiredPosition = pivot + dirFromPivot.normalized * (hit.distance - collisionRadius * 0.5f);
            }
        }

        // ── Smooth Position ──
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothing * Time.deltaTime);

        // ── Look Direction ──
        // Camera looks at a point far ahead of the pivot, so the crosshair
        // (screen center) aims at what's in front of the player, not at the player
        Vector3 aimDirection = orbitRotation * Vector3.forward;
        Vector3 lookTarget = pivot + aimDirection * 50f;
        transform.LookAt(lookTarget);
    }
}
