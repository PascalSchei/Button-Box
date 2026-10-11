public class GridBlockOutlineTests
{
    private static GridBlock Block(string id, params (int Row, int Col)[] cells) =>
        new(id, id, cells.Select(c => new OpacityInfoTileDef(c.Row, c.Col)).ToArray());

    [Fact]
    public void Rectangle_OnlyOuterEdgesAreDrawn()
    {
        var block = Block("square", (0, 0), (0, 1), (1, 0), (1, 1));

        var outline = GridBlockOutline.Build(block.Placed(0, 0)).ToHashSet();

        Assert.Equal(4, outline.Count);
        Assert.Contains(new GridOutlineCell(0, 0, true, false, false, true), outline);
        Assert.Contains(new GridOutlineCell(0, 1, true, true, false, false), outline);
        Assert.Contains(new GridOutlineCell(1, 0, false, false, true, true), outline);
        Assert.Contains(new GridOutlineCell(1, 1, false, true, true, false), outline);
    }

    [Fact]
    public void TShape_ContourFollowsTheStemAndConcaveCorners()
    {
        var block = Block("tee", (0, 0), (0, 1), (0, 2), (1, 1), (2, 1));

        var outline = GridBlockOutline.Build(block.Placed(2, 3)).ToHashSet();

        Assert.Equal(5, outline.Count);
        Assert.Contains(new GridOutlineCell(2, 3, true, false, true, true), outline);
        Assert.Contains(new GridOutlineCell(2, 4, true, false, false, false), outline);
        Assert.Contains(new GridOutlineCell(2, 5, true, true, true, false), outline);
        Assert.Contains(new GridOutlineCell(3, 4, false, true, false, true), outline);
        Assert.Contains(new GridOutlineCell(4, 4, false, true, true, true), outline);
    }

    [Fact]
    public void TouchingBlocks_KeepTheirSharedBoundary()
    {
        var items = Block("a", (0, 0)).Placed(0, 0)
            .Concat(Block("b", (0, 0)).Placed(0, 1));

        var outline = GridBlockOutline.Build(items).ToArray();

        Assert.Equal(2, outline.Length);
        Assert.All(outline, cell => Assert.True(cell.Top && cell.Right && cell.Bottom && cell.Left));
    }

    [Fact]
    public void UngroupedTiles_DoNotGetABlockFrame() =>
        Assert.Empty(GridBlockOutline.Build([new OpacityInfoTileDef(0, 0)]));

    [Fact]
    public void PackedPages_RetainBlockIdentityAfterShiftAndPageBreak()
    {
        var pages = LayoutPacker.Pack([
            Block("a", (0, 0)), Block("b", (0, 0)),
            Block("wide", (0, 0), (0, 1), (0, 2), (0, 3))
        ], 4, 1);

        Assert.Equal(2, pages.Count);
        Assert.Contains(pages[0].Items, i => i.BlockId == "b" && i.Col == 3);
        Assert.All(pages[1].Items, i => Assert.Equal("wide", i.BlockId));
    }
}
