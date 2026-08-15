using Recommender.Api.Domain;

namespace Recommender.Api;

/// <summary>
/// Un produs recomandat, în forma consumată direct de Store API la
/// <c>GET /products/{id}/related</c>.
/// </summary>
public sealed record RelatedProductResponse(Guid ProductId, string Name, double Score);

public sealed record CustomerRecommendationResponse(
    Guid CustomerId,
    string Strategy,
    double ContentWeight,
    int Limit,
    IReadOnlyList<RecommendationItem> Items);

public sealed record TextRecommendationResponse(
    string Query,
    string Strategy,
    int Limit,
    IReadOnlyList<RecommendationItem> Items);
