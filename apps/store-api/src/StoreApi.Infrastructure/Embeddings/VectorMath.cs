using Pgvector;

namespace StoreApi.Infrastructure.Embeddings;

/// <summary>
/// Operații vectoriale pure (cosine similarity etc.) pentru căutare semantică.
/// </summary>
public static class VectorMath
{
    public static double CosineSimilarity(Vector a, Vector b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var av = a.ToArray();
        var bv = b.ToArray();

        if (av.Length != bv.Length)
            throw new ArgumentException($"Vector dimension mismatch: {av.Length} vs {bv.Length}.");

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < av.Length; i++)
        {
            dot += av[i] * bv[i];
            normA += av[i] * av[i];
            normB += bv[i] * bv[i];
        }

        if (normA == 0 || normB == 0)
            return 0;

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
