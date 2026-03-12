using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    Inventory,
    Dead,
    Cutscene
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Playing;

    public event Action<GameState> OnGameStateChanged;

    // Cached system references (populated in Start)
    [HideInInspector] public PlayerVitals PlayerVitals;
    [HideInInspector] public Inventory Inventory;
    [HideInInspector] public DayNightCycle DayNight;

    // Player spawn point — drag a GameObject here in the inspector for a fixed respawn location.
    // If left empty, falls back to the player's position at game start.
    [Header("Spawn")]
    public Transform respawnPoint;
    private Vector3 spawnPoint;
    private bool spawnCaptured;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Disable all Debug.Log output in release builds to prevent GC pressure
#if !UNITY_EDITOR
        Debug.unityLogger.logEnabled = false;
#endif
    }

    void Start()
    {
        CacheReferences();

        // Auto-create managers if not in scene
        if (EquipmentManager.Instance == null)
            gameObject.AddComponent<EquipmentManager>();
        if (ToolDurabilityManager.Instance == null)
            gameObject.AddComponent<ToolDurabilityManager>();
        if (SFXManager.Instance == null)
            gameObject.AddComponent<SFXManager>();
        if (FindAnyObjectByType<EquipmentHUD>() == null)
            gameObject.AddComponent<EquipmentHUD>();
        if (FindAnyObjectByType<MissionTransitionUI>() == null)
            gameObject.AddComponent<MissionTransitionUI>();
        if (FindAnyObjectByType<CompassUI>() == null)
            gameObject.AddComponent<CompassUI>();
        if (WeatherManager.Instance == null)
            gameObject.AddComponent<WeatherManager>();
        if (MusicManager.Instance == null)
            gameObject.AddComponent<MusicManager>();
        if (FindAnyObjectByType<MapSystem>() == null)
            gameObject.AddComponent<MapSystem>();
        if (FindAnyObjectByType<CookingProgressUI>() == null)
            gameObject.AddComponent<CookingProgressUI>();
        if (FindAnyObjectByType<HotbarUI>() == null)
            gameObject.AddComponent<HotbarUI>();
        if (FindAnyObjectByType<PickupNotificationUI>() == null)
            gameObject.AddComponent<PickupNotificationUI>();
        if (SleepManager.Instance == null)
            gameObject.AddComponent<SleepManager>();
        if (SettingsManager.Instance == null)
        {
            var smGo = new GameObject("SettingsManager");
            smGo.AddComponent<SettingsManager>();
        }
        if (FindAnyObjectByType<SettingsUI>() == null)
            gameObject.AddComponent<SettingsUI>();

        // Torch light on the player (listens to EquipmentManager)
        if (PlayerVitals != null && PlayerVitals.GetComponent<TorchLight>() == null)
            PlayerVitals.gameObject.AddComponent<TorchLight>();

        // Lantern light on the player (listens to EquipmentManager)
        if (PlayerVitals != null && PlayerVitals.GetComponent<LanternLight>() == null)
            PlayerVitals.gameObject.AddComponent<LanternLight>();

        // Weapon/tool visual in right hand (listens to EquipmentManager)
        if (PlayerVitals != null && PlayerVitals.GetComponent<WeaponHolder>() == null)
            PlayerVitals.gameObject.AddComponent<WeaponHolder>();

        // Determine respawn point — priority: inspector Transform > Cabin object > player start
        if (respawnPoint != null)
        {
            spawnPoint = respawnPoint.position;
        }
        else
        {
            GameObject cabin = GameObject.Find("Cabin");
            if (cabin != null)
                spawnPoint = cabin.transform.position - cabin.transform.right * 4f + Vector3.up * 1.2f;
            else if (PlayerVitals != null)
                spawnPoint = PlayerVitals.transform.position;
        }

        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void CacheReferences()
    {
        PlayerVitals = FindAnyObjectByType<PlayerVitals>();
        Inventory = FindAnyObjectByType<Inventory>();
        DayNight = FindAnyObjectByType<DayNightCycle>();
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        GameState oldState = CurrentState;
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                PlayerMovement.inputBlocked    = false;
                PlayerMovement.movementBlocked = false;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                PlayerMovement.inputBlocked    = true;
                PlayerMovement.movementBlocked = true;
                break;

            case GameState.Dead:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                PlayerMovement.inputBlocked    = true;
                PlayerMovement.movementBlocked = true;
                break;

            case GameState.Inventory:
                // Inventory/Crafting UI handles cursor + input themselves
                // movementBlocked stays false so player can still walk
                break;

            case GameState.Cutscene:
                Time.timeScale = 1f;
                PlayerMovement.inputBlocked    = true;
                PlayerMovement.movementBlocked = true;
                break;
        }

        OnGameStateChanged?.Invoke(newState);
    }

    public void RespawnPlayer()
    {
        if (PlayerVitals == null) return;

        // --- Death penalty: drop all non-quest items at death location ---
        if (Inventory != null)
        {
            Vector3 deathPos = PlayerVitals.transform.position;
            for (int i = 0; i < Inventory.slots.Length; i++)
            {
                InventorySlot slot = Inventory.slots[i];
                if (slot.IsEmpty) continue;
                if (slot.item.category == ItemCategory.QuestItem) continue;

                SpawnDroppedItemAtPosition(slot.item, slot.quantity, deathPos);
                slot.Clear();
            }
            Inventory.NotifyChanged();
        }

        // Unequip everything (items are gone from inventory)
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.UnequipAll();

        // --- Teleport to cabin ---
        CharacterController cc = PlayerVitals.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        PlayerVitals.transform.position = spawnPoint;
        // Face toward the cabin door
        GameObject cabin = GameObject.Find("Cabin");
        if (cabin != null)
            PlayerVitals.transform.rotation = Quaternion.LookRotation(cabin.transform.right, Vector3.up);
        if (cc != null) cc.enabled = true;

        // Reset animator back to idle — clears the death pose
        Animator anim = PlayerVitals.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger("Death");
            anim.SetFloat("Speed", 0f);
            anim.SetFloat("Direction", 0f);
            anim.SetFloat("Vertical", 0f);
            anim.SetBool("isJumping", false);
            anim.SetBool("isCrouching", false);
            anim.CrossFade("Idle Blend", 0.1f);
        }

        // Tough respawn: 25% health, 15% hunger — player needs to recover fast
        PlayerVitals.Health  = PlayerVitals.maxHealth  * 0.25f;
        PlayerVitals.Hunger  = PlayerVitals.maxHunger  * 0.15f;
        PlayerVitals.Stamina = PlayerVitals.maxStamina;
        PlayerVitals.enabled = true;

        SetState(GameState.Playing);
    }

    void SpawnDroppedItemAtPosition(ItemData item, int quantity, Vector3 center)
    {
        Vector3 scatter = new Vector3(
            UnityEngine.Random.Range(-1.5f, 1.5f),
            0.5f,
            UnityEngine.Random.Range(-1.5f, 1.5f));

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Dropped_" + item.itemName;
        go.transform.position = center + scatter;
        go.transform.localScale = Vector3.one * 0.25f;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = DeathDropColor(item.category);
        go.GetComponent<Renderer>().material = mat;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.AddForce(scatter.normalized * 2f + Vector3.up * 3f, ForceMode.VelocityChange);

        DroppedItem dropped = go.AddComponent<DroppedItem>();
        dropped.item     = item;
        dropped.quantity = quantity;
    }

    static Color DeathDropColor(ItemCategory cat)
    {
        switch (cat)
        {
            case ItemCategory.Resource: return new Color(0.6f, 0.4f, 0.2f);
            case ItemCategory.Tool:     return new Color(0.5f, 0.5f, 0.6f);
            case ItemCategory.Weapon:   return new Color(0.7f, 0.2f, 0.2f);
            case ItemCategory.Food:     return new Color(0.3f, 0.7f, 0.3f);
            default:                    return Color.white;
        }
    }

    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("WelcomeScene");
    }
}
