/// <summary>
/// Items placed together as a block. Item positions are relative to the block origin, so a block moves by changing only the origin passed to <see cref="Placed"/>.
/// </summary>
/// <param name="id">Stable kebab-case identifier used in persisted settings.</param>
/// <param name="name">Display name in the settings page.</param>
/// <param name="defaultColor">CSS color for all items without their own color.</param>
public sealed class GridBlock(string id, string name, string defaultColor, params GridItemDef[] items)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string DefaultColor { get; } = defaultColor;
    public GridItemDef[] Items { get; } = items;

    public int Width { get; } = items.Max(i => i.Col) + 1;
    public int Height { get; } = items.Max(i => i.Row) + 1;

    /// <summary>Items with their positions translated to absolute grid coordinates.</summary>
    public IEnumerable<GridItemDef> Placed(int row, int col) => Items.Select(i => i with { Row = row + i.Row, Col = col + i.Col, Color = i.Color ?? DefaultColor, BlockId = Id });
}
