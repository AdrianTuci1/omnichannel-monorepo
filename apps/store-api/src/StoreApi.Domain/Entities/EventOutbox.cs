namespace StoreApi.Domain.Entities;

public sealed class EventOutbox
{
    private EventOutbox()
    {
    }

    public EventOutbox(string type, string payload)
    {
        Id = Guid.NewGuid();

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type is required.", nameof(type));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.", nameof(payload));

        Type = type;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
}
