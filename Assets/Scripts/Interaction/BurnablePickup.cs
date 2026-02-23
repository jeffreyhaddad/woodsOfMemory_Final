using UnityEngine;

/// <summary>
/// Attached to a cooked-food pickup near the bonfire.
/// Visually shifts from golden → orange → charred as the burn timer counts down.
/// Destroys itself (burning the food) if not picked up in time.
/// </summary>
public class BurnablePickup : MonoBehaviour
{
    [Tooltip("Seconds before this pickup burns and disappears")]
    public float burnDuration = 30f;

    private float timer;
    private Renderer visual;
    private Material mat; // owned instance — won't affect shared materials

    // Colour stages
    private static readonly Color ColReady  = new Color(0.88f, 0.62f, 0.12f); // golden-brown
    private static readonly Color ColWarn   = new Color(0.95f, 0.22f, 0.04f); // hot red
    private static readonly Color ColBurned = new Color(0.18f, 0.08f, 0.04f); // charred

    void Start()
    {
        timer = burnDuration;

        visual = GetComponentInChildren<Renderer>();
        if (visual != null)
        {
            // Always create a fresh material on the correct shader
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");
            mat = new Material(shader);
            visual.material = mat;
            ApplyColour(ColReady);
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (visual != null)
        {
            float t = 1f - Mathf.Clamp01(timer / burnDuration); // 0 fresh → 1 burned

            // Transition: golden → red over first 60%, then red → charred for last 40%
            Color c = t < 0.6f
                ? Color.Lerp(ColReady, ColWarn,   t / 0.6f)
                : Color.Lerp(ColWarn,  ColBurned, (t - 0.6f) / 0.4f);
            ApplyColour(c);
        }

        if (timer <= 0f)
            Burn();
    }

    void Burn()
    {
        SFXManager.PlayHit();
        Debug.Log("[BurnablePickup] Venison burned — left it too long!");
        Destroy(gameObject);
    }

    void ApplyColour(Color c)
    {
        if (mat == null) return;
        mat.color = c;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
    }
}
