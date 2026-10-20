using UnityEngine;

/// <summary>
/// Walks PacStudent (the penguin) clockwise around the top-left inner block of
/// the level, for ever.
///
/// The path is a list of grid cells; each leg is handed to the Tweener with a
/// duration of distance / speed, so the penguin covers every leg at exactly the
/// same constant speed.  Turns are instant - on reaching a corner the walk
/// animation simply switches to the next direction's clip.
/// </summary>
[RequireComponent(typeof(Tweener))]
public class PacStudentController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Constant speed in world units per second.")]
    [SerializeField] private float speed = 3f;

    [Tooltip("Path cells given as (x = column, y = row), walked in order and then looped.")]
    [SerializeField] private Vector2Int[] pathCells =
    {
        new Vector2Int(1, 1),
        new Vector2Int(6, 1),
        new Vector2Int(6, 5),
        new Vector2Int(1, 5)
    };

    [Header("Presentation")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource movementAudio;

    [Tooltip("Loops the walking sound for as long as PacStudent is moving.")]
    [SerializeField] private bool playMovementAudio = true;

    private Tweener _tweener;
    private int _targetIndex;
    private string _currentState;

    private void Awake()
    {
        _tweener = GetComponent<Tweener>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (movementAudio == null)
        {
            movementAudio = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (pathCells == null || pathCells.Length < 2)
        {
            enabled = false;
            return;
        }

        transform.position = LevelGrid.CellToWorld(pathCells[0]);
        _targetIndex = 0;

        // The walking sound is the "moving but not eating a pellet" clip. It is
        // wired up on the AudioSource either way; this switch decides whether it
        // actually loops while PacStudent walks.
        if (movementAudio != null)
        {
            movementAudio.loop = true;
            if (playMovementAudio && !movementAudio.isPlaying)
            {
                movementAudio.Play();
            }
            else if (!playMovementAudio && movementAudio.isPlaying)
            {
                movementAudio.Stop();
            }
        }

        StartNextLeg();
    }

    private void Update()
    {
        if (!_tweener.TweenExists)
        {
            StartNextLeg();
        }
    }

    private void StartNextLeg()
    {
        Vector3 from = LevelGrid.CellToWorld(pathCells[_targetIndex]);
        _targetIndex = (_targetIndex + 1) % pathCells.Length;
        Vector3 to = LevelGrid.CellToWorld(pathCells[_targetIndex]);

        PlayWalkAnimation(to - from);

        float distance = (to - from).magnitude;
        _tweener.AddTween(transform, from, to, distance / Mathf.Max(0.0001f, speed));
    }

    /// <summary>Instant turn: swap straight to the clip for the new heading.</summary>
    private void PlayWalkAnimation(Vector3 heading)
    {
        string state;
        if (Mathf.Abs(heading.x) >= Mathf.Abs(heading.y))
        {
            state = heading.x >= 0f ? "Right" : "Left";
        }
        else
        {
            state = heading.y >= 0f ? "Up" : "Down";
        }

        if (state == _currentState || animator == null)
        {
            return;
        }

        _currentState = state;
        animator.Play(state, 0, 0f);
    }
}
