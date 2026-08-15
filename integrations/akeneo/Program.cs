using AkeneoBridge.Clients;
using AkeneoBridge.Configuration;
using AkeneoBridge.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AkeneoOptions>(builder.Configuration.GetSection(AkeneoOptions.Section));
builder.Services.Configure<StoreApiOptions>(builder.Configuration.GetSection(StoreApiOptions.Section));
builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection(SyncOptions.Section));

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AkeneoOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<StoreApiOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SyncOptions>>().Value);

builder.Services.AddSingleton<AkeneoProductMapper>();
builder.Services.AddHttpClient<AkeneoClient>();
builder.Services.AddHttpClient<StoreApiClient>();
builder.Services.AddScoped<SyncService>();
builder.Services.AddHostedService<SyncWorker>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

await builder.Build().RunAsync();
