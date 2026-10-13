using System.Text.Json;

/// <summary>The key a button sends and the text shown on its key cap.</summary>
public readonly record struct ResolvedKey(string Tag, string Caption);

/// <summary>
/// Holds the path to Condor's controls.ini (persisted as JSON in the data directory) and the keyboard bindings read from it.
/// Buttons whose function is not bound to a keyboard key there (unbound or bound to a controller) keep their default key.
/// </summary>
public sealed class ControlsStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly ILogger<ControlsStore> _logger;
    private string? _iniPath;
    private IReadOnlyDictionary<int, int> _keys = new Dictionary<int, int>();

    public ControlsStore(IConfiguration configuration, ILogger<ControlsStore> logger)
    {
        _logger = logger;
        var directory = configuration["ButtonBox:DataPath"];
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ButtonBox");
        }

        _filePath = Path.Combine(directory, "controls-settings.json");
        _iniPath = Load()?.ControlsIniPath is { Length: > 0 } stored ? stored : null;
        Reload();
    }

    private sealed record Stored(string? ControlsIniPath);

    /// <summary>Raised on the calling thread after the path or the bindings changed.</summary>
    public event Action? Changed;

    /// <summary>Configured path to controls.ini, or null when the defaults are used.</summary>
    public string? IniPath
    {
        get
        {
            lock (_lock)
            {
                return _iniPath;
            }
        }
    }

    /// <summary>Result of the last load: null without a configured path, otherwise a short message.</summary>
    public string? Status { get; private set; }

    /// <summary>Number of functions bound to a keyboard key in the loaded file.</summary>
    public int BoundCount => _keys.Count;

    /// <summary>Sets the path (null or blank to use the defaults) and reads the file.</summary>
    public void SetPath(string? path)
    {
        lock (_lock)
        {
            _iniPath = string.IsNullOrWhiteSpace(path) ? null : Normalize(path.Trim().Trim('"'));
            Save();
        }

        Reload();
    }

    private static string Normalize(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    /// <summary>Reads the configured controls.ini again, e.g. after the bindings were changed in Condor.</summary>
    public void Reload()
    {
        var path = IniPath;
        IReadOnlyDictionary<int, int> keys = new Dictionary<int, int>();

        if (path is null)
        {
            Status = null;
        }
        else if (!string.Equals(Path.GetExtension(path), ".ini", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        {
            Status = "File not found, default keys are used.";
        }
        else
        {
            try
            {
                keys = ControlsIni.ParseKeyboard(File.ReadAllText(path));
                Status = $"{keys.Count} keyboard bindings loaded.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "controls.ini '{Path}' could not be read, using the default keys.", path);
                Status = "File could not be read, default keys are used.";
            }
        }

        lock (_lock)
        {
            _keys = keys;
        }

        Changed?.Invoke();
    }

    /// <summary>Paths of the controls.ini files found in the usual Condor folders, most recent first.</summary>
    public IReadOnlyList<string> FindCandidates() => ControlsIni.FindControlsFiles(ControlsIni.DefaultDocumentFolders());

    /// <summary>Folder a file dialog should start in: the current file's folder, else a pilot folder, else Condor3\Pilots.</summary>
    public string? SuggestedFolder()
    {
        if (IniPath is { } current && Path.GetDirectoryName(current) is { } currentFolder && Directory.Exists(currentFolder))
        {
            return currentFolder;
        }

        if (FindCandidates().FirstOrDefault() is { } candidate)
        {
            return Path.GetDirectoryName(candidate);
        }

        return ControlsIni.DefaultDocumentFolders().Select(d => Path.Combine(d, "Condor3", "Pilots")).FirstOrDefault(Directory.Exists);
    }

    /// <summary>The key controls.ini binds to the button's function, or the button's own key when there is none.</summary>
    public ResolvedKey Resolve(ButtonDef button)
    {
        var fallback = new ResolvedKey(button.Tag, button.KeyCaption);

        IReadOnlyDictionary<int, int> keys;
        lock (_lock)
        {
            keys = _keys;
        }

        if (button.Function is not { } function || !keys.TryGetValue(function, out var value))
        {
            return fallback;
        }

        var key = Key.FromDirectInput(value);
        if (Key.TryParse(button.Tag, out var defaultKey) && defaultKey.ScanCode == key.ScanCode && defaultKey.Extended == key.Extended)
        {
            return fallback;
        }

        return new ResolvedKey(key.Name, key.Name);
    }

    private Stored? Load()
    {
        try
        {
            return File.Exists(_filePath) ? JsonSerializer.Deserialize<Stored>(File.ReadAllText(_filePath), Json) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Controls settings '{Path}' could not be read, using the defaults.", _filePath);
            return null;
        }
    }

    // Called with the lock held.
    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new Stored(_iniPath), Json));
            File.Move(temp, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Controls settings '{Path}' could not be saved.", _filePath);
        }
    }
}
