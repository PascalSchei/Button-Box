/// <summary>Exposed cell edges form the contour of each block, including concave shapes and holes.</summary>
public static class GridBlockOutline
{
    public static IEnumerable<GridOutlineCell> Build(IEnumerable<GridItemDef> items)
    {
        foreach (var block in items.Where(i => i.BlockId is not null).GroupBy(i => i.BlockId))
        {
            var cells = block.Select(i => (i.Row, i.Col)).ToHashSet();
            foreach (var (row, col) in cells)
            {
                var cell = new GridOutlineCell(row, col,
                    !cells.Contains((row - 1, col)),
                    !cells.Contains((row, col + 1)),
                    !cells.Contains((row + 1, col)),
                    !cells.Contains((row, col - 1)));
                if (cell.Top || cell.Right || cell.Bottom || cell.Left)
                {
                    yield return cell;
                }
            }
        }
    }
}

public sealed record GridOutlineCell(int Row, int Col, bool Top, bool Right, bool Bottom, bool Left)
{
    public string Style() =>
        $"grid-row:{Row + 1};grid-column:{Col + 1};" +
        $"border-width:{(Top ? "var(--grid-block-outline-width)" : "0")} " +
        $"{(Right ? "var(--grid-block-outline-width)" : "0")} " +
        $"{(Bottom ? "var(--grid-block-outline-width)" : "0")} " +
        $"{(Left ? "var(--grid-block-outline-width)" : "0")};" +
        // Round contour corners only, keeping straight edges continuous across cells.
        $"border-radius:{(Top && Left ? "var(--grid-block-outline-radius)" : "0")} " +
        $"{(Top && Right ? "var(--grid-block-outline-radius)" : "0")} " +
        $"{(Bottom && Right ? "var(--grid-block-outline-radius)" : "0")} " +
        $"{(Bottom && Left ? "var(--grid-block-outline-radius)" : "0")};";
}
