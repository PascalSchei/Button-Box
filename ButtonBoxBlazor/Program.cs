using ButtonBoxBlazor.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<CondorUdpService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<CondorUdpService>());
builder.Services.AddSingleton<WindowTransparencyService>();

var app = builder.Build();

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

//app.Lifetime.ApplicationStarted.Register(() =>
//{
//    var ports = (app.Urls.Count > 0 ? app.Urls : new[] { "http://0.0.0.0:54738" })
//        .Select(u => new Uri(u.Replace("0.0.0.0", "localhost").Replace("[::]", "localhost").Replace("+", "localhost").Replace("*", "localhost")).Port)
//        .Distinct();

//    var ips = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
//        .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
//                    && n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
//        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
//        .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
//        .Select(a => a.Address.ToString())
//        .ToList();

//    foreach (var port in ports)
//        foreach (var ip in ips)
//            Console.WriteLine($"Erreichbar unter: http://{ip}:{port}");
//});

app.Run();
