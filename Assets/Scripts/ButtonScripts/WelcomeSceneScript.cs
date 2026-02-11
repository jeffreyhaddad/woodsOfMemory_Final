using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeSceneScript :MonoBehaviour
{

    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    
}
