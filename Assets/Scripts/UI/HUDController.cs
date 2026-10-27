using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the in-game HUD: level name, score, lives, the round timer and the
/// ghost scared countdown.
///
/// Everything here is display only. The HUD does not decide anything about the
/// game; the round logic calls into it. The timer is held still by default so
/// that at this stage the Exit button is the only element that actually does
/// something, and the round logic turns it on when it takes over.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private string levelName = "LEVEL 1";
    [SerializeField] private Text levelNameText;

    [Header("Score")]
    [SerializeField] private Text scoreText;

    [Header("Lives")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private GameObject[] lifeIcons;

    [Header("Timers")]
    [SerializeField] private Text timerText;
    [SerializeField] private GameObject scaredRoot;
    [SerializeField] private Text scaredText;

    [Tooltip("Left off until the round logic takes over the clock.")]
    [SerializeField] private bool runTimer = false;

    [Header("Centre message")]
    [Tooltip("Used for the round countdown and the game over banner.")]
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private Text messageText;

    [Header("Exit")]
    [SerializeField] private Button exitButton;

    [Tooltip("Scene the Exit button returns to. Empty quits the application.")]
    [SerializeField] private string exitSceneName = "StartScene";

    private int _score;
    private int _lives;
    private float _elapsed;

    /// <summary>Seconds the round has been running, for the high score write.</summary>
    public float Elapsed
    {
        get { return _elapsed; }
    }

    public int Score
    {
        get { return _score; }
    }

    public int Lives
    {
        get { return _lives; }
    }

    private void Awake()
    {
        _lives = startingLives;
    }

    private void Start()
    {
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(Exit);
        }

        if (levelNameText != null)
        {
            levelNameText.text = levelName;
        }

        SetScore(0);
        SetLives(startingLives);
        SetTime(0f);
        HideScared();
        HideMessage();
    }

    /// <summary>Big centred text, for the countdown and the game over banner.</summary>
    public void ShowMessage(string message)
    {
        if (messageRoot != null)
        {
            messageRoot.SetActive(true);
        }

        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    public void HideMessage()
    {
        if (messageRoot != null)
        {
            messageRoot.SetActive(false);
        }

        if (messageText != null)
        {
            messageText.text = "";
        }
    }

    private void Update()
    {
        if (runTimer)
        {
            _elapsed += Time.deltaTime;
            SetTime(_elapsed);
        }
    }

    /// <summary>Starts or stops the round clock. Called by the round logic.</summary>
    public void SetTimerRunning(bool running)
    {
        runTimer = running;
    }

    public void ResetTimer()
    {
        _elapsed = 0f;
        SetTime(0f);
    }

    public void AddScore(int amount)
    {
        SetScore(_score + amount);
    }

    public void SetScore(int value)
    {
        _score = Mathf.Max(0, value);

        if (scoreText != null)
        {
            scoreText.text = HighScoreDisplay.FormatScore(_score);
        }
    }

    /// <summary>Shows one icon per remaining life and hides the rest.</summary>
    public void SetLives(int value)
    {
        _lives = Mathf.Max(0, value);

        if (lifeIcons == null)
        {
            return;
        }

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] != null)
            {
                lifeIcons[i].SetActive(i < _lives);
            }
        }
    }

    public void SetTime(float seconds)
    {
        if (timerText != null)
        {
            timerText.text = HighScoreDisplay.FormatTime(seconds);
        }
    }

    /// <summary>Shows the scared countdown. At zero or below it hides itself.</summary>
    public void ShowScared(float secondsRemaining)
    {
        if (secondsRemaining <= 0f)
        {
            HideScared();
            return;
        }

        if (scaredRoot != null)
        {
            scaredRoot.SetActive(true);
        }

        if (scaredText != null)
        {
            scaredText.text = Mathf.CeilToInt(secondsRemaining).ToString();
        }
    }

    public void HideScared()
    {
        if (scaredRoot != null)
        {
            scaredRoot.SetActive(false);
        }

        if (scaredText != null)
        {
            scaredText.text = "";
        }
    }

    /// <summary>
    /// Locks the HUD's controls. Used at game over, where the brief asks for
    /// any UI buttons to be unusable.
    /// </summary>
    public void SetButtonsInteractable(bool interactable)
    {
        if (exitButton != null)
        {
            exitButton.interactable = interactable;
        }
    }

    /// <summary>Leaves the level. Returns to the menu, or quits if none is set.</summary>
    public void Exit()
    {
        if (!string.IsNullOrEmpty(exitSceneName))
        {
            SceneManager.LoadScene(exitSceneName);
            return;
        }

        Application.Quit();
    }
}
