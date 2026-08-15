using Microsoft.EntityFrameworkCore;
using Pgvector;
using Recommender.Api.Configuration;
using Recommender.Api.Domain;
using Recommender.Api.Embeddings;
using Recommender.Api.Persistence;

namespace Recommender.Api.Recommendations;

/// <summary>
/// Recomandări content-based: produse similare după similaritatea cosinus a embedding-urilor
/// (feature hashing, 384 dimensiuni), calculate local la încărcarea catalogului.
/// </summary>
public interface IContentBasedRecommender
{
    Task<IReadOnlyList<RecommendationItem>> RecommendAsync(Guid productId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<RecommendationItem>> SearchByTextAsync(string text, int limit, CancellationToken ct = default);
}

public sealed class ContentBasedRecommender : IContentBasedRecommender
{
    private readonly RecommenderDbContext _db;
    private readonly IEmbeddingStore _embeddings;
    private readonly IEmbeddingService _embedding;
    private readonly RecommenderOptions _options;

    public ContentBasedRecommender(
        RecommenderDbContext db,
        IEmbeddingStore embeddings,
        IEmbeddingService embedding,
        RecommenderOptions options)
    {
        _db = db;
        _embeddings = embeddings;
        _embedding = embedding;
        _options = options;
    }

    public async Task<IReadOnlyList<RecommendationItem>> RecommendAsync(
        Guid productId, int limit, CancellationToken ct = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required.", nameof(productId));

        EnsurePositiveLimit(limit);

        if (!_embeddings.Snapshot().TryGetValue(productId, out var seed))
            return Array.Empty<RecommendationItem>();

        return await SearchByVectorAsync(seed, productId, limit, ct);
    }

    public async Task<IReadOnlyList<RecommendationItem>> SearchByTextAsync(
        string text, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Query text is required.", nameof(text));

        EnsurePositiveLimit(limit);

        var query = _embedding.Embed(text);
        return await SearchByVectorAsync(query, null, limit, ct);
    }

    private async Task<IReadOnlyList<RecommendationItem>> SearchByVectorAsync(
        Vector query, Guid? excludeId, int limit, CancellationToken ct)
    {
        var embeddings = _embeddings.Snapshot();
        var productIds = embeddings.Keys.ToList();

        var products = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var scored = new List<(Product Product, double Similarity)>();
        foreach (var (productId, vector) in embeddings)
        {
            if (excludeId is { } id && productId == id)
                continue;

            if (!products.TryGetValue(productId, out var product))
                continue;

            var similarity = VectorMath.CosineSimilarity(query, vector);
            if (similarity >= _options.MinSimilarity)
                scored.Add((product, similarity));
        }

        return scored
            .OrderByDescending(x => x.Similarity)
            .Take(limit)
            .Select(x => ToItem(x.Product, x.Similarity))
            .ToList();
    }

    private static RecommendationItem ToItem(Product product, double similarity)
        => new(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.PriceAmount,
            product.PriceCurrency,
            product.CategoryId,
            similarity,
            similarity,
            0.0);

    private static void EnsurePositiveLimit(int limit)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
    }
}
