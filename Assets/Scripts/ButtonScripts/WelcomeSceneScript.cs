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

