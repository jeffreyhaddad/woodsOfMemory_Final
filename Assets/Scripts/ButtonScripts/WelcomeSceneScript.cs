using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeSceneScript :MonoBehaviour
{

    public void LoadGameScene()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    
}
