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

    // Player spawn point (set in inspector or auto-captured)
    [Header("Spawn")]
    public Vector3 spawnPoint;
    private bool spawnCaptured;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        CacheReferences();

        // Auto-create managers if not in scene
        if (EquipmentManager.Instance == null)
            gameObject.AddComponent<EquipmentManager>();
        if (SFXManager.Instance == null)
            gameObject.AddComponent<SFXManager>();
        if (FindAnyObjectByType<EquipmentHUD>() == null)
            gameObject.AddComponent<EquipmentHUD>();

        // Capture initial player position as spawn point
        if (!spawnCaptured && PlayerVitals != null)
        {
            spawnPoint = PlayerVitals.transform.position;
            spawnCaptured = true;
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
                PlayerMovement.inputBlocked = false;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                PlayerMovement.inputBlocked = true;
                break;

            case GameState.Dead:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                PlayerMovement.inputBlocked = true;
                break;

            case GameState.Inventory:
                // Inventory/Crafting UI handles cursor + input themselves
                break;

            case GameState.Cutscene:
                Time.timeScale = 1f;
                PlayerMovement.inputBlocked = true;
                break;
        }

        OnGameStateChanged?.Invoke(newState);
    }

    public void RespawnPlayer()
    {
        if (PlayerVitals == null) return;

        // Reset vitals
        PlayerVitals.Health = PlayerVitals.maxHealth;
        PlayerVitals.Hunger = PlayerVitals.maxHunger;
        PlayerVitals.Stamina = PlayerVitals.maxStamina;
        PlayerVitals.enabled = true;

        // Move player to spawn
        CharacterController cc = PlayerVitals.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            PlayerVitals.transform.position = spawnPoint;
            cc.enabled = true;
        }
        else
        {
            PlayerVitals.transform.position = spawnPoint;
        }

        SetState(GameState.Playing);
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
