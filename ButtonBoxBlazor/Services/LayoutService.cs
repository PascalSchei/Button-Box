/// <summary>
/// Per-circuit view of the shared layout settings. Pages are the global block order filtered by the selection and packed into the grid.
/// </summary>
public sealed class LayoutService : IDisposable
{
    private readonly BlockOrderStore _orderStore;
    private readonly LayoutSettingsStore _settingsStore;
    private LayoutSettings _settings;

    public LayoutService(BlockOrderStore orderStore, LayoutSettingsStore settingsStore)
    {
        _orderStore = orderStore;
        _settingsStore = settingsStore;
        _settings = settingsStore.Settings;
        Pages = Pack();
        _orderStore.Changed += OnSharedChanged;
        _settingsStore.Changed += OnSharedChanged;
    }

    /// <summary>Raised when the layout changed; may run on a non-render thread.</summary>
    public event Action? Changed;

    public int Cols => _settings.Cols;

    public int Rows => _settings.Rows;

    public ButtonDisplay Display => _settings.Display;

    public bool ShowBlockOutline => _settings.ShowBlockOutline;

    public IReadOnlyList<LayoutPage> Pages { get; private set; }

    public bool IsSelected(string blockId) => _settings.Blocks.Contains(blockId);

    public void SetGrid(int cols, int rows) => _settingsStore.SetGrid(cols, rows);

    public void SetBlockSelected(string blockId, bool selected) => _settingsStore.SetBlockSelected(blockId, selected);

    public void SetDisplay(ButtonDisplay display) => _settingsStore.SetDisplay(display);

    public void SetBlockOutline(bool show) => _settingsStore.SetBlockOutline(show);

    public void Dispose()
    {
        _orderStore.Changed -= OnSharedChanged;
        _settingsStore.Changed -= OnSharedChanged;
    }

    private IReadOnlyList<LayoutPage> Pack()
    {
        var settings = _settings;
        return LayoutPacker.Pack(
            _orderStore.Order.Where(settings.Blocks.Contains).Select(id => BlockCatalog.ById[id]), settings.Cols, settings.Rows);
    }

    private void OnSharedChanged()
    {
        _settings = _settingsStore.Settings;
        Pages = Pack();
        Changed?.Invoke();
    }
}
