using UnityEngine;

/// <summary>
/// Draws a purple health bar above the zombie in screen space using OnGUI + WorldToScreenPoint.
/// No Canvas, no TMP, no world-space objects — guaranteed to work.
/// </summary>
public class ZombieHealthBar : MonoBehaviour
{
    private CreatureAI creature;

    private const float BarW       = 80f;
    private const float BarH       = 7f;
    private const float HeadOffset = 7f;   // world units above zombie root (2.5x scale ≈ 6.25 unit tall)
    private const float MaxDist    = 45f;  // don't draw if farther than this

    public void Init(CreatureAI ai)
    {
        creature = ai;
    }

    void OnGUI()
    {
        if (creature == null || creature.State == CreatureState.Dead) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        if (Vector3.Distance(cam.transform.position, creature.transform.position) > MaxDist)
            return;

        Vector3 worldTop = creature.transform.position + Vector3.up * HeadOffset;
        Vector3 screen   = cam.WorldToScreenPoint(worldTop);
        if (screen.z < 0f) return;   // behind camera

        // GUI y is flipped vs screen y
        float sx = screen.x;
        float sy = Screen.height - screen.y;
        float x  = sx - BarW * 0.5f;
        float y  = sy - BarH * 0.5f;

        float pct = Mathf.Clamp01(creature.HealthPercent);

        // Black border
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(x - 1, y - 1, BarW + 2, BarH + 2), Texture2D.whiteTexture);

        // Dark background
        GUI.color = new Color(0.06f, 0.02f, 0.12f, 0.92f);
        GUI.DrawTexture(new Rect(x, y, BarW, BarH), Texture2D.whiteTexture);

        // Fill — purple at full health, shifts red when low
        if (pct > 0f)
        {
            GUI.color = Color.Lerp(new Color(0.95f, 0.1f, 0.1f),
                                   new Color(0.55f, 0.08f, 0.95f), pct);
            GUI.DrawTexture(new Rect(x, y, BarW * pct, BarH), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }
}
