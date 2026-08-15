using System.Text.Json;

namespace Cdp.Worker.Events;

/// <summary>
/// Normalizează evenimentele din outbox-ul store-api (EventOutbox) în envelope-ul
/// intern <see cref="DomainEvent"/>. Parcurge defensiv câmpurile pentru a tolera
/// variații de denumire (ex. <c>type</c>/<c>eventType</c>, <c>createdAt</c>/<c>occurredAt</c>).
/// </summary>
public static class StoreApiEventMapper
{
    // Ordinea contează: prefixele mai lungi se potrivesc primele (OrderLine înainte de Order).
    private static readonly (string Prefix, string EntityType)[] EntityPrefixes =
    {
        ("OrderLine", "order_line"),
        ("Product", "product"),
        ("Category", "category"),
        ("Customer", "customer"),
        ("Order", "order"),
        ("Inventory", "inventory"),
    };

    public static DomainEvent? ToDomainEvent(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var id = GetString(root, "id");
        var type = GetString(root, "type") ?? GetString(root, "eventType");

        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        var occurredAt = GetDateTimeOffset(root, "createdAt")
            ?? GetDateTimeOffset(root, "occurredAt")
            ?? DateTimeOffset.UtcNow;

        var (entityType, action) = SplitType(type);
        var eventId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;

        var payload = root.TryGetProperty("payload", out var p) ? p.Clone() : default;
        var entityId = ExtractEntityId(payload) ?? eventId;

        return DomainEvent.Create(
            eventId,
            $"{entityType}.{LowerFirst(action)}",
            entityType,
            entityId,
            occurredAt,
            "store-api",
            null,
            payload);
    }

    private static (string EntityType, string Action) SplitType(string type)
    {
        foreach (var (prefix, entityType) in EntityPrefixes)
        {
            if (type.StartsWith(prefix, StringComparison.Ordinal) && type.Length > prefix.Length)
            {
                return (entityType, type[prefix.Length..]);
            }
        }

        return ("unknown", type);
    }

    private static string? ExtractEntityId(JsonElement payload)
    {
        if (payload.ValueKind == JsonValueKind.Object)
        {
            var id = GetString(payload, "id");
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }

        return null;
    }

    private static string LowerFirst(string value)
        => value.Length == 0 ? value : char.ToLowerInvariant(value[0]) + value[1..];

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
