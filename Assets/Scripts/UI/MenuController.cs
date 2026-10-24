using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Title screen buttons. Level 1 loads the main level; Level 2 is reserved for
/// the design iteration scene and does nothing until that scene exists.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string level1SceneName = "Level01";
    [SerializeField] private string level2SceneName = "";

    [Header("Buttons")]
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudio;
    [SerializeField] private AudioClip clickClip;

    private void Start()
    {
        if (level1Button != null)
        {
            level1Button.onClick.AddListener(LoadLevel1);
        }

        if (level2Button != null)
        {
            level2Button.onClick.AddListener(LoadLevel2);
            // Greyed out until the design iteration scene is built.
            level2Button.interactable = !string.IsNullOrEmpty(level2SceneName);
        }
    }

    public void LoadLevel1()
    {
        PlayClick();
        LoadScene(level1SceneName);
    }

    public void LoadLevel2()
    {
        PlayClick();
        LoadScene(level2SceneName);
    }

    private void PlayClick()
    {
        if (uiAudio != null && clickClip != null)
        {
            uiAudio.PlayOneShot(clickClip);
        }
    }

    private static void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
