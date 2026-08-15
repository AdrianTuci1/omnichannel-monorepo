namespace Cdp.Worker.Configuration;

public sealed class ServiceBusOptions
{
    public const string Section = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string QueueName { get; init; } = string.Empty;

    public string TopicName { get; init; } = string.Empty;

    public string SubscriptionName { get; init; } = string.Empty;

    public int MaxConcurrentCalls { get; init; } = 1;

    public int PrefetchCount { get; init; } = 0;

    public int MaxAutoLockRenewalSeconds { get; init; } = 300;

    public bool IsQueue => !string.IsNullOrWhiteSpace(QueueName);

    public bool IsSubscription =>
        !string.IsNullOrWhiteSpace(TopicName) && !string.IsNullOrWhiteSpace(SubscriptionName);

    public bool IsValid => !string.IsNullOrWhiteSpace(ConnectionString) && (IsQueue || IsSubscription);
}

public sealed class DuckDbOptions
{
    public const string Section = "DuckDb";

    public string DatabasePath { get; init; } = "data/cdp.duckdb";
}

/// <summary>
/// Configurație pentru consumul evenimentelor din store-api (GET /events?since=...).
/// Poller-ul folosește un cursor persistent (fișier) pentru consum incremental.
/// </summary>
public sealed class StoreApiOptions
{
    public const string Section = "StoreApi";

    public string BaseUrl { get; init; } = "http://localhost:5180";

    public bool Enabled { get; init; } = true;

    public int PollIntervalSeconds { get; init; } = 15;

    public string CursorFilePath { get; init; } = "data/cdp.cursor";
}

public sealed class IcebergOptions
{
    public const string Section = "Iceberg";

    public bool Enabled { get; init; } = true;

    public string CatalogPath { get; init; } = "data/iceberg";

    public int FlushIntervalSeconds { get; init; } = 30;

    public bool ExportProfiles { get; init; } = true;
}
