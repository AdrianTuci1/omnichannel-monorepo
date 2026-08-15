using Pgvector;

namespace Recommender.Api.Embeddings;

/// <summary>
/// Magazin read-only de embedding-uri vectoriale (produs → vector 384 dimensiuni),
/// populat la încărcarea catalogului din Store API.
/// </summary>
public interface IEmbeddingStore
{
    /// <summary>Returnează o imagine imutabilă a mapării produs → embedding.</summary>
    IReadOnlyDictionary<Guid, Vector> Snapshot();

    /// <summary>Înlocuiește atomic conținutul magazinului.</summary>
    void Replace(IReadOnlyDictionary<Guid, Vector> embeddings);
}

public sealed class InMemoryEmbeddingStore : IEmbeddingStore
{
    private volatile IReadOnlyDictionary<Guid, Vector> _items = new Dictionary<Guid, Vector>();

    public IReadOnlyDictionary<Guid, Vector> Snapshot() => _items;

    public void Replace(IReadOnlyDictionary<Guid, Vector> embeddings) => _items = embeddings;
}
