using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeSceneScript : MonoBehaviour
{
    private SettingsUI settingsUI;

    void Start()
    {
        if (SettingsManager.Instance == null)
        {
            var smGo = new GameObject("SettingsManager");
            smGo.AddComponent<SettingsManager>();
        }

        settingsUI = FindAnyObjectByType<SettingsUI>();
        if (settingsUI == null)
        {
            var suiGo = new GameObject("SettingsUI");
            settingsUI = suiGo.AddComponent<SettingsUI>();
        }
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("TerrainScene");
    }

    public void ContinueGame()
    {
        if (SaveManager.SaveFileExists(0))
            SaveManager.RequestLoad(0);
        SceneManager.LoadScene("TerrainScene");
    }

    /// <summary>Returns true if slot 0 has a save file (for greying out the Continue button).</summary>
    public static bool HasSave() => SaveManager.SaveFileExists(0);

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenSettings()
    {
        if (settingsUI == null)
            settingsUI = FindAnyObjectByType<SettingsUI>();
        settingsUI?.Open();
    }
}

