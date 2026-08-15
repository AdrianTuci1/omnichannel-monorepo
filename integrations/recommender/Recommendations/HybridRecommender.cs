using Recommender.Api.Configuration;
using Recommender.Api.Domain;

namespace Recommender.Api.Recommendations;

/// <summary>
/// Recomandări hibride: combină scorurile content-based (pgvector) cu cele collaborative.
/// </summary>
public interface IHybridRecommender
{
    Task<IReadOnlyList<RecommendationItem>> RecommendAsync(
        Guid productId, int limit, double contentWeight, CancellationToken ct = default);

    Task<IReadOnlyList<RecommendationItem>> RecommendForCustomerAsync(
        Guid customerId, int limit, double contentWeight, CancellationToken ct = default);
}

public sealed class HybridRecommender : IHybridRecommender
{
    private readonly IContentBasedRecommender _content;
    private readonly ICollaborativeRecommender _collaborative;
    private readonly RecommenderOptions _options;

    public HybridRecommender(
        IContentBasedRecommender content,
        ICollaborativeRecommender collaborative,
        RecommenderOptions options)
    {
        _content = content;
        _collaborative = collaborative;
        _options = options;
    }

    public async Task<IReadOnlyList<RecommendationItem>> RecommendAsync(
        Guid productId, int limit, double contentWeight, CancellationToken ct = default)
    {
        var pool = limit * Math.Max(1, _options.CandidateMultiplier);

        var content = await _content.RecommendAsync(productId, pool, ct);
        var collaborative = await _collaborative.RecommendAsync(productId, pool, ct);

        return Blend(content, collaborative, limit, contentWeight);
    }

    public async Task<IReadOnlyList<RecommendationItem>> RecommendForCustomerAsync(
        Guid customerId, int limit, double contentWeight, CancellationToken ct = default)
    {
        var pool = limit * Math.Max(1, _options.CandidateMultiplier);

        var collaborative = await _collaborative.RecommendForCustomerAsync(customerId, pool, ct);

        var seedProductId = await _collaborative.GetMostRecentProductIdAsync(customerId, ct);
        var content = seedProductId is { } id
            ? await _content.RecommendAsync(id, pool, ct)
            : Array.Empty<RecommendationItem>();

        return Blend(content, collaborative, limit, contentWeight);
    }

    private static IReadOnlyList<RecommendationItem> Blend(
        IReadOnlyList<RecommendationItem> content,
        IReadOnlyList<RecommendationItem> collaborative,
        int limit,
        double contentWeight)
    {
        var weight = Math.Clamp(contentWeight, 0.0, 1.0);

        var contentScores = ScoreNormalizer.MinMax(content.Select(c => (c.ProductId, c.Score)));
        var collaborativeScores = ScoreNormalizer.MinMax(collaborative.Select(c => (c.ProductId, c.Score)));

        var merged = new Dictionary<Guid, (RecommendationItem Item, double Content, double Collaborative)>();

        foreach (var item in content)
            merged[item.ProductId] = (item, contentScores.GetValueOrDefault(item.ProductId), 0.0);

        foreach (var item in collaborative)
        {
            if (merged.TryGetValue(item.ProductId, out var existing))
            {
                merged[item.ProductId] = (existing.Item, existing.Content, collaborativeScores.GetValueOrDefault(item.ProductId));
            }
            else
            {
                merged[item.ProductId] = (item, 0.0, collaborativeScores.GetValueOrDefault(item.ProductId));
            }
        }

        return merged.Values
            .Select(x => new RecommendationItem(
                x.Item.ProductId,
                x.Item.Sku,
                x.Item.Name,
                x.Item.Description,
                x.Item.PriceAmount,
                x.Item.PriceCurrency,
                x.Item.CategoryId,
                weight * x.Content + (1.0 - weight) * x.Collaborative,
                x.Content,
                x.Collaborative))
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();
    }
}
