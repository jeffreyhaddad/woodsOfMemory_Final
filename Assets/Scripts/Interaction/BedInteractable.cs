using UnityEngine;

public class BedInteractable : Interactable
{
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

        PlayerVitals vitals = GameManager.Instance.PlayerVitals;
        float standingY = vitals.transform.position.y;

        Vector3 wakePos = FindClearWakePosition(standingY);
        Quaternion wakeRot = Quaternion.LookRotation(transform.position - wakePos, Vector3.up);

        SleepManager.Instance.StartSleep(wakePos, wakeRot);
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
