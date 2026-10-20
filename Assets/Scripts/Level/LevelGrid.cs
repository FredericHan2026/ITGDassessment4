using UnityEngine;

/// <summary>
/// Shared conventions for the level grid.
///
/// The map is addressed as [row, column] with row 0 at the top, and the sprites
/// are authored at 32 px with a Pixels Per Unit of 32, so one cell is exactly one
/// world unit and every tile instance can stay at a scale of (1, 1, 1).
/// </summary>
public static class LevelGrid
{
    public const float TileSize = 1f;

    /// <summary>World position of the centre of cell (row, column).</summary>
    public static Vector3 CellToWorld(int row, int column)
    {
        return new Vector3(column * TileSize, -row * TileSize, 0f);
    }

    /// <summary>World position for a cell given as (x = column, y = row).</summary>
    public static Vector3 CellToWorld(Vector2Int cell)
    {
        return CellToWorld(cell.y, cell.x);
    }
}
