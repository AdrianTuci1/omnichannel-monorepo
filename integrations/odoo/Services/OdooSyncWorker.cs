using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OdooBridge.Configuration;

namespace OdooBridge.Services;

/// <summary>Background service care rulează sincronizarea cu Odoo ciclic sau o singură dată.</summary>
public sealed class OdooSyncWorker : BackgroundService
{
    private readonly SyncService _sync;
    private readonly SyncOptions _options;
    private readonly ILogger<OdooSyncWorker> _logger;
    private readonly bool _runOnce;

    public OdooSyncWorker(
        SyncService sync,
        IOptions<SyncOptions> options,
        ILogger<OdooSyncWorker> logger,
        bool runOnce)
    {
        _sync = sync;
        _options = options.Value;
        _logger = logger;
        _runOnce = runOnce;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_runOnce)
        {
            await RunSyncAsync(stoppingToken);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSyncAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare în ciclul de sincronizare Odoo.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        if (_options.ProductsEnabled)
        {
            var productReport = await _sync.SyncProductsAsync(ct);
            _logger.LogInformation(
                "Sincronizare produse finalizată: {Created} create, {Updated} actualizate, {Skipped} omise.",
                productReport.Created, productReport.Updated, productReport.Skipped);
        }

        if (_options.OrdersEnabled)
        {
            var orderReport = await _sync.SyncOrdersAsync(ct);
            _logger.LogInformation(
                "Sincronizare comenzi finalizată: {Created} create, {Skipped} omise.",
                orderReport.Created, orderReport.Skipped);
        }

        if (_options.ReverseOrdersEnabled)
        {
            var reverseReport = await _sync.SyncOrdersBackAsync(ct);
            _logger.LogInformation(
                "Sincronizare inversă comenzi finalizată: {Updated} statusuri actualizate în Odoo, {Skipped} omise.",
                reverseReport.Updated, reverseReport.Skipped);
        }
    }
}
