/// <summary>
/// Immutable snapshot of the subset of Condor's generic UDP telemetry fields we track.
/// </summary>
public sealed record CondorTelemetry(
    double Time,
    double Height,
    double WheelHeight,
    double Flaps,
    double MacCready,
    double Water,
    string HudMessages,
    DateTime LastUpdateUtc)
{
    public static readonly CondorTelemetry Empty = new(0, 0, 0, 0, 0, 0, string.Empty, DateTime.MinValue);
}
