using UnityEngine;

/// <summary>
/// Simple bob/hover animation for pickups so they're easier to spot.
/// </summary>
public class PickupBob : MonoBehaviour
{
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;
    public float rotateSpeed = 30f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPos + Vector3.up * offset;
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }
}
