using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// StartScene controls.  StartGame and QuitGame are public so they can be wired
/// to buttons, and the same actions are available from the keyboard.
/// </summary>
public class StartMenu : MonoBehaviour
{
    [SerializeField] private string levelSceneName = "Level01";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
            || Input.GetMouseButtonDown(0))
        {
            StartGame();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(levelSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
