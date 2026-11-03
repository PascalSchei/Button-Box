using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>Accepts loopback, private (RFC1918/link-local) and local-subnet IPv4 clients, or everyone when the network setting allows all.</summary>
public sealed class LocalNetworkFilter
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly NetworkSettingsStore _settings;
    private readonly object _lock = new();
    private List<(IPAddress Net, int Prefix)> _subnets = [];
    private DateTime _loadedAt = DateTime.MinValue;

    public LocalNetworkFilter(NetworkSettingsStore settings) => _settings = settings;

    public bool IsAllowed(IPAddress? ip)
    {
        if (_settings.AllowAllNetworks) return true;
        if (ip is null) return false;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;

        return IsPrivate(ip)
            || GetSubnets().Any(n => InSubnet(ip, n.Net, n.Prefix));
    }

    public static bool IsPrivate(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] is >= 16 and <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 169 && b[1] == 254);
    }

    // Interface list changes at runtime (Wi-Fi switch, hotspot), so it is cached only briefly.
    private List<(IPAddress Net, int Prefix)> GetSubnets()
    {
        lock (_lock)
        {
            if (DateTime.UtcNow - _loadedAt > CacheTtl)
            {
                _subnets = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => (a.Address, a.PrefixLength))
                    .ToList();
                _loadedAt = DateTime.UtcNow;
            }
            return _subnets;
        }
    }

    internal static bool InSubnet(IPAddress ip, IPAddress net, int prefix)
    {
        var a = ip.GetAddressBytes();
        var n = net.GetAddressBytes();
        for (var i = 0; i < 4 && prefix > 0; i++, prefix -= 8)
        {
            var mask = prefix >= 8 ? 0xFF : (0xFF << (8 - prefix)) & 0xFF;
            if ((a[i] & mask) != (n[i] & mask)) return false;
        }
        return true;
    }
}
