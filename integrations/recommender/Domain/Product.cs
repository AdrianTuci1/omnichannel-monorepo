namespace Recommender.Api.Domain;

/// <summary>
/// Proiecție read-only a tabelului <c>products</c> gestionat de Store API (m1).
/// </summary>
public sealed class Product
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal PriceAmount { get; set; }

    public string PriceCurrency { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
