/// <summary>
/// Accumulates Condor's HUD messages into a log. Every packet repeats the messages currently on screen, so only the part that is new compared to the previous packet is appended.
/// New messages are prefixed with the in-game time; an older entry with the same text is removed.
/// </summary>
public sealed class HudMessageHistory(int capacity = 50)
{
    private readonly object _lock = new();
    private readonly List<(string Text, string Display)> _entries = [];
    private string[] _window = [];
    private int _version;

    /// <summary>Increases with every appended message, so a full log that drops old entries still signals news.</summary>
    public int Version
    {
        get
        {
            lock (_lock)
            {
                return _version;
            }
        }
    }

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (_lock)
            {
                return [.. _entries.Select(e => e.Display)];
            }
        }
    }

    /// <param name="gameTime">In-game time in decimal hours (UDP "time"); null leaves messages without a prefix.</param>
    public void Update(string hudMessages, double? gameTime = null)
    {
        var prefix = gameTime is { } hours ? FormatTime(hours) + ": " : string.Empty;
        var current = hudMessages.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        lock (_lock)
        {
            // Largest suffix of the previous packet that starts the current one; the messages after it are new.
            var overlap = Math.Min(_window.Length, current.Length);
            while (overlap > 0 && !_window.AsSpan(_window.Length - overlap).SequenceEqual(current.AsSpan(0, overlap)))
            {
                overlap--;
            }

            foreach (var text in current.Skip(overlap))
            {
                _entries.RemoveAll(e => e.Text == text);
                _entries.Add((text, prefix + text));
            }

            _version += current.Length - overlap;
            if (_entries.Count > capacity)
            {
                _entries.RemoveRange(0, _entries.Count - capacity);
            }

            _window = current;
        }
    }

    private static string FormatTime(double hours)
    {
        var totalMinutes = (int)Math.Floor(Math.Max(0, hours) * 60 + 1e-6) % (24 * 60);
        return $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
    }
}
