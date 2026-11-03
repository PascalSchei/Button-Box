using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

public class LocalNetworkFilterTests : IDisposable
{
    private readonly TempDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    private NetworkSettingsStore CreateStore() =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ButtonBox:DataPath"] = _directory.Path }).Build(),
            NullLogger<NetworkSettingsStore>.Instance);

    private LocalNetworkFilter Create() => new(CreateStore());

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("172.20.10.2")]
    [InlineData("192.168.43.5")]
    [InlineData("169.254.1.1")]
    [InlineData("::1")]
    [InlineData("::ffff:192.168.1.10")]
    public void Allowed(string ip) =>
        Assert.True(Create().IsAllowed(IPAddress.Parse(ip)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("172.32.0.1")]
    [InlineData("172.15.0.1")]
    [InlineData("100.64.0.1")]
    [InlineData("2001:db8::1")]
    public void Rejected(string ip) =>
        Assert.False(Create().IsAllowed(IPAddress.Parse(ip)));

    [Fact]
    public void NullIp_Rejected() =>
        Assert.False(Create().IsAllowed(null));

    [Fact]
    public void AllowAllNetworks_AcceptsPublicAddresses()
    {
        var store = CreateStore();
        var filter = new LocalNetworkFilter(store);

        store.SetAllowAllNetworks(true);
        Assert.True(filter.IsAllowed(IPAddress.Parse("8.8.8.8")));

        store.SetAllowAllNetworks(false);
        Assert.False(filter.IsAllowed(IPAddress.Parse("8.8.8.8")));
    }

    [Fact]
    public void AllowAllNetworks_PersistsAndResets()
    {
        var store = CreateStore();
        Assert.False(store.AllowAllNetworks);

        store.SetAllowAllNetworks(true);
        Assert.True(CreateStore().AllowAllNetworks);

        store.Reset();
        Assert.False(store.AllowAllNetworks);
        Assert.False(CreateStore().AllowAllNetworks);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
