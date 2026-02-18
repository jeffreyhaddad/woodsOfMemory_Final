using UnityEngine;

/// <summary>
/// Simple bob/hover animation for pickups so they're easier to spot.
/// Staggered across 3 frames to reduce per-frame overhead when many pickups exist.
/// </summary>
public class PickupBob : MonoBehaviour
{
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;
    public float rotateSpeed = 30f;

    private Vector3 startPos;
    private int frameOffset;

    void Start()
    {
        startPos = transform.position;
        // Stagger updates across 3 frames so not all pickups update simultaneously
        frameOffset = GetInstanceID() % 3;
    }

    void Update()
    {
        if (Time.frameCount % 3 != frameOffset) return;

        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPos + Vector3.up * offset;
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime * 3f, Space.World);
    }
}
