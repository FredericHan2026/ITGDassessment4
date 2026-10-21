using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the stored best score and time for one level on the title screen.
///
/// Reads PlayerPrefs, so it displays 000000 and 00:00:00 until a run has been
/// saved. The same key names are used when a score is written at game over.
/// </summary>
public class HighScoreDisplay : MonoBehaviour
{
    public const string ScoreKeyPrefix = "HighScore_";
    public const string TimeKeyPrefix = "HighScoreTime_";

    [SerializeField] private string levelKey = "Level01";
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;

    private void Start()
    {
        Refresh();
    }

    /// <summary>Re-reads the stored values and updates both labels.</summary>
    public void Refresh()
    {
        int score = PlayerPrefs.GetInt(ScoreKeyPrefix + levelKey, 0);
        float seconds = PlayerPrefs.GetFloat(TimeKeyPrefix + levelKey, 0f);

        if (scoreText != null)
        {
            scoreText.text = FormatScore(score);
        }

        if (timeText != null)
        {
            timeText.text = FormatTime(seconds);
        }
    }

    /// <summary>Six digits, zero padded, e.g. 1234 becomes 001234.</summary>
    public static string FormatScore(int score)
    {
        return Mathf.Max(0, score).ToString("D6");
    }

    /// <summary>mm:ss:cc, where the last pair is hundredths of a second.</summary>
    public static string FormatTime(float seconds)
    {
        if (seconds < 0f)
        {
            seconds = 0f;
        }

        int minutes = (int)(seconds / 60f);
        int wholeSeconds = (int)(seconds % 60f);
        int hundredths = (int)((seconds * 100f) % 100f);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, wholeSeconds, hundredths);
    }
}
