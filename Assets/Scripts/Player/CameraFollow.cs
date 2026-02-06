using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The player
    public float distance = 7f;
    public float height = 5f;
    public float rotationSpeed = 5f;
    public float smoothSpeed = 10f;
    
    private float currentRotationAngle = 0f;
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Mouse rotation
        float mouseX = Input.GetAxis("Mouse X");
        currentRotationAngle += mouseX * rotationSpeed;
        
        // Calculate camera position behind player
        Quaternion rotation = Quaternion.Euler(0, currentRotationAngle, 0);
        Vector3 targetPosition = target.position - rotation * Vector3.forward * distance + Vector3.up * height;
        
        // Smoothly move camera
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        
        // Look at player
        transform.LookAt(target.position + Vector3.up * 2f);
        
        // Rotate player to match camera direction
        target.rotation = Quaternion.Euler(0, currentRotationAngle, 0);
    }
}