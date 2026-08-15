namespace Recommender.Api.Domain;

/// <summary>
/// Un produs recomandat împreună cu scorurile pe componente (content / collaborative).
/// </summary>
public sealed record RecommendationItem(
    Guid ProductId,
    string Sku,
    string Name,
    string? Description,
    decimal PriceAmount,
    string PriceCurrency,
    Guid CategoryId,
    double Score,
    double ContentScore,
    double CollaborativeScore);
