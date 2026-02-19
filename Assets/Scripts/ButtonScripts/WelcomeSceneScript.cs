using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeSceneScript :MonoBehaviour
{

    public void LoadGameScene()
    {
        SceneManager.LoadScene("TerrainScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    
}
