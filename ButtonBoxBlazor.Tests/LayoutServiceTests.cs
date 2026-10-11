using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

public class LayoutServiceTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("{\"cols\":10,\"rows\":7}", true)]
    [InlineData("{\"showBlockOutline\":false}", false)]
    [InlineData("{\"showBlockOutline\":true}", true)]
    public async Task BlockOutline_LoadsDevicePreferenceAndDefaultsForOldSettings(string? json, bool expected)
    {
        var storage = new DeviceStorage(json);
        await using var layout = CreateLayout(storage);

        await layout.EnsureLoadedAsync();

        Assert.Equal(expected, layout.ShowBlockOutline);
    }

    [Fact]
    public async Task BlockOutline_ToggleNotifiesAndPersistsAcrossReloads()
    {
        var storage = new DeviceStorage(null);
        await using var layout = CreateLayout(storage);
        await layout.EnsureLoadedAsync();
        var changes = 0;
        layout.Changed += () => changes++;

        foreach (var show in new[] { false, true })
        {
            await layout.SetBlockOutlineAsync(show);
            Assert.Equal(show, layout.ShowBlockOutline);
            using var saved = JsonDocument.Parse(storage.Json!);
            Assert.Equal(show, saved.RootElement.GetProperty("showBlockOutline").GetBoolean());
            await using var reloaded = CreateLayout(storage);
            await reloaded.EnsureLoadedAsync();
            Assert.Equal(show, reloaded.ShowBlockOutline);
        }

        Assert.Equal(2, changes);
    }

    private static LayoutService CreateLayout(DeviceStorage storage)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ButtonBox:DataPath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) }).Build();
        return new LayoutService(new BlockOrderStore(configuration, NullLogger<BlockOrderStore>.Instance), storage);
    }

    private sealed class DeviceStorage(string? json) : IJSRuntime, IJSObjectReference
    {
        public string? Json { get; private set; } = json;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "import") return ValueTask.FromResult((TValue)(object)this);
            if (identifier == "load") return ValueTask.FromResult((TValue)(object?)Json!);
            if (identifier == "save")
            {
                Json = (string)args![0]!;
                return ValueTask.FromResult(default(TValue)!);
            }
            throw new InvalidOperationException(identifier);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
