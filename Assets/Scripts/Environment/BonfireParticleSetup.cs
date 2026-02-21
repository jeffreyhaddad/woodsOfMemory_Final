using UnityEngine;

/// <summary>
/// Configures the three particle systems on bonfire_01 (FireEffect, LightEffect/Embers, SmokeEffect)
/// with realistic settings at runtime.
/// Attach this to the bonfire_01 root GameObject alongside FinalBoneFireScript.
/// </summary>
[ExecuteAlways]
public class BonfireParticleSetup : MonoBehaviour
{
    [Header("Fire")]
    public float fireEmissionRate   = 40f;
    public float fireMinSize        = 0.5f;
    public float fireMaxSize        = 1.2f;
    public float fireMinLifetime    = 0.7f;
    public float fireMaxLifetime    = 1.4f;
    public float fireMinSpeed       = 0.4f;
    public float fireMaxSpeed       = 1.2f;

    [Header("Embers")]
    public float emberEmissionRate  = 10f;
    public float emberMinSize       = 0.04f;
    public float emberMaxSize       = 0.10f;
    public float emberMinLifetime   = 1f;
    public float emberMaxLifetime   = 3f;

    [Header("Smoke")]
    public float smokeEmissionRate  = 6f;
    public float smokeMinSize       = 0.5f;
    public float smokeMaxSize       = 1.0f;
    public float smokeMinLifetime   = 4f;
    public float smokeMaxLifetime   = 7f;

    void Awake()
    {
        Apply();
    }

    /// <summary>
    /// Call from the Inspector context menu (three-dot menu on this component) to preview in Edit mode.
    /// </summary>
    [ContextMenu("Apply Particle Settings")]
    public void Apply()
    {
        ConfigureFire();
        ConfigureEmbers();
        ConfigureSmoke();
    }

    // ──────────────────────────────────────────────────────────────
    // FIRE
    // ──────────────────────────────────────────────────────────────
    void ConfigureFire()
    {
        Transform t = transform.Find("FireEffect");
        if (t == null) { Debug.LogWarning("[BonfireSetup] FireEffect child not found."); return; }
        ParticleSystem ps = t.GetComponent<ParticleSystem>();
        if (ps == null) return;

        // Reset local scale so scalingMode:Local gives correct world sizes
        t.localScale = Vector3.one;

        // ── Main ──────────────────────────────────────────────────
        var main = ps.main;
        main.loop             = true;
        main.playOnAwake      = false;
        main.duration         = 2f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(fireMinLifetime, fireMaxLifetime);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(fireMinSpeed, fireMaxSpeed);
        main.startSize        = new ParticleSystem.MinMaxCurve(fireMinSize, fireMaxSize);
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor       = new ParticleSystem.MinMaxGradient(
            new Color(1.0f, 0.75f, 0.10f, 0.95f),   // bright yellow-orange
            new Color(1.0f, 0.20f, 0.00f, 0.85f)    // deep red-orange
        );
        main.gravityModifier  = new ParticleSystem.MinMaxCurve(-0.08f, -0.04f);
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.scalingMode      = ParticleSystemScalingMode.Local;
        main.maxParticles     = 120;

        // ── Emission ──────────────────────────────────────────────
        var em = ps.emission;
        em.enabled       = true;
        em.rateOverTime  = fireEmissionRate;

        // ── Shape (cone at base of logs) ─────────────────────────
        var shape = ps.shape;
        shape.enabled    = true;
        shape.shapeType  = ParticleSystemShapeType.Cone;
        shape.radius     = 0.28f;
        shape.angle      = 18f;
        shape.radiusMode = ParticleSystemShapeMultiModeValue.Random;

        // ── Color over Lifetime ───────────────────────────────────
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient fireGrad = new Gradient();
        fireGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.0f, 0.95f, 0.40f), 0.00f),
                new GradientColorKey(new Color(1.0f, 0.45f, 0.05f), 0.45f),
                new GradientColorKey(new Color(0.40f, 0.08f, 0.02f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.00f, 0.00f),
                new GradientAlphaKey(1.00f, 0.08f),
                new GradientAlphaKey(0.75f, 0.50f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(fireGrad);

        // ── Size over Lifetime (bud → bloom → taper) ─────────────
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve fireSizeCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.20f),
            new Keyframe(0.25f, 1.00f),
            new Keyframe(0.70f, 0.80f),
            new Keyframe(1.00f, 0.05f)
        );
        sol.size = new ParticleSystem.MinMaxCurve(1f, fireSizeCurve);

        // ── Noise (organic flicker) ───────────────────────────────
        var noise = ps.noise;
        noise.enabled      = true;
        noise.strength     = new ParticleSystem.MinMaxCurve(0.20f, 0.40f);
        noise.frequency    = 0.6f;
        noise.scrollSpeed  = 0.5f;
        noise.octaveCount  = 2;
        noise.quality      = ParticleSystemNoiseQuality.Medium;

        // ── Renderer ──────────────────────────────────────────────
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode  = ParticleSystemRenderMode.Billboard;
        rend.sortMode    = ParticleSystemSortMode.YoungestInFront;
        rend.minParticleSize = 0f;
        rend.maxParticleSize = 2f;
    }

    // ──────────────────────────────────────────────────────────────
    // EMBERS (was "LightEffect")
    // ──────────────────────────────────────────────────────────────
    void ConfigureEmbers()
    {
        Transform t = transform.Find("LightEffect");
        if (t == null) { Debug.LogWarning("[BonfireSetup] LightEffect child not found."); return; }
        ParticleSystem ps = t.GetComponent<ParticleSystem>();
        if (ps == null) return;

        t.localScale = Vector3.one;

        var main = ps.main;
        main.loop            = true;
        main.playOnAwake     = false;
        main.duration        = 2f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(emberMinLifetime, emberMaxLifetime);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.6f, 2.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(emberMinSize, emberMaxSize);
        main.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1.0f, 0.85f, 0.20f, 1f),
            new Color(1.0f, 0.40f, 0.00f, 1f)
        );
        main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.05f, 0.08f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode     = ParticleSystemScalingMode.Local;
        main.maxParticles    = 60;

        var em = ps.emission;
        em.enabled      = true;
        em.rateOverTime = emberEmissionRate;
        em.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 1, 4, 3, 2f)   // random mini-bursts of sparks
        });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.22f;

        // Color over lifetime — embers cool from yellow to dim red then fade
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient emberGrad = new Gradient();
        emberGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.0f, 0.95f, 0.50f), 0.00f),
                new GradientColorKey(new Color(1.0f, 0.35f, 0.00f), 0.40f),
                new GradientColorKey(new Color(0.45f, 0.05f, 0.00f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1.00f, 0.00f),
                new GradientAlphaKey(1.00f, 0.55f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(emberGrad);

        // Erratic noise so embers drift and swirl unpredictably
        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
        noise.frequency   = 1.2f;
        noise.scrollSpeed = 1.0f;
        noise.octaveCount = 3;
        noise.quality     = ParticleSystemNoiseQuality.Medium;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sortMode   = ParticleSystemSortMode.YoungestInFront;
    }

    // ──────────────────────────────────────────────────────────────
    // SMOKE
    // ──────────────────────────────────────────────────────────────
    void ConfigureSmoke()
    {
        Transform t = transform.Find("SmokeEffect");
        if (t == null) { Debug.LogWarning("[BonfireSetup] SmokeEffect child not found."); return; }
        ParticleSystem ps = t.GetComponent<ParticleSystem>();
        if (ps == null) return;

        t.localScale = Vector3.one;

        // Offset smoke to emit from just above the flame
        t.localPosition = new Vector3(0f, 1.2f, 0f);

        var main = ps.main;
        main.loop            = true;
        main.playOnAwake     = false;
        main.duration        = 3f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(smokeMinLifetime, smokeMaxLifetime);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startSize       = new ParticleSystem.MinMaxCurve(smokeMinSize, smokeMaxSize);
        main.startRotation   = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.28f, 0.26f, 0.24f, 0.35f),
            new Color(0.14f, 0.13f, 0.12f, 0.20f)
        );
        main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.04f, -0.01f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode     = ParticleSystemScalingMode.Local;
        main.maxParticles    = 35;

        var em = ps.emission;
        em.enabled      = true;
        em.rateOverTime = smokeEmissionRate;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius    = 0.10f;
        shape.angle     = 8f;

        // Color over lifetime — wisp of brown smoke turns light grey and fades
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient smokeGrad = new Gradient();
        smokeGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.35f, 0.25f, 0.15f), 0.00f),
                new GradientColorKey(new Color(0.30f, 0.30f, 0.30f), 0.30f),
                new GradientColorKey(new Color(0.65f, 0.65f, 0.65f), 1.00f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.00f, 0.00f),
                new GradientAlphaKey(0.45f, 0.15f),
                new GradientAlphaKey(0.20f, 0.70f),
                new GradientAlphaKey(0.00f, 1.00f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(smokeGrad);

        // Size over lifetime — smoke puff expands as it rises
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve smokeSizeCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.15f),
            new Keyframe(0.40f, 0.60f),
            new Keyframe(1.00f, 1.00f)
        );
        sol.size = new ParticleSystem.MinMaxCurve(1f, smokeSizeCurve);

        // Slow drift
        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        noise.frequency   = 0.15f;
        noise.scrollSpeed = 0.08f;
        noise.octaveCount = 1;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sortMode   = ParticleSystemSortMode.OldestInFront;
    }
}
