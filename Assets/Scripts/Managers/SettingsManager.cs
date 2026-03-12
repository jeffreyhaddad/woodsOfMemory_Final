using System.Collections.Generic;
using UnityEngine;

public enum GameAction
{
    Jump,
    Crouch,
    Sprint,
    Interact,
    Journal,
    Inventory,
    Crafting
}

/// <summary>
/// Persistent settings manager. Stores audio volumes, camera settings, and keybinds.
/// Persists across scenes via DontDestroyOnLoad and PlayerPrefs.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // ── Defaults ─────────────────────────────────────────────
    public const float DefaultSFXVolume              = 0.5f;
    public const float DefaultMusicVolume            = 0.25f;
    public const float DefaultAmbientVolume          = 0.5f;
    public const float DefaultSensitivityX           = 2.5f;
    public const float DefaultSensitivityY           = 1.5f;
    public const float DefaultFieldOfView            = 80f;
    public const bool  DefaultInvertY                = false;
    public const bool  DefaultShowPickupNotifications = true;
    public const bool  DefaultHotbarAutoPopulate      = true;

    private static readonly Dictionary<GameAction, KeyCode> DefaultKeys = new Dictionary<GameAction, KeyCode>
    {
        { GameAction.Jump,      KeyCode.Space },
        { GameAction.Crouch,    KeyCode.LeftControl },
        { GameAction.Sprint,    KeyCode.LeftShift },
        { GameAction.Interact,  KeyCode.E },
        { GameAction.Journal,   KeyCode.J },
        { GameAction.Inventory, KeyCode.Tab },
        { GameAction.Crafting,  KeyCode.C },
    };

    // ── Stored Values ─────────────────────────────────────────
    public float SFXVolume                { get; private set; }
    public float MusicVolume              { get; private set; }
    public float AmbientVolume            { get; private set; }
    public float SensitivityX             { get; private set; }
    public float SensitivityY             { get; private set; }
    public float FieldOfView              { get; private set; }
    public bool  InvertY                  { get; private set; }
    public bool  ShowPickupNotifications  { get; private set; }
    public bool  HotbarAutoPopulate       { get; private set; }

    private Dictionary<GameAction, KeyCode> keybinds = new Dictionary<GameAction, KeyCode>();

    // ── PlayerPrefs Keys ──────────────────────────────────────
    private const string K_SFX       = "set_sfx";
    private const string K_MUS       = "set_music";
    private const string K_AMB       = "set_amb";
    private const string K_SENSX     = "set_sensx";
    private const string K_SENSY     = "set_sensy";
    private const string K_FOV       = "set_fov";
    private const string K_INVERTY   = "set_inverty";
    private const string K_PICKUPNOTIF = "set_pickupnotif";
    private const string K_HOTBARAUTO  = "set_hotbarauto";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    // ── Load / Save ───────────────────────────────────────────

    void Load()
    {
        SFXVolume     = PlayerPrefs.GetFloat(K_SFX,    DefaultSFXVolume);
        MusicVolume   = PlayerPrefs.GetFloat(K_MUS,    DefaultMusicVolume);
        AmbientVolume = PlayerPrefs.GetFloat(K_AMB,    DefaultAmbientVolume);
        SensitivityX  = PlayerPrefs.GetFloat(K_SENSX,  DefaultSensitivityX);
        SensitivityY  = PlayerPrefs.GetFloat(K_SENSY,  DefaultSensitivityY);
        FieldOfView   = PlayerPrefs.GetFloat(K_FOV,    DefaultFieldOfView);
        InvertY                 = PlayerPrefs.GetInt(K_INVERTY,      DefaultInvertY ? 1 : 0) == 1;
        ShowPickupNotifications = PlayerPrefs.GetInt(K_PICKUPNOTIF, DefaultShowPickupNotifications ? 1 : 0) == 1;
        HotbarAutoPopulate      = PlayerPrefs.GetInt(K_HOTBARAUTO,  DefaultHotbarAutoPopulate ? 1 : 0) == 1;

        foreach (GameAction action in System.Enum.GetValues(typeof(GameAction)))
        {
            string prefKey = "set_key_" + action.ToString();
            int stored = PlayerPrefs.GetInt(prefKey, -1);
            keybinds[action] = (stored == -1) ? DefaultKeys[action] : (KeyCode)stored;
        }
    }

    // ── Static Key Lookup ─────────────────────────────────────

    public static KeyCode GetKey(GameAction action)
    {
        if (Instance != null && Instance.keybinds.TryGetValue(action, out KeyCode k))
            return k;
        return DefaultKeys.TryGetValue(action, out KeyCode def) ? def : KeyCode.None;
    }

    public KeyCode GetKeybind(GameAction action) => GetKey(action);

    // Check if any OTHER action already uses this key
    public bool IsKeyConflict(GameAction action, KeyCode key, out GameAction conflicting)
    {
        foreach (var kvp in keybinds)
        {
            if (kvp.Key != action && kvp.Value == key)
            {
                conflicting = kvp.Key;
                return true;
            }
        }
        conflicting = default;
        return false;
    }

    // ── Setters (apply live + save) ───────────────────────────

    public void SetSFXVolume(float v)
    {
        SFXVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(K_SFX, SFXVolume);
        if (SFXManager.Instance != null)
            SFXManager.Instance.masterVolume = SFXVolume;
    }

    public void SetMusicVolume(float v)
    {
        MusicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(K_MUS, MusicVolume);
        if (MusicManager.Instance != null)
            MusicManager.Instance.masterVolume = MusicVolume;
    }

    public void SetAmbientVolume(float v)
    {
        AmbientVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(K_AMB, AmbientVolume);
        if (AmbientAudio.Instance != null)
        {
            AmbientAudio.Instance.dayVolume   = AmbientVolume * 0.6f;
            AmbientAudio.Instance.nightVolume  = AmbientVolume;
        }
    }

    public void SetSensitivityX(float v)
    {
        SensitivityX = Mathf.Clamp(v, 0.5f, 5f);
        PlayerPrefs.SetFloat(K_SENSX, SensitivityX);
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.sensitivityX = SensitivityX;
    }

    public void SetSensitivityY(float v)
    {
        SensitivityY = Mathf.Clamp(v, 0.5f, 5f);
        PlayerPrefs.SetFloat(K_SENSY, SensitivityY);
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.sensitivityY = SensitivityY;
    }

    public void SetFOV(float v)
    {
        FieldOfView = Mathf.Clamp(v, 60f, 110f);
        PlayerPrefs.SetFloat(K_FOV, FieldOfView);
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.fieldOfView = FieldOfView;
    }

    public void SetInvertY(bool v)
    {
        InvertY = v;
        PlayerPrefs.SetInt(K_INVERTY, v ? 1 : 0);
        if (CameraFollow.Instance != null)
            CameraFollow.Instance.invertY = InvertY;
    }

    public void SetKeybind(GameAction action, KeyCode key)
    {
        keybinds[action] = key;
        PlayerPrefs.SetInt("set_key_" + action.ToString(), (int)key);
    }

    public void SetShowPickupNotifications(bool v)
    {
        ShowPickupNotifications = v;
        PlayerPrefs.SetInt(K_PICKUPNOTIF, v ? 1 : 0);
    }

    public void SetHotbarAutoPopulate(bool v)
    {
        HotbarAutoPopulate = v;
        PlayerPrefs.SetInt(K_HOTBARAUTO, v ? 1 : 0);
    }

    // ── Apply All ─────────────────────────────────────────────

    /// <summary>Push all stored settings to live components. Call from component Start().</summary>
    public void ApplyAll()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.masterVolume = SFXVolume;

        if (MusicManager.Instance != null)
            MusicManager.Instance.masterVolume = MusicVolume;

        if (AmbientAudio.Instance != null)
        {
            AmbientAudio.Instance.dayVolume   = AmbientVolume * 0.6f;
            AmbientAudio.Instance.nightVolume  = AmbientVolume;
        }

        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.sensitivityX = SensitivityX;
            CameraFollow.Instance.sensitivityY = SensitivityY;
            CameraFollow.Instance.fieldOfView  = FieldOfView;
            CameraFollow.Instance.invertY      = InvertY;
        }
    }

    // ── Reset ─────────────────────────────────────────────────

    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(K_SFX);
        PlayerPrefs.DeleteKey(K_MUS);
        PlayerPrefs.DeleteKey(K_AMB);
        PlayerPrefs.DeleteKey(K_SENSX);
        PlayerPrefs.DeleteKey(K_SENSY);
        PlayerPrefs.DeleteKey(K_FOV);
        PlayerPrefs.DeleteKey(K_INVERTY);
        foreach (GameAction action in System.Enum.GetValues(typeof(GameAction)))
            PlayerPrefs.DeleteKey("set_key_" + action.ToString());
        PlayerPrefs.DeleteKey(K_PICKUPNOTIF);
        PlayerPrefs.DeleteKey(K_HOTBARAUTO);

        Load();
        ApplyAll();
    }
}
