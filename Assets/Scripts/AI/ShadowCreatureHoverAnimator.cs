using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedural animation for the ShadowCreature child model.
/// Attach to the ShadowModel child. No animation clips required.
/// </summary>
public class ShadowCreatureHoverAnimator : MonoBehaviour
{
    [Header("Hover")]
    public float idleHoverHeight = 0.18f;
    public float idleHoverSpeed  = 1.2f;
    public float chaseHoverHeight = 0.1f;
    public float chaseHoverSpeed  = 2.8f;

    [Header("Sway")]
    public float idleSwayDegrees = 4f;
    public float idleSwaySpeed   = 1.6f;

    [Header("Chase Lean")]
    public float chaseLeanDegrees = 12f;

    [Header("Attack Arm Swing")]
    public float armSwingDegrees = 22f;
    public float armSwingSpeed   = 3f;

    // ── private ──────────────────────────────────────────────────────
    private CreatureAI          ai;
    private Vector3             baseLocalPos;
    private float               phase;
    private float               currentLean;

    // Arm bones auto-detected from child transforms
    private readonly List<Transform> armBones = new List<Transform>();
    private readonly List<Quaternion> armBaseRots = new List<Quaternion>();

    // Keywords used to find arm/claw bones (case-insensitive)
    private static readonly string[] ArmKeywords =
        { "arm", "hand", "claw", "fore", "tentacle", "limb", "wing", "shoulder" };

    void Start()
    {
        ai           = GetComponentInParent<CreatureAI>();
        baseLocalPos = transform.localPosition;
        phase        = Random.Range(0f, Mathf.PI * 2f);

        Transform[] all = GetComponentsInChildren<Transform>();

        // Pass 1 — keyword match
        foreach (Transform t in all)
        {
            if (t == transform) continue;
            foreach (string kw in ArmKeywords)
            {
                if (t.name.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    armBones.Add(t);
                    armBaseRots.Add(t.localRotation);
                    break;
                }
            }
        }

        // Pass 2 — position-based fallback if no keywords matched
        // Take transforms in the upper half of the model that are offset to the sides
        if (armBones.Count == 0 && all.Length > 1)
        {
            // Estimate model height from bone Y range
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (Transform t in all)
            {
                if (t == transform) continue;
                minY = Mathf.Min(minY, t.localPosition.y);
                maxY = Mathf.Max(maxY, t.localPosition.y);
            }
            float midY = (minY + maxY) * 0.5f;

            foreach (Transform t in all)
            {
                if (t == transform) continue;
                // Upper half, with meaningful sideways offset — likely arm/limb bones
                if (t.localPosition.y > midY && Mathf.Abs(t.localPosition.x) > 0.05f)
                {
                    armBones.Add(t);
                    armBaseRots.Add(t.localRotation);
                }
            }
        }

        // Pass 3 — last resort: use ALL direct children (works for low-poly mesh-piece models)
        if (armBones.Count == 0 && all.Length > 1)
        {
            foreach (Transform t in all)
            {
                if (t == transform) continue;
                armBones.Add(t);
                armBaseRots.Add(t.localRotation);
            }
        }
    }

    void LateUpdate()
    {
        if (ai == null) return;

        CreatureState state = ai.State;
        float t = Time.time + phase;

        // ── Hover ────────────────────────────────────────────────────
        float hoverH = (state == CreatureState.Patrol || state == CreatureState.Idle)
            ? idleHoverHeight : chaseHoverHeight;
        float hoverS = (state == CreatureState.Patrol || state == CreatureState.Idle)
            ? idleHoverSpeed : chaseHoverSpeed;

        float hover = Mathf.Sin(t * hoverS) * hoverH;
        transform.localPosition = baseLocalPos + Vector3.up * hover;

        // ── Lean / sway ──────────────────────────────────────────────
        float targetLean = (state == CreatureState.Chase || state == CreatureState.Attack)
            ? chaseLeanDegrees : 0f;
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * 3f);

        float sway = (state == CreatureState.Patrol || state == CreatureState.Idle)
            ? Mathf.Sin(t * idleSwaySpeed) * idleSwayDegrees : 0f;

        transform.localRotation = Quaternion.Euler(currentLean, 0f, sway);

        // ── Arm swing ────────────────────────────────────────────────
        if (armBones.Count > 0)
        {
            if (state == CreatureState.Attack)
            {
                // Alternating swing: left arm forward, right arm back, then swap
                float swing = Mathf.Sin(t * armSwingSpeed) * armSwingDegrees;
                for (int i = 0; i < armBones.Count; i++)
                {
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    armBones[i].localRotation = armBaseRots[i]
                        * Quaternion.Euler(swing * dir, 0f, 0f);
                }
            }
            else
            {
                // Gentle idle arm drift
                float idleDrift = Mathf.Sin(t * 0.8f) * 8f;
                for (int i = 0; i < armBones.Count; i++)
                {
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    armBones[i].localRotation = Quaternion.Slerp(
                        armBones[i].localRotation,
                        armBaseRots[i] * Quaternion.Euler(idleDrift * dir, 0f, 0f),
                        Time.deltaTime * 3f);
                }
            }
        }
    }
}
