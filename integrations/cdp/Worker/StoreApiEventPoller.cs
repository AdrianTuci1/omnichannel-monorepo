using Cdp.Worker.Clients;
using Cdp.Worker.Configuration;
using Cdp.Worker.Events;
using Cdp.Worker.Sinks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cdp.Worker.Worker;

/// <summary>
/// Poller periodic care consumă evenimentele din outbox-ul store-api
/// (<c>GET /events?since=...</c>) și le livrează către <see cref="IEventSink"/>.
/// Consumul este incremental printr-un cursor persistent (fișier), cu semantică
/// at-least-once (scrierile în sink sunt idempotente).
/// </summary>
public sealed class StoreApiEventPoller : BackgroundService
{
    private readonly StoreApiClient _storeApi;
    private readonly IEventSink _sink;
    private readonly StoreApiOptions _options;
    private readonly ILogger<StoreApiEventPoller> _logger;

    public StoreApiEventPoller(
        StoreApiClient storeApi,
        IEventSink sink,
        StoreApiOptions options,
        ILogger<StoreApiEventPoller> logger)
    {
        _storeApi = storeApi;
        _sink = sink;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Poller store-api dezactivat (StoreApi:Enabled=false).");
            return;
        }

        var cursor = LoadCursor();
        _logger.LogInformation("Poller store-api pornit (cursor: {Cursor}, interval: {Interval}s).",
            cursor == DateTimeOffset.MinValue ? "<început>" : cursor.ToString("O"),
            _options.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var events = await _storeApi.GetEventsAsync(cursor, stoppingToken);
                var processed = 0;

                foreach (var raw in events)
                {
                    stoppingToken.ThrowIfCancellationRequested();

                    var domainEvent = StoreApiEventMapper.ToDomainEvent(raw);
                    if (domainEvent is null)
                    {
                        _logger.LogWarning("Eveniment outbox ignorat (envelope invalid).");
                        continue;
                    }

                    _sink.Append(domainEvent);
                    processed++;

                    if (domainEvent.OccurredAt > cursor)
                    {
                        cursor = domainEvent.OccurredAt;
                    }
                }

                if (processed > 0)
                {
                    SaveCursor(cursor);
                    _logger.LogInformation(
                        "Poller store-api: {Processed} evenimente procesate (cursor {Cursor}).",
                        processed, cursor.ToString("O"));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare în poller-ul store-api.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(_options.PollIntervalSeconds, 1)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private DateTimeOffset LoadCursor()
    {
        try
        {
            if (File.Exists(_options.CursorFilePath)
                && DateTimeOffset.TryParse(File.ReadAllText(_options.CursorFilePath), out var saved))
            {
                return saved;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nu s-a putut citi cursorul din {Path}; se reia de la început.", _options.CursorFilePath);
        }

        return DateTimeOffset.MinValue;
    }

    private void SaveCursor(DateTimeOffset cursor)
    {
        var fullPath = Path.GetFullPath(_options.CursorFilePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, cursor.ToString("O"));
    }
}
