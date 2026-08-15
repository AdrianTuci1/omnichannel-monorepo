using Cdp.Worker.Configuration;
using Cdp.Worker.Consumers;
using Cdp.Worker.Sinks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cdp.Worker.Worker;

/// <summary>
/// Serviciul gazdă al CDP: pornește consumatorul Service Bus și rulează în paralel
/// un flush periodic către Iceberg. La oprire face un flush final înainte de a elibera
/// resursele de transport.
/// </summary>
public sealed class CdpWorkerService : BackgroundService
{
    private readonly ServiceBusEventProcessor _processor;
    private readonly DuckDbIcebergSink _sink;
    private readonly ServiceBusOptions _serviceBus;
    private readonly DuckDbOptions _duckDb;
    private readonly IcebergOptions _iceberg;
    private readonly ILogger<CdpWorkerService> _logger;

    public CdpWorkerService(
        ServiceBusEventProcessor processor,
        DuckDbIcebergSink sink,
        ServiceBusOptions serviceBus,
        DuckDbOptions duckDb,
        IcebergOptions iceberg,
        ILogger<CdpWorkerService> logger)
    {
        _processor = processor;
        _sink = sink;
        _serviceBus = serviceBus;
        _duckDb = duckDb;
        _iceberg = iceberg;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_serviceBus.IsValid)
        {
            await _processor.StartAsync(stoppingToken);
        }
        else
        {
            _logger.LogInformation("Service Bus neconfigurat — consumul se face doar prin poller-ul store-api.");
        }

        _logger.LogInformation(
            "CDP worker pornit. DuckDB: {DatabasePath}, Iceberg: {Enabled} (flush la {Interval}s -> {CatalogPath})",
            Path.GetFullPath(_duckDb.DatabasePath),
            _iceberg.Enabled,
            _iceberg.FlushIntervalSeconds,
            Path.GetFullPath(_iceberg.CatalogPath));

        using var flushTimer = new PeriodicTimer(TimeSpan.FromSeconds(_iceberg.FlushIntervalSeconds));

        try
        {
            while (await flushTimer.WaitForNextTickAsync(stoppingToken))
            {
                if (_iceberg.Enabled)
                {
                    FlushSafely();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Oprire normală.
        }
        finally
        {
            if (_iceberg.Enabled)
            {
                FlushSafely();
            }

            if (_serviceBus.IsValid)
            {
                await _processor.StopAsync(CancellationToken.None);
            }

            _logger.LogInformation("CDP worker oprit.");
        }
    }

    private void FlushSafely()
    {
        try
        {
            _sink.FlushIceberg();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eroare la exportul Iceberg.");
        }
    }
}
