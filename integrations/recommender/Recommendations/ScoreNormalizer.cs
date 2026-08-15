namespace Recommender.Api.Recommendations;

/// <summary>
/// Normalizare min-max a scorurilor pe intervalul [0,1], necesară pentru a combina
/// corect scorurile content-based și collaborative care au scale diferite.
/// </summary>
public static class ScoreNormalizer
{
    public static IReadOnlyDictionary<Guid, double> MinMax(IEnumerable<(Guid ProductId, double Score)> scored)
    {
        var list = scored.ToList();
        var result = new Dictionary<Guid, double>();

        if (list.Count == 0)
            return result;

        var min = list.Min(x => x.Score);
        var max = list.Max(x => x.Score);
        var range = max - min;

        foreach (var item in list)
        {
            // Când toate scorurile sunt egale nu există diferențiere; le acordăm 1.0.
            result[item.ProductId] = range == 0.0 ? 1.0 : (item.Score - min) / range;
        }

        return result;
    }
}
