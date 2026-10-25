using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs one round: the opening countdown, scoring, the power pellet timer,
/// lives, and what happens when the level is cleared or the lives run out.
///
/// Pickups are settled on the grid rather than through physics. PacStudent tells
/// this class which cell he has just arrived on and the pellet sitting there, if
/// any, is taken. At five cells a second that can never miss one, and it needs
/// no Rigidbody or collider anywhere. The two things that move freely, the seals
/// and the bonus squid, are checked by distance instead.
/// </summary>
public class GameManager : MonoBehaviour
{
    private enum Phase
    {
        Countdown,
        Playing,
        Dying,
        Finished
    }

    [Header("Scene")]
    [SerializeField] private HUDController hud;
    [SerializeField] private PacStudentController pacStudent;
    [SerializeField] private LevelGenerator level;
    [SerializeField] private CherryController cherries;
    [SerializeField] private GhostController[] ghosts;

    [Header("Pickups")]
    [SerializeField] private Transform pelletParent;
    [SerializeField] private Transform powerPelletParent;

    [Header("Audio")]
    [SerializeField] private AudioManager music;
    [SerializeField] private SfxLibrary sfx;

    [Header("Scoring")]
    [SerializeField] private int pelletScore = 10;
    [SerializeField] private int powerPelletScore = 50;
    [SerializeField] private int cherryScore = 100;
    [SerializeField] private int ghostScore = 300;

    [Header("Timing")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float countdownSeconds = 3f;

    [Tooltip("How long GO! stays up after the count reaches zero.")]
    [SerializeField] private float goSeconds = 0.7f;

    [Tooltip("How long the seals stay edible after a power pellet.")]
    [SerializeField] private float scaredSeconds = 10f;

    [Tooltip("Final stretch of the scared time, spent flashing as a warning.")]
    [SerializeField] private float recoveringSeconds = 3f;

    [SerializeField] private float deathSeconds = 1.4f;
    [SerializeField] private float gameOverSeconds = 3.5f;

    [Header("Reach")]
    [Tooltip("How close PacStudent has to get to a seal or the squid.")]
    [SerializeField] private float contactRadius = 0.6f;

    [Header("High score")]
    [SerializeField] private string levelKey = "Level01";
    [SerializeField] private string menuSceneName = "StartScene";

    private readonly Dictionary<Vector2Int, GameObject> _pellets =
        new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> _powerPellets =
        new Dictionary<Vector2Int, GameObject>();

    private Phase _phase = Phase.Countdown;
    private float _phaseTimer;
    private float _scaredRemaining;
    private int _score;
    private int _lives;
    private int _remainingPickups;

    private void Start()
    {
        _lives = startingLives;
        CollectPickups();

        if (hud != null)
        {
            hud.SetScore(0);
            hud.SetLives(_lives);
            hud.ResetTimer();
            hud.HideScared();
        }

        BeginCountdown();
    }

    // ------------------------------------------------------------- pickups

    /// <summary>
    /// Indexes the pellets already in the scene by the cell they sit on, so a
    /// pickup is a dictionary lookup rather than a collision.
    /// </summary>
    private void CollectPickups()
    {
        Index(pelletParent, _pellets);
        Index(powerPelletParent, _powerPellets);
        _remainingPickups = _pellets.Count + _powerPellets.Count;
    }

    private static void Index(Transform parent, Dictionary<Vector2Int, GameObject> into)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Vector2Int cell = new Vector2Int(
                Mathf.RoundToInt(child.position.x),
                Mathf.RoundToInt(-child.position.y));

            if (!into.ContainsKey(cell))
            {
                into.Add(cell, child.gameObject);
            }
        }
    }

    /// <summary>Called by PacStudent each time he finishes a step onto a cell.</summary>
    public void OnPacStudentArrived(Vector2Int cell)
    {
        TryTakeAt(cell);
    }

    /// <summary>
    /// Called from PacStudent's trigger contacts. Both this and the step-based
    /// route end up in the same place, and taking a pickup removes it from the
    /// table, so whichever gets there first wins and the other does nothing.
    /// </summary>
    public void OnPacStudentTouched(Collider2D other)
    {
        if (other == null || _phase != Phase.Playing)
        {
            return;
        }

        GhostController ghost = other.GetComponent<GhostController>();

        if (ghost != null)
        {
            TouchGhost(ghost);
            return;
        }

        if (other.GetComponent<BonusItem>() != null)
        {
            TakeCherry(other.gameObject);
            return;
        }

        TryTakeAt(new Vector2Int(
            Mathf.RoundToInt(other.transform.position.x),
            Mathf.RoundToInt(-other.transform.position.y)));
    }

    /// <summary>Takes whatever pellet is on a cell. Safe to call twice.</summary>
    private void TryTakeAt(Vector2Int cell)
    {
        if (_phase != Phase.Playing)
        {
            return;
        }

        GameObject pickup;

        if (_pellets.TryGetValue(cell, out pickup))
        {
            _pellets.Remove(cell);
            Take(pickup, pelletScore);

            if (sfx != null)
            {
                sfx.PlayEatPellet();
            }
        }
        else if (_powerPellets.TryGetValue(cell, out pickup))
        {
            _powerPellets.Remove(cell);
            Take(pickup, powerPelletScore);

            if (sfx != null)
            {
                sfx.PlayEatPellet();
            }

            StartScared();
        }
        else
        {
            return;
        }

        if (_remainingPickups <= 0)
        {
            Finish(true);
        }
    }

    private void Take(GameObject pickup, int points)
    {
        if (pickup != null)
        {
            Destroy(pickup);
        }

        _remainingPickups--;
        AddScore(points);
    }

    private void AddScore(int points)
    {
        _score += points;

        if (hud != null)
        {
            hud.SetScore(_score);
        }
    }

    // ------------------------------------------------------------- phases

    private void BeginCountdown()
    {
        _phase = Phase.Countdown;
        _phaseTimer = countdownSeconds + goSeconds;

        if (pacStudent != null)
        {
            pacStudent.Frozen = true;
        }

        SetGhostsFrozen(true);

        if (hud != null)
        {
            hud.SetTimerRunning(false);
            hud.ShowMessage(Mathf.CeilToInt(countdownSeconds).ToString());
        }
    }

    private void BeginPlaying()
    {
        _phase = Phase.Playing;

        if (pacStudent != null)
        {
            pacStudent.Frozen = false;
        }

        SetGhostsFrozen(false);

        if (hud != null)
        {
            hud.HideMessage();
            hud.SetTimerRunning(true);
        }
    }

    private void Update()
    {
        if (_phase == Phase.Countdown)
        {
            TickCountdown();
            return;
        }

        if (_phase == Phase.Dying)
        {
            TickDying();
            return;
        }

        if (_phase != Phase.Playing)
        {
            return;
        }

        TickScared();
        CheckGhosts();
        CheckCherry();
    }

    private void TickCountdown()
    {
        _phaseTimer -= Time.deltaTime;

        if (_phaseTimer <= 0f)
        {
            BeginPlaying();
            return;
        }

        if (hud == null)
        {
            return;
        }

        // Counts 3, 2, 1 and then holds on GO! for the last moment.
        if (_phaseTimer > goSeconds)
        {
            hud.ShowMessage(Mathf.CeilToInt(_phaseTimer - goSeconds).ToString());
        }
        else
        {
            hud.ShowMessage("GO!");
        }
    }

    private void TickDying()
    {
        _phaseTimer -= Time.deltaTime;

        if (_phaseTimer > 0f)
        {
            return;
        }

        _lives--;

        if (hud != null)
        {
            hud.SetLives(_lives);
        }

        if (_lives <= 0)
        {
            Finish(false);
            return;
        }

        ResetPositions();
        BeginCountdown();
    }

    // -------------------------------------------------------- power pellet

    private void StartScared()
    {
        _scaredRemaining = scaredSeconds;
        SetGhostStates(GhostState.Scared);

        if (music != null)
        {
            music.PlayGhostScared();
        }
    }

    private void TickScared()
    {
        if (_scaredRemaining <= 0f)
        {
            return;
        }

        _scaredRemaining -= Time.deltaTime;

        if (_scaredRemaining <= 0f)
        {
            _scaredRemaining = 0f;
            SetGhostStates(GhostState.Normal);

            if (hud != null)
            {
                hud.HideScared();
            }

            if (music != null)
            {
                music.PlayGhostNormal();
            }

            return;
        }

        // Set every frame, so a seal that respawned mid-scare rejoins the state.
        SetGhostStates(_scaredRemaining <= recoveringSeconds
                           ? GhostState.Recovering
                           : GhostState.Scared);

        if (hud != null)
        {
            hud.ShowScared(_scaredRemaining);
        }
    }

    /// <summary>Changes state on every seal that is not already on its way home.</summary>
    private void SetGhostStates(GhostState state)
    {
        if (ghosts == null)
        {
            return;
        }

        for (int i = 0; i < ghosts.Length; i++)
        {
            if (ghosts[i] == null || ghosts[i].State == GhostState.Dead)
            {
                continue;
            }

            if (ghosts[i].State != state)
            {
                ghosts[i].SetState(state);
            }
        }
    }

    // ------------------------------------------------------------ contact

    private void CheckGhosts()
    {
        if (ghosts == null || pacStudent == null)
        {
            return;
        }

        Vector3 here = pacStudent.transform.position;

        for (int i = 0; i < ghosts.Length; i++)
        {
            GhostController ghost = ghosts[i];

            if (ghost == null || ghost.State == GhostState.Dead)
            {
                continue;
            }

            if (Vector3.Distance(ghost.transform.position, here) > contactRadius)
            {
                continue;
            }

            if (TouchGhost(ghost))
            {
                return;
            }
        }
    }

    /// <summary>
    /// Settles one contact with a seal. Returns true when it cost a life, which
    /// ends the round for this frame. Safe to call twice: an eaten seal is
    /// already dead and gets skipped.
    /// </summary>
    private bool TouchGhost(GhostController ghost)
    {
        if (ghost == null || ghost.State == GhostState.Dead || _phase != Phase.Playing)
        {
            return false;
        }

        if (ghost.IsEdible)
        {
            ghost.SetState(GhostState.Dead);
            AddScore(ghostScore);

            if (sfx != null)
            {
                sfx.PlayEatGhost();
            }

            if (music != null)
            {
                music.PlayGhostDead();
            }

            return false;
        }

        BeginDying();
        return true;
    }

    private void CheckCherry()
    {
        if (cherries == null || pacStudent == null)
        {
            return;
        }

        if (cherries.TryEat(pacStudent.transform.position, contactRadius))
        {
            ScoreCherry();
        }
    }

    /// <summary>Takes one bonus item by object, for the trigger route.</summary>
    private void TakeCherry(GameObject cherry)
    {
        if (cherries != null && cherries.TryEat(cherry))
        {
            ScoreCherry();
        }
    }

    private void ScoreCherry()
    {
        AddScore(cherryScore);

        if (sfx != null)
        {
            sfx.PlayEatCherry();
        }
    }

    private void SetGhostsFrozen(bool frozen)
    {
        if (ghosts == null)
        {
            return;
        }

        for (int i = 0; i < ghosts.Length; i++)
        {
            if (ghosts[i] != null)
            {
                ghosts[i].Frozen = frozen;
            }
        }
    }

    private void BeginDying()
    {
        _phase = Phase.Dying;
        _phaseTimer = deathSeconds;
        _scaredRemaining = 0f;

        if (hud != null)
        {
            hud.SetTimerRunning(false);
            hud.HideScared();
        }

        SetGhostsFrozen(true);

        if (pacStudent != null)
        {
            pacStudent.Die();
        }

        if (sfx != null)
        {
            sfx.PlayDeath();
        }
    }

    private void ResetPositions()
    {
        if (pacStudent != null)
        {
            pacStudent.Frozen = true;
            pacStudent.ResetToStart();
        }

        if (ghosts != null)
        {
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i] != null)
                {
                    ghosts[i].ResetToStart();
                }
            }
        }

        if (music != null)
        {
            music.PlayGhostNormal();
        }
    }

    // --------------------------------------------------------- end of round

    private void Finish(bool cleared)
    {
        if (_phase == Phase.Finished)
        {
            return;
        }

        _phase = Phase.Finished;
        _phaseTimer = gameOverSeconds;

        if (pacStudent != null)
        {
            pacStudent.Frozen = true;
        }

        SetGhostsFrozen(true);

        if (hud != null)
        {
            hud.SetTimerRunning(false);
            hud.HideScared();
            hud.ShowMessage(cleared ? "LEVEL CLEARED" : "GAME OVER");
        }

        SaveHighScore();
        Invoke("ReturnToMenu", gameOverSeconds);
    }

    /// <summary>
    /// Keeps the better run: a higher score wins, and on a tie the quicker time
    /// does. Both are stored under the same keys the title screen reads.
    /// </summary>
    private void SaveHighScore()
    {
        float elapsed = hud != null ? hud.Elapsed : 0f;
        string scoreKey = HighScoreDisplay.ScoreKeyPrefix + levelKey;
        string timeKey = HighScoreDisplay.TimeKeyPrefix + levelKey;

        int bestScore = PlayerPrefs.GetInt(scoreKey, 0);
        float bestTime = PlayerPrefs.GetFloat(timeKey, 0f);

        bool better = _score > bestScore
                      || (_score == bestScore && bestTime > 0f && elapsed < bestTime)
                      || (_score == bestScore && bestTime <= 0f && elapsed > 0f);

        if (!better)
        {
            return;
        }

        PlayerPrefs.SetInt(scoreKey, _score);
        PlayerPrefs.SetFloat(timeKey, elapsed);
        PlayerPrefs.Save();
    }

    private void ReturnToMenu()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
