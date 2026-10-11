/// <summary>Named grid size selectable in the settings.</summary>
public sealed record GridPreset(string Name, int Cols, int Rows)
{
    public const int MinCols = 3;
    public const int MaxCols = 20;
    public const int MinRows = 3;
    public const int MaxRows = 14;

    public static readonly GridPreset Tablet = new("Tablet", 10, 7);

    public static readonly IReadOnlyList<GridPreset> All =
    [
        Tablet,
        new("Phone", 10, 5),
        new("Compact", 8, 4),
        new("Large", 14, 8)
    ];

    public static int ClampCols(int cols) => Math.Clamp(cols, MinCols, MaxCols);

    public static int ClampRows(int rows) => Math.Clamp(rows, MinRows, MaxRows);

    /// <summary>Grid size for a viewport given in pixels and a minimum cell size.</summary>
    public static (int Cols, int Rows) Suggest(int width, int height, int minCell) =>
        (ClampCols(width / minCell), ClampRows(height / minCell));
}
