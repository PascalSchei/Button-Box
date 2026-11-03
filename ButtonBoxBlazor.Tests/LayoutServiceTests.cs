using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

public class LayoutServiceTests
{
    [Fact]
    public void Defaults_WithoutStoredSettings()
    {
        using var directory = new TempDirectory();
        var layout = CreateLayout(CreateStore(directory));

        Assert.Equal(GridPreset.Tablet.Cols, layout.Cols);
        Assert.Equal(GridPreset.Tablet.Rows, layout.Rows);
        Assert.True(layout.ShowBlockOutline);
        Assert.Equal(ButtonDisplay.Default, layout.Display);
        Assert.All(BlockCatalog.All, b => Assert.Equal(!b.HiddenByDefault, layout.IsSelected(b.Id)));
    }

    [Fact]
    public void Grid_ChangeAppliesToAllLayoutsAndNotifies()
    {
        using var directory = new TempDirectory();
        var store = CreateStore(directory);
        var first = CreateLayout(store);
        var second = CreateLayout(store);
        var changes = 0;
        second.Changed += () => changes++;

        first.SetGrid(12, 9);

        Assert.Equal((12, 9), (second.Cols, second.Rows));
        Assert.Equal(1, changes);
    }

    [Fact]
    public void Settings_PersistAcrossRestarts()
    {
        using var directory = new TempDirectory();
        var layout = CreateLayout(CreateStore(directory));

        layout.SetGrid(12, 9);
        layout.SetBlockOutline(false);
        layout.SetDisplay(new ButtonDisplay(ShowIcon: false, ShowText: true, ShowKey: true));
        layout.SetBlockSelected("lx-pad", false);

        var reloaded = CreateLayout(CreateStore(directory));
        Assert.Equal((12, 9), (reloaded.Cols, reloaded.Rows));
        Assert.False(reloaded.ShowBlockOutline);
        Assert.Equal(new ButtonDisplay(false, true, true), reloaded.Display);
        Assert.False(reloaded.IsSelected("lx-pad"));
        Assert.True(reloaded.IsSelected("lx-turnpoint"));
    }

    [Fact]
    public void Grid_IsClamped_AndUnchangedValuesDoNotNotify()
    {
        using var directory = new TempDirectory();
        var layout = CreateLayout(CreateStore(directory));
        var changes = 0;
        layout.Changed += () => changes++;

        layout.SetGrid(1000, 1);
        Assert.Equal((GridPreset.MaxCols, GridPreset.MinRows), (layout.Cols, layout.Rows));

        layout.SetGrid(1000, 1);
        layout.SetDisplay(new ButtonDisplay(false, false, false));

        Assert.Equal(1, changes);
    }

    [Fact]
    public void CustomPageBreak_CanBeNamedMovedDeletedAndPersisted()
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        using var layout = new LayoutService(order, pageBreaks, CreateStore(directory));

        foreach (var block in BlockCatalog.All.Skip(3))
        {
            layout.SetBlockSelected(block.Id, false);
        }

        layout.SetGrid(9, 6);
        Assert.Single(layout.Pages);

        pageBreaks.EnsureBreak(1);
        Assert.Equal(2, layout.Pages.Count);
        Assert.Equal("Page 2", layout.Pages[1].Name);

        pageBreaks.Rename(1, "Navigation");
        Assert.Equal("Navigation", layout.Pages[1].Name);
        pageBreaks.Rename(0, "Overview");
        Assert.Equal("Overview", layout.Pages[0].Name);

        pageBreaks.Move(1, 1);
        Assert.Equal(2, layout.Pages.Count);

        var reloadedBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        Assert.Contains(reloadedBreaks.Breaks, pageBreak => pageBreak is { Position: 2, Name: "Navigation" });
        Assert.Contains(reloadedBreaks.Breaks, pageBreak => pageBreak is { Position: 0, Name: "Overview" });
        Assert.True(reloadedBreaks.IsCustomized(0));
        Assert.True(reloadedBreaks.IsCustomized(2));
        Assert.False(reloadedBreaks.IsCustomized(1));

        pageBreaks.Delete(2);
        Assert.Single(layout.Pages);
        Assert.DoesNotContain(pageBreaks.Breaks, pageBreak => pageBreak.Position == 2);
        Assert.False(pageBreaks.IsCustomized(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Overview")]
    public void DeleteFirstPage_ResetsCustomizationAndPreservesLayoutAcrossReloads(string? name)
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        using var layout = new LayoutService(order, pageBreaks, CreateStore(directory));
        pageBreaks.EnsureBreak(0);
        pageBreaks.Rename(0, name);
        pageBreaks.EnsureBreak(1);
        var pageCount = layout.Pages.Count;
        var items = layout.Pages.SelectMany(page => page.Items).ToArray();
        var changes = 0;
        layout.Changed += () => changes++;

        pageBreaks.Delete(0);
        pageBreaks.Delete(0);

        Assert.Equal(1, changes);
        Assert.False(pageBreaks.IsCustomized(0));
        Assert.True(pageBreaks.IsCustomized(1));
        Assert.Equal("Page 1", layout.Pages[0].Name);
        Assert.Equal(pageCount, layout.Pages.Count);
        Assert.Equal(items, layout.Pages.SelectMany(page => page.Items).ToArray());

        var reloaded = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        Assert.False(reloaded.IsCustomized(0));
        Assert.Contains(reloaded.Breaks, pageBreak => pageBreak is { Position: 0, Name: null, IsCustomized: false });
        Assert.True(reloaded.IsCustomized(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Customize_WithoutRenaming_PersistsAcrossReloads(int position)
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        var changes = 0;
        pageBreaks.Changed += () => changes++;

        Assert.False(pageBreaks.IsCustomized(position));
        pageBreaks.EnsureBreak(position);
        pageBreaks.EnsureBreak(position);

        Assert.True(pageBreaks.IsCustomized(position));
        Assert.Equal(1, changes);
        var reloaded = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        Assert.True(reloaded.IsCustomized(position));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("Overview", true)]
    public void ExistingPageBreaks_AutomaticallyRecognizeCustomization(string? rootName, bool rootIsCustomized)
    {
        using var directory = new TempDirectory();
        Directory.CreateDirectory(directory.Path);
        File.WriteAllText(
            System.IO.Path.Combine(directory.Path, "page-breaks.json"),
            System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new { position = 0, name = rootName },
                new { position = 1, name = (string?)null }
            }));
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);

        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);

        Assert.Equal(rootIsCustomized, pageBreaks.IsCustomized(0));
        Assert.True(pageBreaks.IsCustomized(1));
        Assert.False(pageBreaks.IsCustomized(2));
    }

    [Fact]
    public void AddPage_InsertsImmediatelyBelowSource_AndPreservesEmptyPagesAcrossReloads()
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        var settings = CreateStore(directory);
        using var layout = new LayoutService(order, pageBreaks, settings);
        using var secondDevice = new LayoutService(order, pageBreaks, settings);
        var originalItems = layout.Pages.SelectMany(page => page.Items).ToArray();
        var originalPageCount = layout.Pages.Count;
        var firstPage = layout.Pages[0];

        pageBreaks.AddAfter(0, firstPage.BreakId);

        Assert.Equal(originalPageCount + 1, layout.Pages.Count);
        Assert.Empty(layout.Pages[0].Items);
        Assert.Equal(0, layout.Pages[1].BreakPosition);
        Assert.NotEqual(firstPage.BreakId, layout.Pages[1].BreakId);
        Assert.True(pageBreaks.IsCustomized(0, layout.Pages[1].BreakId));
        Assert.Equal(originalItems, layout.Pages.SelectMany(page => page.Items).ToArray());
        Assert.Equal(
            layout.Pages.Select(page => (page.Name, page.StartOrderIndex, page.BreakPosition, page.BreakId)),
            secondDevice.Pages.Select(page => (page.Name, page.StartOrderIndex, page.BreakPosition, page.BreakId)));
        Assert.Equal(originalItems, secondDevice.Pages.SelectMany(page => page.Items).ToArray());

        var addedId = layout.Pages[1].BreakId;
        pageBreaks.Rename(0, "New page", addedId);
        pageBreaks.AddAfter(0, firstPage.BreakId);
        Assert.Empty(layout.Pages[0].Items);
        Assert.Empty(layout.Pages[1].Items);
        Assert.Equal("New page", layout.Pages[2].Name);

        var reloadedBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        using var reloaded = new LayoutService(order, reloadedBreaks, settings);
        Assert.Equal(layout.Pages.Count, reloaded.Pages.Count);
        Assert.Empty(reloaded.Pages[0].Items);
        Assert.Empty(reloaded.Pages[1].Items);
        Assert.Equal(addedId, reloaded.Pages[2].BreakId);
        Assert.Equal("New page", reloaded.Pages[2].Name);

        var emptyPage = reloaded.Pages[1];
        reloadedBreaks.Delete(0, emptyPage.BreakId);
        Assert.Equal(originalPageCount + 1, reloaded.Pages.Count);
        Assert.Equal(addedId, reloaded.Pages[1].BreakId);
    }

    [Fact]
    public void AddedPage_MovesByOneRow_AndSwapsAdjacentPageBreaks()
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        var settings = CreateStore(directory);
        foreach (var block in BlockCatalog.All.Skip(3))
        {
            settings.SetBlockSelected(block.Id, false);
        }

        using var layout = new LayoutService(order, pageBreaks, settings);
        pageBreaks.AddAfter(0, layout.Pages[0].BreakId);
        var addedId = layout.Pages[1].BreakId;

        Assert.False(pageBreaks.CanMove(0, -1, addedId));
        Assert.True(pageBreaks.CanMove(0, 1, addedId));
        pageBreaks.Move(0, 1, addedId);
        Assert.Equal(1, layout.Pages[1].BreakPosition);
        Assert.Equal(new[] { order.Order[0] }, layout.Pages[0].BlockIds);
        Assert.Equal(order.Order.Skip(1).Take(2), layout.Pages[1].BlockIds);

        pageBreaks.Move(1, -1, addedId);
        Assert.Empty(layout.Pages[0].Items);
        Assert.Equal(addedId, layout.Pages[1].BreakId);
        pageBreaks.AddAfter(0, addedId);
        var nextId = layout.Pages[2].BreakId;
        Assert.True(pageBreaks.CanMove(0, 1, addedId));
        Assert.True(pageBreaks.CanMove(0, -1, nextId));
        pageBreaks.Move(0, 1, addedId);
        Assert.Equal(nextId, layout.Pages[1].BreakId);
        Assert.Equal(addedId, layout.Pages[2].BreakId);
        Assert.Equal(0, layout.Pages[1].BreakPosition);
        pageBreaks.Move(0, -1, addedId);
        Assert.Equal(addedId, layout.Pages[1].BreakId);
        Assert.Equal(nextId, layout.Pages[2].BreakId);
        Assert.False(pageBreaks.CanMove(0, -1, addedId));

        pageBreaks.Move(0, 1, nextId);
        Assert.True(pageBreaks.CanMove(0, 1, addedId));
        pageBreaks.Move(0, 1, addedId);
        Assert.Equal(1, layout.Pages[1].BreakPosition);
        Assert.Equal(1, layout.Pages[2].BreakPosition);
        Assert.Empty(layout.Pages[1].Items);
    }

    [Fact]
    public void AddPage_OnAutomaticPage_PreservesBoundaryAndAddsEmptyPage()
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        var settings = CreateStore(directory);
        settings.SetGrid(3, 3);
        using var layout = new LayoutService(order, pageBreaks, settings);
        var automaticPage = layout.Pages.First(page => page.BreakId is null);
        var sourceIndex = layout.Pages.ToList().IndexOf(automaticPage);
        var originalPageCount = layout.Pages.Count;

        pageBreaks.AddAfter(automaticPage.StartOrderIndex);

        Assert.Equal(originalPageCount + 1, layout.Pages.Count);
        Assert.Empty(layout.Pages[sourceIndex].Items);
        Assert.Equal(automaticPage.StartOrderIndex, layout.Pages[sourceIndex].BreakPosition);
        Assert.Equal(automaticPage.StartOrderIndex, layout.Pages[sourceIndex + 1].BreakPosition);
        Assert.Equal(automaticPage.BlockIds, layout.Pages[sourceIndex + 1].BlockIds);
    }

    [Fact]
    public void PageBreak_CanMoveToEnd_AndPersistAsEmptyLastPage()
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        var position = order.Order.Count - 1;
        pageBreaks.EnsureBreak(position);
        var id = pageBreaks.Breaks.Single(pageBreak => pageBreak.Position == position).Id;

        pageBreaks.Move(position, 1, id);

        Assert.False(pageBreaks.CanMove(order.Order.Count, 1, id));
        var reloadedBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        using var layout = new LayoutService(order, reloadedBreaks, CreateStore(directory));
        Assert.Equal(id, layout.Pages[^1].BreakId);
        Assert.Empty(layout.Pages[^1].Items);
        Assert.Equal(order.Order.Count, layout.Pages[^1].BreakPosition);

        reloadedBreaks.Delete(order.Order.Count, id);
        Assert.NotEmpty(layout.Pages[^1].Items);
    }

    [Fact]
    public void Reset_RestoresSharedLayoutDefaultsAndRemovesStoredSettings()
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        var settings = CreateStore(directory);
        using var layout = new LayoutService(order, pageBreaks, settings);
        using var otherDevice = new LayoutService(order, pageBreaks, settings);
        layout.SetGrid(12, 9);
        layout.SetDisplay(new ButtonDisplay(false, true, false));
        layout.SetBlockOutline(false);
        layout.SetBlockSelected("lx-pad", false);
        order.Move("lx-pad", 2);
        pageBreaks.EnsureBreak(0);
        pageBreaks.Rename(0, "Overview");
        pageBreaks.AddAfter(0);
        var changes = 0;
        otherDevice.Changed += () => changes++;

        pageBreaks.Reset();
        order.Reset();
        settings.Reset();

        Assert.Equal(3, changes);
        Assert.Equal((GridPreset.Tablet.Cols, GridPreset.Tablet.Rows), (otherDevice.Cols, otherDevice.Rows));
        Assert.Equal(ButtonDisplay.Default, otherDevice.Display);
        Assert.True(otherDevice.ShowBlockOutline);
        Assert.All(BlockCatalog.All, block => Assert.Equal(!block.HiddenByDefault, otherDevice.IsSelected(block.Id)));
        Assert.Equal(BlockCatalog.All.Select(block => block.Id), order.Order);
        Assert.Single(pageBreaks.Breaks);
        Assert.False(pageBreaks.IsCustomized(0));
        Assert.All(otherDevice.Pages.Select((page, index) => (page, index)),
            entry => Assert.Equal($"Page {entry.index + 1}", entry.page.Name));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.json"));

        var reloadedOrder = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var reloadedBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, reloadedOrder);
        using var reloaded = new LayoutService(reloadedOrder, reloadedBreaks, CreateStore(directory));
        Assert.Equal(order.Order, reloadedOrder.Order);
        Assert.Equal(otherDevice.Display, reloaded.Display);
        Assert.Equal((otherDevice.Cols, otherDevice.Rows), (reloaded.Cols, reloaded.Rows));
        Assert.True(reloaded.ShowBlockOutline);
        Assert.All(BlockCatalog.All, block => Assert.Equal(!block.HiddenByDefault, reloaded.IsSelected(block.Id)));
        Assert.Equal(otherDevice.Pages.Select(page => page.Name), reloaded.Pages.Select(page => page.Name));

        pageBreaks.Reset();
        order.Reset();
        settings.Reset();
        Assert.Empty(Directory.GetFiles(directory.Path, "*.json"));
    }

    [Theory]
    [InlineData("layout-settings.json")]
    [InlineData("block-order.json")]
    [InlineData("page-breaks.json")]
    [InlineData("controls-settings.json")]
    public void Reset_WhenSettingsCannotBeDeleted_ThrowsWithoutChangingState(string fileName)
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        var settings = CreateStore(directory);
        var controls = new ControlsStore(configuration, NullLogger<ControlsStore>.Instance);
        settings.SetGrid(12, 9);
        order.Move("lx-pad", 2);
        pageBreaks.AddAfter(0);
        controls.SetPath(System.IO.Path.Combine(directory.Path, "missing.ini"));
        var path = System.IO.Path.Combine(directory.Path, fileName);
        File.Delete(path);
        Directory.CreateDirectory(path);
        Action reset = fileName switch
        {
            "layout-settings.json" => settings.Reset,
            "block-order.json" => order.Reset,
            "page-breaks.json" => pageBreaks.Reset,
            _ => controls.Reset
        };

        var exception = Record.Exception(reset);

        Assert.True(exception is IOException or UnauthorizedAccessException);
        Assert.Equal((12, 9), (settings.Settings.Cols, settings.Settings.Rows));
        Assert.NotEqual("lx-pad", order.Order[0]);
        Assert.Equal(2, pageBreaks.Breaks.Count);
        Assert.NotNull(controls.IniPath);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void MoveBlock_SwapsAdjacentPageBreakInsteadOfNextBlock_AndPersists(int delta)
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        using var layout = new LayoutService(order, pageBreaks, CreateStore(directory));
        using var otherDevice = new LayoutService(order, pageBreaks, CreateStore(directory));
        pageBreaks.EnsureBreak(1);
        pageBreaks.Rename(1, "Navigation");
        var originalOrder = order.Order.ToArray();
        var blockId = order.Order[delta > 0 ? 0 : 1];
        var changes = 0;
        otherDevice.Changed += () => changes++;

        Assert.True(layout.CanMoveBlock(blockId, delta));
        layout.MoveBlock(blockId, delta);

        Assert.Equal(originalOrder, order.Order);
        var moved = Assert.Single(pageBreaks.Breaks, pageBreak => pageBreak.Name == "Navigation");
        Assert.Equal(1 - delta, moved.Position);
        Assert.Equal(1, changes);
        Assert.Equal(layout.Pages.Select(page => page.Name), otherDevice.Pages.Select(page => page.Name));
        var reloaded = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        Assert.Contains(reloaded.Breaks, pageBreak => pageBreak.Id == moved.Id && pageBreak.Position == 1 - delta);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void MoveBlock_CrossesOnlyOneOfConsecutivePageBreaks(int delta)
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        using var layout = new LayoutService(order, pageBreaks, CreateStore(directory));
        pageBreaks.EnsureBreak(1);
        pageBreaks.AddAfter(1);
        var firstId = pageBreaks.Breaks[1].Id;
        var secondId = pageBreaks.Breaks[2].Id;
        var blockId = order.Order[delta > 0 ? 0 : 1];
        var originalOrder = order.Order.ToArray();

        layout.MoveBlock(blockId, delta);

        Assert.Equal(originalOrder, order.Order);
        Assert.Equal(delta > 0 ? 0 : 1, pageBreaks.Breaks.Single(pageBreak => pageBreak.Id == firstId).Position);
        Assert.Equal(delta < 0 ? 2 : 1, pageBreaks.Breaks.Single(pageBreak => pageBreak.Id == secondId).Position);
        layout.MoveBlock(blockId, delta);
        Assert.Equal(1 - delta, pageBreaks.Breaks.Single(pageBreak => pageBreak.Id == firstId).Position);
        Assert.Equal(1 - delta, pageBreaks.Breaks.Single(pageBreak => pageBreak.Id == secondId).Position);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void MoveBlock_AcrossAutomaticPageBreak_PersistsItWithoutReorderingBlocks(int delta)
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        var settings = CreateStore(directory);
        settings.SetGrid(3, 3);
        using var layout = new LayoutService(order, pageBreaks, settings);
        var page = layout.Pages.First(candidate => candidate.BreakId is null);
        var position = page.StartOrderIndex;
        var originalOrder = order.Order.ToArray();
        var blockId = order.Order[delta > 0 ? position - 1 : position];

        Assert.True(layout.CanMoveBlock(blockId, delta));
        layout.MoveBlock(blockId, delta);

        Assert.Equal(originalOrder, order.Order);
        Assert.Contains(pageBreaks.Breaks, pageBreak => pageBreak.Position == position - delta && pageBreak.IsCustomized);
    }

    [Fact]
    public void MoveBlock_HandlesFixedFirstPageTrailingPageAndOrdinaryBlockSwaps()
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        using var layout = new LayoutService(order, pageBreaks, CreateStore(directory));
        var first = order.Order[0];
        var second = order.Order[1];
        Assert.False(layout.CanMoveBlock(first, -1));
        layout.MoveBlock(first, -1);
        Assert.Equal(first, order.Order[0]);

        layout.MoveBlock(first, 1);
        Assert.Equal(second, order.Order[0]);
        Assert.Equal(first, order.Order[1]);
        layout.MoveBlock(first, -1);
        Assert.Equal(first, order.Order[0]);

        var last = order.Order[^1];
        Assert.False(layout.CanMoveBlock(last, 1));
        pageBreaks.EnsureBreak(order.Order.Count);
        Assert.True(layout.CanMoveBlock(last, 1));
        layout.MoveBlock(last, 1);
        Assert.Equal(last, order.Order[^1]);
        Assert.Contains(pageBreaks.Breaks, pageBreak => pageBreak.Position == order.Order.Count - 1);
        Assert.False(layout.CanMoveBlock("unknown", 1));
        Assert.False(layout.CanMoveBlock(first, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void AdjacentPageBreaks_SwapNamesAndIdentity_AndPersistAcrossReloads(int delta)
    {
        using var directory = new TempDirectory();
        var configuration = Configure(directory.Path);
        var order = new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        var settings = CreateStore(directory);
        using var layout = new LayoutService(order, pageBreaks, settings);
        using var otherDevice = new LayoutService(order, pageBreaks, settings);
        pageBreaks.EnsureBreak(1);
        pageBreaks.AddAfter(1);
        var upper = pageBreaks.Breaks[1];
        var lower = pageBreaks.Breaks[2];
        pageBreaks.Rename(1, "Upper", upper.Id);
        pageBreaks.Rename(1, "Lower", lower.Id);
        var originalOrder = order.Order.ToArray();
        var changes = 0;
        otherDevice.Changed += () => changes++;

        var movedId = delta > 0 ? upper.Id : lower.Id;
        Assert.True(pageBreaks.CanMove(1, delta, movedId));
        pageBreaks.Move(1, delta, movedId);

        Assert.Equal(1, changes);
        Assert.Equal(lower.Id, pageBreaks.Breaks[1].Id);
        Assert.Equal(upper.Id, pageBreaks.Breaks[2].Id);
        Assert.Equal(1, pageBreaks.Breaks[1].Position);
        Assert.Equal(1, pageBreaks.Breaks[2].Position);
        Assert.Equal("Lower", layout.Pages[1].Name);
        Assert.Equal("Upper", layout.Pages[2].Name);
        Assert.Empty(layout.Pages[1].Items);
        Assert.Equal(originalOrder, order.Order);
        Assert.Equal(layout.Pages.Select(page => page.Name), otherDevice.Pages.Select(page => page.Name));

        var reloadedBreaks = new PageBreakStore(configuration, NullLogger<PageBreakStore>.Instance, order);
        using var reloaded = new LayoutService(order, reloadedBreaks, settings);
        Assert.Equal(pageBreaks.Breaks.Select(pageBreak => pageBreak.Id), reloadedBreaks.Breaks.Select(pageBreak => pageBreak.Id));
        Assert.Equal(layout.Pages.Select(page => page.Name), reloaded.Pages.Select(page => page.Name));
    }

    private static LayoutSettingsStore CreateStore(TempDirectory directory) =>
        new(Configure(directory.Path), NullLogger<LayoutSettingsStore>.Instance);

    private static LayoutService CreateLayout(LayoutSettingsStore settings)
    {
        var orderConfiguration = Configure(System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString()));
        var order = new BlockOrderStore(orderConfiguration, NullLogger<BlockOrderStore>.Instance);
        var pageBreaks = new PageBreakStore(orderConfiguration, NullLogger<PageBreakStore>.Instance, order);
        return new LayoutService(order, pageBreaks, settings);
    }

    private static IConfiguration Configure(string dataPath) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ButtonBox:DataPath"] = dataPath }).Build();

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
