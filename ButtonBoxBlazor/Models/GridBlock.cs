/// <summary>
/// Items placed together as a block. Item positions are relative to the block origin (<paramref name="row"/>, <paramref name="col"/>), so a block moves by changing only its origin.
/// </summary>
public sealed class GridBlock(int row, int col, params GridItemDef[] items)
{
    public int Row { get; } = row;
    public int Col { get; } = col;
    public GridItemDef[] Items { get; } = items;

    /// <summary>Items with their positions translated to absolute grid coordinates.</summary>
    public IEnumerable<GridItemDef> Placed() => Items.Select(i => i with { Row = Row + i.Row, Col = Col + i.Col });
}
