using ButtonBoxBlazor.Components;

BlockCatalog.Validate();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<CondorUdpService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<CondorUdpService>());
builder.Services.AddSingleton<WindowTransparencyService>();
builder.Services.AddSingleton<IKeyboardService, KeyboardService>();
builder.Services.AddSingleton<BlockOrderStore>();
builder.Services.AddSingleton<PageBreakStore>();
builder.Services.AddSingleton<LayoutSettingsStore>();
builder.Services.AddSingleton<ControlsStore>();
builder.Services.AddScoped<LayoutService>();
builder.Services.AddSingleton<NetworkSettingsStore>();
builder.Services.AddSingleton<LocalNetworkFilter>();

var app = builder.Build();

var netFilter = app.Services.GetRequiredService<LocalNetworkFilter>();
app.Use(async (ctx, next) =>
{
    var remote = ctx.Connection.RemoteIpAddress;
    if (!netFilter.IsAllowed(remote))
    {
        app.Logger.LogWarning("Rejected connection from {Ip} (not a local network address)", remote);
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var ports = (app.Urls.Count > 0 ? app.Urls : new[] { "http://0.0.0.0:54738" })
        .Select(u => new Uri(u.Replace("0.0.0.0", "localhost").Replace("[::]", "localhost").Replace("+", "localhost").Replace("*", "localhost")).Port)
        .Distinct();

    var ips = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                    && n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .Where(a => LocalNetworkFilter.IsPrivate(a.Address))
        .Select(a => a.Address.ToString())
        .ToList();

    foreach (var port in ports)
        foreach (var ip in ips)
            Console.WriteLine($"Available at: http://{ip}:{port}");
});

app.Run();
