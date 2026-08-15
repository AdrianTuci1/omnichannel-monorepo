namespace Recommender.Api.Domain;

/// <summary>
/// Proiecție read-only a tabelului <c>order_lines</c>.
/// </summary>
public sealed class OrderLine
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int Quantity { get; set; }
}
