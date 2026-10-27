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

    [Tooltip("Told when this seal gets home, so it can be put into whatever "
             + "state the others are in and the music can be set back.")]
    [SerializeField] private GameManager round;

    [Header("Speed")]
    [SerializeField] private float normalTilesPerSecond = 4.5f;
    [SerializeField] private float scaredTilesPerSecond = 2.25f;

    [Tooltip("World units per second on the way home, in a straight line.")]
    [SerializeField] private float deadUnitsPerSecond = 2.25f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Colour a frightened seal is tinted. The artwork underneath is "
             + "flat grey, and every seal's is normalised to the same "
             + "brightness, so all four come out the same tone of pink.")]
    [SerializeField] private Color scaredTint = new Color(1f, 0.58f, 0.80f, 1f);

    [Tooltip("The lighter end of the pulse while frightened.")]
    [SerializeField] private Color scaredPulseTint = new Color(1f, 0.82f, 0.92f, 1f);

    [Tooltip("Seal 4 only: how hard it is pushed out towards the outside wall "
             + "as it circles. Higher hugs the boundary more tightly.")]
    [SerializeField] private float outwardPull = 2f;

    [Tooltip("Pulses per second while frightened. Slow and soft.")]
    [SerializeField] private float pulseRate = 2f;

    [Tooltip("Flashes per second while recovering. Fast and hard.")]
    [SerializeField] private float flashRate = 6f;

    private Tweener _tweener;
    private GhostState _state = GhostState.Normal;
    private Vector2Int _cell;
    private Vector2Int _direction = Vector2Int.zero;
    private bool _leaving;
    private readonly Vector2Int[] _candidates = new Vector2Int[4];
    private bool _hugging;

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
    /// Looks only at the four cells next door, so no pathfinding is involved.
    /// Turning back on itself is a last resort: only a dead end, or a tunnel
    /// mouth being treated as a wall, can force it.
    /// </summary>
    private Vector2Int ChooseDirection()
    {
        int n = Gather();
        Vector2Int back = new Vector2Int(-_direction.x, -_direction.y);

        if (n == 0)
        {
            return CanEnter(_cell + back) ? back : Vector2Int.zero;
        }

        if (_leaving)
        {
            return TowardsExit(n);
        }

        // Frightened and recovering seals all hunt the way seal 1 does.
        GhostBehaviour mode = IsEdible ? GhostBehaviour.Furthest : behaviour;

        if (mode == GhostBehaviour.Furthest)
        {
            return PickByDistance(n, true);
        }

        if (mode == GhostBehaviour.Closest)
        {
            return PickByDistance(n, false);
        }

        if (mode == GhostBehaviour.Clockwise)
        {
            return FollowOutsideWall(n);
        }

        return _candidates[Random.Range(0, n)];
    }

    /// <summary>Collects the valid moves that are not a reversal.</summary>
    private int Gather()
    {
        Vector2Int back = new Vector2Int(-_direction.x, -_direction.y);
        int n = 0;

        for (int i = 0; i < All.Length; i++)
        {
            Vector2Int dir = All[i];

            if (_direction != Vector2Int.zero && dir == back)
            {
                continue;
            }

            if (CanEnter(_cell + dir))
            {
                _candidates[n++] = dir;
            }
        }

        return n;
    }

    /// <summary>
    /// Seals 1 and 2. Keeps every move that leaves PacStudent at least as far
    /// away (or at least as close) as he is now, then picks among them at
    /// random, so the same corner does not produce the same answer every time.
    /// </summary>
    private Vector2Int PickByDistance(int n, bool further)
    {
        float here = DistanceToTarget(_cell);
        int keep = 0;

        for (int i = 0; i < n; i++)
        {
            float there = DistanceToTarget(_cell + _candidates[i]);

            if (further ? there >= here : there <= here)
            {
                _candidates[keep++] = _candidates[i];
            }
        }

        // Nothing qualified, so take any valid move: a seal may never stop.
        // Nothing was overwritten in that case, so the list is still intact.
        if (keep == 0)
        {
            keep = n;
        }

        return _candidates[Random.Range(0, keep)];
    }

    /// <summary>
    /// Seal 4. Two stages: strike out for the boundary, then hug it.
    ///
    /// Wall following on its own traces whichever wall it first touches, and
    /// since it starts in the middle of the map that would be an inside block.
    /// So it heads outward until it reaches the outer corridor, and only then
    /// keeps the wall on its left, which carries it clockwise round the edge.
    /// </summary>
    private Vector2Int FollowOutsideWall(int n)
    {
        if (!_hugging && OnOuterRing(_cell))
        {
            _hugging = true;
        }

        return _hugging ? HugWall(n) : HeadOutward(n);
    }

    /// <summary>The corridor that runs just inside the outer wall.</summary>
    private bool OnOuterRing(Vector2Int cell)
    {
        return cell.y <= 1 || cell.y >= level.Rows - 2
               || cell.x <= 1 || cell.x >= level.Columns - 2;
    }

    /// <summary>Whichever step gets furthest from the middle, circling clockwise.</summary>
    private Vector2Int HeadOutward(int n)
    {
        float centreX = (level.Columns - 1) * 0.5f;
        float centreY = -(level.Rows - 1) * 0.5f;
        float offsetX = _cell.x - centreX;
        float offsetY = -_cell.y - centreY;

        float tangentX = offsetY;
        float tangentY = -offsetX;
        float length = Mathf.Sqrt(tangentX * tangentX + tangentY * tangentY);

        if (length > 0.0001f)
        {
            tangentX /= length;
            tangentY /= length;
        }

        float radius = Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY);
        Vector2Int best = _candidates[0];
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < n; i++)
        {
            float stepX = _candidates[i].x;
            float stepY = -_candidates[i].y;
            float nextX = offsetX + stepX;
            float nextY = offsetY + stepY;
            float outward = Mathf.Sqrt(nextX * nextX + nextY * nextY) - radius;

            float score = outwardPull * outward + stepX * tangentX + stepY * tangentY;

            if (score > bestScore)
            {
                bestScore = score;
                best = _candidates[i];
            }
        }

        return best;
    }

    /// <summary>Wall on the left: try the left turn, then straight, then right.</summary>
    private Vector2Int HugWall(int n)
    {
        Vector2Int[] order = { LeftOf(_direction), _direction, RightOf(_direction) };

        for (int k = 0; k < order.Length; k++)
        {
            if (order[k] == Vector2Int.zero)
            {
                continue;
            }

            for (int i = 0; i < n; i++)
            {
                if (_candidates[i] == order[k])
                {
                    return _candidates[i];
                }
            }
        }

        return _candidates[0];
    }

    /// <summary>On the way out of the pen, the nearest step to the gap wins.</summary>
    private Vector2Int TowardsExit(int n)
    {
        Vector2Int best = _candidates[0];
        float bestDistance = Manhattan(_cell + best, ExitCell);

        for (int i = 1; i < n; i++)
        {
            float d = Manhattan(_cell + _candidates[i], ExitCell);

            if (d < bestDistance)
            {
                bestDistance = d;
                best = _candidates[i];
            }
        }

        return best;
    }

    /// <summary>Straight-line distance from a cell to PacStudent.</summary>
    private float DistanceToTarget(Vector2Int cell)
    {
        if (target == null)
        {
            return 0f;
        }

        Vector3 here = LevelGrid.CellToWorld(cell);
        float dx = here.x - target.position.x;
        float dy = here.y - target.position.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Grid distance, used only for the short walk out of the pen.</summary>
    private static float Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    /// <summary>A quarter turn to the left, as seen on screen.</summary>
    private static Vector2Int LeftOf(Vector2Int dir)
    {
        if (dir == Up)
        {
            return Left;
        }

        if (dir == Left)
        {
            return Down;
        }

        if (dir == Down)
        {
            return Right;
        }

        if (dir == Right)
        {
            return Up;
        }

        return Vector2Int.zero;
    }

    /// <summary>A quarter turn to the right, as seen on screen.</summary>
    private static Vector2Int RightOf(Vector2Int dir)
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

        if (dir == Left)
        {
            return Up;
        }

        return Vector2Int.zero;
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

        // The round decides which state to come back in, so a seal eaten while
        // the others are still frightened rejoins them rather than turning up
        // as a hunter. It also puts the music back if nothing else is dead.
        if (round != null)
        {
            round.OnGhostReturnedHome(this);
            return;
        }

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
            // Hard flash between pink and white: the power pellet is nearly out.
            bool bright = Mathf.PingPong(Time.time * flashRate, 1f) > 0.5f;
            spriteRenderer.color = bright ? Color.white : scaredTint;
        }
        else if (_state == GhostState.Scared)
        {
            // Soft pulse between the two pinks, so it reads as alive rather
            // than as a flat recolour, without being mistaken for the warning.
            float t = Mathf.PingPong(Time.time * pulseRate, 1f);
            spriteRenderer.color = Color.Lerp(scaredTint, scaredPulseTint, t);
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
        _hugging = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        Face(Left);
    }
}
