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
        Assert.All(BlockCatalog.All, b => Assert.True(layout.IsSelected(b.Id)));
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

    private static LayoutSettingsStore CreateStore(TempDirectory directory) =>
        new(Configure(directory.Path), NullLogger<LayoutSettingsStore>.Instance);

    private static LayoutService CreateLayout(LayoutSettingsStore settings)
    {
        var orderConfiguration = Configure(System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString()));
        return new LayoutService(new BlockOrderStore(orderConfiguration, NullLogger<BlockOrderStore>.Instance), settings);
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
