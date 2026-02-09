using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The player
    public float distance = 7f;
    public float rotationSpeed = 5f;
    public float smoothSpeed = 10f;

    [Header("Vertical Look")]
    public float minPitch = -20f;
    public float maxPitch = 60f;

    private float yaw = 0f;
    private float pitch = 20f;

    void LateUpdate()
    {
        if (target == null) return;
        if (PlayerMovement.inputBlocked) return;

        // Mouse rotation — horizontal and vertical
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * rotationSpeed;
        pitch -= mouseY * rotationSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Calculate camera position using pitch and yaw
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distance);
        Vector3 targetPosition = target.position + Vector3.up * 1.5f + offset;

        // Smoothly move camera
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // Look at player upper body
        transform.LookAt(target.position + Vector3.up * 1.5f);

        // Rotate player to match camera horizontal direction
        target.rotation = Quaternion.Euler(0, yaw, 0);
    }
}