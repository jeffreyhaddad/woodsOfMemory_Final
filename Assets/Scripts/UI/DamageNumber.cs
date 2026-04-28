using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Screen-space floating damage numbers. Single singleton renders all active numbers
/// via OnGUI so there are no world-space objects or TMP dependencies.
/// Call DamageNumber.Spawn(amount, worldPos) from anywhere.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    private static DamageNumber instance;

    private struct Entry
    {
        public Vector3 worldPos;
        public float   amount;
        public float   age;
        public float   xDrift;   // screen-space horizontal drift in pixels
        public string  text;     // pre-built string, avoids allocation each repaint
    }

    private readonly List<Entry> active = new List<Entry>();
    private GUIStyle style;
    private Camera cachedCam;

    private const float Duration = 1.5f;

    // ── Public API ───────────────────────────────────────────

    public static void Spawn(float amount, Vector3 worldPos)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("DamageNumberSystem");
            instance = go.AddComponent<DamageNumber>();
        }
        float rounded = Mathf.Round(amount);
        instance.active.Add(new Entry
        {
            worldPos = worldPos + Vector3.up * 0.5f,
            amount   = rounded,
            age      = 0f,
            xDrift   = Random.Range(-20f, 20f),
            text     = "-" + (int)rounded
        });
    }

    // ── Lifecycle ────────────────────────────────────────────

    void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Entry e = active[i];
            e.age += Time.deltaTime;
            active[i] = e;
            if (e.age >= Duration) active.RemoveAt(i);
        }
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (active.Count == 0) return;

        if (cachedCam == null) cachedCam = Camera.main;
        Camera cam = cachedCam;
        if (cam == null) return;

        if (style == null)
        {
            style = new GUIStyle();
            style.alignment  = TextAnchor.MiddleCenter;
            style.fontStyle  = FontStyle.Normal;
        }

        foreach (Entry e in active)
        {
            float t = e.age / Duration;

            Vector3 screen = cam.WorldToScreenPoint(e.worldPos);
            if (screen.z < 0f) continue;

            // Rise fast at first, ease out
            float rise = 110f * Mathf.Sqrt(t);
            float sx   = screen.x + e.xDrift * t;
            float sy   = (Screen.height - screen.y) - rise;

            // Scale punch: big on spawn, snaps to normal
            float scale = t < 0.12f ? Mathf.Lerp(2.0f, 1f, t / 0.12f) : 1f;

            // Alpha: fully opaque until 55%, then fade
            float alpha = t < 0.55f ? 1f : 1f - (t - 0.55f) / 0.45f;

            // Color by damage tier — white for normal, red-orange for high hits
            Color col = e.amount >= 50f ? new Color(1f, 0.25f, 0.05f) : Color.white;
            col.a = alpha;

            int fs  = Mathf.RoundToInt(28f * scale);
            style.fontSize = fs;

            string text = e.text;
            float  rw   = 100f;
            float  rh   = 48f;
            Rect   r    = new Rect(sx - rw * 0.5f, sy - rh * 0.5f, rw, rh);

            // Thick black outline — draw at 4 corners
            style.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.9f);
            GUI.Label(new Rect(r.x - 2, r.y - 2, rw, rh), text, style);
            GUI.Label(new Rect(r.x + 2, r.y - 2, rw, rh), text, style);
            GUI.Label(new Rect(r.x - 2, r.y + 2, rw, rh), text, style);
            GUI.Label(new Rect(r.x + 2, r.y + 2, rw, rh), text, style);

            // Main colored text on top
            style.normal.textColor = col;
            GUI.Label(r, text, style);
        }
    }
}
