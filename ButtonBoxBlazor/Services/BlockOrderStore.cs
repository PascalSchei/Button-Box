using System.Text.Json;

/// <summary>
/// Global block order shared by all devices, persisted as a JSON array of block ids.
/// </summary>
public sealed class BlockOrderStore
{
    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly ILogger<BlockOrderStore> _logger;
    private string[] _order;

    public BlockOrderStore(IConfiguration configuration, ILogger<BlockOrderStore> logger)
    {
        _logger = logger;
        var directory = configuration["ButtonBox:DataPath"];
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ButtonBox");
        }

        _filePath = Path.Combine(directory, "block-order.json");
        _order = Normalize(Load());
    }

    /// <summary>Raised on the calling thread after the order changed.</summary>
    public event Action? Changed;

    public IReadOnlyList<string> Order
    {
        get
        {
            lock (_lock)
            {
                return _order;
            }
        }
    }

    /// <summary>Moves a block by <paramref name="delta"/> positions (clamped to the list bounds).</summary>
    public void Move(string id, int delta)
    {
        lock (_lock)
        {
            var list = _order.ToList();
            var index = list.IndexOf(id);
            if (index < 0)
            {
                return;
            }

            var target = Math.Clamp(index + delta, 0, list.Count - 1);
            if (target == index)
            {
                return;
            }

            list.RemoveAt(index);
            list.Insert(target, id);
            _order = [.. list];
            Save(_order);
        }

        Changed?.Invoke();
    }

    // Unknown ids are dropped, catalog blocks missing from the stored order are appended.
    private static string[] Normalize(IEnumerable<string>? stored) =>
        [.. (stored ?? []).Where(BlockCatalog.ById.ContainsKey).Distinct().Concat(BlockCatalog.All.Select(b => b.Id)).Distinct()];

    private string[]? Load()
    {
        try
        {
            return File.Exists(_filePath) ? JsonSerializer.Deserialize<string[]>(File.ReadAllText(_filePath)) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Block order '{Path}' could not be read, using the default order.", _filePath);
            return null;
        }
    }

    private void Save(string[] order)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(order));
            File.Move(temp, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Block order '{Path}' could not be saved.", _filePath);
        }
    }
}
