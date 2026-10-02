/// <summary>
/// Repeatedly invokes a callback while a button is held, mimicking physical key-repeat behavior.
/// </summary>
public sealed class ButtonHoldRepeater(int initialDelayMs = 400, int intervalMs = 120) : IDisposable
{
    private readonly Dictionary<string, Timer> _activeHolds = [];

    public void Start(string tag, Action<string> send)
    {
        send(tag);

        if (_activeHolds.ContainsKey(tag))
        {
            return;
        }

        _activeHolds[tag] = new Timer(_ => send(tag), null, initialDelayMs, intervalMs);
    }

    public void Stop(string tag)
    {
        if (_activeHolds.Remove(tag, out var timer))
        {
            timer.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var timer in _activeHolds.Values)
        {
            timer.Dispose();
        }

        _activeHolds.Clear();
    }
}
