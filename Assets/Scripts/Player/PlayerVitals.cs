using System;
using UnityEngine;

public class PlayerVitals : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    [Tooltip("Health regen per second when hunger > 50")]
    public float healthRegenRate = 0.5f;
    [Tooltip("Health lost per second when hunger is 0")]
    public float starvationDamage = 2f;

    [Header("Hunger")]
    public float maxHunger = 100f;
    [Tooltip("Hunger lost per second (1 = lose 1 per minute at 1/60)")]
    public float hungerDrainRate = 1f / 60f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    [Tooltip("Stamina lost per second while sprinting")]
    public float staminaDrainRate = 15f;
    [Tooltip("Stamina regained per second when not sprinting")]
    public float staminaRegenRate = 10f;
    [Tooltip("Stamina cost per jump")]
    public float jumpStaminaCost = 10f;
    [Tooltip("Must recover this % of max stamina before running again after exhaustion")]
    public float exhaustionRecoveryThreshold = 20f;
    [Tooltip("Hunger cost per unit of stamina regenerated (body burns calories to restore energy)")]
    public float staminaRegenHungerCost = 0.04f;

    private float health;
    private float hunger;
    private float stamina;
    private float damageFlashTimer;
    private bool isExhausted = false;

    public float Health
    {
        get => health;
        set { health = Mathf.Clamp(value, 0f, maxHealth); OnVitalsChanged?.Invoke(); }
    }

    public float Hunger
    {
        get => hunger;
        set { hunger = Mathf.Clamp(value, 0f, maxHunger); OnVitalsChanged?.Invoke(); }
    }

    public float Stamina
    {
        get => stamina;
        set { stamina = Mathf.Clamp(value, 0f, maxStamina); OnVitalsChanged?.Invoke(); }
    }

    public bool IsExhausted => isExhausted;
    public bool CanRun => !isExhausted;
    public bool CanJump => !isExhausted && stamina >= jumpStaminaCost;

    public event Action OnVitalsChanged;
    public event Action OnPlayerDeath;

    void Awake()
    {
        health = maxHealth;
        hunger = maxHunger;
        stamina = maxStamina;
    }

    private float lastNotifiedHealth;
    private float lastNotifiedHunger;
    private float lastNotifiedStamina;

    void Update()
    {
        // Death check first — before regen can reverse lethal damage
        if (health <= 0f)
        {
            health = 0f;
            OnPlayerDeath?.Invoke();
            enabled = false;
            return;
        }

        // Hunger drains passively
        if (hunger > 0f)
            hunger = Mathf.Max(0f, hunger - hungerDrainRate * Time.deltaTime);

        // Starvation: lose health when hunger is 0
        if (hunger <= 0f)
            health = Mathf.Max(0f, health - starvationDamage * Time.deltaTime);
        // Health regen when well-fed — scales 1× at 50 hunger up to 3× at max hunger
        else if (hunger > 50f && health < maxHealth)
        {
            float regenMult = Mathf.Lerp(1f, 3f, (hunger - 50f) / (maxHunger - 50f));
            health = Mathf.Min(maxHealth, health + healthRegenRate * regenMult * Time.deltaTime);
        }

        // Stop heartbeat once health regens above 25%
        if (health > maxHealth * 0.25f)
            SFXManager.StopHeartbeat();

        // Only notify UI when display values actually change (whole numbers)
        if (Mathf.CeilToInt(health) != Mathf.CeilToInt(lastNotifiedHealth) ||
            Mathf.CeilToInt(hunger) != Mathf.CeilToInt(lastNotifiedHunger) ||
            Mathf.CeilToInt(stamina) != Mathf.CeilToInt(lastNotifiedStamina))
        {
            lastNotifiedHealth = health;
            lastNotifiedHunger = hunger;
            lastNotifiedStamina = stamina;
            OnVitalsChanged?.Invoke();
        }
    }

    /// <summary>Drain stamina while running. Called by PlayerMovement each frame.
    /// UI notification is handled by the throttled check in Update().</summary>
    public void DrainStamina(float amount)
    {
        stamina = Mathf.Max(0f, stamina - amount);
        if (stamina <= 0f && !isExhausted)
            isExhausted = true;
    }

    /// <summary>Regenerate stamina when not running. Called by PlayerMovement each frame.
    /// UI notification is handled by the throttled check in Update().</summary>
    public void RegenStamina(float amount)
    {
        if (stamina < maxStamina)
        {
            float actual = Mathf.Min(maxStamina, stamina + amount) - stamina;
            stamina += actual;
            hunger = Mathf.Max(0f, hunger - actual * staminaRegenHungerCost);
            if (isExhausted && stamina >= exhaustionRecoveryThreshold)
                isExhausted = false;
        }
    }

    /// <summary>Spend stamina on jump. Returns false if not enough stamina.</summary>
    public bool UseStaminaForJump()
    {
        if (stamina < jumpStaminaCost)
            return false;

        stamina -= jumpStaminaCost;
        return true;
    }

    /// <summary>Take damage from enemies or hazards. Armor reduces incoming damage.</summary>
    public void TakeDamage(float amount)
    {
        if (EquipmentManager.Instance != null)
            amount = Mathf.Max(1f, amount - EquipmentManager.Instance.ArmorDefenseBonus);

        Health -= amount;
        damageFlashTimer = 0.4f;
        SFXManager.PlayHurt();

        // Low health heartbeat warning
        if (health > 0f && health <= maxHealth * 0.25f)
            SFXManager.StartHeartbeat();
        else
            SFXManager.StopHeartbeat();
    }

    /// <summary>Restore hunger from eating food.</summary>
    public void Eat(float hungerRestore)
    {
        Hunger += hungerRestore;
    }

    private Rect cachedFullRect;
    private int  cachedScreenW, cachedScreenH;

    Rect GetFullRect()
    {
        int w = Screen.width, h = Screen.height;
        if (w != cachedScreenW || h != cachedScreenH)
        {
            cachedFullRect = new Rect(0, 0, w, h);
            cachedScreenW = w; cachedScreenH = h;
        }
        return cachedFullRect;
    }

    void OnGUI()
    {
        Rect full = GetFullRect();

        // Damage flash — sharp red full-screen
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            float alpha = (damageFlashTimer / 0.4f) * 0.5f;
            GUI.color = new Color(1f, 0f, 0f, alpha);
            GUI.DrawTexture(full, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Low health vignette — pulsing red edges, gets worse below 30%
        float healthPct = maxHealth > 0f ? health / maxHealth : 0f;
        if (healthPct < 0.30f)
        {
            float severity = 1f - healthPct / 0.30f;              // 0 at 30%, 1 at 0%
            float rate     = 2.5f + severity * 3f;                 // pulse faster when critical
            float pulse    = 0.55f + 0.45f * Mathf.Sin(Time.time * rate);
            float alpha    = severity * 0.55f * pulse;
            GUI.color = new Color(0.75f, 0f, 0f, alpha);
            // Draw four edge strips to fake a vignette without a shader
            int w = Screen.width, h = Screen.height;
            int thickness = Mathf.RoundToInt(Mathf.Lerp(40f, 120f, severity));
            GUI.DrawTexture(new Rect(0,         0,          w,         thickness), Texture2D.whiteTexture); // top
            GUI.DrawTexture(new Rect(0,         h-thickness, w,        thickness), Texture2D.whiteTexture); // bottom
            GUI.DrawTexture(new Rect(0,         0,          thickness, h),         Texture2D.whiteTexture); // left
            GUI.DrawTexture(new Rect(w-thickness, 0,        thickness, h),         Texture2D.whiteTexture); // right
            GUI.color = Color.white;
        }

    }
}
