using UnityEngine;

/// <summary>
/// Draws a purple health bar above the zombie in screen space using OnGUI + WorldToScreenPoint.
/// No Canvas, no TMP, no world-space objects — guaranteed to work.
/// </summary>
public class ZombieHealthBar : MonoBehaviour
{
    private CreatureAI creature;
    private Camera cam;

    private const float BarW        = 80f;
    private const float BarH        = 7f;
    private const float HeadOffset  = 7f;   // world units above zombie root (2.5x scale ≈ 6.25 unit tall)
    private const float MaxDist     = 45f;  // don't draw if farther than this
    private const float MaxDistSqr  = MaxDist * MaxDist;

    private static readonly Rect s_borderRect = new Rect(0, 0, BarW + 2, BarH + 2);
    private static readonly Rect s_bgRect     = new Rect(0, 0, BarW, BarH);
    private static readonly Rect s_fillRect   = new Rect(0, 0, 0, BarH);

    public void Init(CreatureAI ai)
    {
        creature = ai;
        cam = Camera.main;
    }

    void OnGUI()
    {
        if (creature == null || creature.State == CreatureState.Dead) return;

        if (cam == null) { cam = Camera.main; if (cam == null) return; }

        if ((cam.transform.position - creature.transform.position).sqrMagnitude > MaxDistSqr)
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

        Rect border = s_borderRect; border.x = x - 1; border.y = y - 1;
        Rect bg     = s_bgRect;     bg.x = x;         bg.y = y;

        // Black border
        GUI.color = Color.black;
        GUI.DrawTexture(border, Texture2D.whiteTexture);

        // Dark background
        GUI.color = new Color(0.06f, 0.02f, 0.12f, 0.92f);
        GUI.DrawTexture(bg, Texture2D.whiteTexture);

        // Fill — purple at full health, shifts red when low
        if (pct > 0f)
        {
            Rect fill = s_fillRect; fill.x = x; fill.y = y; fill.width = BarW * pct;
            GUI.color = Color.Lerp(new Color(0.95f, 0.1f, 0.1f),
                                   new Color(0.55f, 0.08f, 0.95f), pct);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }
}
