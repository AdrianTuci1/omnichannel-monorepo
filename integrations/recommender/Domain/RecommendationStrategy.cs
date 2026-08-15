namespace Recommender.Api.Domain;

/// <summary>
/// Strategia de recomandare solicitată de client.
/// </summary>
public enum RecommendationStrategy
{
    Hybrid = 0,
    ContentBased = 1,
    Collaborative = 2,
}
