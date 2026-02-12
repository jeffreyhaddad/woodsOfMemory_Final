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

    private float health;
    private float hunger;
    private float stamina;
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
        // Hunger drains passively
        if (hunger > 0f)
            hunger = Mathf.Max(0f, hunger - hungerDrainRate * Time.deltaTime);

        // Starvation: lose health when hunger is 0
        if (hunger <= 0f)
            health = Mathf.Max(0f, health - starvationDamage * Time.deltaTime);
        // Health regen when well-fed
        else if (hunger > 50f && health < maxHealth)
            health = Mathf.Min(maxHealth, health + healthRegenRate * Time.deltaTime);

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

        // Death check
        if (health <= 0f)
        {
            health = 0f;
            Debug.Log("Player has died!");
            OnPlayerDeath?.Invoke();
            enabled = false;
        }
    }

    /// <summary>Drain stamina while running. Called by PlayerMovement each frame.</summary>
    public void DrainStamina(float amount)
    {
        stamina = Mathf.Max(0f, stamina - amount);
        if (stamina <= 0f)
            isExhausted = true;
        OnVitalsChanged?.Invoke();
    }

    /// <summary>Regenerate stamina when not running. Called by PlayerMovement each frame.</summary>
    public void RegenStamina(float amount)
    {
        if (stamina < maxStamina)
        {
            stamina = Mathf.Min(maxStamina, stamina + amount);
            if (isExhausted && stamina >= exhaustionRecoveryThreshold)
                isExhausted = false;
            OnVitalsChanged?.Invoke();
        }
    }

    /// <summary>Spend stamina on jump. Returns false if not enough stamina.</summary>
    public bool UseStaminaForJump()
    {
        if (stamina < jumpStaminaCost)
            return false;

        stamina -= jumpStaminaCost;
        OnVitalsChanged?.Invoke();
        return true;
    }

    /// <summary>Take damage from enemies or hazards. Armor reduces incoming damage.</summary>
    public void TakeDamage(float amount)
    {
        if (EquipmentManager.Instance != null)
            amount = Mathf.Max(1f, amount - EquipmentManager.Instance.ArmorDefenseBonus);

        Health -= amount;
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
}
