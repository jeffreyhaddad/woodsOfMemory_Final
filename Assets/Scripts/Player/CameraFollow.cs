using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The player

    [Header("Camera Position")]
    [Tooltip("Distance behind the player")]
    public float distance = 4f;
    [Tooltip("Height above the player's feet for the orbit pivot")]
    public float pivotHeight = 3.8f;
    [Tooltip("Horizontal offset (positive = character on left side of screen)")]
    public float shoulderOffset = 0.2f;

    [Header("Field of View")]
    [Tooltip("Camera FOV — Fortnite uses ~80")]
    public float fieldOfView = 80f;

    [Header("Mouse Sensitivity")]
    public float sensitivityX = 2.5f;
    public float sensitivityY = 1.5f;

    [Header("Vertical Limits")]
    public float minPitch = -40f;
    public float maxPitch = 70f;

    [Header("Collision")]
    public float collisionRadius = 0.2f;
    public LayerMask collisionLayers = ~0;
    [Tooltip("Speed at which camera eases back out after a collision clip")]
    public float collisionRecoverySpeed = 8f;

    private float yaw;
    private float pitch = 12f;
    private float currentDistance;
    private Camera cam;

    void Start()
    {
        if (target != null)
            yaw = target.eulerAngles.y;

        currentDistance = distance;
        cam = GetComponent<Camera>();
        if (cam != null)
            cam.fieldOfView = fieldOfView;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (PlayerMovement.inputBlocked) return;

        // ── FOV (allow runtime tweaks) ──
        if (cam != null && cam.fieldOfView != fieldOfView)
            cam.fieldOfView = fieldOfView;

        // ── Mouse Input (no smoothing = responsive) ──
        yaw += Input.GetAxis("Mouse X") * sensitivityX;
        pitch -= Input.GetAxis("Mouse Y") * sensitivityY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ── Rotate Player Body to Match Camera Yaw ──
        target.rotation = Quaternion.Euler(0f, yaw, 0f);

        // ── Compute Camera Position ──
        // Pivot is well above the character's head so the crosshair
        // (screen center) sits above the character, not on them
        Vector3 pivot = target.position + Vector3.up * pivotHeight;
        Quaternion orbitRot = Quaternion.Euler(pitch, yaw, 0f);

        // Full-distance offset in camera-local space (right + back)
        Vector3 idealOffset = orbitRot * new Vector3(shoulderOffset, 0f, -distance);
        float idealMag = idealOffset.magnitude;
        Vector3 offsetDir = idealOffset / idealMag; // normalized

        // ── Collision ──
        float clippedDist = idealMag;

        if (Physics.SphereCast(pivot, collisionRadius, offsetDir, out RaycastHit hit, idealMag, collisionLayers))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
                clippedDist = Mathf.Max(hit.distance - collisionRadius, 0.5f);
        }

        // Snap closer instantly; ease back out slowly
        if (clippedDist < currentDistance)
            currentDistance = clippedDist;
        else
            currentDistance = Mathf.MoveTowards(currentDistance, clippedDist, collisionRecoverySpeed * Time.deltaTime);

        // ── Apply Position (no Lerp = zero input lag) ──
        transform.position = pivot + offsetDir * currentDistance;

        // ── Camera Rotation = Orbit Rotation (no LookAt) ──
        transform.rotation = orbitRot;
    }
}
