using UnityEngine;

/// <summary>
/// Place this on a GameObject with a trigger collider.
/// When the player enters, it reports a location reached to the MissionManager.
/// Use for "discover cabin" and "find exit" objectives.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionTrigger : MonoBehaviour
{
    [Tooltip("Name reported to MissionManager (e.g. 'cabin', 'exit')")]
    public string locationName = "location";

    [Tooltip("Destroy this trigger after activation so it only fires once")]
    public bool oneShot = true;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        // Only react to the player
        if (other.GetComponent<PlayerMovement>() == null &&
            other.GetComponentInParent<PlayerMovement>() == null)
            return;

        triggered = true;

        if (MissionManager.Instance != null)
            MissionManager.Instance.ReportLocationReached(locationName);

        // Map location name → compass POI label and guarantee discovery
        // even if the player somehow bypassed the proximity radius check.
        string poiLabel = locationName switch
        {
            "start_cabin" => "Main Cabin",
            "cabin"       => "Second Cabin",
            "exit"        => "Forest Exit",
            _             => null
        };
        if (poiLabel != null)
            CompassUI.DiscoverPOI(poiLabel);

        Debug.Log("MissionTrigger: Player reached " + locationName);

        if (oneShot)
            Destroy(gameObject);
    }
}
