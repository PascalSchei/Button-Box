/// <summary>One page of the packed layout with absolutely positioned items.</summary>
public sealed record LayoutPage(IReadOnlyList<GridItemDef> Items);

/// <summary>
/// Skyline packing in sweeps: blocks go left to right at the first column where they fit, dropping onto the column heights below.
/// A sweep ends on width overflow and the next one starts at the left edge again, so space below short blocks next to tall ones is reused.
/// Holes above the skyline are never back-filled.
/// </summary>
public static class LayoutPacker
{
    public static IReadOnlyList<LayoutPage> Pack(IEnumerable<GridBlock> blocks, int cols, int rows)
    {
        var pages = new List<LayoutPage>();
        var page = new PageState(cols, rows);

        foreach (var block in blocks)
        {
            if (block.Width > cols || block.Height > rows)
            {
                continue;
            }

            if (page.TryPlace(block, page.Cursor))
            {
                continue;
            }

            // Gap between the blocks instead of at the right edge.
            if (page.Cursor + block.Width > cols)
            {
                page.ShiftLastToRightEdge();
            }

            page.StartSweep();
            if (!page.TryPlace(block, 0))
            {
                pages.Add(page.ToPage());
                page = new PageState(cols, rows);
                page.TryPlace(block, 0);
            }
        }

        if (page.HasBlocks)
        {
            pages.Add(page.ToPage());
        }

        return pages;
    }

    private sealed class PageState(int cols, int rows)
    {
        private readonly int[] _heights = new int[cols];
        private readonly List<Placement> _placements = [];
        private int _sweepStart;

        public int Cursor { get; private set; }

        public bool HasBlocks => _placements.Count > 0;

        public bool TryPlace(GridBlock block, int fromCol)
        {
            for (var col = fromCol; col + block.Width <= cols; col++)
            {
                var row = MaxHeight(col, block.Width);
                if (row + block.Height > rows)
                {
                    continue;
                }

                _placements.Add(new Placement(block, row, col, _heights[col..(col + block.Width)]));
                Array.Fill(_heights, row + block.Height, col, block.Width);
                Cursor = col + block.Width;
                return true;
            }

            return false;
        }

        public void StartSweep()
        {
            _sweepStart = _placements.Count;
            Cursor = 0;
        }

        // Only rows of at least two blocks are shifted, and only where the new columns are not already built up higher.
        public void ShiftLastToRightEdge()
        {
            if (_placements.Count - _sweepStart < 2)
            {
                return;
            }

            var last = _placements[^1];
            var width = last.Block.Width;
            var newCol = cols - width;
            if (newCol <= last.Col)
            {
                return;
            }

            last.Below.CopyTo(_heights, last.Col);
            var bottom = last.Row + last.Block.Height;
            if (MaxHeight(newCol, width) > last.Row)
            {
                Array.Fill(_heights, bottom, last.Col, width);
                return;
            }

            _placements[^1] = new Placement(last.Block, last.Row, newCol, _heights[newCol..(newCol + width)]);
            Array.Fill(_heights, bottom, newCol, width);
        }

        public LayoutPage ToPage() =>
            new([.. _placements.SelectMany(p => p.Block.Placed(p.Row, p.Col))]);

        private int MaxHeight(int col, int width)
        {
            var max = 0;
            for (var i = col; i < col + width; i++)
            {
                max = Math.Max(max, _heights[i]);
            }

            return max;
        }
    }

    /// <param name="Below">Column heights covered by the block before it was placed.</param>
    private sealed record Placement(GridBlock Block, int Row, int Col, int[] Below);
}
