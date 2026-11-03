using System.Text.Json;

/// <summary>
/// Whether connections from outside the local network are accepted, persisted as JSON in the data directory.
/// Defaults to local networks only.
/// </summary>
public sealed class NetworkSettingsStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly ILogger<NetworkSettingsStore> _logger;
    private volatile bool _allowAllNetworks;

    public NetworkSettingsStore(IConfiguration configuration, ILogger<NetworkSettingsStore> logger)
    {
        _logger = logger;
        var directory = configuration["ButtonBox:DataPath"];
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ButtonBox");
        }

        _filePath = Path.Combine(directory, "network-settings.json");
        _allowAllNetworks = Load()?.AllowAllNetworks ?? false;
    }

    private sealed record Stored(bool AllowAllNetworks);

    /// <summary>Raised on the calling thread after the setting changed.</summary>
    public event Action? Changed;

    public bool AllowAllNetworks => _allowAllNetworks;

    public void SetAllowAllNetworks(bool allow)
    {
        lock (_lock)
        {
            if (_allowAllNetworks == allow)
            {
                return;
            }

            _allowAllNetworks = allow;
            Save();
        }

        Changed?.Invoke();
    }

    public void Reset()
    {
        lock (_lock)
        {
            try
            {
                File.Delete(_filePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Network settings '{Path}' could not be reset.", _filePath);
                throw;
            }

            _allowAllNetworks = false;
        }

        Changed?.Invoke();
    }

    private Stored? Load()
    {
        try
        {
            return File.Exists(_filePath) ? JsonSerializer.Deserialize<Stored>(File.ReadAllText(_filePath), Json) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Network settings '{Path}' could not be read, using the defaults.", _filePath);
            return null;
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new Stored(_allowAllNetworks), Json));
            File.Move(temp, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Network settings '{Path}' could not be saved.", _filePath);
        }
    }
}
