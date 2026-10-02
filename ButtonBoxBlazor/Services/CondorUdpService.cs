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
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(2);

    private volatile CondorTelemetry _current = CondorTelemetry.Empty;

    /// <summary>Raised on the receive loop's thread whenever a new packet has been parsed.</summary>
    public event Action? Updated;

    public CondorTelemetry Current => _current;

    public bool IsConnected => DateTime.UtcNow - _current.LastUpdateUtc < ConnectionTimeout;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Settings.EnableUdp)
        {
            logger.LogInformation("UDP-Empfang ist per Settings.EnableUdp deaktiviert.");
            return;
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

    private static CondorTelemetry ParsePacket(string text, CondorTelemetry previous)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = line.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            values[line[..separatorIndex].Trim()] = line[(separatorIndex + 1)..].Trim();
        }

        return new CondorTelemetry(
            Time: ParseDouble(values, "time", previous.Time),
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
