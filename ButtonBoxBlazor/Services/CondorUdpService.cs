using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Listens for Condor's generic UDP telemetry stream (see UDP.ini/UDP_output.md) and
/// exposes the latest values plus a connected/disconnected state derived from packet age.
/// </summary>
public sealed class CondorUdpService(ILogger<CondorUdpService> logger) : BackgroundService
{
    public const int DefaultPort = 55278;
    private static readonly string[] DebugHudMessages =
    [
        "Hans X started Race", "Claire Y started Race", "Bruno Z started Race", "Hans X arrived Turnpoint Balsbach",
        "Claire Y arrived Turnpoint Balsbach", "Bruno Z arrived Turnpoint Balsbach", "Hans X started Race",
        "Hans X arrived Turnpoint Balsbach", "Claire Y landed", "Bruno Z landed", "Hans X landed"
    ];
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(2);

    private volatile CondorTelemetry _current = CondorTelemetry.Empty;
    private readonly HashSet<string> _seenKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _seenMalformedLines = new(StringComparer.Ordinal);

    /// <summary>Raised on the receive loop's thread whenever a new packet has been parsed.</summary>
    public event Action? Updated;

    public CondorTelemetry Current => _current;

    public HudMessageHistory HudMessages { get; } = new();
    public bool IsConnected => DateTime.UtcNow - _current.LastUpdateUtc < ConnectionTimeout;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Debugger.IsAttached)
        {
            for (int i = 0; i < DebugHudMessages.Length; i++)
            {
                HudMessages.Update(DebugHudMessages[i], (12 * 60 + 35 + i) / 60.0);
            }

            // Drive the water ballast so the fill-level gauge is visible without a running Condor.
            _ = SimulateWaterAsync(stoppingToken);
        }

        UdpClient client;
        try
        {
            client = new UdpClient(new IPEndPoint(IPAddress.Any, DefaultPort));
        }
        catch (SocketException ex)
        {
            logger.LogWarning(ex, "UDP-Socket konnte nicht geöffnet werden, UDP-Empfang wird deaktiviert.");
            return;
        }

        using (client)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await client.ReceiveAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    logger.LogWarning(ex, "UDP-Empfang fehlgeschlagen, versuche weiter zu lauschen.");
                    continue;
                }

                string text = Encoding.ASCII.GetString(result.Buffer);
                _current = ParsePacket(text, _current);
                Updated?.Invoke();
            }
        }
    }

    // In Debug: fill the tank, slowly drain it (gauge blinks) down to ~70 % (150 L), hold there,
    // then empty it and repeat. Lets the fill-level gauge be inspected without a running Condor.
    private async Task SimulateWaterAsync(CancellationToken token)
    {
        const double full = 214;      // 150 L is ~70 % of a full tank
        const double showcase = 150;
        const double drainStart = 160;

        try
        {
            while (!token.IsCancellationRequested)
            {
                SetWater(full);
                await Task.Delay(TimeSpan.FromSeconds(2), token);

                for (double liters = drainStart; liters >= showcase; liters -= 1)
                {
                    SetWater(liters);
                    await Task.Delay(TimeSpan.FromSeconds(1), token); // ~1 L/s like the real drain
                }

                // Hold at ~70 % long enough to inspect; blinking stops after the hold, gauge stays visible.
                await Task.Delay(TimeSpan.FromSeconds(8), token);

                SetWater(0);
                await Task.Delay(TimeSpan.FromSeconds(3), token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void SetWater(double liters)
    {
        _current = _current with { Water = liters, LastUpdateUtc = DateTime.UtcNow };
        Updated?.Invoke();
    }

    private CondorTelemetry ParsePacket(string text, CondorTelemetry previous)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = line.IndexOf('=');
            if (separatorIndex < 0)
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0 && _seenMalformedLines.Add(trimmed))
                {
                    logger.LogWarning("UDP-Zeile ohne Separator '=' ignoriert: {Line}", trimmed);
                }

                continue;
            }

            string key = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();
            values[key] = value;

            if (Debugger.IsAttached && _seenKeys.Add(key))
            {
                logger.LogInformation("Neuer UDP-Key: {Key} = {Value}", key, value);
            }
        }

        double time = ParseDouble(values, "time", previous.Time);

        if (values.TryGetValue("hudmessages", out var hudMessages))
        {
            HudMessages.Update(hudMessages, time);
        }

        return new CondorTelemetry(
            Time: time,
            Height: ParseDouble(values, "height", previous.Height),
            WheelHeight: ParseDouble(values, "wheelheight", previous.WheelHeight),
            Flaps: ParseDouble(values, "flaps", previous.Flaps),
            MacCready: ParseDouble(values, "MC", previous.MacCready),
            Water: ParseDouble(values, "water", previous.Water),
            HudMessages: values.TryGetValue("hudmessages", out var hud) ? hud : previous.HudMessages,
            LastUpdateUtc: DateTime.UtcNow);
    }

    private static double ParseDouble(Dictionary<string, string> values, string key, double fallback) =>
        values.TryGetValue(key, out var raw) &&
        double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
}
