using UnityEngine;

/// <summary>Which of the four behaviours a seal is currently showing.</summary>
public enum GhostState
{
    Normal,
    Scared,
    Recovering,
    Dead
}

/// <summary>How a seal picks its way around the maze while it is hunting.</summary>
public enum GhostBehaviour
{
    /// <summary>Takes whichever turn leads furthest from PacStudent.</summary>
    Furthest,

    /// <summary>Takes whichever turn leads closest to PacStudent.</summary>
    Closest,

    /// <summary>Picks at random.</summary>
    Random,

    /// <summary>Keeps bearing right, so it works its way around clockwise.</summary>
    Clockwise
}

/// <summary>
/// A seal: its identity, its state, and how it moves.
///
/// Decisions are made one cell at a time from what is immediately around it,
/// so there is no pathfinding anywhere. A seal never stops, never turns back on
/// itself unless that is the only way out of a dead end, and never uses the
/// tunnel. Travel goes through <see cref="Tweener"/> like everything else.
///
/// Once eaten it heads home in a straight line, ignoring walls, through the gap
/// it was given, and goes back to normal when it gets there.
/// </summary>
[RequireComponent(typeof(Tweener))]
public class GhostController : MonoBehaviour
{
    private static readonly Vector2Int Up = new Vector2Int(0, -1);
    private static readonly Vector2Int Down = new Vector2Int(0, 1);
    private static readonly Vector2Int Left = new Vector2Int(-1, 0);
    private static readonly Vector2Int Right = new Vector2Int(1, 0);
    private static readonly Vector2Int[] All = { Up, Right, Down, Left };

    [Header("Identity")]
    [Tooltip("1 to 4, matching the label above the seal.")]
    [SerializeField] private int ghostNumber = 1;

    [SerializeField] private GhostBehaviour behaviour = GhostBehaviour.Furthest;

    [Tooltip("Spawn cell, as column then row, with row 0 at the top.")]
    [SerializeField] private int startColumn;
    [SerializeField] private int startRow;

    [Tooltip("The gap this seal comes back through after being eaten.")]
    [SerializeField] private int respawnColumn;
    [SerializeField] private int respawnRow;

    [Header("Level")]
    [SerializeField] private LevelGenerator level;
    [SerializeField] private Transform target;

    [Header("Speed")]
    [SerializeField] private float normalTilesPerSecond = 3.5f;
    [SerializeField] private float scaredTilesPerSecond = 2.5f;

    [Tooltip("World units per second on the way home, in a straight line.")]
    [SerializeField] private float deadUnitsPerSecond = 12f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Colour a frightened seal is tinted. The artwork underneath is "
             + "flat grey so this shows through cleanly.")]
    [SerializeField] private Color scaredTint = new Color(1f, 0.38f, 0.68f, 1f);

    [Tooltip("Flashes per second while recovering.")]
    [SerializeField] private float flashRate = 6f;

    [Header("Pen")]
    [Tooltip("How long it sits in the pen after being eaten before heading out.")]
    [SerializeField] private float penPauseSeconds = 1f;

    private Tweener _tweener;
    private GhostState _state = GhostState.Normal;
    private Vector2Int _cell;
    private Vector2Int _direction = Vector2Int.zero;
    private bool _leaving;
    private float _hold;

    /// <summary>While frozen the seal holds still, for the round countdown.</summary>
    public bool Frozen { get; set; }

    public int Number
    {
        get { return ghostNumber; }
    }

    public GhostState State
    {
        get { return _state; }
    }

    /// <summary>True while PacStudent can eat this seal rather than the reverse.</summary>
    public bool IsEdible
    {
        get { return _state == GhostState.Scared || _state == GhostState.Recovering; }
    }

    private void Awake()
    {
        _tweener = GetComponent<Tweener>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        ResetToStart();
    }

    private void Update()
    {
        Flash();

        if (Frozen)
        {
            return;
        }

        if (_state == GhostState.Dead)
        {
            TickDead();
            return;
        }

        // Sitting in the pen for a beat after being eaten, so it does not pop
        // straight back out of the doorway it arrived at.
        if (_hold > 0f)
        {
            _hold -= Time.deltaTime;
            return;
        }

        if (!_tweener.TweenExists)
        {
            Step();
        }
    }

    // ------------------------------------------------------------ movement

    private void Step()
    {
        Vector2Int chosen = ChooseDirection();

        if (chosen == Vector2Int.zero)
        {
            return;
        }

        Vector2Int next = _cell + chosen;
        float speed = IsEdible ? scaredTilesPerSecond : normalTilesPerSecond;

        if (!_tweener.AddTween(transform, LevelGrid.CellToWorld(_cell),
                               LevelGrid.CellToWorld(next),
                               1f / Mathf.Max(0.01f, speed)))
        {
            return;
        }

        _cell = next;
        _direction = chosen;
        Face(chosen);

        if (_leaving && _cell == ExitCell)
        {
            _leaving = false;
        }
    }

    /// <summary>The gap in the house this seal uses, both on the way out and back.</summary>
    private Vector2Int Gap
    {
        get { return new Vector2Int(respawnColumn, respawnRow); }
    }

    /// <summary>Which way is out: up through a gap above the pen, down below it.</summary>
    private Vector2Int Outward
    {
        get { return respawnRow < startRow ? Up : Down; }
    }

    /// <summary>
    /// The first cell properly outside the house. Heading for the gap itself is
    /// not enough: standing in the doorway, every direction looks equally good
    /// and the seal can turn straight back in.
    /// </summary>
    private Vector2Int ExitCell
    {
        get { return Gap + Outward; }
    }

    /// <summary>
    /// Looks only at the four cells next door. Turning back on itself is a last
    /// resort, so a seal in a corridor keeps going rather than dithering.
    /// </summary>
    private Vector2Int ChooseDirection()
    {
        Vector2Int back = new Vector2Int(-_direction.x, -_direction.y);
        Vector2Int best = Vector2Int.zero;
        float bestScore = 0f;
        int seen = 0;

        for (int i = 0; i < All.Length; i++)
        {
            Vector2Int dir = All[i];

            if (_direction != Vector2Int.zero && dir == back)
            {
                continue;
            }

            if (!CanEnter(_cell + dir))
            {
                continue;
            }

            seen++;
            float score = Score(dir, seen);

            if (seen == 1 || score > bestScore)
            {
                best = dir;
                bestScore = score;
            }
        }

        if (seen > 0)
        {
            return best;
        }

        // Dead end: turning round is the only way out.
        return CanEnter(_cell + back) ? back : Vector2Int.zero;
    }

    /// <summary>Higher is better. The seal's behaviour decides what that means.</summary>
    private float Score(Vector2Int dir, int seen)
    {
        // On the way out of the house it heads for its gap and nothing else.
        // Right-hand following would otherwise circle the open pen forever.
        if (_leaving)
        {
            return -Manhattan(_cell + dir, ExitCell);
        }

        // A frightened seal stops hunting and just scatters.
        GhostBehaviour mode = IsEdible ? GhostBehaviour.Random : behaviour;

        if (mode == GhostBehaviour.Random)
        {
            return Random.value;
        }

        if (mode == GhostBehaviour.Clockwise)
        {
            // Bear right first, then straight on, then left.
            if (dir == Clockwise(_direction))
            {
                return 3f;
            }

            return dir == _direction ? 2f : 1f;
        }

        float distance = DistanceToTarget(_cell + dir);
        return mode == GhostBehaviour.Furthest ? distance : -distance;
    }

    private float DistanceToTarget(Vector2Int cell)
    {
        if (target == null)
        {
            return 0f;
        }

        return Manhattan(cell, new Vector2Int(
            Mathf.RoundToInt(target.position.x),
            Mathf.RoundToInt(-target.position.y)));
    }

    /// <summary>Grid distance. Not a path, just which way is nearer.</summary>
    private static float Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static Vector2Int Clockwise(Vector2Int dir)
    {
        if (dir == Up)
        {
            return Right;
        }

        if (dir == Right)
        {
            return Down;
        }

        if (dir == Down)
        {
            return Left;
        }

        return Up;
    }

    /// <summary>
    /// Seals may use their own gate but never the tunnel, so anything off the
    /// edge of the map is simply a wall to them.
    /// </summary>
    private bool CanEnter(Vector2Int cell)
    {
        if (level == null || !level.IsWalkableForGhost(cell.y, cell.x))
        {
            return false;
        }

        // The gap is one way: out when it is released, in only when it is going
        // home. Otherwise a seal can loiter in the doorway instead of hunting.
        if (level.At(cell.y, cell.x) == LevelGenerator.GhostGate)
        {
            return _leaving || _state == GhostState.Dead;
        }

        return true;
    }

    // ---------------------------------------------------------------- dead

    /// <summary>Straight back to its place inside the pen, walls and all.</summary>
    private void BeginDeadTrip()
    {
        _tweener.Clear();

        Vector2Int home = new Vector2Int(startColumn, startRow);
        Vector3 from = transform.position;
        Vector3 to = LevelGrid.CellToWorld(home);
        float distance = (to - from).magnitude;

        if (distance < 0.01f)
        {
            ArriveHome();
            return;
        }

        _tweener.AddTween(transform, from, to,
                          distance / Mathf.Max(0.01f, deadUnitsPerSecond));

        Vector3 delta = to - from;
        Face(Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                 ? (delta.x > 0f ? Right : Left)
                 : (delta.y > 0f ? Up : Down));
    }

    private void TickDead()
    {
        if (!_tweener.TweenExists)
        {
            ArriveHome();
        }
    }

    private void ArriveHome()
    {
        _cell = new Vector2Int(startColumn, startRow);
        transform.position = LevelGrid.CellToWorld(_cell);

        // Back where it started inside the pen, waits a moment, then walks out
        // through its own gap under the leaving rule, exactly as at the start of
        // a round. Leaving has to stay on until it is clear of the doorway: the
        // gap is one way, so a seal that steps back inside is stuck there.
        _direction = Vector2Int.zero;
        _leaving = true;
        _hold = penPauseSeconds;
        SetState(GhostState.Normal);
    }

    // ------------------------------------------------------------- visuals

    private void Face(Vector2Int dir)
    {
        if (animator == null || dir == Vector2Int.zero)
        {
            return;
        }

        string prefix = "Normal";

        if (IsEdible)
        {
            prefix = "Scared";
        }
        else if (_state == GhostState.Dead)
        {
            prefix = "Dead";
        }

        string suffix = "Right";

        if (dir == Up)
        {
            suffix = "Up";
        }
        else if (dir == Down)
        {
            suffix = "Down";
        }
        else if (dir == Left)
        {
            suffix = "Left";
        }

        animator.Play(prefix + "_" + suffix);
    }

    private void Flash()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (_state == GhostState.Recovering)
        {
            // Flashes between the scared pink and white as the warning.
            bool bright = Mathf.PingPong(Time.time * flashRate, 1f) > 0.5f;
            spriteRenderer.color = bright ? Color.white : scaredTint;
        }
        else if (_state == GhostState.Scared)
        {
            spriteRenderer.color = scaredTint;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    // --------------------------------------------------------------- state

    public void SetState(GhostState state)
    {
        bool wasDead = _state == GhostState.Dead;
        _state = state;

        Face(_direction != Vector2Int.zero ? _direction : Left);

        if (state == GhostState.Dead && !wasDead)
        {
            BeginDeadTrip();
        }
    }

    /// <summary>Back to the spawn cell in the normal state.</summary>
    public void ResetToStart()
    {
        if (_tweener == null)
        {
            _tweener = GetComponent<Tweener>();
        }

        _tweener.Clear();
        _cell = new Vector2Int(startColumn, startRow);
        transform.position = LevelGrid.CellToWorld(_cell);
        _direction = Vector2Int.zero;
        _state = GhostState.Normal;
        _leaving = true;
        _hold = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        Face(Left);
    }
}
