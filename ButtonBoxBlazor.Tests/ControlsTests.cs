using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

public class ControlsTests
{
    private const string Ini = """
        [{6F1D2B61-D5A0-11CF-BFC7-444553540000}]
        3000=0
        3016=19
        3046=199
        3048=157
        3059=61
        [{B7BA8280-5A06-11EC-8001-444553540000}]
        3012=53
        3016=7
        """;

    [Fact]
    public void ParseKeyboard_ReadsOnlyKeyboardSectionAndSkipsUnassigned()
    {
        var keys = ControlsIni.ParseKeyboard(Ini.ReplaceLineEndings("\r\n"));

        Assert.Equal(4, keys.Count);
        Assert.Equal(19, keys[3016]);
        Assert.False(keys.ContainsKey(3000));
        Assert.False(keys.ContainsKey(3012));
    }

    [Theory]
    [InlineData(19, 0x13, false, "R")]
    [InlineData(199, 0x47, true, "Home")]
    [InlineData(157, 0x1D, true, "RightCtrl")]
    [InlineData(100, 0x64, false, "Scan100")]
    public void FromDirectInput_DecodesExtendedFlag(int value, int scanCode, bool extended, string name)
    {
        var key = Key.FromDirectInput(value);

        Assert.Equal(((ushort)scanCode, extended, name), (key.ScanCode, key.Extended, key.Name));
        Assert.True(Key.TryParse(key.Name, out var parsed));
        Assert.Equal((key.ScanCode, key.Extended), (parsed.ScanCode, parsed.Extended));
    }

    [Fact]
    public void Resolve_UsesIniKey_AndFallsBackToDefault()
    {
        using var directory = new TempDirectory();
        var store = CreateStore(directory);
        var release = new ButtonDef(0, 0, "R", "Release", "R", Function: 3016);
        var camera = new ButtonDef(0, 1, "F3", "Chase camera", "F3", Function: 3059);
        var combo = new ButtonDef(0, 2, "Control+F1", "Cockpit visibility", "Ctrl + F1");
        var unbound = new ButtonDef(0, 3, "G", "Gear", "G", Function: 3099);

        Assert.Equal(new ResolvedKey("R", "R"), store.Resolve(release));

        var ini = Path.Combine(directory.Path, "controls.ini");
        File.WriteAllText(ini, Ini);
        store.SetPath(ini);

        Assert.Equal(new ResolvedKey("R", "R"), store.Resolve(release));
        Assert.Equal(new ResolvedKey("F3", "F3"), store.Resolve(camera));
        Assert.Equal(new ResolvedKey("Control+F1", "Ctrl + F1"), store.Resolve(combo));
        Assert.Equal(new ResolvedKey("G", "G"), store.Resolve(unbound));

        File.WriteAllText(ini, Ini.Replace("3016=19", "3016=20"));
        store.Reload();

        Assert.Equal(new ResolvedKey("T", "T"), store.Resolve(release));
    }

    [Fact]
    public void Path_PersistsAndMissingFileUsesDefaults()
    {
        using var directory = new TempDirectory();
        var store = CreateStore(directory);
        var missing = Path.Combine(directory.Path, "missing.ini");

        store.SetPath(missing);

        Assert.Equal(missing, CreateStore(directory).IniPath);
        Assert.NotNull(store.Status);
        Assert.Equal(0, store.BoundCount);

        store.SetPath(" ");

        Assert.Null(CreateStore(directory).IniPath);
    }

    [Fact]
    public void FindControlsFiles_FindsPilotFolders()
    {
        using var directory = new TempDirectory();
        var pilot = Path.Combine(directory.Path, "Condor3", "Pilots", "Pascal");
        Directory.CreateDirectory(pilot);
        File.WriteAllText(Path.Combine(pilot, "controls.ini"), Ini);
        Directory.CreateDirectory(Path.Combine(directory.Path, "Condor3", "Pilots", "NoControls"));

        var found = ControlsIni.FindControlsFiles([directory.Path, directory.Path]);

        Assert.Equal([Path.Combine(pilot, "controls.ini")], found);
    }

    private static ControlsStore CreateStore(TempDirectory directory) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ButtonBox:DataPath"] = directory.Path }).Build(),
            NullLogger<ControlsStore>.Instance);

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
