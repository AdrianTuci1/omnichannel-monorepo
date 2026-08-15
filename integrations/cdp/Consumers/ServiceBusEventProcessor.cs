using Azure.Messaging.ServiceBus;
using Cdp.Worker.Configuration;
using Cdp.Worker.Events;
using Cdp.Worker.Sinks;
using Microsoft.Extensions.Logging;

namespace Cdp.Worker.Consumers;

/// <summary>
/// Consumatorul de evenimente din Azure Service Bus. Ascultă o coadă sau un
/// topic/subscripție și livrează fiecare eveniment valid către <see cref="IEventSink"/>.
/// Semantica de livrare: completează mesajul doar după ce evenimentul a fost persistat;
/// la erori tranzitorii de scriere mesajul rămâne blocat (retry), iar envelope-urile
/// invalide sunt trimise direct în dead-letter.
/// </summary>
public sealed class ServiceBusEventProcessor : IAsyncDisposable
{
    private readonly ServiceBusOptions _options;
    private readonly IEventSink _sink;
    private readonly ILogger<ServiceBusEventProcessor> _logger;

    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public ServiceBusEventProcessor(
        ServiceBusOptions options,
        IEventSink sink,
        ILogger<ServiceBusEventProcessor> logger)
    {
        _options = options;
        _sink = sink;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsValid)
        {
            throw new InvalidOperationException(
                "Configurația Service Bus este invalidă: ConnectionString și fie QueueName, fie TopicName+SubscriptionName sunt obligatorii.");
        }

        _client = new ServiceBusClient(_options.ConnectionString);

        var processorOptions = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = _options.MaxConcurrentCalls,
            PrefetchCount = _options.PrefetchCount,
            AutoCompleteMessages = false,
            MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(_options.MaxAutoLockRenewalSeconds),
        };

        _processor = _options.IsQueue
            ? _client.CreateProcessor(_options.QueueName, processorOptions)
            : _client.CreateProcessor(_options.TopicName, _options.SubscriptionName, processorOptions);

        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
            _processor = null;
        }

        if (_client is not null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        DomainEvent? evt = null;

        try
        {
            if (!DomainEvent.TryParse(args.Message.Body, out evt) || evt is null)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "InvalidEnvelope",
                    "Mesajul nu respectă contractul de envelope al CDP (eventId/eventType/entityType/entityId obligatorii).");

                return;
            }

            _sink.Append(evt);
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            // Nu completăm mesajul: lock-ul expiră și mesajul este re-livrat (retry),
            // apoi dead-letter după MaxDeliveryCount dacă eșecul persistă.
            _logger.LogError(ex, "Eroare la procesarea evenimentului {EventType} ({EventId})", evt?.EventType, evt?.EventId);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Eroare Service Bus (sursa: {ErrorSource}, entitate: {EntityPath})", args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }
}
