using Microsoft.EntityFrameworkCore;
using Recommender.Api.Configuration;
using Recommender.Api.Domain;
using Recommender.Api.Persistence;

namespace Recommender.Api.Recommendations;

/// <summary>
/// Recomandări collaborative de bază: co-ocurență item-item și agregare user-based
/// peste istoricul de comenzi al clientului.
/// </summary>
public interface ICollaborativeRecommender
{
    Task<IReadOnlyList<RecommendationItem>> RecommendAsync(Guid productId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<RecommendationItem>> RecommendForCustomerAsync(Guid customerId, int limit, CancellationToken ct = default);

    Task<Guid?> GetMostRecentProductIdAsync(Guid customerId, CancellationToken ct = default);
}

public sealed class CollaborativeRecommender : ICollaborativeRecommender
{
    private readonly RecommenderDbContext _db;
    private readonly RecommenderOptions _options;

    public CollaborativeRecommender(RecommenderDbContext db, RecommenderOptions options)
    {
        _db = db;
        _options = options;
    }

    public async Task<IReadOnlyList<RecommendationItem>> RecommendAsync(
        Guid productId, int limit, CancellationToken ct = default)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required.", nameof(productId));

        EnsurePositiveLimit(limit);

        var seedOrderIds = await _db.OrderLines.AsNoTracking()
            .Where(ol => ol.ProductId == productId)
            .Select(ol => ol.OrderId)
            .Distinct()
            .ToListAsync(ct);

        if (seedOrderIds.Count == 0)
            return Array.Empty<RecommendationItem>();

        var candidateCount = limit * Math.Max(1, _options.CandidateMultiplier);

        var coOccurrences = await _db.OrderLines.AsNoTracking()
            .Where(ol => seedOrderIds.Contains(ol.OrderId) && ol.ProductId != productId)
            .GroupBy(ol => ol.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(candidateCount)
            .ToListAsync(ct);

        // Scor = fracția comenzilor produsului seed care conțin și produsul candidat.
        var scored = coOccurrences
            .Select(x => new ScoredProduct(x.ProductId, (double)x.Count / seedOrderIds.Count))
            .ToList();

        return await MaterializeAsync(scored, limit, ct);
    }

    public async Task<IReadOnlyList<RecommendationItem>> RecommendForCustomerAsync(
        Guid customerId, int limit, CancellationToken ct = default)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.", nameof(customerId));

        EnsurePositiveLimit(limit);

        var customerOrderIds = await _db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == customerId && o.Status != RecommenderDbContext.CancelledOrderStatus)
            .Select(o => o.Id)
            .Distinct()
            .ToListAsync(ct);

        if (customerOrderIds.Count == 0)
            return Array.Empty<RecommendationItem>();

        var purchased = await _db.OrderLines.AsNoTracking()
            .Where(ol => customerOrderIds.Contains(ol.OrderId))
            .Select(ol => ol.ProductId)
            .Distinct()
            .ToListAsync(ct);

        if (purchased.Count == 0)
            return Array.Empty<RecommendationItem>();

        var candidateCount = limit * Math.Max(1, _options.CandidateMultiplier);

        var coOccurrences = await _db.OrderLines.AsNoTracking()
            .Where(ol => customerOrderIds.Contains(ol.OrderId) && !purchased.Contains(ol.ProductId))
            .GroupBy(ol => ol.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(candidateCount)
            .ToListAsync(ct);

        var scored = coOccurrences
            .Select(x => new ScoredProduct(x.ProductId, (double)x.Count / customerOrderIds.Count))
            .ToList();

        return await MaterializeAsync(scored, limit, ct);
    }

    public async Task<Guid?> GetMostRecentProductIdAsync(Guid customerId, CancellationToken ct = default)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.", nameof(customerId));

        var recentOrderId = await _db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == customerId && o.Status != RecommenderDbContext.CancelledOrderStatus)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync(ct);

        if (recentOrderId is null)
            return null;

        return await _db.OrderLines.AsNoTracking()
            .Where(ol => ol.OrderId == recentOrderId.Value)
            .Select(ol => (Guid?)ol.ProductId)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<IReadOnlyList<RecommendationItem>> MaterializeAsync(
        IReadOnlyList<ScoredProduct> scored, int limit, CancellationToken ct)
    {
        var productIds = scored.Select(s => s.ProductId).ToList();

        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id, ct);

        return scored
            .Where(s => products.ContainsKey(s.ProductId))
            .Select(s =>
            {
                var p = products[s.ProductId];
                return new RecommendationItem(
                    p.Id, p.Sku, p.Name, p.Description, p.PriceAmount, p.PriceCurrency,
                    p.CategoryId, s.Score, 0.0, s.Score);
            })
            .Take(limit)
            .ToList();
    }

    private static void EnsurePositiveLimit(int limit)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
    }

    private sealed record ScoredProduct(Guid ProductId, double Score);
}
