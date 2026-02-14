using UnityEngine;

/// <summary>
/// Manages the torch light attached to the player.
/// When a torch is equipped (EquipSlot.Tool with itemName "Torch"), spawns a flickering
/// point light near the player's hand. Removes it when unequipped.
/// Auto-created by GameManager — no editor setup needed.
/// </summary>
public class TorchLight : MonoBehaviour
{
    [Header("Light Settings")]
    public Color torchColor = new Color(1f, 0.7f, 0.3f);
    public float baseIntensity = 1.8f;
    public float lightRange = 12f;

    [Header("Flicker")]
    public float flickerSpeed = 8f;
    public float flickerAmount = 0.3f;

    [Header("Fire Particles")]
    public int particleCount = 30;

    private GameObject torchObj;
    private Light torchLight;
    private ParticleSystem fireParticles;
    private bool torchActive;

    void Start()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;

        // Check if torch is already equipped at start (e.g. after loading a save)
        OnEquipmentChanged();
    }

    void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
    }

    void Update()
    {
        if (!torchActive || torchLight == null) return;

        // Flicker the light
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        torchLight.intensity = baseIntensity + (noise - 0.5f) * flickerAmount * 2f;

        // Keep torch positioned relative to player (right side, slightly forward and up)
        if (torchObj != null)
        {
            torchObj.transform.position = transform.position
                + transform.right * 0.4f
                + transform.forward * 0.3f
                + Vector3.up * 1.6f;
        }
    }

    void OnEquipmentChanged()
    {
        EquipmentManager equip = EquipmentManager.Instance;
        if (equip == null) return;

        bool shouldBeActive = equip.EquippedTool != null
            && equip.EquippedTool.itemName == "Torch";

        if (shouldBeActive && !torchActive)
            EnableTorch();
        else if (!shouldBeActive && torchActive)
            DisableTorch();
    }

    void EnableTorch()
    {
        torchActive = true;

        torchObj = new GameObject("PlayerTorch");
        // Don't parent to player — we position manually so it doesn't inherit rotation weirdly
        torchObj.transform.position = transform.position + Vector3.up * 1.6f;

        // Point light
        torchLight = torchObj.AddComponent<Light>();
        torchLight.type = LightType.Point;
        torchLight.color = torchColor;
        torchLight.intensity = baseIntensity;
        torchLight.range = lightRange;
        torchLight.shadows = LightShadows.Soft;
        torchLight.shadowStrength = 0.6f;

        // Fire particles
        CreateFireParticles();

        Debug.Log("Torch lit!");
    }

    void DisableTorch()
    {
        torchActive = false;

        if (torchObj != null)
            Destroy(torchObj);

        torchLight = null;
        fireParticles = null;

        Debug.Log("Torch extinguished.");
    }

    void CreateFireParticles()
    {
        GameObject particleObj = new GameObject("TorchFire");
        particleObj.transform.SetParent(torchObj.transform, false);
        particleObj.transform.localPosition = Vector3.zero;

        fireParticles = particleObj.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = fireParticles.main;
        main.maxParticles = particleCount;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0.2f, 0.8f),
            new Color(1f, 0.4f, 0.1f, 0.6f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f; // float upward

        // Emission
        ParticleSystem.EmissionModule emission = fireParticles.emission;
        emission.rateOverTime = particleCount * 2;

        // Shape: small cone pointing up
        ParticleSystem.ShapeModule shape = fireParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.03f;

        // Size over lifetime: shrink
        ParticleSystem.SizeOverLifetimeModule sizeOverLife = fireParticles.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve shrink = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f));
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, shrink);

        // Color over lifetime: fade out
        ParticleSystem.ColorOverLifetimeModule colorOverLife = fireParticles.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.7f),
                new GradientColorKey(new Color(0.3f, 0.1f, 0.05f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLife.color = gradient;

        // URP-compatible particle material
        ParticleSystemRenderer rend = particleObj.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader != null)
        {
            Material mat = new Material(particleShader);
            mat.SetColor("_BaseColor", new Color(1f, 0.7f, 0.3f, 0.8f));
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive
            mat.renderQueue = 3000;
            rend.material = mat;
        }
    }
}
