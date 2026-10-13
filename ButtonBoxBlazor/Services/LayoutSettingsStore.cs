using System.Text.Json;

/// <summary>Layout settings shared by all devices.</summary>
/// <param name="Blocks">Ids of the blocks shown on the pages.</param>
public sealed record LayoutSettings(int Cols, int Rows, ButtonDisplay Display, bool ShowBlockOutline, string[] Blocks);

/// <summary>
/// Grid size, button display, block outline and block selection shared by all devices, persisted as JSON in the data directory.
/// </summary>
public sealed class LayoutSettingsStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly ILogger<LayoutSettingsStore> _logger;
    private LayoutSettings _settings;

    public LayoutSettingsStore(IConfiguration configuration, ILogger<LayoutSettingsStore> logger)
    {
        _logger = logger;
        var directory = configuration["ButtonBox:DataPath"];
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ButtonBox");
        }

        _filePath = Path.Combine(directory, "layout-settings.json");
        _settings = Normalize(Load());
    }

    /// <summary>Raised on the calling thread after the settings changed.</summary>
    public event Action? Changed;

    public LayoutSettings Settings
    {
        get
        {
            lock (_lock)
            {
                return _settings;
            }
        }
    }

    public void SetGrid(int cols, int rows)
    {
        cols = GridPreset.ClampCols(cols);
        rows = GridPreset.ClampRows(rows);
        Update(s => s.Cols == cols && s.Rows == rows ? null : s with { Cols = cols, Rows = rows });
    }

    public void SetBlockSelected(string blockId, bool selected) =>
        Update(s =>
        {
            if (!BlockCatalog.ById.ContainsKey(blockId) || s.Blocks.Contains(blockId) == selected)
            {
                return null;
            }

            return s with { Blocks = selected ? [.. s.Blocks, blockId] : [.. s.Blocks.Where(id => id != blockId)] };
        });

    public void SetDisplay(ButtonDisplay display) =>
        Update(s => !display.IsValid || s.Display == display ? null : s with { Display = display });

    public void SetBlockOutline(bool show) =>
        Update(s => s.ShowBlockOutline == show ? null : s with { ShowBlockOutline = show });

    // A null result means nothing changed.
    private void Update(Func<LayoutSettings, LayoutSettings?> change)
    {
        lock (_lock)
        {
            if (change(_settings) is not { } updated)
            {
                return;
            }

            _settings = updated;
            Save(updated);
        }

        Changed?.Invoke();
    }

    // Anything unreadable or inconsistent falls back to the defaults.
    private static LayoutSettings Normalize(LayoutSettings? stored) => new(
        stored is { Cols: > 0 } ? GridPreset.ClampCols(stored.Cols) : GridPreset.Tablet.Cols,
        stored is { Rows: > 0 } ? GridPreset.ClampRows(stored.Rows) : GridPreset.Tablet.Rows,
        stored?.Display is { IsValid: true } display ? display : ButtonDisplay.Default,
        stored?.ShowBlockOutline ?? true,
        stored?.Blocks is { } blocks ? [.. blocks.Where(BlockCatalog.ById.ContainsKey).Distinct()] : [.. BlockCatalog.All.Select(b => b.Id)]);

    private LayoutSettings? Load()
    {
        try
        {
            return File.Exists(_filePath) ? JsonSerializer.Deserialize<LayoutSettings>(File.ReadAllText(_filePath), Json) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Layout settings '{Path}' could not be read, using the defaults.", _filePath);
            return null;
        }
    }

    private void Save(LayoutSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, Json));
            File.Move(temp, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Layout settings '{Path}' could not be saved.", _filePath);
        }
    }
}
