using Microsoft.AspNetCore.Components;

/// <summary>
/// Base for components that display Condor telemetry: re-renders on every UDP packet and once per second.
/// </summary>
public abstract class TelemetryComponentBase : ComponentBase, IDisposable
{
    private const int RefreshIntervalMs = 1000;

    private Timer? _refreshTimer;

    [Inject]
    protected CondorUdpService Telemetry { get; set; } = null!;

    protected override void OnInitialized()
    {
        Telemetry.Updated += OnTelemetryUpdated;
        // Keeps the display current even when no packet arrives (e.g. ticking seconds, disconnect state).
        _refreshTimer = new Timer(_ => InvokeAsync(StateHasChanged), null, RefreshIntervalMs, RefreshIntervalMs);
    }

    private void OnTelemetryUpdated() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Telemetry.Updated -= OnTelemetryUpdated;
        _refreshTimer?.Dispose();
    }
}
