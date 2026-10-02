/// <summary>
/// Hardcoded, app-wide feature switches. Edit the values below and rebuild to change behavior;
/// there is no UI for this yet.
/// </summary>
public static class Settings
{
    /// <summary>Shows the key-cap label (e.g. "Num 8") on every button when true.</summary>
    public static readonly bool ShowKey = false;

    /// <summary>Enables the Condor UDP telemetry listener. Disabled for the first deliverable version.</summary>
    public static readonly bool EnableUdp = false;
}
