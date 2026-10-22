using UnityEngine;

/// <summary>
/// Moves PacStudent one cell at a time with the lastInput / currentInput
/// algorithm.
///
/// lastInput remembers the most recent key the player pressed. currentInput is
/// the direction actually being travelled. Every time a step finishes, lastInput
/// gets first refusal: if the cell that way is clear it becomes the new
/// currentInput, otherwise the old currentInput carries on. If neither is
/// possible PacStudent stops and the wall clip loops.
///
/// Motion goes through <see cref="Tweener"/>, which interpolates from
/// accumulated Time.deltaTime. There is no Rigidbody, no CharacterController,
/// no Vector3.MoveTowards and no third party tweening anywhere in this path.
/// </summary>
[RequireComponent(typeof(Tweener))]
public class PacStudentController : MonoBehaviour
{
    private static readonly Vector2Int Up = new Vector2Int(0, -1);
    private static readonly Vector2Int Down = new Vector2Int(0, 1);
    private static readonly Vector2Int Left = new Vector2Int(-1, 0);
    private static readonly Vector2Int Right = new Vector2Int(1, 0);

    [Header("Level")]
    [SerializeField] private LevelGenerator level;

    [Tooltip("Starting cell, as column then row, with row 0 at the top.")]
    [SerializeField] private int startColumn = 1;
    [SerializeField] private int startRow = 1;

    [Header("Movement")]
    [Tooltip("Cells crossed per second. One cell is one world unit.")]
    [SerializeField] private float tilesPerSecond = 5f;

    [Header("Presentation")]
    [SerializeField] private Animator animator;
    [SerializeField] private DustTrail dustTrail;

    [Header("Audio")]
    [SerializeField] private AudioSource moveAudio;
    [SerializeField] private AudioClip movingClip;
    [SerializeField] private AudioClip wallClip;

    private Tweener _tweener;
    private Vector2Int _cell;
    private Vector2Int _lastInput;
    private Vector2Int _currentInput;
    private bool _blocked;

    /// <summary>Cell PacStudent currently occupies, as (column, row).</summary>
    public Vector2Int Cell
    {
        get { return _cell; }
    }

    /// <summary>Direction being travelled, or zero when stopped.</summary>
    public Vector2Int Direction
    {
        get { return _currentInput; }
    }

    private void Awake()
    {
        _tweener = GetComponent<Tweener>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        _cell = new Vector2Int(startColumn, startRow);
        transform.position = LevelGrid.CellToWorld(_cell);

        // Facing right, standing still, waiting for the first key.
        Face(Right);
        SetAnimating(false);

        if (dustTrail != null)
        {
            dustTrail.Emitting = false;
        }
    }

    private void Update()
    {
        ReadInput();

        if (!_tweener.TweenExists)
        {
            Step();
        }

        UpdateAudio();
    }

    private void ReadInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            _lastInput = Up;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            _lastInput = Down;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _lastInput = Left;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            _lastInput = Right;
        }
    }

    /// <summary>Picks a direction and starts one cell of travel, if it can.</summary>
    private void Step()
    {
        if (_lastInput != Vector2Int.zero && CanEnter(_cell + _lastInput))
        {
            _currentInput = _lastInput;
        }

        if (_currentInput != Vector2Int.zero && CanEnter(_cell + _currentInput))
        {
            Travel(_currentInput);
            return;
        }

        Halt();
    }

    private void Travel(Vector2Int direction)
    {
        Vector2Int target = _cell + direction;
        Vector3 from = LevelGrid.CellToWorld(_cell);
        Vector3 to = LevelGrid.CellToWorld(target);

        float speed = Mathf.Max(0.01f, tilesPerSecond);
        if (!_tweener.AddTween(transform, from, to, 1f / speed))
        {
            return;
        }

        _cell = target;
        _blocked = false;

        Face(direction);
        SetAnimating(true);

        if (dustTrail != null)
        {
            dustTrail.Emitting = true;
        }
    }

    private void Halt()
    {
        // Blocked only counts if the player actually wants to go somewhere.
        _blocked = _lastInput != Vector2Int.zero || _currentInput != Vector2Int.zero;

        SetAnimating(false);

        if (dustTrail != null)
        {
            dustTrail.Emitting = false;
        }
    }

    /// <summary>True when the cell is inside the level and is not a wall.</summary>
    private bool CanEnter(Vector2Int cell)
    {
        if (level == null)
        {
            return false;
        }

        return level.IsWalkable(cell.y, cell.x);
    }

    private void Face(Vector2Int direction)
    {
        if (animator == null)
        {
            return;
        }

        if (direction == Up)
        {
            animator.Play("Up");
        }
        else if (direction == Down)
        {
            animator.Play("Down");
        }
        else if (direction == Left)
        {
            animator.Play("Left");
        }
        else if (direction == Right)
        {
            animator.Play("Right");
        }
    }

    /// <summary>The walk cycle runs while travelling and freezes when stopped.</summary>
    private void SetAnimating(bool animating)
    {
        if (animator != null)
        {
            animator.speed = animating ? 1f : 0f;
        }
    }

    /// <summary>
    /// One looping clip while travelling, a different one while held against a
    /// wall, silence otherwise.
    /// </summary>
    private void UpdateAudio()
    {
        if (moveAudio == null)
        {
            return;
        }

        AudioClip wanted = null;

        if (_tweener.TweenExists)
        {
            wanted = movingClip;
        }
        else if (_blocked)
        {
            wanted = wallClip;
        }

        if (wanted == null)
        {
            if (moveAudio.isPlaying)
            {
                moveAudio.Stop();
            }

            return;
        }

        if (moveAudio.clip != wanted)
        {
            moveAudio.Stop();
            moveAudio.clip = wanted;
        }

        moveAudio.loop = true;

        if (!moveAudio.isPlaying)
        {
            moveAudio.Play();
        }
    }
}
