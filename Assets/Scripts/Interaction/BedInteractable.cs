using UnityEngine;

public class BedInteractable : Interactable
{
    [Tooltip("Transform placed on the bed where the player lies during the sleep animation.")]
    [SerializeField] private Transform sleepAnchor;

    [Tooltip("Distance from the bed centre to stand when waking up.")]
    [SerializeField] private float wakeDistance = 1.5f;

    private void Update()
    {
        promptText = SleepManager.Instance != null && SleepManager.Instance.IsNightTime()
            ? "Sleep (Skip to Dawn)"
            : "Can only sleep at night";
    }

    public override void OnInteract()
    {
        if (SleepManager.Instance == null) return;
        if (!SleepManager.Instance.IsNightTime()) return;

        Vector3 sleepPos = sleepAnchor != null ? sleepAnchor.position : transform.position;
        // Face forward during lay-down animation
        Quaternion sleepRot = sleepAnchor != null ? sleepAnchor.rotation : transform.rotation;
        // Rotate 90° once the idle-sleeping state begins
        Quaternion sleepIdleRot = sleepRot * Quaternion.Euler(0f, 90f, 0f);

        Vector3 wakePos = sleepPos;
        Quaternion wakeRot = sleepRot;

        SleepManager.Instance.StartSleep(sleepPos, sleepRot, sleepIdleRot, wakePos, wakeRot);
    }

    // Try each side of the bed in order; return the first spot that isn't inside geometry.
    Vector3 FindClearWakePosition(float groundY)
    {
        Vector3[] candidates = new Vector3[]
        {
            transform.position + transform.right   * wakeDistance,
            transform.position - transform.right   * wakeDistance,
            transform.position + transform.forward * wakeDistance,
            transform.position - transform.forward * wakeDistance,
        };

        float capsuleRadius = 0.3f;
        float capsuleHeight = 1.8f;

        foreach (Vector3 c in candidates)
        {
            Vector3 pos    = new Vector3(c.x, groundY, c.z);
            Vector3 bottom = pos + Vector3.up * capsuleRadius;
            Vector3 top    = pos + Vector3.up * (capsuleHeight - capsuleRadius);

            if (!Physics.CheckCapsule(bottom, top, capsuleRadius))
                return pos;
        }

        // All sides blocked — fall back to the player's current position (always valid)
        return GameManager.Instance.PlayerVitals.transform.position;
    }
}
