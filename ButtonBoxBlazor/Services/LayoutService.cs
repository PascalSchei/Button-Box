/// <summary>
/// Per-circuit view of the shared layout settings. Pages are the selected blocks packed into the grid and split at custom page breaks.
/// </summary>
public sealed class LayoutService : IDisposable
{
    private readonly BlockOrderStore _orderStore;
    private readonly PageBreakStore _pageBreakStore;
    private readonly LayoutSettingsStore _settingsStore;
    private LayoutSettings _settings;

    public LayoutService(BlockOrderStore orderStore, PageBreakStore pageBreakStore, LayoutSettingsStore settingsStore)
    {
        _orderStore = orderStore;
        _pageBreakStore = pageBreakStore;
        _settingsStore = settingsStore;
        _settings = settingsStore.Settings;
        Pages = Pack();
        _orderStore.Changed += OnSharedChanged;
        _pageBreakStore.Changed += OnSharedChanged;
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

    public bool CanMoveBlock(string blockId, int delta)
    {
        var index = _orderStore.Order.ToList().IndexOf(blockId);
        if (index < 0 || delta is not (-1 or 1))
        {
            return false;
        }

        var adjacentPage = AdjacentPage(index, delta);
        if (adjacentPage is not null)
        {
            var position = adjacentPage.BreakPosition ?? adjacentPage.StartOrderIndex;
            return _pageBreakStore.CanMove(position, -delta, adjacentPage.BreakId);
        }

        return delta < 0 ? index > 0 : index < _orderStore.Order.Count - 1;
    }

    public void MoveBlock(string blockId, int delta)
    {
        if (!CanMoveBlock(blockId, delta))
        {
            return;
        }

        var index = _orderStore.Order.ToList().IndexOf(blockId);
        var adjacentPage = AdjacentPage(index, delta);
        if (adjacentPage is not null)
        {
            var position = adjacentPage.BreakPosition ?? adjacentPage.StartOrderIndex;
            _pageBreakStore.Move(position, -delta, adjacentPage.BreakId);
        }
        else
        {
            _orderStore.Move(blockId, delta);
        }
    }

    private LayoutPage? AdjacentPage(int blockIndex, int delta)
    {
        var position = delta > 0 ? blockIndex + 1 : blockIndex;
        var pages = Pages.Where(page => (page.BreakPosition ?? page.StartOrderIndex) == position);
        return delta > 0 ? pages.FirstOrDefault() : pages.LastOrDefault();
    }

    public void Dispose()
    {
        _orderStore.Changed -= OnSharedChanged;
        _pageBreakStore.Changed -= OnSharedChanged;
        _settingsStore.Changed -= OnSharedChanged;
    }

    private IReadOnlyList<LayoutPage> Pack()
    {
        var settings = _settings;
        var order = _orderStore.Order;
        var orderIndices = order.Select((id, index) => (id, index)).ToDictionary(entry => entry.id, entry => entry.index);
        var customBreaks = _pageBreakStore.Breaks;
        var pages = new List<LayoutPage>();

        for (var breakIndex = 0; breakIndex < customBreaks.Count; breakIndex++)
        {
            var pageBreak = customBreaks[breakIndex];
            var end = breakIndex + 1 < customBreaks.Count ? customBreaks[breakIndex + 1].Position : order.Count;
            var blocks = order.Skip(pageBreak.Position).Take(end - pageBreak.Position)
                .Where(settings.Blocks.Contains).Select(id => BlockCatalog.ById[id]);
            var segmentPages = LayoutPacker.Pack(blocks, settings.Cols, settings.Rows);
            if (segmentPages.Count == 0 && customBreaks.Count > 1)
            {
                segmentPages = [new LayoutPage([], [])];
            }

            for (var pageIndex = 0; pageIndex < segmentPages.Count; pageIndex++)
            {
                var page = segmentPages[pageIndex];
                var startIndex = page.BlockIds.Count == 0 ? pageBreak.Position : orderIndices[page.BlockIds[0]];
                var name = pageIndex == 0 ? pageBreak.Name : null;
                pages.Add(page with
                {
                    Name = string.IsNullOrWhiteSpace(name) ? $"Page {pages.Count + 1}" : name,
                    StartOrderIndex = startIndex,
                    BreakPosition = pageIndex == 0 ? pageBreak.Position : null,
                    BreakId = pageIndex == 0 ? pageBreak.Id : null
                });
            }
        }

        return pages;
    }

    private void OnSharedChanged()
    {
        _settings = _settingsStore.Settings;
        Pages = Pack();
        Changed?.Invoke();
    }
}
