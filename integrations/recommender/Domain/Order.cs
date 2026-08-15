namespace Recommender.Api.Domain;

/// <summary>
/// Proiecție read-only a tabelului <c>orders</c> (status-ul este stocat ca int, cf. OrderStatus).
/// </summary>
public sealed class Order
{
    public Guid Id { get; set; }

    public string OrderNumber { get; set; } = null!;

    public Guid CustomerId { get; set; }

    public int Status { get; set; }

    public string Currency { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
