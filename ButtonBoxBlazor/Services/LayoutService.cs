using System.Text.Json;
using Microsoft.JSInterop;

/// <summary>
/// Per-device (circuit) layout state: grid size, block selection and display options, persisted in the browser's localStorage.
/// Pages are the global block order filtered by the selection and packed into the grid.
/// </summary>
public sealed class LayoutService : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly BlockOrderStore _orderStore;
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private Task? _loadTask;
    private HashSet<string> _selected = [.. BlockCatalog.All.Select(b => b.Id)];

    public LayoutService(BlockOrderStore orderStore, IJSRuntime js)
    {
        _orderStore = orderStore;
        _js = js;
        Cols = GridPreset.Tablet.Cols;
        Rows = GridPreset.Tablet.Rows;
        Pages = LayoutPacker.Pack([], Cols, Rows);
        Recompute();
        _orderStore.Changed += OnOrderChanged;
    }

    /// <summary>Raised when the layout changed; may run on a non-render thread.</summary>
    public event Action? Changed;

    /// <summary>False until the device settings were read from the browser (prerendering has no JS).</summary>
    public bool IsLoaded { get; private set; }

    public int Cols { get; private set; }

    public int Rows { get; private set; }

    public ButtonDisplay Display { get; private set; } = ButtonDisplay.Default;

    public bool ShowBlockOutline { get; private set; } = true;

    public IReadOnlyList<LayoutPage> Pages { get; private set; }

    public bool IsSelected(string blockId) => _selected.Contains(blockId);

    public Task EnsureLoadedAsync() => _loadTask ??= LoadAsync();

    public async Task SetGridAsync(int cols, int rows)
    {
        Cols = GridPreset.ClampCols(cols);
        Rows = GridPreset.ClampRows(rows);
        await ChangeAsync();
    }

    public async Task SetBlockSelectedAsync(string blockId, bool selected)
    {
        var copy = new HashSet<string>(_selected);
        if (selected ? !copy.Add(blockId) : !copy.Remove(blockId))
        {
            return;
        }

        _selected = copy;
        await ChangeAsync();
    }

    public async Task SetDisplayAsync(ButtonDisplay display)
    {
        if (!display.IsValid)
        {
            return;
        }

        Display = display;
        await ChangeAsync();
    }

    public async Task SetBlockOutlineAsync(bool show)
    {
        ShowBlockOutline = show;
        await ChangeAsync();
    }

    public async Task<(int Width, int Height)?> GetViewportAsync()
    {
        if (_module is null)
        {
            return null;
        }

        try
        {
            var viewport = await _module.InvokeAsync<ViewportData>("getViewport");
            return (viewport.Width, viewport.Height);
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _orderStore.Changed -= OnOrderChanged;
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already closed.
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./js/deviceStorage.js");
            Apply(await _module.InvokeAsync<string?>("load"));
        }
        catch (JSDisconnectedException)
        {
            _loadTask = null;
            return;
        }
        catch (JSException)
        {
            // Storage unavailable: keep the defaults.
        }

        IsLoaded = true;
        Recompute();
        Changed?.Invoke();
    }

    // Anything unreadable or inconsistent falls back to the defaults.
    private void Apply(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        DeviceData? data;
        try
        {
            data = JsonSerializer.Deserialize<DeviceData>(json, Json);
        }
        catch (JsonException)
        {
            return;
        }

        if (data is null)
        {
            return;
        }

        ShowBlockOutline = data.ShowBlockOutline ?? true;

        if (data.Cols > 0 && data.Rows > 0)
        {
            Cols = GridPreset.ClampCols(data.Cols);
            Rows = GridPreset.ClampRows(data.Rows);
        }

        if (data.Blocks is not null)
        {
            _selected = [.. data.Blocks.Where(BlockCatalog.ById.ContainsKey)];
        }

        if (data.Show is { } show && new ButtonDisplay(show.Icon, show.Text, show.Key) is { IsValid: true } display)
        {
            Display = display;
        }
    }

    private async Task ChangeAsync()
    {
        Recompute();
        Changed?.Invoke();
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        if (_module is null)
        {
            return;
        }

        var data = new DeviceData(Cols, Rows, [.. _selected], new DisplayData(Display.ShowIcon, Display.ShowText, Display.ShowKey), ShowBlockOutline);
        try
        {
            await _module.InvokeVoidAsync("save", JsonSerializer.Serialize(data, Json));
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already closed.
        }
    }

    private void Recompute()
    {
        var selected = _selected;
        Pages = LayoutPacker.Pack(_orderStore.Order.Where(selected.Contains).Select(id => BlockCatalog.ById[id]), Cols, Rows);
    }

    private void OnOrderChanged()
    {
        Recompute();
        Changed?.Invoke();
    }

    private sealed record DeviceData(int Cols, int Rows, string[]? Blocks, DisplayData? Show, bool? ShowBlockOutline = null);

    private sealed record DisplayData(bool Icon, bool Text, bool Key);

    private sealed record ViewportData(int Width, int Height);
}
