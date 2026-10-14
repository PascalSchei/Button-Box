/// <summary>Named grid size selectable in the settings.</summary>
public sealed record GridPreset(string Name, int Cols, int Rows)
{
    public const int MinCols = 3;
    public const int MaxCols = 20;
    public const int MinRows = 3;
    public const int MaxRows = 14;

    public static readonly GridPreset Tablet = new("Tablet", 9, 6);

    public static readonly IReadOnlyList<GridPreset> All =
    [
        Tablet,
        new("Phone", 7, 3),
        new("Large", 13, 9)
    ];

    public static int ClampCols(int cols) => Math.Clamp(cols, MinCols, MaxCols);

    public static int ClampRows(int rows) => Math.Clamp(rows, MinRows, MaxRows);
}
