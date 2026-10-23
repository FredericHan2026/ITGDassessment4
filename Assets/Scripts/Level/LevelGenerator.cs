using UnityEngine;

/// <summary>
/// The level data and the rules that turn it into tiles.
///
/// Only the top-left quadrant is stored. It is mirrored right, then down
/// (dropping the shared middle row), which is what gives the maze its matching
/// corners. Wall pieces are not stored with a rotation: each one works out its
/// own from the neighbours it connects to, so editing the quadrant is enough to
/// change the level.
///
/// Level01 already has its tiles placed in the scene, so Generate() is left off
/// there and this behaves as the level's source of truth: PacStudent and the
/// ghosts ask it which cells they are allowed to enter. Turn
/// generateOnStart on, or call Generate() yourself, to build the maze from the
/// prefabs instead.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    public const int Empty = 0;
    public const int OutsideCorner = 1;
    public const int OutsideWall = 2;
    public const int InsideCorner = 3;
    public const int InsideWall = 4;
    public const int Pellet = 5;
    public const int PowerPellet = 6;
    public const int TJunction = 7;
    public const int GhostGate = 8;

    /// <summary>Top-left quadrant of the maze, as [row, column].</summary>
    private static readonly int[,] Quadrant = new int[15, 14]
    {
        { 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7 },
        { 2, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 4 },
        { 2, 5, 3, 4, 4, 3, 5, 3, 3, 5, 3, 3, 5, 4 },
        { 2, 6, 4, 0, 0, 4, 5, 4, 4, 5, 4, 4, 5, 4 },
        { 2, 5, 3, 4, 4, 3, 5, 3, 3, 5, 3, 3, 5, 3 },
        { 2, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 },
        { 2, 5, 3, 4, 4, 3, 5, 3, 3, 5, 3, 4, 4, 4 },
        { 2, 5, 3, 4, 4, 3, 5, 4, 4, 5, 3, 4, 4, 3 },
        { 2, 5, 5, 5, 5, 5, 5, 4, 4, 5, 5, 5, 5, 4 },
        { 1, 2, 2, 2, 2, 1, 5, 4, 3, 4, 4, 3, 0, 4 },
        { 0, 0, 0, 0, 0, 2, 5, 4, 3, 4, 4, 3, 0, 3 },
        { 0, 0, 0, 0, 0, 2, 5, 4, 4, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 2, 5, 4, 4, 0, 3, 4, 4, 8 },
        { 2, 2, 2, 2, 2, 1, 5, 3, 3, 0, 4, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 4, 0, 0, 0 }
    };

    [Header("Generation")]
    [Tooltip("Off in Level01: the tiles are already placed in the scene.")]
    [SerializeField] private bool generateOnStart = false;

    [Tooltip("One prefab per tile code, indexed 0 to 8.")]
    [SerializeField] private GameObject[] tilePrefabs;

    [Tooltip("Parent for generated tiles. Defaults to this transform.")]
    [SerializeField] private Transform tileParent;

    private int[,] _map;

    public int Rows
    {
        get { return Map.GetLength(0); }
    }

    public int Columns
    {
        get { return Map.GetLength(1); }
    }

    /// <summary>The full mirrored map, built on first use.</summary>
    public int[,] Map
    {
        get
        {
            if (_map == null)
            {
                _map = BuildFullMap();
            }

            return _map;
        }
    }

    private void Awake()
    {
        _map = BuildFullMap();
    }

    private void Start()
    {
        if (generateOnStart)
        {
            Generate();
        }
    }

    // ---------------------------------------------------------------- data

    /// <summary>Mirrors the quadrant right, then down without doubling the middle row.</summary>
    private static int[,] BuildFullMap()
    {
        int qRows = Quadrant.GetLength(0);
        int qCols = Quadrant.GetLength(1);

        int rows = qRows * 2 - 1;
        int cols = qCols * 2;
        int[,] map = new int[rows, cols];

        for (int r = 0; r < qRows; r++)
        {
            for (int c = 0; c < qCols; c++)
            {
                int value = Quadrant[r, c];
                map[r, c] = value;
                map[r, cols - 1 - c] = value;
            }
        }

        for (int r = qRows; r < rows; r++)
        {
            int source = rows - 1 - r;

            for (int c = 0; c < cols; c++)
            {
                map[r, c] = map[source, c];
            }
        }

        return map;
    }

    public bool InBounds(int row, int column)
    {
        return row >= 0 && row < Rows && column >= 0 && column < Columns;
    }

    public int At(int row, int column)
    {
        return InBounds(row, column) ? Map[row, column] : Empty;
    }

    /// <summary>True for cells PacStudent may enter: floor and the two pickups.</summary>
    public bool IsWalkable(int row, int column)
    {
        if (!InBounds(row, column))
        {
            return false;
        }

        int value = Map[row, column];
        return value == Empty || value == Pellet || value == PowerPellet;
    }

    /// <summary>
    /// Wraps a cell that has run off the left or right edge round to the other
    /// side, which is how the tunnel row works. False for anything else, so
    /// walking off the top or bottom is simply blocked.
    /// </summary>
    public bool TryWrap(int row, int column, out int wrappedColumn)
    {
        wrappedColumn = column;

        if (row < 0 || row >= Rows)
        {
            return false;
        }

        if (column < 0)
        {
            wrappedColumn = Columns - 1;
        }
        else if (column >= Columns)
        {
            wrappedColumn = 0;
        }
        else
        {
            return false;
        }

        // Only a row that is open at both edges is a tunnel.
        return IsWalkable(row, 0) && IsWalkable(row, Columns - 1);
    }

    /// <summary>As IsWalkable, but ghosts may also pass through their own gate.</summary>
    public bool IsWalkableForGhost(int row, int column)
    {
        if (!InBounds(row, column))
        {
            return false;
        }

        return IsWalkable(row, column) || Map[row, column] == GhostGate;
    }

    // ----------------------------------------------------------- rotation

    private static readonly int[] DirRow = { -1, 0, 1, 0 };     // up, right, down, left
    private static readonly int[] DirCol = { 0, 1, 0, -1 };

    private const int Up = 0;
    private const int Right = 1;
    private const int Down = 2;
    private const int Left = 3;

    // A corner sprite at 0 degrees joins RIGHT and DOWN; rotations go anticlockwise.
    private static readonly int[] CornerA = { Right, Up, Left, Down };
    private static readonly int[] CornerB = { Down, Right, Up, Left };
    private static readonly int[] CornerRotation = { 0, 90, 180, 270 };

    private static bool IsOutsideFamily(int value)
    {
        return value == OutsideCorner || value == OutsideWall || value == TJunction;
    }

    private static bool IsInsideFamily(int value)
    {
        return value == InsideCorner || value == InsideWall || value == GhostGate;
    }

    private static bool IsWall(int value)
    {
        return IsOutsideFamily(value) || IsInsideFamily(value);
    }

    private bool Connects(int row, int column, int direction, bool outside)
    {
        int neighbour = At(row + DirRow[direction], column + DirCol[direction]);
        return outside ? IsOutsideFamily(neighbour) : IsInsideFamily(neighbour);
    }

    /// <summary>Rotation in degrees for the wall piece at (row, column).</summary>
    public int PieceRotation(int row, int column)
    {
        int value = At(row, column);

        if (!IsWall(value))
        {
            return 0;
        }

        bool outside = IsOutsideFamily(value);

        if (value == TJunction)
        {
            // The stub points at the inside-wall piece hanging off the boundary.
            int[] order = { Down, Right, Up, Left };
            int[] rotations = { 0, 90, 180, 270 };

            for (int i = 0; i < order.Length; i++)
            {
                int d = order[i];

                if (IsInsideFamily(At(row + DirRow[d], column + DirCol[d])))
                {
                    return rotations[i];
                }
            }

            return 0;
        }

        if (value == OutsideCorner || value == InsideCorner)
        {
            int found = -1;
            int matches = 0;

            for (int i = 0; i < CornerA.Length; i++)
            {
                if (Connects(row, column, CornerA[i], outside)
                    && Connects(row, column, CornerB[i], outside))
                {
                    matches++;

                    if (found < 0)
                    {
                        found = i;
                    }
                }
            }

            if (matches == 1)
            {
                return CornerRotation[found];
            }

            if (matches > 1)
            {
                // Ambiguous junction: curve the arc around the empty diagonal.
                for (int i = 0; i < CornerA.Length; i++)
                {
                    if (!Connects(row, column, CornerA[i], outside)
                        || !Connects(row, column, CornerB[i], outside))
                    {
                        continue;
                    }

                    int dr = DirRow[CornerA[i]] + DirRow[CornerB[i]];
                    int dc = DirCol[CornerA[i]] + DirCol[CornerB[i]];

                    if (!IsWall(At(row + dr, column + dc)))
                    {
                        return CornerRotation[i];
                    }
                }

                return CornerRotation[found];
            }

            // No clean pair: fall back to whichever single side connects.
            for (int i = 0; i < CornerA.Length; i++)
            {
                if (Connects(row, column, CornerA[i], outside)
                    || Connects(row, column, CornerB[i], outside))
                {
                    return CornerRotation[i];
                }
            }

            return 0;
        }

        // Straight pieces: outside wall, inside wall and the ghost gate.
        bool vertical = Connects(row, column, Up, outside)
                        && Connects(row, column, Down, outside);
        bool horizontal = Connects(row, column, Left, outside)
                          && Connects(row, column, Right, outside);

        if (vertical && !horizontal)
        {
            return 90;
        }

        if (horizontal)
        {
            return 0;
        }

        if (Connects(row, column, Up, outside) || Connects(row, column, Down, outside))
        {
            return 90;
        }

        return 0;
    }

    // --------------------------------------------------------- generation

    /// <summary>Clears any previous output and lays the whole maze out again.</summary>
    public void Generate()
    {
        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogWarning("LevelGenerator has no tile prefabs assigned.");
            return;
        }

        Transform parent = tileParent != null ? tileParent : transform;
        Clear(parent);

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                int value = Map[row, column];

                if (value < 0 || value >= tilePrefabs.Length)
                {
                    continue;
                }

                GameObject prefab = tilePrefabs[value];

                if (prefab == null)
                {
                    continue;
                }

                GameObject tile = Instantiate(prefab);
                tile.name = prefab.name + "_R" + row + "_C" + column;
                tile.transform.SetParent(parent, false);
                tile.transform.position = LevelGrid.CellToWorld(row, column);
                tile.transform.rotation = Quaternion.Euler(
                    0f, 0f, PieceRotation(row, column));
            }
        }
    }

    private static void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
