using Microsoft.EntityFrameworkCore;
using Pgvector;
using StoreApi.Domain.Search;
using StoreApi.Infrastructure.Embeddings;
using StoreApi.Infrastructure.Persistence;

namespace StoreApi.Infrastructure.Search;

/// <summary>
/// Implementare reală a căutării vectoriale folosind operatorul pgvector <c>&lt;=&gt;</c> (cosine distance).
/// Funcționează doar cu PostgreSQL + pgvector; nu poate rula pe InMemory.
/// </summary>
public sealed class VectorProductSearchService : IProductSearchService
{
    private readonly StoreDbContext _db;

    public VectorProductSearchService(StoreDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductSearchResult>> SearchAsync(
        Vector query,
        int topK,
        double minSimilarity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (topK <= 0)
            throw new ArgumentOutOfRangeException(nameof(topK), "topK must be greater than zero.");

        // cosine distance = 1 - similarity
        var maxDistance = 1.0 - Math.Clamp(minSimilarity, 0.0, 1.0);

        var matches = await _db.ProductEmbeddings
            .FromSql($"""
                SELECT pe."ProductId", pe."Embedding", pe."ModelVersion", pe."UpdatedAt"
                FROM product_embeddings AS pe
                WHERE (pe."Embedding" <=> {query}) <= {maxDistance}
                ORDER BY pe."Embedding" <=> {query}
                LIMIT {topK}
                """)
            .Include(pe => pe.Product)
            .AsNoTracking()
            .ToListAsync(ct);

        return matches
            .Where(pe => pe.Product is not null)
            .Select(pe => new ProductSearchResult(
                pe.Product!,
                VectorMath.CosineSimilarity(query, pe.Embedding)))
            .ToList();
    }
}
