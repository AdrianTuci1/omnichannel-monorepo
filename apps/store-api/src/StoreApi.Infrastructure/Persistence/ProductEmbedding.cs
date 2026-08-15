using Pgvector;
using StoreApi.Domain.Entities;

namespace StoreApi.Infrastructure.Persistence;

/// <summary>
/// Înglobarea (embedding) vectorială a unui produs, stocată separat pentru căutare cu pgvector.
/// </summary>
public sealed class ProductEmbedding
{
    private ProductEmbedding()
    {
    }

    public ProductEmbedding(Guid productId, Vector embedding, int modelVersion = 1)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required.", nameof(productId));

        if (modelVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(modelVersion));

        Embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
        ProductId = productId;
        ModelVersion = modelVersion;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid ProductId { get; private set; }

    public Product Product { get; private set; } = null!;

    public Vector Embedding { get; private set; } = null!;

    public int ModelVersion { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public void Update(Vector embedding, int modelVersion)
    {
        if (modelVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(modelVersion));

        Embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
        ModelVersion = modelVersion;
        UpdatedAt = DateTime.UtcNow;
    }
}
