using System.Text.Json;

/// <summary>A page break positioned before a block in the global block order.</summary>
public sealed record PageBreakDefinition(int Position, string? Name, bool IsCustomized = false)
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
}

/// <summary>Custom page breaks and page names shared by all devices.</summary>
public sealed class PageBreakStore
{
    private const string FirstPageId = "first-page";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly ILogger<PageBreakStore> _logger;
    private readonly BlockOrderStore _orderStore;
    private PageBreakDefinition[] _breaks;

    public PageBreakStore(IConfiguration configuration, ILogger<PageBreakStore> logger, BlockOrderStore orderStore)
    {
        _logger = logger;
        _orderStore = orderStore;
        var directory = configuration["ButtonBox:DataPath"];
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ButtonBox");
        }

        _filePath = Path.Combine(directory, "page-breaks.json");
        _breaks = Normalize(Load());
    }

    public event Action? Changed;

    public IReadOnlyList<PageBreakDefinition> Breaks
    {
        get
        {
            lock (_lock)
            {
                return [.. _breaks];
            }
        }
    }

    public bool IsCustomized(int position, string? id = null)
    {
        lock (_lock)
        {
            return _breaks.Any(pageBreak => Matches(pageBreak, position, id) && pageBreak.IsCustomized);
        }
    }

    public void EnsureBreak(int position, string? id = null) =>
        Update(breaks =>
        {
            var index = Array.FindIndex(breaks, pageBreak => Matches(pageBreak, position, id));
            if (index < 0)
            {
                return [.. breaks, new PageBreakDefinition(position, null, IsCustomized: true)];
            }

            if (breaks[index].IsCustomized)
            {
                return null;
            }

            var updated = (PageBreakDefinition[])breaks.Clone();
            updated[index] = updated[index] with { IsCustomized = true };
            return updated;
        });

    public void AddAfter(int position, string? id = null) =>
        Update(breaks =>
        {
            var updated = breaks.ToList();
            var index = updated.FindIndex(pageBreak => Matches(pageBreak, position, id));
            if (index < 0)
            {
                index = updated.FindIndex(pageBreak => pageBreak.Position > position);
                if (index < 0)
                {
                    index = updated.Count;
                }

                updated.Insert(index, new PageBreakDefinition(position, null, IsCustomized: true));
            }

            updated.Insert(index + 1, new PageBreakDefinition(position, null, IsCustomized: true));
            return [.. updated];
        });

    public bool CanMove(int position, int delta, string? id = null)
    {
        lock (_lock)
        {
            return CanMove(_breaks, position, delta, id);
        }
    }

    private bool CanMove(PageBreakDefinition[] breaks, int position, int delta, string? id)
    {
        var index = Array.FindIndex(breaks, pageBreak => Matches(pageBreak, position, id));
        if (index == 0 || delta is not (-1 or 1))
        {
            return false;
        }

        var adjacentIndex = index + delta;
        if (index > 0 && adjacentIndex >= 0 && adjacentIndex < breaks.Length
            && breaks[adjacentIndex].Position == position)
        {
            return adjacentIndex > 0;
        }

        var target = position + delta;
        var previous = index < 0
            ? breaks.Last(pageBreak => pageBreak.Position <= position).Position
            : breaks[index - 1].Position;
        int? next = index < 0
            ? breaks.FirstOrDefault(pageBreak => pageBreak.Position > position)?.Position
            : index + 1 < breaks.Length ? breaks[index + 1].Position : null;
        return target >= previous && target <= (next ?? _orderStore.Order.Count);
    }

    public void Move(int position, int delta, string? id = null)
    {
        Update(breaks =>
        {
            if (!CanMove(breaks, position, delta, id))
            {
                return null;
            }

            var index = Array.FindIndex(breaks, pageBreak => Matches(pageBreak, position, id));
            var adjacentIndex = index + delta;
            if (index > 0 && adjacentIndex > 0 && adjacentIndex < breaks.Length
                && breaks[adjacentIndex].Position == position)
            {
                var swapped = (PageBreakDefinition[])breaks.Clone();
                swapped[index] = breaks[adjacentIndex] with { IsCustomized = true };
                swapped[adjacentIndex] = breaks[index] with { IsCustomized = true };
                return swapped;
            }

            if (index < 0)
            {
                var inserted = breaks.ToList();
                var insertionIndex = inserted.FindIndex(pageBreak => pageBreak.Position > position);
                inserted.Insert(insertionIndex < 0 ? inserted.Count : insertionIndex,
                    new PageBreakDefinition(position + delta, null, IsCustomized: true));
                return [.. inserted];
            }

            var updated = (PageBreakDefinition[])breaks.Clone();
            updated[index] = updated[index] with { Position = position + delta, IsCustomized = true };
            return updated;
        });
    }

    public void Delete(int position, string? id = null)
    {
        Update(breaks =>
        {
            var index = Array.FindIndex(breaks, pageBreak => Matches(pageBreak, position, id));
            if (index < 0)
            {
                return null;
            }

            if (breaks[index].Id == FirstPageId)
            {
                if (breaks[index] is { Name: null, IsCustomized: false })
                {
                    return null;
                }

                var reset = (PageBreakDefinition[])breaks.Clone();
                reset[index] = new PageBreakDefinition(0, null) { Id = FirstPageId };
                return reset;
            }

            return breaks.Where(pageBreak => pageBreak.Id != breaks[index].Id).ToArray();
        });
    }

    public void Rename(int position, string? name, string? id = null)
    {
        var normalizedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        if (normalizedName?.Length > 60)
        {
            normalizedName = normalizedName[..60];
        }

        Update(breaks =>
        {
            var index = Array.FindIndex(breaks, pageBreak => Matches(pageBreak, position, id));
            if (index < 0 || breaks[index].Name == normalizedName)
            {
                return null;
            }

            var updated = (PageBreakDefinition[])breaks.Clone();
            updated[index] = updated[index] with { Name = normalizedName, IsCustomized = true };
            return updated;
        });
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
                _logger.LogError(ex, "Page breaks '{Path}' could not be reset.", _filePath);
                throw;
            }

            _breaks = Normalize(null);
        }

        Changed?.Invoke();
    }

    private void Update(Func<PageBreakDefinition[], PageBreakDefinition[]?> change)
    {
        lock (_lock)
        {
            if (change(_breaks) is not { } updated)
            {
                return;
            }

            _breaks = updated.OrderBy(pageBreak => pageBreak.Position).ToArray();
            Save(_breaks);
        }

        Changed?.Invoke();
    }

    private PageBreakDefinition[] Normalize(IEnumerable<PageBreakDefinition>? stored)
    {
        var maxPosition = _orderStore.Order.Count;
        var storedBreaks = (stored ?? []).ToArray();
        var root = storedBreaks.FirstOrDefault(pageBreak => pageBreak.Id == FirstPageId)
            ?? storedBreaks.FirstOrDefault(pageBreak => pageBreak.Position == 0);
        var rootName = NormalizeName(root?.Name);
        var breaks = storedBreaks
            .Where(pageBreak => pageBreak != root && pageBreak.Position >= 0 && pageBreak.Position <= maxPosition)
            .GroupBy(pageBreak => pageBreak.Id)
            .Select(group => group.Last() with { Name = NormalizeName(group.Last().Name), IsCustomized = true })
            .ToList();

        breaks.Insert(0, new PageBreakDefinition(0, rootName, root?.IsCustomized == true || rootName is not null) { Id = FirstPageId });
        return [.. breaks.OrderBy(pageBreak => pageBreak.Position)];
    }

    private static bool Matches(PageBreakDefinition pageBreak, int position, string? id) =>
        id is null ? pageBreak.Position == position : pageBreak.Id == id;

    private static string? NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        name = name.Trim();
        return name.Length > 60 ? name[..60] : name;
    }

    private PageBreakDefinition[]? Load()
    {
        try
        {
            return File.Exists(_filePath)
                ? JsonSerializer.Deserialize<PageBreakDefinition[]>(File.ReadAllText(_filePath), Json)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Page breaks '{Path}' could not be read, using the defaults.", _filePath);
            return null;
        }
    }

    private void Save(PageBreakDefinition[] breaks)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(breaks, Json));
            File.Move(temp, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Page breaks '{Path}' could not be saved.", _filePath);
        }
    }
}
