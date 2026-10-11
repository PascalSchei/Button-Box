public class LayoutPackerTests
{
    private static GridBlock Block(string id, int width, int height) =>
        new(id, id, new OpacityInfoTileDef(0, 0), new OpacityInfoTileDef(height - 1, width - 1));

    private static IEnumerable<(int Row, int Col)> Cells(LayoutPage page) => page.Items.Select(i => (i.Row, i.Col));

    [Fact]
    public void EmptySelection_NoPages() =>
        Assert.Empty(LayoutPacker.Pack([], 3, 3));

    [Fact]
    public void SingleBlock_OnePageAtOrigin()
    {
        var pages = LayoutPacker.Pack([Block("a", 3, 3)], 3, 3);

        var page = Assert.Single(pages);
        Assert.Contains((0, 0), Cells(page));
        Assert.Contains((2, 2), Cells(page));
    }

    [Fact]
    public void FullRow_StaysLeftToRightWithoutShift()
    {
        var pages = LayoutPacker.Pack([Block("a", 1, 1), Block("b", 1, 1), Block("c", 1, 1)], 3, 3);

        var page = Assert.Single(pages);
        Assert.Equal([(0, 0), (0, 1), (0, 2)], Cells(page).Distinct().OrderBy(c => c.Col));
    }

    [Fact]
    public void WidthOverflow_ShiftsRightmostBlockOfClosedRowToEdge()
    {
        var pages = LayoutPacker.Pack([Block("a", 1, 1), Block("b", 1, 1), Block("c", 3, 1)], 4, 3);

        var cells = Cells(Assert.Single(pages)).ToList();
        Assert.Contains((0, 0), cells);
        Assert.Contains((0, 3), cells);
        Assert.DoesNotContain((0, 1), cells);
        Assert.Contains((1, 0), cells);
    }

    [Fact]
    public void WidthOverflow_SingleBlockRowStaysLeft()
    {
        var pages = LayoutPacker.Pack([Block("a", 3, 1), Block("b", 3, 1)], 4, 3);

        var cells = Cells(Assert.Single(pages)).ToList();
        Assert.Contains((0, 0), cells);
        Assert.Contains((1, 0), cells);
    }

    [Fact]
    public void HeightOverflow_StartsNewPage()
    {
        var pages = LayoutPacker.Pack([Block("a", 3, 2), Block("b", 3, 2)], 3, 3);

        Assert.Equal(2, pages.Count);
        Assert.Contains((0, 0), Cells(pages[1]));
    }

    [Fact]
    public void RowHeight_IsTallestBlock()
    {
        var pages = LayoutPacker.Pack([Block("a", 1, 2), Block("b", 1, 1), Block("c", 3, 1)], 3, 4);

        var cells = Cells(Assert.Single(pages)).ToList();
        Assert.Contains((2, 0), cells);
    }

    [Fact]
    public void NewSweep_ReusesSpaceBesideTallBlocks()
    {
        GridBlock[] blocks =
        [
            Block("pad", 3, 3), Block("turn", 2, 2), Block("info", 1, 3), Block("mac", 1, 3), Block("opa", 1, 3), Block("alt", 2, 3),
            Block("vol", 1, 3), Block("flow", 3, 1), Block("chat", 2, 1), Block("blue", 4, 2),
            Block("helpers", 2, 2), Block("view", 3, 3)
        ];

        var cells = Cells(Assert.Single(LayoutPacker.Pack(blocks, 10, 7))).ToList();

        Assert.Contains((4, 1), cells);
        Assert.Contains((5, 2), cells);
        Assert.Contains((4, 3), cells);
        Assert.Contains((6, 5), cells);
    }

    [Fact]
    public void BlockLargerThanGrid_IsSkipped()
    {
        var pages = LayoutPacker.Pack([Block("big", 5, 2), Block("a", 1, 1)], 3, 3);

        var page = Assert.Single(pages);
        Assert.Equal(2, page.Items.Count);
    }
}
