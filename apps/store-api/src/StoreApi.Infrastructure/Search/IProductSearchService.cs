using Pgvector;
using StoreApi.Domain.Search;

namespace StoreApi.Infrastructure.Search;

/// <summary>
/// Căutare vectorială de produse folosind pgvector (cosine distance).
/// </summary>
public interface IProductSearchService
{
    Task<IReadOnlyList<ProductSearchResult>> SearchAsync(
        Vector query,
        int topK,
        double minSimilarity,
        CancellationToken ct = default);
}
