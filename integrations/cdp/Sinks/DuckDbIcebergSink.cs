using System.Text.Json;
using Cdp.Worker.Configuration;
using Cdp.Worker.Events;
using DuckDB.NET.Data;

namespace Cdp.Worker.Sinks;

/// <summary>
/// Destinația de date a CDP: un sink tranzacțional DuckDB (append de evenimente +
/// upsert de profiluri) cu export periodic al tabelelor în format Iceberg.
/// DuckDB este un motor single-writer, deci toate operațiile sunt serializate printr-un lock.
/// </summary>
public sealed class DuckDbIcebergSink : IEventSink, IDisposable
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly DuckDbOptions _duckDb;
    private readonly IcebergOptions _iceberg;
    private readonly object _gate = new();

    private DuckDBConnection? _connection;
    private bool _icebergLoaded;
    private bool _disposed;

    public DuckDbIcebergSink(DuckDbOptions duckDb, IcebergOptions iceberg)
    {
        _duckDb = duckDb;
        _iceberg = iceberg;
    }

    /// <inheritdoc />
    public void Append(DomainEvent evt)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            EnsureOpen();
            AppendEvent(evt);

            switch (evt.EntityType)
            {
                case EntityTypes.Customer:
                    UpsertCustomer(evt);
                    break;
                case EntityTypes.Order:
                    UpsertOrder(evt);
                    break;
                case EntityTypes.Product:
                    UpsertProduct(evt);
                    break;
            }
        }
    }

    /// <summary>
    /// Exportă starea curentă a tabelelor DuckDB în tabele Iceberg sub
    /// <see cref="IcebergOptions.CatalogPath"/>. Fiecare export rescrie snapshot-ul
    /// complet al tabelului (metadata + manifest + date parquet).
    /// </summary>
    public void FlushIceberg()
    {
        if (!_iceberg.Enabled)
        {
            return;
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            EnsureOpen();
            EnsureIcebergLoaded();

            ExportTableIfNotEmpty("events", Path.Combine(_iceberg.CatalogPath, "events"));

            if (_iceberg.ExportProfiles)
            {
                ExportTableIfNotEmpty("customers", Path.Combine(_iceberg.CatalogPath, "customers"));
                ExportTableIfNotEmpty("orders", Path.Combine(_iceberg.CatalogPath, "orders"));
                ExportTableIfNotEmpty("products", Path.Combine(_iceberg.CatalogPath, "products"));
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _connection?.Dispose();
            _connection = null;
        }
    }

    private void EnsureOpen()
    {
        if (_connection is not null)
        {
            return;
        }

        var fullPath = Path.GetFullPath(_duckDb.DatabasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connection = new DuckDBConnection($"Data Source={fullPath}");
        _connection.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        Execute("""
            CREATE TABLE IF NOT EXISTS events (
                event_id        VARCHAR PRIMARY KEY,
                event_type      VARCHAR NOT NULL,
                entity_type     VARCHAR NOT NULL,
                entity_id       VARCHAR NOT NULL,
                occurred_at     TIMESTAMP NOT NULL,
                source          VARCHAR NOT NULL,
                correlation_id  VARCHAR,
                payload         VARCHAR
            );
            """);

        Execute("""
            CREATE INDEX IF NOT EXISTS idx_events_entity
            ON events (entity_type, entity_id);
            """);

        // payload este stocat ca VARCHAR (compatibil Iceberg, care nu are tip JSON);
        // acest view expune payload-ul ca JSON pentru interogări în DuckDB.
        Execute("""
            CREATE VIEW IF NOT EXISTS events_json AS
            SELECT
                event_id,
                event_type,
                entity_type,
                entity_id,
                occurred_at,
                source,
                correlation_id,
                CAST(payload AS JSON) AS payload
            FROM events;
            """);

        Execute("""
            CREATE TABLE IF NOT EXISTS customers (
                customer_id     VARCHAR PRIMARY KEY,
                email           VARCHAR,
                first_name      VARCHAR,
                last_name       VARCHAR,
                phone           VARCHAR,
                created_at      TIMESTAMP,
                last_updated_at TIMESTAMP
            );
            """);

        Execute("""
            CREATE TABLE IF NOT EXISTS orders (
                order_id        VARCHAR PRIMARY KEY,
                order_number    VARCHAR,
                customer_id     VARCHAR,
                status          VARCHAR,
                currency        VARCHAR,
                total_amount    DECIMAL(18,2),
                created_at      TIMESTAMP,
                last_updated_at TIMESTAMP
            );
            """);

        Execute("""
            CREATE TABLE IF NOT EXISTS products (
                product_id      VARCHAR PRIMARY KEY,
                sku             VARCHAR,
                name            VARCHAR,
                price_amount    DECIMAL(18,2),
                price_currency  VARCHAR,
                category_id     VARCHAR,
                is_active       BOOLEAN,
                created_at      TIMESTAMP,
                last_updated_at TIMESTAMP
            );
            """);
    }

    private void AppendEvent(DomainEvent evt)
    {
        var payloadRaw = evt.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : evt.Payload.GetRawText();

        Execute(
            """
            INSERT INTO events
                (event_id, event_type, entity_type, entity_id, occurred_at, source, correlation_id, payload)
            VALUES
                (?, ?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT (event_id) DO NOTHING;
            """,
            evt.EventId,
            evt.EventType,
            evt.EntityType,
            evt.EntityId,
            evt.OccurredAt.UtcDateTime,
            evt.Source,
            evt.CorrelationId,
            payloadRaw);
    }

    private void UpsertCustomer(DomainEvent evt)
    {
        var payload = DeserializePayload<CustomerPayload>(evt);
        if (payload is null)
        {
            return;
        }

        var createdAt = (payload.CreatedAt ?? evt.OccurredAt).UtcDateTime;

        Execute(
            """
            INSERT INTO customers
                (customer_id, email, first_name, last_name, phone, created_at, last_updated_at)
            VALUES
                (?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT (customer_id) DO UPDATE SET
                email           = COALESCE(excluded.email, customers.email),
                first_name      = COALESCE(excluded.first_name, customers.first_name),
                last_name       = COALESCE(excluded.last_name, customers.last_name),
                phone           = COALESCE(excluded.phone, customers.phone),
                created_at      = COALESCE(customers.created_at, excluded.created_at),
                last_updated_at = excluded.last_updated_at;
            """,
            evt.EntityId,
            payload.Email,
            payload.FirstName,
            payload.LastName,
            payload.Phone,
            createdAt,
            evt.OccurredAt.UtcDateTime);
    }

    private void UpsertOrder(DomainEvent evt)
    {
        var payload = DeserializePayload<OrderPayload>(evt);
        if (payload is null)
        {
            return;
        }

        var createdAt = (payload.CreatedAt ?? evt.OccurredAt).UtcDateTime;

        Execute(
            """
            INSERT INTO orders
                (order_id, order_number, customer_id, status, currency, total_amount, created_at, last_updated_at)
            VALUES
                (?, ?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT (order_id) DO UPDATE SET
                order_number    = COALESCE(excluded.order_number, orders.order_number),
                customer_id     = COALESCE(excluded.customer_id, orders.customer_id),
                status          = COALESCE(excluded.status, orders.status),
                currency        = COALESCE(excluded.currency, orders.currency),
                total_amount    = COALESCE(excluded.total_amount, orders.total_amount),
                created_at      = COALESCE(orders.created_at, excluded.created_at),
                last_updated_at = excluded.last_updated_at;
            """,
            evt.EntityId,
            payload.OrderNumber,
            payload.CustomerId,
            payload.Status,
            payload.Currency,
            payload.TotalAmount,
            createdAt,
            evt.OccurredAt.UtcDateTime);
    }

    private void UpsertProduct(DomainEvent evt)
    {
        var payload = DeserializePayload<ProductPayload>(evt);
        if (payload is null)
        {
            return;
        }

        var createdAt = (payload.CreatedAt ?? evt.OccurredAt).UtcDateTime;

        Execute(
            """
            INSERT INTO products
                (product_id, sku, name, price_amount, price_currency, category_id, is_active, created_at, last_updated_at)
            VALUES
                (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT (product_id) DO UPDATE SET
                sku             = COALESCE(excluded.sku, products.sku),
                name            = COALESCE(excluded.name, products.name),
                price_amount    = COALESCE(excluded.price_amount, products.price_amount),
                price_currency  = COALESCE(excluded.price_currency, products.price_currency),
                category_id     = COALESCE(excluded.category_id, products.category_id),
                is_active       = COALESCE(excluded.is_active, products.is_active),
                created_at      = COALESCE(products.created_at, excluded.created_at),
                last_updated_at = excluded.last_updated_at;
            """,
            evt.EntityId,
            payload.Sku,
            payload.Name,
            payload.PriceAmount,
            payload.PriceCurrency,
            payload.CategoryId,
            payload.IsActive,
            createdAt,
            evt.OccurredAt.UtcDateTime);
    }

    private void EnsureIcebergLoaded()
    {
        if (_icebergLoaded)
        {
            return;
        }

        // LOAD este suficient când extensia e deja instalată; fallback INSTALL acoperă prima rulare.
        try
        {
            Execute("LOAD iceberg;");
        }
        catch (Exception)
        {
            Execute("INSTALL iceberg; LOAD iceberg;");
        }

        _icebergLoaded = true;
    }

    private void ExportTableIfNotEmpty(string table, string targetDirectory)
    {
        if (ScalarLong($"SELECT count(*) FROM {table}") == 0)
        {
            return;
        }

        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, recursive: true);
        }

        Directory.CreateDirectory(targetDirectory);

        Execute($"COPY {table} TO {Literal(targetDirectory)} (FORMAT ICEBERG);");
    }

    private long ScalarLong(string sql)
    {
        using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private void Execute(string sql)
    {
        using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private void Execute(string sql, params object?[] args)
    {
        using var command = _connection!.CreateCommand();
        command.CommandText = sql;

        foreach (var arg in args)
        {
            var parameter = command.CreateParameter();
            parameter.Value = arg ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        command.ExecuteNonQuery();
    }

    private static T? DeserializePayload<T>(DomainEvent evt)
        where T : class
    {
        if (evt.Payload.ValueKind is not JsonValueKind.Object)
        {
            return null;
        }

        try
        {
            return evt.Payload.Deserialize<T>(PayloadJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";
}
