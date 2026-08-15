using Pgvector;

namespace StoreApi.Infrastructure.Embeddings;

/// <summary>
/// Generează un embedding vectorial determinist dintr-un text.
/// </summary>
public interface IEmbeddingService
{
    Vector Embed(string text);
}

/// <summary>
/// Embedding determinist bazat pe feature hashing (bag-of-words) + normalizare L2.
/// Nu depinde de servicii externe; aceeași intrare produce mereu același vector.
/// </summary>
public sealed class HashingEmbeddingService : IEmbeddingService
{
    public const int Dimension = 384;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "of", "to", "in", "on", "for", "with", "is", "are", "at", "by", "from",
        "un", "o", "și", "si", "de", "la", "cu", "pe", "în", "in", "din",
    };

    public Vector Embed(string text)
    {
        var vector = new float[Dimension];
        var tokens = Tokenize(text);

        foreach (var token in tokens)
        {
            var hash = StableHash(token);
            var index = (int)((hash & 0x7FFFFFFF) % Dimension);
            var sign = ((hash >> 31) & 1) == 0 ? 1f : -1f;
            vector[index] += sign;
        }

        NormalizeL2(vector);
        return new Vector(vector);
    }

    public static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Enumerable.Empty<string>();

        return text
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim(' ', ',', '.', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '-', '/', '\\'))
            .Where(t => t.Length > 0 && !StopWords.Contains(t));
    }

    private static void NormalizeL2(float[] vector)
    {
        var norm = 0f;
        foreach (var value in vector)
            norm += value * value;

        norm = (float)Math.Sqrt(norm);
        if (norm > 0f)
        {
            for (var i = 0; i < vector.Length; i++)
                vector[i] /= norm;
        }
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in value)
                hash = hash * 31 + c;
            return hash;
        }
    }
}
