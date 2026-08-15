using Cdp.Worker.Clients;
using Cdp.Worker.Configuration;
using Cdp.Worker.Consumers;
using Cdp.Worker.Sinks;
using Cdp.Worker.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<ServiceBusOptions>()
    .Bind(builder.Configuration.GetSection(ServiceBusOptions.Section));

builder.Services
    .AddOptions<DuckDbOptions>()
    .Bind(builder.Configuration.GetSection(DuckDbOptions.Section));

builder.Services
    .AddOptions<IcebergOptions>()
    .Bind(builder.Configuration.GetSection(IcebergOptions.Section));

builder.Services
    .AddOptions<StoreApiOptions>()
    .Bind(builder.Configuration.GetSection(StoreApiOptions.Section));

// Expune opțiunile tipizate ca singletoni direcți, pe lângă IOptions<T>.
builder.Services.AddSingleton(static sp => sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value);
builder.Services.AddSingleton(static sp => sp.GetRequiredService<IOptions<DuckDbOptions>>().Value);
builder.Services.AddSingleton(static sp => sp.GetRequiredService<IOptions<IcebergOptions>>().Value);
builder.Services.AddSingleton(static sp => sp.GetRequiredService<IOptions<StoreApiOptions>>().Value);

builder.Services.AddSingleton<DuckDbIcebergSink>();
builder.Services.AddSingleton<IEventSink>(static sp => sp.GetRequiredService<DuckDbIcebergSink>());
builder.Services.AddSingleton<ServiceBusEventProcessor>();

builder.Services.AddHttpClient<StoreApiClient>();
builder.Services.AddHostedService<StoreApiEventPoller>();
builder.Services.AddHostedService<CdpWorkerService>();

var host = builder.Build();
await host.RunAsync();
