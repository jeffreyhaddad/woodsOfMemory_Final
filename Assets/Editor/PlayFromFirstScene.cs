#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Automatically loads WelcomeScene (build index 0) when entering Play Mode,
/// regardless of which scene is currently open in the Editor.
/// </summary>
[InitializeOnLoad]
public static class PlayFromFirstScene
{
    const string MenuPath = "Edit/Always Start From Menu";
    const string PrefKey  = "PlayFromFirstScene_Enabled";

    static PlayFromFirstScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath, priority = 200)]
    static void ToggleEnabled()
    {
        Enabled = !Enabled;
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleEnabledValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!Enabled) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Save the currently open scene so we can restore it later
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // Load WelcomeScene (build index 0) as the startup scene
            string firstScene = EditorBuildSettings.scenes[0].path;
            if (EditorSceneManager.GetActiveScene().path != firstScene)
                EditorSceneManager.OpenScene(firstScene);
        }
    }
}
#endif
