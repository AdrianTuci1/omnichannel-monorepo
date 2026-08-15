using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OdooBridge.Clients;
using OdooBridge.Configuration;
using OdooBridge.Services;

var runOnce = args.Any(a => a.Equals("--run-once", StringComparison.OrdinalIgnoreCase));

var builder = Host.CreateApplicationBuilder(args);

// Variabile de mediu fără prefix (convenția Secțiune__Cheie, ex. Odoo__BaseUrl).
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<OdooOptions>(builder.Configuration.GetSection(OdooOptions.Section));
builder.Services.Configure<StoreApiOptions>(builder.Configuration.GetSection(StoreApiOptions.Section));
builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection(SyncOptions.Section));

builder.Services.AddHttpClient<OdooClient>(client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient<StoreApiClient>(client => client.Timeout = TimeSpan.FromSeconds(30));

builder.Services.AddSingleton<SyncService>();

builder.Services.AddHostedService(sp =>
{
    var sync = sp.GetRequiredService<SyncService>();
    var options = sp.GetRequiredService<IOptions<SyncOptions>>();
    var logger = sp.GetRequiredService<ILogger<OdooSyncWorker>>();
    return new OdooSyncWorker(sync, options, logger, runOnce);
});

await builder.Build().RunAsync();
