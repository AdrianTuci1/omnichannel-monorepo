using System.Text.Json;

namespace Cdp.Worker.Events;

/// <summary>
/// Envelope-ul standard al unui eveniment de domeniu consumat din Service Bus.
/// Contractul JSON (camelCase) este documentat în README.
/// </summary>
public sealed class DomainEvent
{
    public string EventId { get; private init; } = string.Empty;

    public string EventType { get; private init; } = string.Empty;

    public string EntityType { get; private init; } = string.Empty;

    public string EntityId { get; private init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private init; }

    public string Source { get; private init; } = "unknown";

    public string? CorrelationId { get; private init; }

    public JsonElement Payload { get; private init; }

    /// <summary>
    /// Parsează corpul unui mesaj Service Bus într-un <see cref="DomainEvent"/>.
    /// Returnează <c>false</c> dacă mesajul nu este un obiect JSON valid sau dacă
    /// îi lipsesc câmpurile obligatorii de identitate (eventId, eventType, entityType, entityId).
    /// </summary>
    public static bool TryParse(BinaryData body, out DomainEvent? evt)
    {
        evt = null;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return false;
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var eventId = GetString(root, "eventId");
            var eventType = GetString(root, "eventType");
            var entityType = GetString(root, "entityType");
            var entityId = GetString(root, "entityId");

            if (string.IsNullOrWhiteSpace(eventId)
                || string.IsNullOrWhiteSpace(eventType)
                || string.IsNullOrWhiteSpace(entityType)
                || string.IsNullOrWhiteSpace(entityId))
            {
                return false;
            }

            var occurredAt = GetDateTimeOffset(root, "occurredAt") ?? DateTimeOffset.UtcNow;
            var source = GetString(root, "source") ?? "unknown";
            var correlationId = GetString(root, "correlationId");
            var payload = root.TryGetProperty("payload", out var p) ? p.Clone() : default;

            evt = new DomainEvent
            {
                EventId = eventId,
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                OccurredAt = occurredAt,
                Source = source,
                CorrelationId = correlationId,
                Payload = payload,
            };

            return true;
        }
    }

    /// <summary>
    /// Construiește un <see cref="DomainEvent"/> dintr-un eveniment normalizat (folosit de
    /// poller-ul store-api, care nu trece prin Service Bus).
    /// </summary>
    public static DomainEvent Create(
        string eventId,
        string eventType,
        string entityType,
        string entityId,
        DateTimeOffset occurredAt,
        string source,
        string? correlationId,
        JsonElement payload) => new()
    {
        EventId = eventId,
        EventType = eventType,
        EntityType = entityType,
        EntityId = entityId,
        OccurredAt = occurredAt,
        Source = source,
        CorrelationId = correlationId,
        Payload = payload,
    };

    private static string? GetString(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    private static DateTimeOffset? GetDateTimeOffset(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var value) && value.TryGetDateTimeOffset(out var result))
        {
            return result;
        }

        return null;
    }
}
