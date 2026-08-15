using AkeneoBridge.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AkeneoBridge.Services;

/// <summary>
/// Worker care rulează ciclul de sincronizare pe un interval configurabil
/// (sau o singură dată când <see cref="SyncOptions.RunOnce"/> este activat).
/// </summary>
public sealed class SyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SyncOptions _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SyncWorker> _logger;

    public SyncWorker(
        IServiceScopeFactory scopeFactory,
        SyncOptions options,
        IHostApplicationLifetime lifetime,
        ILogger<SyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<SyncService>();
                var result = await sync.RunAsync(stoppingToken);

                _logger.LogInformation(
                    "Sincronizare Akeneo completă: {ProductsFetched} produse preluate, {ProductsCreated} create, {ProductsUpdated} actualizate, {ProductsSkipped} omise, {AttributesFetched} atribute, {CategoriesFetched} categorii preluate, {CategoriesCreated} categorii create.",
                    result.ProductsFetched,
                    result.ProductsCreated,
                    result.ProductsUpdated,
                    result.ProductsSkipped,
                    result.AttributesFetched,
                    result.CategoriesFetched,
                    result.CategoriesCreated);

                if (_options.ReverseProductsEnabled)
                {
                    var reverse = await sync.RunReverseAsync(stoppingToken);
                    _logger.LogInformation(
                        "Sincronizare inversă Akeneo completă: {ProductsFetched} produse preluate din store, {ProductsUpserted} exportate în Akeneo, {ProductsSkipped} omise.",
                        reverse.ProductsFetched,
                        reverse.ProductsUpserted,
                        reverse.ProductsSkipped);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare în timpul sincronizării Akeneo.");
            }

            if (_options.RunOnce)
            {
                _lifetime.StopApplication();
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(_options.IntervalSeconds, 1)), stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested);
    }
}
