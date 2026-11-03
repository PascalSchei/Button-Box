/// <summary>
/// Tracks the fill level of a draining tank (e.g. water ballast) for a button background gauge.
/// The first non-zero value seen since the tank was last empty becomes the maximum (100%); the fill
/// shrinks from the top as the value drops. While the value is actively dropping the gauge blinks,
/// and keeps blinking until <see cref="BlinkHold"/> has elapsed without a change (or the tank is empty).
/// </summary>
public sealed class FillLevelIndicator(TimeSpan? blinkHold = null)
{
    /// <summary>How long blinking continues after the last observed change.</summary>
    public TimeSpan BlinkHold { get; } = blinkHold ?? TimeSpan.FromSeconds(2);

    private double _max;
    private double _previous;
    private bool _hasPrevious;

    /// <summary>Current fill, 0 (empty) to 1 (full relative to the remembered maximum).</summary>
    public double FillFraction { get; private set; }

    /// <summary>True while the gauge should blink between visible and transparent.</summary>
    public bool IsBlinking { get; private set; }

    /// <summary>Instant at which blinking stops unless another change extends it.</summary>
    public DateTime BlinkUntilUtc { get; private set; }

    /// <summary>Feeds a new telemetry value and updates fill and blink state.</summary>
    public void Update(double value, DateTime nowUtc)
    {
        if (value <= 0)
        {
            _max = 0;
            FillFraction = 0;
            IsBlinking = false;
        }
        else
        {
            if (value > _max)
            {
                _max = value;
            }

            FillFraction = _max > 0 ? Math.Clamp(value / _max, 0, 1) : 0;

            if (_hasPrevious && value < _previous)
            {
                IsBlinking = true;
                BlinkUntilUtc = nowUtc + BlinkHold;
            }
        }

        _previous = value;
        _hasPrevious = true;
    }

    /// <summary>Expires blinking once the hold time has passed; leaves the gauge visible.</summary>
    public void Tick(DateTime nowUtc)
    {
        if (IsBlinking && nowUtc >= BlinkUntilUtc)
        {
            IsBlinking = false;
        }
    }
}
