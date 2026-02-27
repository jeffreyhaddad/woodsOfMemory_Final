using UnityEngine;

/// <summary>
/// Attached to world-dropped item spheres. Settles with physics for 0.6s,
/// then becomes interactable — press E to pick it up. Auto-destroys after 2 minutes.
/// </summary>
public class DroppedItem : Interactable
{
    public ItemData item;
    public int quantity = 1;

    private bool ready = false;

    void Start()
    {
        promptText = "Pick up " + (item != null ? item.itemName : "item");
        Invoke(nameof(EnablePickup), 0.6f);
        Destroy(gameObject, 120f);
    }

    void EnablePickup()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        ready = true;
    }

    public override void OnInteract()
    {
        if (!ready) return;

        Inventory inv = FindAnyObjectByType<Inventory>();
        if (inv == null) return;

        if (inv.AddItem(item, quantity))
            Destroy(gameObject);
    }
}
